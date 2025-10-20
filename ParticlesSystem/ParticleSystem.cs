using System.Runtime.CompilerServices;

namespace Raylib_cs_fx;

// Particle structure
public enum RotationDirectionType
{
    One,
    Both,
}
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
    //public RotationDirectionType rotationType;
};


public class ParticleSystem : IDisposable
{
    public float RotationPerSecond = 0f;
    public int InitialRotationJitter = 0;
    public Func<Particle,Vector2> VelocityPerSecond;
    public (Vector2 min, Vector2 max) VelocityJitter = (Vector2.Zero,Vector2.Zero);
    public float StartingAlpha = 1f;
    public float ParticleLifetime = 2f;
    public float ParticleLifetimeJitter = 0f;
    public float SystemLifeTime = -1f;
    public float SystemAge = 0f;
    public int ParticlesPerFrame;
    public int MaxParticles = 100;
    public float ParticleStartSize;
    public int ParticleStartSizeJitter = 0;
    public float ParticleEndSize =0;
    public Texture2D Texture;
    public (Vector2 min, Vector2 max) SpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
    /// <summary>
    /// This Jitter works in reverse
    /// So Red jitter has a randomized reduction effect on the Red Channel between 0 and n
    /// So that channel doesn't exceed 255
    /// This will error if a channel goes less than 0
    /// </summary>
    public Color InitialColorJitter = Color.Black; //black is no jitter 
    public Color Tint = Color.White; //white here represents the texture as is

    public Func<Vector2> SpawnPosition = () => GetMousePosition();

    public bool ScaleImage = false;
    public Rectangle? ScaledSize { get; set; }


    private Particle[] particles;
    private int[] activeIndices;
    private int activeCount = 0;
    private Stack<int> freeIndices = new Stack<int>();
    private Random random = new Random();


    public ParticleSystem(Texture2D texture)
    {
        Texture = texture;
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];
        VelocityPerSecond = (_) => Vector2.Zero;
    }

    /// <summary>
    /// Call after updating public fields' values
    /// </summary>
    public void Start()
    {
        particles = new Particle[MaxParticles];
        activeIndices = new int[MaxParticles];
        freeIndices = new Stack<int>();

        for (int i = MaxParticles - 1; i >= 0; i--)
            freeIndices.Push(i);

        // Initialize particles
        for (int i = 0; i < MaxParticles; i++)
        {
            particles[i].Position = new Vector2(0, 0);
            particles[i].Color = new Color(GetRandomValue(Math.Max(255 - InitialColorJitter.R,0), Tint.R),
                                           GetRandomValue(Math.Max(255 - InitialColorJitter.G,0), Tint.G),
                                           GetRandomValue(Math.Max(255 - InitialColorJitter.B,0), Tint.B), 
                                           255);
            particles[i].StartSize = (ParticleStartSizeJitter * random.NextSingle())  + ParticleStartSize;
            particles[i].Size = particles[i].StartSize;
            particles[i].Rotation = GetRandomValue(0, 0+InitialRotationJitter);
            particles[i].Age = 0.0f;
            particles[i].Lifetime = ParticleLifetime + (random.NextSingle() * ParticleLifetimeJitter);
            particles[i].BaseVelocity = GetVector2Jitter(VelocityJitter);
        }
    }

    public void Update(float frameTime)
    {
        // Spawn new particles
        int particlesToSpawn = ParticlesPerFrame;


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

        // Update age
        particles[particleIndex].Age += deltaTime;

        // Apply velocity
        var velocity = VelocityPerSecond(particle);

        particle.Position.X += (velocity.X +particle.BaseVelocity.X) *deltaTime;
        particle.Position.Y += (velocity.Y +particle.BaseVelocity.Y) *deltaTime;

        // Apply rotation
        particle.Rotation += RotationPerSecond * deltaTime;

        // Calculate lifetime progress (0.0 to 1.0)
        particle.LifetimeProgress = particle.Age / particle.Lifetime;

        // Update size over lifetime (lerp from start to end)
        particle.Size = particle.StartSize + (ParticleEndSize - particle.StartSize) * particle.LifetimeProgress;
    }

    public void Stop() 
    {
        particles = [];
        activeIndices = [];
        freeIndices = new Stack<int>();
    }

    private void SpawnParticle(int index)
    {
        particles[index].Age = 0.0f;
        particles[index].Lifetime = ParticleLifetime + (random.NextSingle() * ParticleLifetimeJitter);
        particles[index].Position = SpawnPosition() + GetVector2Jitter(SpawnPositionJitter);
        particles[index].Rotation = GetRandomValue(0, 0 + InitialRotationJitter);
        // Add to active list
        activeIndices[activeCount] = index;
        activeCount++;
    }
    public void Draw()
    {
        BeginBlendMode(BlendMode.Additive);

        // Draw active particles
        for (int i = 0; i < activeCount; i++)
        {
            int particleIndex = activeIndices[i];

            // lifetimeProgress = p[index].age * p[index].OneOverLifetime
            float alpha = StartingAlpha * (1.0f - particles[particleIndex].LifetimeProgress);

            DrawTexturePro(Texture,
                // Original size and position
                new Rectangle(0.0f, 0.0f, Texture.Width, Texture.Height),
                // New size and position
                new Rectangle(particles[particleIndex].Position.X,
                            particles[particleIndex].Position.Y,
                            Texture.Width * particles[particleIndex].Size,
                            Texture.Height * particles[particleIndex].Size),
                // Origin
                new Vector2(Texture.Width * particles[particleIndex].Size *0.5f,
                          Texture.Height * particles[particleIndex].Size  *0.5f),
                // Rotation  
                particles[particleIndex].Rotation,
                // Get color with alpha applied
                Fade(particles[particleIndex].Color, alpha)
            );
        }

        EndBlendMode();
    }

    public void Dispose()
    {
        UnloadTexture(Texture);
    }
}