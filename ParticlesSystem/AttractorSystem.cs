using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Raylib_cs;

namespace Raylib_cs_fx
{
    public enum AttractorType
    {
        Sierpinski,        // Sierpinski triangle
        BarnsleyFern,      // Barnsley fern
        DragonCurve,       // Dragon curve fractal
        LevyCCurve,        // Levy C curve fractal
        Tree1,             // Tree (January 1999, first variant)
        Tree2,             // Tree (January 1999, second variant)
        Tree3,             // Tree (January 1999, third variant)
        TreeDerby,         // Tree (Derby, Western Australia)
        MapleLeaf,         // Maple leaf
        Spiral,            // IFS spiral
        MandelbrotLike,    // Mandelbrot-like IFS
        Leaf,              // Leaf IFS
        SandDollarSnowflake, // Sand dollar snowflake IFS
        Twig,              // Twig IFS
        ChristmasTree,     // Christmas tree IFS
        ChaosText,         // Chaos text IFS
        Custom             // User-defined
    }

    [Experimental("Attractor01")]
    public class AttractorSystem : IDisposable, ISystem
    {
        // Public properties
        public float StartingAlpha = 1f;
        public float ParticleLifetime = 2f;
        public float ParticleLifetimeJitter = 0f;
        public float SystemLifeTime = -1f;
        public float SystemAge = 0f;
        public int ParticlesPerFrame = 10;
        public int MaxParticles = 1000;
        public float ParticleStartSize = 1f;
        public float ParticleStartSizeJitter = 0f;
        public Color InitialColorJitter = Color.Black;
        public Color Tint = Color.White;
        public Func<Vector2> SpawnPosition = () => Vector2.Zero;
        public (Vector2 min, Vector2 max) SpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
        public List<AttractorType> AttractorTypes = new List<AttractorType>();
        public Action<(Particle particle, Color calculatedColor,Vector2 calculatedPosition)> DrawCommand = ((Particle particle, Color calculatedColor, Vector2 calculatedPosition) details) => DrawPixelV(details.calculatedPosition, details.calculatedColor);
        private List<(List<Matrix3x2> matrices, float[] probabilities)> Attractors =new List<(List<Matrix3x2>, float[])>();
        public Vector2 Scale = new Vector2(400, 400);
        public float RotationPerSecond { get; set; }

        // Mapping of attractor types to matrices and probabilities
        private Dictionary<AttractorType, (Matrix3x2[], float[])> attractorPresets =
            new Dictionary<AttractorType, (Matrix3x2[], float[])>
        {
            {
                AttractorType.Sierpinski,
                (new Matrix3x2[]
                {
                    Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateTranslation(0, 0),
                    Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateTranslation(0.5f, 0),
                    Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateTranslation(0.25f, 0.5f)
                }, new float[] { 1f/3, 1f/3, 1f/3 })
            },
            {
                AttractorType.BarnsleyFern,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0, 0, 0, 0, 0.16f, 0),
                    new Matrix3x2(0.85f, 0.04f, 0, -0.04f, 0.85f, 1.6f),
                    new Matrix3x2(0.2f, -0.26f, 0, 0.23f, 0.22f, 1.6f),
                    new Matrix3x2(-0.15f, 0.28f, 0, 0.26f, 0.24f, 0.44f)
                }, new float[] { 0.01f, 0.85f, 0.07f, 0.07f })
            },
            {
                AttractorType.DragonCurve,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.824074f, 0.281428f, -1.88229f, -0.212346f, 0.864198f, -0.110607f),
                    new Matrix3x2(0.088272f, 0.520988f, 0.78536f, -0.463889f, -0.377778f, 8.095795f)
                }, new float[] { 0.8f, 0.2f })
            },
            {
                AttractorType.LevyCCurve,
                (new Matrix3x2[]
                {
                    Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateRotation(MathF.PI / 4) * Matrix3x2.CreateTranslation(0, 0),
                    Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateRotation(-MathF.PI / 4) * Matrix3x2.CreateTranslation(0.5f, 0)
                }, new float[] { 0.5f, 0.5f })
            },
            {
                AttractorType.Tree1,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.05f, 0, -0.06f, 0, 0.4f, -0.47f),
                    new Matrix3x2(-0.05f, 0, -0.06f, 0, -0.4f, -0.47f),
                    new Matrix3x2(0.03f, -0.14f, -0.16f, 0, 0.26f, -0.01f),
                    new Matrix3x2(-0.03f, 0.14f, -0.16f, 0, -0.26f, -0.01f),
                    new Matrix3x2(0.56f, 0.44f, 0.3f, -0.37f, 0.51f, 0.15f),
                    new Matrix3x2(0.19f, 0.07f, -0.2f, -0.1f, 0.15f, 0.28f),
                    new Matrix3x2(-0.33f, -0.34f, -0.54f, -0.33f, 0.34f, 0.39f)
                }, new float[] { 1f/7, 1f/7, 1f/7, 1f/7, 1f/7, 1f/7, 1f/7 })
            },
            {
                AttractorType.Tree2,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.01f, 0, 0, 0, 0.45f, 0),
                    new Matrix3x2(-0.01f, 0, 0, 0, -0.45f, 0.4f),
                    new Matrix3x2(0.42f, -0.42f, 0, 0.42f, 0.42f, 0.4f),
                    new Matrix3x2(0.42f, 0.42f, 0, -0.42f, 0.42f, 0.4f)
                }, new float[] { 0.25f, 0.25f, 0.25f, 0.25f })
            },
            {
                AttractorType.Tree3,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.195f, -0.488f, 0.4431f, 0.344f, 0.443f, 0.2452f),
                    new Matrix3x2(0.462f, 0.414f, 0.2511f, -0.252f, 0.361f, 0.5692f),
                    new Matrix3x2(-0.637f, 0, 0.8562f, 0, 0.501f, 0.2512f),
                    new Matrix3x2(-0.035f, 0.07f, 0.4884f, -0.469f, 0.022f, 0.5069f),
                    new Matrix3x2(-0.058f, -0.07f, 0.5976f, 0.453f, -0.111f, 0.0969f)
                }, new float[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f })
            },
            {
                AttractorType.TreeDerby,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.05f, 0, 0, 0, 0.6f, 0),
                    new Matrix3x2(0.05f, 0, 0, 0, -0.5f, 1.0f),
                    new Matrix3x2(0.6f * MathF.Cos(0.698f), -0.5f * MathF.Sin(0.698f), 0, 0.6f * MathF.Sin(0.698f), 0.5f * MathF.Cos(0.698f), 0.6f),
                    new Matrix3x2(0.5f * MathF.Cos(0.349f), -0.45f * MathF.Sin(0.3492f), 0, 0.5f * MathF.Sin(0.349f), 0.45f * MathF.Cos(0.3492f), 1.1f),
                    new Matrix3x2(0.5f * MathF.Cos(-0.524f), -0.55f * MathF.Sin(-0.524f), 0, 0.5f * MathF.Sin(-0.524f), 0.55f * MathF.Cos(-0.524f), 1.0f),
                    new Matrix3x2(0.55f * MathF.Cos(-0.698f), -0.4f * MathF.Sin(-0.698f), 0, 0.55f * MathF.Sin(-0.698f), 0.4f * MathF.Cos(-0.698f), 0.7f)
                }, new float[] { 1f/6, 1f/6, 1f/6, 1f/6, 1f/6, 1f/6 })
            },
            {
                AttractorType.MapleLeaf,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.14f, 0.01f, -0.08f, 0, 0.51f, -1.31f),
                    new Matrix3x2(0.43f, 0.52f, 1.49f, -0.45f, 0.5f, -0.75f),
                    new Matrix3x2(0.45f, -0.49f, -1.62f, 0.47f, 0.47f, -0.74f),
                    new Matrix3x2(0.49f, 0, 0.02f, 0, 0.51f, 1.62f)
                }, new float[] { 0.25f, 0.25f, 0.25f, 0.25f })
            },
            {
                AttractorType.Spiral,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.787879f, -0.424242f, 1.758647f, 0.242424f, 0.859848f, 1.408065f),
                    new Matrix3x2(-0.121212f, 0.257576f, -6.721654f, 0.151515f, 0.05303f, 1.377236f),
                    new Matrix3x2(0.181818f, -0.136364f, 6.086107f, 0.090909f, 0.181818f, 1.568035f)
                }, new float[] { 0.9f, 0.05f, 0.05f })
            },
            {
                AttractorType.MandelbrotLike,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.202f, -0.805f, -0.373f, -0.689f, -0.342f, -0.653f),
                    new Matrix3x2(0.138f, 0.665f, 0.66f, -0.502f, -0.222f, -0.277f)
                }, new float[] { 0.5f, 0.5f })
            },
            {
                AttractorType.Leaf,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0, 0.2439f, 0, 0, 0.3053f, 0),
                    new Matrix3x2(0.7248f, 0.0337f, 0.206f, -0.0253f, 0.7426f, 0.2538f),
                    new Matrix3x2(0.1583f, -0.1297f, 0.1383f, 0.355f, 0.3676f, 0.175f),
                    new Matrix3x2(0.3386f, 0.3694f, 0.0679f, 0.2227f, -0.0756f, 0.0826f)
                }, new float[] { 0.25f, 0.25f, 0.25f, 0.25f })
            },
            {
                AttractorType.SandDollarSnowflake,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.382f, 0, 0.309f, 0, 0.382f, 0.57f),
                    new Matrix3x2(0.118f, -0.3633f, 0.3633f, 0.3633f, 0.118f, 0.3306f),
                    new Matrix3x2(0.118f, 0.3633f, 0.5187f, -0.3633f, 0.118f, 0.694f),
                    new Matrix3x2(-0.309f, -0.2245f, 0.607f, 0.2245f, -0.309f, 0.309f),
                    new Matrix3x2(-0.309f, 0.2245f, 0.7016f, -0.2245f, -0.309f, 0.5335f),
                    new Matrix3x2(0.382f, 0, 0.309f, 0, -0.382f, 0.677f)
                }, new float[] { 1f/6, 1f/6, 1f/6, 1f/6, 1f/6, 1f/6 })
            },
            {
                AttractorType.Twig,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0.387f, 0.43f, 0.256f, 0.43f, -0.387f, 0.522f),
                    new Matrix3x2(0.441f, -0.091f, 0.4219f, -0.009f, -0.322f, 0.5059f),
                    new Matrix3x2(-0.468f, 0.02f, 0.4f, -0.113f, 0.015f, 0.4f)
                }, new float[] { 1f/3, 1f/3, 1f/3 })
            },
            {
                AttractorType.ChristmasTree,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0, -0.5f, 0.5f, 0.5f, 0, 0),
                    new Matrix3x2(0, 0.5f, 0.5f, -0.5f, 0, 0.5f),
                    new Matrix3x2(0.5f, 0, 0.25f, 0, 0.5f, 0.5f)
                }, new float[] { 1f/3, 1f/3, 1f/3 })
            },
            {
                AttractorType.ChaosText,
                (new Matrix3x2[]
                {
                    new Matrix3x2(0, 0.053f, -7.083f, -0.429f, 0, 5.43f),
                    new Matrix3x2(0.143f, 0, -5.619f, 0, -0.053f, 8.513f),
                    new Matrix3x2(0.143f, 0, -5.619f, 0, 0.083f, 2.057f),
                    new Matrix3x2(0, 0.053f, -3.952f, 0.429f, 0, 5.43f),
                    new Matrix3x2(0.119f, 0, -2.555f, 0, 0.053f, 4.536f),
                    new Matrix3x2(-0.0123806f, -0.0649723f, -1.226f, 0.423819f, 0.00189797f, 5.235f),
                    new Matrix3x2(0.0852291f, 0.0506328f, -0.421f, 0.420449f, 0.0156626f, 4.569f),
                    new Matrix3x2(0.104432f, 0.00529117f, 0.976f, 0.0570516f, 0.0527352f, 8.113f),
                    new Matrix3x2(-0.00814186f, -0.0417935f, 1.934f, 0.423922f, 0.00415972f, 5.37f),
                    new Matrix3x2(0.093f, 0, 0.861f, 0, 0.053f, 4.536f),
                    new Matrix3x2(0, 0.053f, 2.447f, -0.429f, 0, 5.43f),
                    new Matrix3x2(0.119f, 0, 3.363f, 0, -0.053f, 8.513f),
                    new Matrix3x2(0.119f, 0, 3.363f, 0, 0.053f, 1.487f),
                    new Matrix3x2(0, 0.053f, 3.972f, 0.429f, 0, 4.569f),
                    new Matrix3x2(0.123998f, -0.00183957f, 6.275f, 0.000691208f, 0.0629731f, 7.716f),
                    new Matrix3x2(0, 0.053f, 5.215f, 0.167f, 0, 6.483f),
                    new Matrix3x2(0.071f, 0, 6.279f, 0, 0.053f, 5.298f),
                    new Matrix3x2(0, -0.053f, 6.805f, -0.238f, 0, 3.714f),
                    new Matrix3x2(-0.121f, 0, 5.941f, 0, 0.053f, 1.487f)
                }, new float[] { 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19,
                                 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19, 1f/19 })
            },
            {
                AttractorType.Custom,
                (new Matrix3x2[] { Matrix3x2.Identity }, new float[] { 1f })
            }
        };

        private Particle[] particles;
        private int[] activeIndices;
        private int activeCount = 0;
        private Stack<int> freeIndices = new Stack<int>();
        private Random random = new Random();

        public AttractorSystem()
        {
            particles = new Particle[MaxParticles];
            activeIndices = new int[MaxParticles];
        }

        public void Start()
        {
            particles = new Particle[MaxParticles];
            activeIndices = new int[MaxParticles];
            freeIndices = new Stack<int>();
            for (int i = MaxParticles - 1; i >= 0; i--)
                freeIndices.Push(i);

            Attractors.Clear();
            foreach (var type in AttractorTypes)
            {
                if (attractorPresets.ContainsKey(type))
                {
                    (var matrices, var probs)= attractorPresets[type];
                    Attractors.Add((matrices.ToList(), probs));
                }
            }

            for (int i = 0; i < MaxParticles; i++)
            {
                particles[i].Position = SpawnPosition() + GetVector2Jitter(SpawnPositionJitter);
                particles[i].Color = new Color(
                    GetRandomValue(Math.Max(Tint.R - InitialColorJitter.R, 0), Tint.R),
                    GetRandomValue(Math.Max(Tint.G - InitialColorJitter.G, 0), Tint.G),
                    GetRandomValue(Math.Max(Tint.B - InitialColorJitter.B, 0), Tint.B),
                    255
                );
                particles[i].Size = ParticleStartSize + (random.NextSingle() * ParticleStartSizeJitter);
                particles[i].Age = 0.0f;
                particles[i].Lifetime = ParticleLifetime + (random.NextSingle() * ParticleLifetimeJitter);
                particles[i].LifetimeProgress = 0.0f;
            }
        }

        private Vector2 GetVector2Jitter((Vector2 min, Vector2 max) jitter)
        {
            return new Vector2(
                jitter.min.X + random.NextSingle() * (jitter.max.X - jitter.min.X),
                jitter.min.Y + random.NextSingle() * (jitter.max.Y - jitter.min.Y)
            );
        }

        private void SpawnParticle(int index)
        {
            ref var particle = ref particles[index];
            particle.Age = 0.0f;
            particle.Lifetime = ParticleLifetime + (random.NextSingle() * ParticleLifetimeJitter);
            particle.Position = SpawnPosition() + GetVector2Jitter(SpawnPositionJitter);
            particle.Size = ParticleStartSize + (random.NextSingle() * ParticleStartSizeJitter);
            activeIndices[activeCount] = index;
            activeCount++;
        }

        private void DespawnParticle(int i, int particleIndex)
        {
            freeIndices.Push(particleIndex);
            activeIndices[i] = activeIndices[activeCount - 1];
            activeCount--;
        }

        private void UpdateParticle(float deltaTime, int particleIndex)
        {
            ref var particle = ref particles[particleIndex];
            if (particle.Lifetime > 0)
            {
                particle.Age += deltaTime;
                particle.LifetimeProgress = particle.Age / particle.Lifetime;
            }
            particle.Rotation += RotationPerSecond * deltaTime;
            Vector2 newPosition = particle.Position;
            foreach (var (matrices, probabilities) in Attractors)
            {
                if (matrices.Count > 0)
                {
                    float r = random.NextSingle();
                    float cumulative = 0f;
                    int attractorIndex = 0;
                    for (int i = 0; i < probabilities.Length; i++)
                    {
                        cumulative += probabilities[i];
                        if (r < cumulative)
                        {
                            attractorIndex = i;
                            break;
                        }
                    }
                    newPosition = Vector2.Transform(newPosition, matrices[attractorIndex]);
                }
            }
            particle.Position = newPosition;
        }

        public void Update(float frameTime)
        {
            if (SystemLifeTime > 0)
            {
                SystemAge += frameTime;
                if (SystemAge >= SystemLifeTime)
                {
                    Stop();
                    SystemAge = 0f;
                }
            }

            int particlesToSpawn = ParticlesPerFrame;
            while (particlesToSpawn > 0 && freeIndices.Count > 0)
            {
                int index = freeIndices.Pop();
                SpawnParticle(index);
                particlesToSpawn--;
            }

            for (int i = activeCount - 1; i >= 0; i--)
            {
                int particleIndex = activeIndices[i];
                UpdateParticle(frameTime, particleIndex);
                if (particles[particleIndex].Lifetime > 0 &&
                    particles[particleIndex].Age >= particles[particleIndex].Lifetime)
                {
                    DespawnParticle(i, particleIndex);
                }
            }
        }

        public void Draw()
        {
            //BeginBlendMode(BlendMode.Additive);
            for (int i = 0; i < activeCount; i++)
            {
                int particleIndex = activeIndices[i];
                var particle = particles[particleIndex];
                float alpha = StartingAlpha * (1.0f - particle.LifetimeProgress);
                Vector2 screenPos = new Vector2(
                    particle.Position.X * Scale.X,
                    particle.Position.Y * Scale.Y
                );
                DrawCommand((particle, ColorAlpha(particle.Color, alpha), screenPos));
            }
            //EndBlendMode();
        }

        private Color ColorAlpha(Color color, float alpha)
        {
            return new Color(color.R, color.G, color.B, (byte)(color.A * alpha));
        }

        public void Stop()
        {
            particles = [];
            activeIndices = [];
            freeIndices = new Stack<int>();
        }

        public void Dispose()
        {
            // No texture to unload
        }
    }
}
