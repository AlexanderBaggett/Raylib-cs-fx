using System.Runtime.CompilerServices;

namespace Raylib_cs_fx;

public struct Particle
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
    public Queue<Vector2> TrailKeyframes;
};

public class ParticleSystem : IDisposable, ISystem
{
    public float RotationPerSecond = 0f;
    public FuncOrVal<Vector2> VelocityPerSecond;
    public FuncOrVal<Vector2> AccelerationPerSecond;
    public float StartingAlpha = 1f;
    public float ParticleLifetime = 2f;
    public float SystemLifeTime = -1f;
    public float SystemAge = 0f;
    public int ParticlesPerSecond;
    public int MaxParticles = 100;
    public float ParticleStartSize;
    public float ParticleEndSize =0;
    public int TrailSegments =0;
    public BlendMode BlendMode = BlendMode.Additive;
    private ParticleRenderer ParticleRenderer = new CircleRenderer();
    public TrailSegmentRenderer TrailSegmentRenderer = new LineTrailSegmentRenderer();
    public ParticleEmitter ParticleEmitter = new DefaultEmitter();
    public Color Tint = Color.White; //white here represents the texture as is

    public Func<Vector2> SpawnPosition { get; set; } = () => GetMousePosition();

    private Particle[] particles;
    private int[] activeIndices;
    private int activeCount = 0;
    private Stack<int> freeIndices = new Stack<int>();
    private Random random = new Random();

    public ParticleSystem(Texture2D texture)
    {
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];

        ParticleRenderer = new TextureParticleRender { Texture = texture };
    }
    public ParticleSystem()
    {
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];
    }

    public ParticleSystem(ParticleRenderer CustomRenderer)
    {
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];
        ParticleRenderer = CustomRenderer;
    }

    /// <summary>
    /// Call after updating public fields values
    /// </summary>
    public void Start()
    {
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];
        freeIndices = new Stack<int>();
        activeCount = 0;

        for (int i = MaxParticles - 1; i >= 0; i--)
            freeIndices.Push(i);

        // Initialize particle structs (minimal defaults)
        for (int i = 0; i < MaxParticles; i++)
        {
            particles[i] = new Particle
            {
                TrailKeyframes = new Queue<Vector2>(TrailSegments + 1)
            };
        }
    }

    public void Update(float frameTime)
    {
        // Spawn new particles
        float particlesToSpawn = ParticlesPerSecond *frameTime;

        if (SystemLifeTime > 0)
        {
            SystemAge += frameTime;

            if (SystemAge >= SystemLifeTime)
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

    private void UpdateParticle(float deltaTime, int particleIndex)
    {
        ref var particle = ref particles[particleIndex];

        // Update age
        particle.Age += deltaTime;

        // Apply acceleration to velocity (accumulate effect over time)
        var acceleration = AccelerationPerSecond;
        particle.BaseVelocity += acceleration.Value * deltaTime;

        // Apply velocity
        var velocity = VelocityPerSecond;
        particle.Position.X += (velocity.Value.X + particle.BaseVelocity.X) * deltaTime;
        particle.Position.Y += (velocity.Value.Y + particle.BaseVelocity.Y) * deltaTime;

        // Apply rotation
        particle.Rotation += RotationPerSecond * deltaTime;

        // Calculate lifetime progress (0.0 to 1.0)
        particle.LifetimeProgress = particle.Age / particle.Lifetime;

        // Update size over lifetime (lerp from start to end)
        particle.Size = particle.StartSize + (ParticleEndSize - particle.StartSize) * particle.LifetimeProgress;

        // Update trail if enabled
        if (TrailSegments > 0)
        {
            particle.TrailKeyframes.Enqueue(particle.Position);
            if (particle.TrailKeyframes.Count > TrailSegments)
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
        ParticleEmitter.Emit(ref particle, random, SpawnPosition(), ParticleLifetime, ParticleStartSize, Tint);

        if (TrailSegments > 0)
        {
            var pos = particle.Position;
            for (int j = 0; j < TrailSegments; j++)
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
            float alpha = StartingAlpha * (1.0f - particle.LifetimeProgress);
            for (int j = 0; j < trailPoints.Length - 1; j++)
            {
                float lerp = (float)(j + 1) / trailPoints.Length;
                var trailAlpha = lerp * TrailSegmentRenderer.Color.A / 255.0f * alpha;
                TrailSegmentRenderer.Draw(trailPoints[j], trailPoints[j + 1], trailAlpha);
            }
            ParticleRenderer.Draw(particle, alpha);
        }

        EndBlendMode();
    }

    public void Dispose()
    {
        ParticleRenderer.Dispose();
    }
}