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
