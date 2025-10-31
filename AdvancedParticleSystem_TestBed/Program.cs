using Raylib_cs_fx;
using System.Numerics;

namespace AdvancedParticleSystem_TestBed;

internal class Program
{
    static void Main(string[] args)
    {
        const int screenWidth = 1280;
        const int screenHeight = 1024;

        InitWindow(screenWidth, screenHeight, "Particles!");

        using AdvancedParticleSystem particleSystem = new AdvancedParticleSystem(LoadTexture("Assets/cloud3.png"))
        {
            RotationPerSecond = 0f,
            ParticleLifetime = 1f, // Increased to allow visible orbits
            AccelerationPersecond = new Vector2(0, 900),
            VelocityJitter = (new Vector2(-500, -500), new Vector2(500, 500)),
            StartingAlpha = 0.4f,
            ParticlesPerSecond = 20 * 60,
            MaxParticles = 40_000,
            ParticleStartSize = 1f,
            ParticleEndSize = 0.5f,
            InitialRotationJitter = 180,
            SpawnPosition = GetMousePosition,
            Tint = Color.Magenta,
            InitialColorJitter = Color.Green,
            SpawnPositionJitter = (new Vector2(-200, -200), new Vector2(200, 200)),
            Segments =new Func<Particle,TrailSegmentRenderer[]>((particle) => Enumerable.Repeat(new LineTrailSegmentRenderer(), 10).ToArray()),
        };

        particleSystem.Start();


        SetTargetFPS(60);

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
