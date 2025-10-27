namespace Raylib_cs_fx
{
    public interface ISystem
    {
        void Draw();
        void Update(float frameTime);
        void Start();
        void Stop();

    }
}
