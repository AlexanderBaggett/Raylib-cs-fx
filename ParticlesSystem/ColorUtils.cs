
namespace Raylib_cs_fx
{
    public static class ColorUtils
    {
        public static (float R, float G, float B) ToFloats(this Color color)
        {
            return (color.R / 255f, color.G / 255f, color.B / 255f);
        }
    }
}
