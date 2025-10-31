using System.Runtime.CompilerServices;

namespace Raylib_cs_fx
{
    public abstract class ParticleEmitter
    {
        // Common jitter fields (moved from ParticleSystem)
        public (Vector2 min, Vector2 max) VelocityJitter = (Vector2.Zero, Vector2.Zero);
        public (Vector2 min, Vector2 max) SpawnPositionJitter = (Vector2.Zero, Vector2.Zero);
        public float LifetimeJitter = 0f;
        public int RotationJitter = 0;
        public float StartSizeJitter = 0f;
        /// <summary>
        /// This Jitter works in reverse
        /// So Red jitter has a randomized reduction effect on the Red Channel between 0 and n
        /// So that channel doesn't exceed 255
        /// This will error if a channel goes less than 0
        /// </summary>
        public Color ColorJitter = Color.Black;//blacks means none

        /// <summary>
        /// Shape-specific position offset (relative to basePosition).
        /// Subclasses override this for custom shapes (e.g., circle, polygon).
        /// </summary>
        protected abstract Vector2 GetSpawnOffset(Random random);

        /// <summary>
        /// Sets up the initial particle state with jitters applied.
        /// Call this from ParticleSystem.SpawnParticle.
        /// Base values (lifetime, size, tint) come from ParticleSystem.
        /// </summary>
        public virtual void Emit(ref Particle particle, Random random, Vector2 basePosition, float baseLifetime, float baseStartSize, Color baseTint)
        {
            particle.Position = basePosition + GetSpawnOffset(random);
            particle.BaseVelocity = GetVector2Jitter(VelocityJitter, random);
            particle.Lifetime = baseLifetime + random.NextSingle() * LifetimeJitter;
            particle.Rotation = random.Next(0, RotationJitter);
            particle.StartSize = baseStartSize + random.NextSingle() * StartSizeJitter;
            particle.Size = particle.StartSize;
            particle.Color = new Color(
                random.Next(Math.Max(baseTint.R - ColorJitter.R, 0), baseTint.R),
                random.Next(Math.Max(baseTint.G - ColorJitter.G, 0), baseTint.G),
                random.Next(Math.Max(baseTint.B - ColorJitter.B, 0), baseTint.B),
                255
            );
            particle.Age = 0.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector2 GetVector2Jitter((Vector2 min, Vector2 max) jitter, Random random)
        {
            return new Vector2(
                jitter.min.X + random.NextSingle() * (jitter.max.X - jitter.min.X),
                jitter.min.Y + random.NextSingle() * (jitter.max.Y - jitter.min.Y)
            );
        }
    }

    /// <summary>
    /// Default emitter: Spawns at exact position or with box jitter (matches current ParticleSystem behavior).
    /// Use SpawnPositionJitter for box variation (set to (Zero, Zero) for pure point).
    /// </summary>
    public class DefaultEmitter : ParticleEmitter
    {
        protected override Vector2 GetSpawnOffset(Random random)
        {
            return GetVector2Jitter(SpawnPositionJitter, random);
        }
    }
    public class TriangleEmitter : ParticleEmitter
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;

        public TriangleEmitter(float size = 10f)
        {
            A = new Vector2(-size, -size);
            B = new Vector2(size, -size);
            C = new Vector2(0f, size);
        }

        protected override Vector2 GetSpawnOffset(Random random)
        {
            // Barycentric coordinates – uniform sampling inside a triangle
            float r1 = random.NextSingle();
            float r2 = random.NextSingle();

            if (r1 + r2 > 1f)
            {
                r1 = 1f - r1;
                r2 = 1f - r2;
            }

            float wA = 1f - r1 - r2;
            float wB = r1;
            float wC = r2;

            return wA * A + wB * B + wC * C;
        }
    }

    /// <summary>
    /// Rectangular emitter: Spawns uniformly within a centered rectangle.
    /// For asymmetric boxes, use DefaultEmitter with SpawnPositionJitter.
    /// </summary>
    public class RectangularEmitter : ParticleEmitter
    {
        public Vector2 Size = Vector2.Zero; // Width/height of rectangle (set to Zero for point)

        protected override Vector2 GetSpawnOffset(Random random)
        {
            return new Vector2(
                (random.NextSingle() - 0.5f) * Size.X,
                (random.NextSingle() - 0.5f) * Size.Y
            );
        }
    }

    public class CircularEmitter : ParticleEmitter
    {
        public float Radius = 0f;   // 0 → point emitter
        protected override Vector2 GetSpawnOffset(Random random)
        {
            if (Radius <= 0f) return Vector2.Zero;

            // Uniform disk sampling: random angle + sqrt(random) for uniform area
            float angle = random.NextSingle() * 2f * MathF.PI;
            float r = MathF.Sqrt(random.NextSingle()) * Radius;

            return new Vector2(
                MathF.Cos(angle) * r,
                MathF.Sin(angle) * r);
        }
    }

    public class PolygonEmitter : ParticleEmitter
    {
        public int Sides = 3;      // ≥ 3
        public float Radius = 10f; // distance from centre to vertex

        protected override Vector2 GetSpawnOffset(Random random)
        {
            if (Sides < 3) Sides = 3;
            if (Radius <= 0f) return Vector2.Zero;

            // 1. Choose a random vertex index
            int idx = random.Next(Sides);
            float angle = idx * 2f * MathF.PI / Sides;

            // 2. Vertex position
            Vector2 v = new Vector2(
                MathF.Cos(angle) * Radius,
                MathF.Sin(angle) * Radius);

            float r1 = random.NextSingle();
            float r2 = random.NextSingle();

            if (r1 + r2 > 1f)
            {
                r1 = 1f - r1;
                r2 = 1f - r2;
            }

            float wCenter = 1f - r1 - r2;
            float wV = r1;
            float wVnext = r2;

            // Next vertex
            float nextAngle = (idx + 1) * 2f * MathF.PI / Sides;
            Vector2 vNext = new Vector2(
                MathF.Cos(nextAngle) * Radius,
                MathF.Sin(nextAngle) * Radius);

            return wCenter * Vector2.Zero + wV * v + wVnext * vNext;
        }
    }

    public class StarEmitter : ParticleEmitter
    {
        public int Sides = 3;        // number of points (≥ 3)
        public float RadiusOne = 10f; // inner radius (valley)
        public float RadiusTwo = 12f; // outer radius (tip)

        protected override Vector2 GetSpawnOffset(Random random)
        {
            if (Sides < 3) Sides = 3;
            if (RadiusOne <= 0f && RadiusTwo <= 0f) return Vector2.Zero;

            // Choose a random tip (outer radius)
            int tipIdx = random.Next(Sides);
            float tipAngle = tipIdx * 2f * MathF.PI / Sides;

            Vector2 tip = new Vector2(
                MathF.Cos(tipAngle) * RadiusTwo,
                MathF.Sin(tipAngle) * RadiusTwo);

            // Adjacent inner points (valleys)
            float innerAngle1 = (tipIdx - 0.5f) * 2f * MathF.PI / Sides;
            float innerAngle2 = (tipIdx + 0.5f) * 2f * MathF.PI / Sides;

            Vector2 inner1 = new Vector2(
                MathF.Cos(innerAngle1) * RadiusOne,
                MathF.Sin(innerAngle1) * RadiusOne);

            Vector2 inner2 = new Vector2(
                MathF.Cos(innerAngle2) * RadiusOne,
                MathF.Sin(innerAngle2) * RadiusOne);

            // Uniformly sample inside the triangle centre-tip-inner1-inner2
            // (use barycentric coordinates on the three sub-triangles)
            float r = random.NextSingle();
            if (r < 0.5f) // centre-tip-inner1
            {
                float u = random.NextSingle();
                float v = random.NextSingle();
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                float wC = 1f - u - v;
                return wC * Vector2.Zero + u * tip + v * inner1;
            }
            else // centre-tip-inner2
            {
                float u = random.NextSingle();
                float v = random.NextSingle();
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                float wC = 1f - u - v;
                return wC * Vector2.Zero + u * tip + v * inner2;
            }
        }
    }
}