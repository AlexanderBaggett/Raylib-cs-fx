
using Raylib_cs_fx;

namespace AdvancedParticleTestBed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int screenWidth = 1280;
            const int screenHeight = 1024;

            InitWindow(screenWidth, screenHeight, "Particles!");

            SetTargetFPS(60);

            using CompoundParticleSystem particleSystem = new CompoundParticleSystem(
                LoadTexture("Assets/camo.png"),
                LoadTexture("Assets/cloud3.png"))
            {
                PrimarySpawnPositionJitter = new (new Vector2(-100,-100), new Vector2(100,100)),
                PrimaryInitialRotationJitter = 360,
                SecondaryVelocityJitter = new (new Vector2(-200,-200), new Vector2(200,200)),
                PrimaryParticleLifetime = 1.1f,
                SecondaryParticleLifetime = 3,
                MaxPrimaryParticles = 1000,
                PrimaryParticlesPerFrame = 5,
                MaxSecondaryParticlesPerPrimary = 300,
                MaxTotalSecondaryParticles = 20_000_00,
                SecondaryParticleStartSize = 0.5f,
                SecondaryParticleEndSize =  0.001f,
                SpawnMode = CompoundParticleSystem.SecondarySpawnMode.OnIntervalDuringPrimaryLifetime,
                SecondarySpawnInterval = .1f
            };

            particleSystem.Start();


            //SetTargetFPS(60);

            while (!WindowShouldClose())
            {

                BeginDrawing();
                ClearBackground(Color.DarkGray);

                particleSystem.Update(GetFrameTime());
                particleSystem.Draw();

                DrawFPS(20, 20);
                EndDrawing();

            }
            CloseWindow();
        }
    }
}
