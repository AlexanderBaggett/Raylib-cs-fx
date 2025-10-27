using Raylib_cs_fx;
using System.Numerics;

namespace AttractorSystemTestBed
{
    #pragma warning disable Attractor01
    internal class Program
    {
        static void Main(string[] args)
        {
            const int screenWidth = 1280;
            const int screenHeight = 1024;

            SetConfigFlags(ConfigFlags.VSyncHint);
            InitWindow(screenWidth, screenHeight, "Particles!");

            using AttractorSystem particleSystem = new()
            {
                RotationPerSecond = 0f,
                ParticleLifetime = -1,
                StartingAlpha = 0.4f,
                ParticlesPerFrame = 40_000,
                MaxParticles = 50_000,
                ParticleStartSize = 1f,
                Scale = new Vector2(screenWidth, screenHeight),
                SpawnPosition = GetMousePosition,
                Tint = Color.DarkPurple,
                SpawnPositionJitter = (Vector2.Zero, Vector2.Zero),
                //Tint = Color.Red,
                AttractorTypes = new List<AttractorType>() { AttractorType.Tree3 },
                DrawCommand = ((Particle particle, Color calculatedColor, Vector2 calculatedPosition) details) => DrawCircleV(details.calculatedPosition,5 ,details.calculatedColor)

            };

            particleSystem.Start();


            SetTargetFPS(60);

            while (!WindowShouldClose())
            {

                BeginDrawing();
                ClearBackground(Color.DarkGray);
                particleSystem.Update(GetFrameTime());
                particleSystem.Draw();
                EndDrawing();

            }
            CloseWindow();
        }
    }
#pragma warning restore Attractor01
}
