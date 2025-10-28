using System.Runtime.CompilerServices;

namespace Raylib_cs_fx;

/// <summary>
/// An immensely customizable version of the basic particle system
/// Nearly Every property can be assigend a fucntion
/// </summary>
public class AdvancedParticleSystem : IDisposable, ISystem
{
    public FuncOrVal<Particle, float> RotationPerSecond = 0f;
    public FuncOrVal<Particle, int> InitialRotationJitter = 0;
    public FuncOrVal<Particle, Vector2> VelocityPerSecond;
    public FuncOrVal<Particle, Vector2> AccelerationPersecond;
    public FuncOrVal<Particle, (Vector2 min, Vector2 max)> VelocityJitter = (Vector2.Zero, Vector2.Zero);
    public FuncOrVal<Particle, float> StartingAlpha = 1f;
    public FuncOrVal<Particle, float> ParticleLifetime = 2f;
    public FuncOrVal<Particle, float> ParticleLifetimeJitter = 0f;
    public FuncOrVal<float> SystemLifeTime = -1f;
    public float SystemAge = 0f;
    public BlendMode BlendMode = BlendMode.Additive;
    public FuncOrVal<int> ParticlesPerSecond;
    public FuncOrVal<int> MaxParticles = 100;
    public FuncOrVal<Particle, float> ParticleStartSize;
    public FuncOrVal<int> ParticleStartSizeJitter = 0;
    public FuncOrVal<Particle, float> ParticleEndSize = 0;
    public Texture2D Texture;
    public FuncOrVal<Particle, (Vector2 min, Vector2 max)> SpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
    public FuncOrVal<Particle, float> FadeRate;
    public FuncOrVal<Particle, TrailSegmentRenderer[]> Segments;
    public FuncOrVal<Particle, float> TrailFadeRate;
    public ParticleRenderer ParticleRenderer;
    /// <summary>
    /// This Jitter works in reverse
    /// So Red jitter has a randomized reduction effect on the Red Channel between 0 and n
    /// So that channel doesn't exceed 255
    /// This will error if a channel goes less than 0
    /// </summary>
    public FuncOrVal<Color> InitialColorJitter = Color.Black; //black is no jitter 
    public FuncOrVal<Color> Tint = Color.White; //white here represents the texture as is

    public Func<Vector2> SpawnPosition { get; set; } = () => GetMousePosition();

    public bool ScaleImage = false;
    public Rectangle? ScaledSize { get; set; }


    private Particle[] particles;
    private int[] activeIndices;
    private int activeCount = 0;
    private Stack<int> freeIndices = new Stack<int>();
    private Random random = new Random();

    public AdvancedParticleSystem(ParticleRenderer renderer)
    {
        particles = new Particle[MaxParticles.Value];
        activeIndices = new int[MaxParticles.Value];
        VelocityPerSecond = Vector2.Zero;
        AccelerationPersecond = Vector2.Zero;
        ParticleRenderer = renderer;
    }

    public AdvancedParticleSystem(Texture2D texture)
    {
        Texture = texture;
        particles = new Particle[MaxParticles.Value];
        activeIndices = new int[MaxParticles.Value];
        VelocityPerSecond = Vector2.Zero;
        AccelerationPersecond = Vector2.Zero;
        ParticleRenderer = new TextureParticleRender {Texture = texture };
    }

    /// <summary>
    /// Call after updating public fields' values
    /// </summary>
    public void Start()
    {
        particles = new Particle[MaxParticles.Value];
        activeIndices = new int[MaxParticles.Value];
        freeIndices = new Stack<int>();

        for (int i = MaxParticles.Value - 1; i >= 0; i--)
        {
            freeIndices.Push(i);
        }

        // Initialize particles
        for (int i = 0; i < MaxParticles.Value; i++)
        {

            var jitter = InitialColorJitter.Value;
            var tint = Tint.Value;

            ref var particle = ref particles[i];
            particle.Position = new Vector2(0, 0);
            particle.Color = new Color(GetRandomValue(Math.Max(tint.R - jitter.R, 0), tint.R),
                                            GetRandomValue(Math.Max(tint.G - jitter.G, 0), tint.G),
                                            GetRandomValue(Math.Max(tint.B - jitter.B, 0), tint.B),
                                            255);
            particle.StartSize = (ParticleStartSizeJitter.Value * random.NextSingle()) + ParticleStartSize.Value(particle);
            particle.Size = particle.StartSize;
            particle.Rotation = GetRandomValue(0, 0 + InitialRotationJitter.Value(particle));
            particle.Age = 0.0f;
            particle.Lifetime = ParticleLifetime.Value(particle) + (random.NextSingle() * ParticleLifetimeJitter.Value(particle));
            particle.BaseVelocity = GetVector2Jitter(VelocityJitter.Value(particle));
            particle.TrailKeyframes = new Queue<Vector2>(Segments.Value(particle).Length);
        }
    }

    public void Update(float frameTime)
    {
        // Spawn new particles
        float particlesToSpawn = ParticlesPerSecond.Value * frameTime;


        if (SystemLifeTime.Value > 0)
        {
            SystemAge += frameTime;

            if (SystemAge >= SystemLifeTime.Value)
            {
                Stop();
                SystemAge = 0f;
            }
        }

        while (particlesToSpawn > 0 && freeIndices.Count > 0)
        {
            //O(1)
            int index = freeIndices.Pop();

            SpawnParticle(index);

            particlesToSpawn--;
        }

        // Update ONLY active particles - iterate backwards for safe removal
        for (int i = activeCount - 1; i >= 0; i--)
        {
            int particleIndex = activeIndices[i];
            UpdateParticle(frameTime, particleIndex);

            // Check if particle should die
            if (particles[particleIndex].Age >= particles[particleIndex].Lifetime)
            {
                DespawnParticle(i, particleIndex);
            }
        }
    }

    private void DespawnParticle(int i, int particleIndex)
    {
        // Return to free pool
        freeIndices.Push(particleIndex);
        particles[particleIndex].TrailKeyframes.Clear();
        // Remove from active list (swap-and-pop)
        activeIndices[i] = activeIndices[activeCount - 1];
        activeCount--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 GetVector2Jitter((Vector2 min, Vector2 max) jitter)
    {
        return new Vector2(
            jitter.min.X + random.NextSingle() * (jitter.max.X - jitter.min.X),
            jitter.min.Y + random.NextSingle() * (jitter.max.Y - jitter.min.Y)
        );
    }


    private void UpdateParticle(float deltaTime, int particleIndex)
    {
        //remember these are structs need to use ref here so we don't create a copy
        ref var particle = ref particles[particleIndex];

        particles[particleIndex].Age += deltaTime;

        var velocity = VelocityPerSecond.Value(particle);
        var acceleration = AccelerationPersecond.Value(particle);

        particle.BaseVelocity += (acceleration + acceleration) * deltaTime;

        particle.Position.X += (velocity.X + particle.BaseVelocity.X) * deltaTime;
        particle.Position.Y += (velocity.Y + particle.BaseVelocity.Y) * deltaTime;

        particle.Rotation += RotationPerSecond.Value(particle) * deltaTime;

        particle.LifetimeProgress = particle.Age / particle.Lifetime;

        // Update size over lifetime (lerp from start to end)
        particle.Size = particle.StartSize + (ParticleEndSize.Value(particle) - particle.StartSize) * particle.LifetimeProgress;

        if (Segments.Value(particle).Length > 0)
        {
            particle.TrailKeyframes.Enqueue(particle.Position);
            if (particle.TrailKeyframes.Count > Segments.Value(particle).Length)
                particle.TrailKeyframes.Dequeue();
        }
    }

    public void Stop()
    {
        particles = [];
        activeIndices = [];
        freeIndices = new Stack<int>();
    }

    private void SpawnParticle(int index)
    {
        ref var particle = ref particles[index];

        particle.Age = 0.0f;
        particle.Lifetime = ParticleLifetime.Value(particle) + (random.NextSingle() * ParticleLifetimeJitter.Value(particle));
        particle.Position = SpawnPosition() + GetVector2Jitter(SpawnPositionJitter.Value(particle));
        particle.Rotation = GetRandomValue(0, 0 + InitialRotationJitter.Value(particle));
        particle.BaseVelocity = GetVector2Jitter(VelocityJitter.Value(particle));

        if (Segments.Value(particle).Length > 0)
        {
            var pos = particle.Position;
            for (int j = 0; j < Segments.Value(particle).Length; j++)
                particles[index].TrailKeyframes.Enqueue(pos);
        }

        // Add to active list
        activeIndices[activeCount] = index;
        activeCount++;
    }
    public void Draw()
    {
        BeginBlendMode(BlendMode);

        // Draw active particles
        for (int i = 0; i < activeCount; i++)
        {
            int particleIndex = activeIndices[i];
            ref var particle = ref particles[particleIndex];

            var trailPoints = particle.TrailKeyframes.ToArray();
            var segments = Segments.Value(particle);
            float alpha = StartingAlpha.Value(particle) * (1.0f - particle.LifetimeProgress);
            for (int j = 0; j < trailPoints.Length - 1; j++)
            {
                float trailAlpha = (float)(j + 1) / trailPoints.Length * TrailFadeRate.Value(particle) * alpha;
                var segment1 = segments[j];
                var point1 = trailPoints[j];
                var point2 = trailPoints[j + 1];
                var width = segment1.Width;
                segment1.Draw(point1, point2, trailAlpha);
            }

            // lifetimeProgress = p[index].age * p[index].OneOverLifetime


            ParticleRenderer.Draw(particle, alpha);

        }

        EndBlendMode();
    }

    public void Dispose()
    {
        UnloadTexture(Texture);
    }
}
