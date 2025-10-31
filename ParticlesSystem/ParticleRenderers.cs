namespace Raylib_cs_fx;
/// <summary>
/// You can also offer your own custom implemenation by inheriting this class
/// </summary>
public abstract class ParticleRenderer : IDisposable
{
    public abstract void Dispose();

    public abstract void Draw(Particle particle, float alpha);
}

public class TextureParticleRender : ParticleRenderer
{
    public Texture2D Texture;

    public override void Dispose()
    {
        UnloadTexture(Texture);
    }

    public override void Draw(Particle particle, float alpha)
    {
        DrawTexturePro(Texture,
            // Original size and position
            new Rectangle(0.0f, 0.0f, Texture.Width, Texture.Height),
            // New size and position
            new Rectangle(particle.Position.X,
                        particle.Position.Y,
                        Texture.Width * particle.Size,
                        Texture.Height * particle.Size),
            // Origin
            new Vector2(Texture.Width * particle.Size * 0.5f,
                      Texture.Height * particle.Size * 0.5f),
            // Rotation  
            particle.Rotation,
            // Get color with alpha applied
            ColorAlpha(particle.Color, alpha));
    }
}

public class CircleRenderer : ParticleRenderer
{
    public override void Dispose() { }

    public override void Draw(Particle particle, float alpha)
    {
        DrawCircleV(particle.Position, particle.Size, ColorAlpha(particle.Color, alpha));
    }

}

public class RectangleParticleRenderer : ParticleRenderer
{
    public override void Dispose() { }

    public override void Draw(Particle particle, float alpha)
    {
        DrawRectanglePro(
            new Rectangle(particle.Position.X, particle.Position.Y, particle.Size, particle.Size),
            Vector2.Zero,
            // Rotation  
            particle.Rotation,
            // Get color with alpha applied
            ColorAlpha(particle.Color, alpha));
    }
}

public class StarParticleRender: ParticleRenderer
{
    public int Points = 6;
    /// <summary>
    /// 1.0 pointiness is a normal polygon. 
    /// </summary>
    public float Pointiness = 2f;
    public override void Dispose() { }

    public override void Draw(Particle particle, float alpha)
    {
        float pi = MathF.PI;
        float angleStep = pi * 2.0f / Points;

        float radius = particle.Size * Pointiness;
        float innerRadius = particle.Size;

        for (int i = 0; i < Points; i++)
        {
            // Current outer point (tip)
            float outerAngle = i * angleStep - pi *0.5f;
            Vector2 outerPoint = new Vector2(
                particle.Position.X + MathF.Cos(outerAngle) * radius,
                particle.Position.Y + MathF.Sin(outerAngle) * radius
            );

            // Inner points on either side
            float innerAngle1 = outerAngle - angleStep *0.5f;
            float innerAngle2 = outerAngle + angleStep *0.5f;

            Vector2 innerPoint1 = new Vector2(
                particle.Position.X + MathF.Cos(innerAngle1) * innerRadius,
                particle.Position.Y + MathF.Sin(innerAngle1) * innerRadius
            );

            Vector2 innerPoint2 = new Vector2(
                particle.Position.X + MathF.Cos(innerAngle2) * innerRadius,
                particle.Position.Y + MathF.Sin(innerAngle2) * innerRadius
            );

            // Draw triangles with consistent winding order (counter-clockwise)
            DrawTriangle(innerPoint1, particle.Position, outerPoint, ColorAlpha(particle.Color, alpha));
            DrawTriangle(outerPoint, particle.Position, innerPoint2, ColorAlpha(particle.Color, alpha));
        }
    }
}
