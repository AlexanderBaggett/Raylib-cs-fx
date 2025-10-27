namespace Raylib_cs_fx;

/// <summary>
/// You can implement your own by inheriting from this class
/// </summary>
public abstract class TrailSegmentRenderer
{
    public float Width;
    public Color Color;
    public abstract void Draw(Vector2 segPos1, Vector2 segPos2, float trailAlpha);
}

public class LineTrailSegmentRenderer : TrailSegmentRenderer
{
    public override void Draw(Vector2 segPos1, Vector2 segPos2, float trailAlpha)
    {
        DrawLineEx(segPos1, segPos2, Width, ColorAlpha(Color, trailAlpha));
    }
}
public class BezierLineTrailSegmentRenderer : TrailSegmentRenderer
{
    public override void Draw(Vector2 segPos1, Vector2 segPos2, float trailAlpha)
    {
        DrawLineBezier(segPos1, segPos2, Width, ColorAlpha(Color, trailAlpha));
    }
}
public class CirclTrailSegmentRenderer : TrailSegmentRenderer
{

    public override void Draw(Vector2 segPos1, Vector2 segPos2, float trailAlpha)
    {
        DrawCircleV(segPos1, Width, ColorAlpha(Color, trailAlpha));
    }
}
public class TextureTrailSegment : TrailSegmentRenderer
{
    public Texture2D Texture;
    public float Rotation;

    public override void Draw(Vector2 segPos1, Vector2 segPos2, float trailAlpha)
    {
        DrawTexturePro(
            Texture,
            new Rectangle(0.0f, 0.0f, Texture.Width, Texture.Height),
                // New size and position
                new Rectangle(segPos1.X,
                            segPos1.Y,
                            Texture.Width * Width,
                            Texture.Height * Width),
                // Origin
                new Vector2(Texture.Width * Width * 0.5f,
                            Texture.Height * Width * 0.5f),
                // Rotation  
                Rotation,
                Color);
    }
}