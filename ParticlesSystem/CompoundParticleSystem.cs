namespace Raylib_cs_fx;

using System.Runtime.CompilerServices;

/// <summary>
/// Two-tier particle system using the same memory management approach as ParticleSystem.
/// Uses activeIndices, freeIndices stack, and activeCount for direct comparison benchmarking.
/// </summary>
public class CompoundParticleSystem : IDisposable, ISystem
{
    #region Primary Particle Configuration

    public float PrimaryRotationPerSecond = 0f;
    public int PrimaryInitialRotationJitter = 0;
    public FuncOrVal<Vector2> PrimaryVelocityPerSecond = Vector2.Zero;
    public FuncOrVal<Vector2> PrimaryAccelerationPerSecond = Vector2.Zero;
    public (Vector2 min, Vector2 max) PrimaryVelocityJitter = (Vector2.Zero, Vector2.Zero);
    public float PrimaryStartingAlpha = 1f;
    public float PrimaryParticleLifetime = 2f;
    public float PrimaryParticleLifetimeJitter = 0f;
    public float SystemLifeTime = -1f;
    public float SystemAge = 0f;
    public int PrimaryParticlesPerSecond = 60;
    public int MaxPrimaryParticles = 50;
    public float PrimaryParticleStartSize = 1f;
    public int PrimaryParticleStartSizeJitter = 0;
    public float PrimaryParticleEndSize = 0f;
    public (Vector2 min, Vector2 max) PrimarySpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
    public int PrimaryTrailSegments = 0;
    public Color PrimaryInitialColorJitter = Color.Black;
    public Color PrimaryTint = Color.White;
    public ParticleRenderer PrimaryParticleRenderer = new CircleRenderer();
    public TrailSegmentRenderer PrimaryTrailSegmentRenderer = new LineTrailSegmentRenderer { Width = 1f, Color = Color.White };

    #endregion

    #region Secondary Particle Configuration

    public float SecondaryRotationPerSecond = 360f;
    public int SecondaryInitialRotationJitter = 360;
    public FuncOrVal<Vector2> SecondaryVelocityPerSecond = Vector2.Zero;
    public FuncOrVal<Vector2> SecondaryAccelerationPerSecond = Vector2.Zero;
    public (Vector2 min, Vector2 max) SecondaryVelocityJitter = (new Vector2(-100, -100), new Vector2(100, 100));
    public float SecondaryStartingAlpha = 1f;
    public float SecondaryParticleLifetime = 1f;
    public float SecondaryParticleLifetimeJitter = 0.3f;
    public int SecondaryParticlesPerPrimary = 5;
    public int MaxSecondaryParticlesPersecondPerPrimary = 10 *60;
    public int MaxTotalSecondaryParticles = 500;
    public float SecondaryParticleStartSize = 0.5f;
    public int SecondaryParticleStartSizeJitter = 0;
    public float SecondaryParticleEndSize = 0f;
    public (Vector2 min, Vector2 max) SecondarySpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
    public int SecondaryTrailSegments = 0;
    public Color SecondaryInitialColorJitter = Color.Black;
    public Color SecondaryTint = Color.White;
    public ParticleRenderer SecondaryParticleRenderer = new CircleRenderer();
    public TrailSegmentRenderer SecondaryTrailSegmentRenderer = new LineTrailSegmentRenderer { Width = 1f, Color = Color.White };

    public enum SecondarySpawnMode
    {
        OnPrimarySpawn,
        OnPrimaryDeath,
        Continuous,
        OnIntervalDuringPrimaryLifetime
    }

    public SecondarySpawnMode SpawnMode = SecondarySpawnMode.OnPrimarySpawn;
    public float SecondarySpawnInterval = 0.5f;

    #endregion

    public BlendMode BlendMode = BlendMode.Additive;
    public Func<Vector2> SpawnPosition { get; set; } = () => GetMousePosition();

    private struct PrimaryParticle
    {
        public Vector2 Position;
        public Color Color;
        public float Size;
        public float StartSize;
        public float Rotation;
        public float Age;
        public float Lifetime;
        public float LifetimeProgress;
        public Vector2 BaseVelocity;
        public int SecondaryStartIndex;
        public int SecondaryActiveCount;
        public float SecondarySpawnTimer;
        public Queue<Vector2> TrailKeyframes;
    }

    // Secondary particle data
    private struct SecondaryParticle
    {
        public Vector2 Position;
        public Color Color;
        public float Size;
        public float StartSize;
        public float Rotation;
        public float Age;
        public float Lifetime;
        public float LifetimeProgress;
        public Vector2 BaseVelocity;
        public int OwnerPrimaryIndex;
        public Queue<Vector2> TrailKeyframes;
    }

    // Primary particle pools - matching original design
    private PrimaryParticle[] primaryParticles = [];
    private int[] primaryActiveIndices = [];
    private int primaryActiveCount = 0;
    private Stack<int> primaryFreeIndices = new Stack<int>();

    // Secondary particle pools - matching original design
    private SecondaryParticle[] secondaryParticles = [];
    private int[] secondaryActiveIndices = [];
    private int secondaryActiveCount = 0;
    private Stack<int> secondaryFreeIndices = new Stack<int>();

    // Mapping from primary index to its secondary pool range
    private int[] primarySecondaryStartIndices = [];

    private Random random = new Random();

    public CompoundParticleSystem(){}

    public CompoundParticleSystem(Texture2D primaryTexture, Texture2D secondaryTexture)
    {
        PrimaryParticleRenderer = new TextureParticleRender { Texture = primaryTexture };
        SecondaryParticleRenderer = new TextureParticleRender { Texture = secondaryTexture };

    }
    public CompoundParticleSystem(ParticleRenderer primary,  ParticleRenderer secondary)
    {
        PrimaryParticleRenderer = primary;
        SecondaryParticleRenderer = secondary;
    }


    public void Start()
    {
        // Initialize primary particles
        primaryParticles = new PrimaryParticle[MaxPrimaryParticles];
        primaryActiveIndices = new int[MaxPrimaryParticles];
        primaryFreeIndices = new Stack<int>();
        primarySecondaryStartIndices = new int[MaxPrimaryParticles];

        for (int i = MaxPrimaryParticles - 1; i >= 0; i--)
            primaryFreeIndices.Push(i);

        // Initialize secondary particles
        secondaryParticles = new SecondaryParticle[MaxTotalSecondaryParticles];
        secondaryActiveIndices = new int[MaxTotalSecondaryParticles];
        secondaryFreeIndices = new Stack<int>();

        for (int i = MaxTotalSecondaryParticles - 1; i >= 0; i--)
            secondaryFreeIndices.Push(i);

        // Pre-allocate secondary slots for each primary
        int secondaryBlockSize = MaxSecondaryParticlesPersecondPerPrimary;
        for (int i = 0; i < MaxPrimaryParticles; i++)
        {
            primarySecondaryStartIndices[i] = i * secondaryBlockSize;
        }

        // Initialize primary particles with default values
        for (int i = 0; i < MaxPrimaryParticles; i++)
        {
            primaryParticles[i].Position = Vector2.Zero;
            primaryParticles[i].Color = GetColorWithJitter(PrimaryTint, PrimaryInitialColorJitter);
            primaryParticles[i].StartSize = (PrimaryParticleStartSizeJitter * random.NextSingle()) + PrimaryParticleStartSize;
            primaryParticles[i].Size = primaryParticles[i].StartSize;
            primaryParticles[i].Rotation = GetRandomValue(0, PrimaryInitialRotationJitter);
            primaryParticles[i].Age = 0f;
            primaryParticles[i].Lifetime = PrimaryParticleLifetime + (random.NextSingle() * PrimaryParticleLifetimeJitter);
            primaryParticles[i].BaseVelocity = GetVector2Jitter(PrimaryVelocityJitter);
            primaryParticles[i].SecondaryStartIndex = primarySecondaryStartIndices[i];
            primaryParticles[i].SecondaryActiveCount = 0;
            primaryParticles[i].SecondarySpawnTimer = 0f;
            primaryParticles[i].TrailKeyframes = new Queue<Vector2>(PrimaryTrailSegments);
        }

        // Initialize secondary particles with default values
        for (int i = 0; i < MaxTotalSecondaryParticles; i++)
        {
            secondaryParticles[i].Position = Vector2.Zero;
            secondaryParticles[i].Color = GetColorWithJitter(SecondaryTint, SecondaryInitialColorJitter);
            secondaryParticles[i].StartSize = (SecondaryParticleStartSizeJitter * random.NextSingle()) + SecondaryParticleStartSize;
            secondaryParticles[i].Size = secondaryParticles[i].StartSize;
            secondaryParticles[i].Rotation = GetRandomValue(0, SecondaryInitialRotationJitter);
            secondaryParticles[i].Age = 0f;
            secondaryParticles[i].Lifetime = SecondaryParticleLifetime + (random.NextSingle() * SecondaryParticleLifetimeJitter);
            secondaryParticles[i].BaseVelocity = GetVector2Jitter(SecondaryVelocityJitter);
            secondaryParticles[i].OwnerPrimaryIndex = -1;
            secondaryParticles[i].TrailKeyframes = new Queue<Vector2>(SecondaryTrailSegments);
        }

        primaryActiveCount = 0;
        secondaryActiveCount = 0;
    }

    public void Update(float frameTime)
    {
        // Handle system lifetime
        if (SystemLifeTime > 0)
        {
            SystemAge += frameTime;
            if (SystemAge >= SystemLifeTime)
            {
                Stop();
                SystemAge = 0f;
                return;
            }
        }

        // Spawn new primary particles
        float particlesToSpawn = PrimaryParticlesPerSecond * frameTime;
        while (particlesToSpawn > 0 && primaryFreeIndices.Count > 0)
        {
            int index = primaryFreeIndices.Pop();
            SpawnPrimaryParticle(index, frameTime);
            particlesToSpawn--;
        }

        // Update primary particles - iterate backwards for safe removal
        for (int i = primaryActiveCount - 1; i >= 0; i--)
        {
            int particleIndex = primaryActiveIndices[i];
            UpdatePrimaryParticle(frameTime, particleIndex);

            // Check if particle should die
            if (primaryParticles[particleIndex].Age >= primaryParticles[particleIndex].Lifetime)
            {
                DespawnPrimaryParticle(i, particleIndex,frameTime);
            }
        }

        // Update secondary particles - iterate backwards for safe removal
        for (int i = secondaryActiveCount - 1; i >= 0; i--)
        {
            int particleIndex = secondaryActiveIndices[i];
            UpdateSecondaryParticle(frameTime, particleIndex);

            // Check if particle should die
            if (secondaryParticles[particleIndex].Age >= secondaryParticles[particleIndex].Lifetime)
            {
                DespawnSecondaryParticle(i, particleIndex);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 GetVector2Jitter((Vector2 min, Vector2 max) jitter)
    {
        return new Vector2(
            jitter.min.X + random.NextSingle() * (jitter.max.X - jitter.min.X),
            jitter.min.Y + random.NextSingle() * (jitter.max.Y - jitter.min.Y)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Color GetColorWithJitter(Color tint, Color jitter)
    {
        return new Color(
            GetRandomValue(Math.Max(tint.R - jitter.R, 0), tint.R),
            GetRandomValue(Math.Max(tint.G - jitter.G, 0), tint.G),
            GetRandomValue(Math.Max(tint.B - jitter.B, 0), tint.B),
            255
        );
    }

    private void SpawnPrimaryParticle(int index, float frameTime)
    {
        primaryParticles[index].Age = 0f;
        primaryParticles[index].Lifetime = PrimaryParticleLifetime + (random.NextSingle() * PrimaryParticleLifetimeJitter);
        primaryParticles[index].Position = SpawnPosition() + GetVector2Jitter(PrimarySpawnPositionJitter);
        primaryParticles[index].Rotation = GetRandomValue(0, PrimaryInitialRotationJitter);
        primaryParticles[index].BaseVelocity = GetVector2Jitter(PrimaryVelocityJitter);
        primaryParticles[index].TrailKeyframes.Clear();

        if (PrimaryTrailSegments > 0)
        {
            var pos = primaryParticles[index].Position;
            for (int j = 0; j < PrimaryTrailSegments; j++)
                primaryParticles[index].TrailKeyframes.Enqueue(pos);
        }

        // Add to active list
        primaryActiveIndices[primaryActiveCount] = index;
        primaryActiveCount++;

        // Handle spawn burst
        if (SpawnMode == SecondarySpawnMode.OnPrimarySpawn)
        {
            SpawnSecondaryBurst(index, frameTime);
        }
    }

    private void UpdatePrimaryParticle(float frameTime, int particleIndex)
    {
        ref var particle = ref primaryParticles[particleIndex];

        // Update age
        particle.Age += frameTime;

        // Apply acceleration to velocity
        particle.BaseVelocity += PrimaryAccelerationPerSecond.Value * frameTime;

        // Apply velocity
        var particleData = new Particle
        {
            Position = particle.Position,
            Age = particle.Age,
            Lifetime = particle.Lifetime
        };

        var velocity = PrimaryVelocityPerSecond.Value;
        particle.Position.X += (velocity.X + particle.BaseVelocity.X) * frameTime;
        particle.Position.Y += (velocity.Y + particle.BaseVelocity.Y) * frameTime;

        // Apply rotation
        particle.Rotation += PrimaryRotationPerSecond * frameTime;

        // Calculate lifetime progress
        particle.LifetimeProgress = particle.Age / particle.Lifetime;

        // Update size over lifetime
        particle.Size = particle.StartSize + (PrimaryParticleEndSize - particle.StartSize) * particle.LifetimeProgress;

        // Update trail if enabled
        if (PrimaryTrailSegments > 0)
        {
            particle.TrailKeyframes.Enqueue(particle.Position);
            if (particle.TrailKeyframes.Count > PrimaryTrailSegments)
                particle.TrailKeyframes.Dequeue();
        }

        // Handle continuous/interval spawning
        if (SpawnMode == SecondarySpawnMode.Continuous || SpawnMode == SecondarySpawnMode.OnIntervalDuringPrimaryLifetime)
        {
            particle.SecondarySpawnTimer += frameTime;

            if (particle.SecondarySpawnTimer >= SecondarySpawnInterval)
            {
                particle.SecondarySpawnTimer -= SecondarySpawnInterval;
                SpawnSecondaryBurst(particleIndex,frameTime);
            }
        }
    }

    private void DespawnPrimaryParticle(int activeIndex, int particleIndex, float frameTime)
    {
        // Kill all associated secondary particles FIRST (before death burst)
        // Iterate backwards through secondary active list
        for (int i = secondaryActiveCount - 1; i >= 0; i--)
        {
            int secIndex = secondaryActiveIndices[i];
            if (secondaryParticles[secIndex].OwnerPrimaryIndex == particleIndex)
            {
                DespawnSecondaryParticle(i, secIndex);
            }
        }

        // THEN spawn death burst (after cleanup)
        if (SpawnMode == SecondarySpawnMode.OnPrimaryDeath)
        {
            SpawnSecondaryBurst(particleIndex, frameTime);
        }

        // Return to free pool
        primaryFreeIndices.Push(particleIndex);

        // Remove from active list (swap-and-pop)
        primaryActiveIndices[activeIndex] = primaryActiveIndices[primaryActiveCount - 1];
        primaryActiveCount--;
    }

    private void SpawnSecondaryBurst(int primaryIndex, float frameTime)
    {
        float toSpawn = SecondaryParticlesPerPrimary *frameTime;

        // Check if we have room in the primary's secondary count
        if (primaryParticles[primaryIndex].SecondaryActiveCount + toSpawn > MaxSecondaryParticlesPersecondPerPrimary)
            return;

        // Check if we have room in the global secondary pool
        if (secondaryFreeIndices.Count < toSpawn)
            return;

        // Spawn the burst
        for (int i = 0; i < toSpawn; i++)
        {
            if (secondaryFreeIndices.Count > 0)
            {
                int index = secondaryFreeIndices.Pop();
                SpawnSecondaryParticle(index, primaryIndex);
            }
        }
    }

    private void SpawnSecondaryParticle(int index, int primaryIndex)
    {
        Vector2 spawnPos = primaryParticles[primaryIndex].Position + GetVector2Jitter(SecondarySpawnPositionJitter);

        secondaryParticles[index].Age = 0f;
        secondaryParticles[index].Lifetime = SecondaryParticleLifetime + (random.NextSingle() * SecondaryParticleLifetimeJitter);
        secondaryParticles[index].Position = spawnPos;
        secondaryParticles[index].Rotation = GetRandomValue(0, SecondaryInitialRotationJitter);
        secondaryParticles[index].BaseVelocity = GetVector2Jitter(SecondaryVelocityJitter);
        secondaryParticles[index].OwnerPrimaryIndex = primaryIndex;
        secondaryParticles[index].TrailKeyframes.Clear();

        if (SecondaryTrailSegments > 0)
        {
            var pos = secondaryParticles[index].Position;
            for (int j = 0; j < SecondaryTrailSegments; j++)
                secondaryParticles[index].TrailKeyframes.Enqueue(pos);
        }

        // Add to active list
        secondaryActiveIndices[secondaryActiveCount] = index;
        secondaryActiveCount++;

        // Increment parent's counter
        primaryParticles[primaryIndex].SecondaryActiveCount++;
    }

    private void UpdateSecondaryParticle(float deltaTime, int particleIndex)
    {
        ref var particle = ref secondaryParticles[particleIndex];

        // Update age
        particle.Age += deltaTime;

        // Apply acceleration to velocity
        particle.BaseVelocity += SecondaryAccelerationPerSecond.Value * deltaTime;

        // Apply velocity
        var particleData = new Particle
        {
            Position = particle.Position,
            Age = particle.Age,
            Lifetime = particle.Lifetime
        };

        var velocity = SecondaryVelocityPerSecond.Value;
        particle.Position.X += (velocity.X + particle.BaseVelocity.X) * deltaTime;
        particle.Position.Y += (velocity.Y + particle.BaseVelocity.Y) * deltaTime;

        // Apply rotation
        particle.Rotation += SecondaryRotationPerSecond * deltaTime;

        // Calculate lifetime progress
        particle.LifetimeProgress = particle.Age / particle.Lifetime;

        // Update size over lifetime
        particle.Size = particle.StartSize + (SecondaryParticleEndSize - particle.StartSize) * particle.LifetimeProgress;

        // Update trail if enabled
        if (SecondaryTrailSegments > 0)
        {
            particle.TrailKeyframes.Enqueue(particle.Position);
            if (particle.TrailKeyframes.Count > SecondaryTrailSegments)
                particle.TrailKeyframes.Dequeue();
        }
    }

    private void DespawnSecondaryParticle(int activeIndex, int particleIndex)
    {
        int primaryIndex = secondaryParticles[particleIndex].OwnerPrimaryIndex;

        // Return to free pool
        secondaryFreeIndices.Push(particleIndex);

        // Remove from active list (swap-and-pop)
        secondaryActiveIndices[activeIndex] = secondaryActiveIndices[secondaryActiveCount - 1];
        secondaryActiveCount--;

        // Decrement parent's counter (if parent still exists in active list)
        // We need to check if the primary is still active
        bool primaryStillActive = false;
        for (int i = 0; i < primaryActiveCount; i++)
        {
            if (primaryActiveIndices[i] == primaryIndex)
            {
                primaryStillActive = true;
                break;
            }
        }

        if (primaryStillActive)
        {
            primaryParticles[primaryIndex].SecondaryActiveCount--;
        }
    }

    public void Draw()
    {
        BeginBlendMode(BlendMode);

        // Draw secondary particles first (background layer)
        DrawSecondaryParticles();

        // Draw primary particles on top
        DrawPrimaryParticles();

        EndBlendMode();
    }

    private void DrawPrimaryParticles()
    {
        for (int i = 0; i < primaryActiveCount; i++)
        {
            int particleIndex = primaryActiveIndices[i];
            ref var particle = ref primaryParticles[particleIndex];

            float alpha = PrimaryStartingAlpha * (1.0f - particle.LifetimeProgress);

            // Draw trail
            if (PrimaryTrailSegments > 0)
            {
                var trailPoints = particle.TrailKeyframes.ToArray();
                for (int j = 0; j < trailPoints.Length - 1; j++)
                {
                    float lerp = (float)(j + 1) / trailPoints.Length;
                    var trailAlpha = lerp * PrimaryTrailSegmentRenderer.Color.A / 255.0f * alpha;
                    PrimaryTrailSegmentRenderer.Draw(trailPoints[j], trailPoints[j + 1], trailAlpha);
                }
            }

            // Draw particle
            PrimaryParticleRenderer.Draw(new Particle
            {
                Position = particle.Position,
                Color = particle.Color,
                Size = particle.Size,
                Rotation = particle.Rotation
            }, alpha);
        }
    }

    private void DrawSecondaryParticles()
    {
        for (int i = 0; i < secondaryActiveCount; i++)
        {
            int particleIndex = secondaryActiveIndices[i];
            ref var particle = ref secondaryParticles[particleIndex];

            float alpha = SecondaryStartingAlpha * (1.0f - particle.LifetimeProgress);

            // Draw trail
            if (SecondaryTrailSegments > 0)
            {
                var trailPoints = particle.TrailKeyframes.ToArray();
                for (int j = 0; j < trailPoints.Length - 1; j++)
                {
                    float lerp = (float)(j + 1) / trailPoints.Length;
                    var trailAlpha = lerp * SecondaryTrailSegmentRenderer.Color.A / 255.0f * alpha;
                    SecondaryTrailSegmentRenderer.Draw(trailPoints[j], trailPoints[j + 1], trailAlpha);
                }
            }

            // Draw particle
            SecondaryParticleRenderer.Draw(new Particle
            {
                Position = particle.Position,
                Color = particle.Color,
                Size = particle.Size,
                Rotation = particle.Rotation
            }, alpha);
        }
    }

    public void Stop()
    {
        primaryParticles = [];
        primaryActiveIndices = [];
        primaryFreeIndices = new Stack<int>();
        primaryActiveCount = 0;

        secondaryParticles = [];
        secondaryActiveIndices = [];
        secondaryFreeIndices = new Stack<int>();
        secondaryActiveCount = 0;
    }

    public void Dispose()
    {
        PrimaryParticleRenderer.Dispose();
        SecondaryParticleRenderer.Dispose();
    }
}