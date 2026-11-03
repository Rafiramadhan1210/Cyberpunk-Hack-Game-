using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace CyberpunkHackGame
{
    // ==========================================
    // 1. INTERFACES (ABSTRACTION)
    // ==========================================
    public interface IDamageable
    {
        void TakeDamage(int amount);
        bool IsAlive { get; }
    }

    public interface IHackable
    {
        bool TryHack(int hackPower);
        void OnHacked();
    }

    // Dynamic Visual FX Structures
    public struct FloatingText
    {
        public string Text;
        public Vector2 Position;
        public Color Color;
        public float Alpha;
        public float LifeTime;
    }

    public struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float LifeTime;
    }

    // ==========================================
    // 2. ABSTRACT CLASS (INHERITANCE & ENCAPSULATION)
    // ==========================================
    public abstract class RobotBody : IDamageable, IHackable
    {
        public string ModelName { get; private set; }
        public string Symbol { get; protected set; }
        public Color RenderColor { get; protected set; }
        public int MaxHP { get; protected set; }
        public int CurrentHP { get; protected set; }
        public int BaseDamage { get; protected set; }
        public int MoveRange { get; protected set; }
        public int HackDifficulty { get; protected set; }
        public bool IsHacked { get; set; }
        
        public (int X, int Y) GridPos { get; set; }
        public Vector2 WorldPos { get; set; }

        public bool IsAlive => CurrentHP > 0;

        protected RobotBody(string modelName, string symbol, Color color, int hp, int damage, int moveRange, int hackDiff, int startX, int startY, int tileSize, int offsetX, int offsetY)
        {
            ModelName = modelName;
            Symbol = symbol;
            RenderColor = color;
            MaxHP = hp;
            CurrentHP = hp;
            BaseDamage = damage;
            MoveRange = moveRange;
            HackDifficulty = hackDiff;
            IsHacked = false;
            GridPos = (startX, startY);
            WorldPos = new Vector2(offsetX + startX * tileSize, offsetY + startY * tileSize);
        }

        public abstract void ExecuteSpecialSkill(RobotBody target);

        public virtual void TakeDamage(int amount)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public bool TryHack(int hackPower)
        {
            return hackPower >= HackDifficulty;
        }

        public void OnHacked()
        {
            IsHacked = true;
        }

        public void UpdateSmoothPosition(int tileSize, int offsetX, int offsetY, float speed = 0.2f)
        {
            Vector2 targetWorld = new Vector2(offsetX + GridPos.X * tileSize, offsetY + GridPos.Y * tileSize);
            WorldPos = Vector2.Lerp(WorldPos, targetWorld, speed);
        }
    }

    // ==========================================
    // 3. CONCRETE CLASSES (POLYMORPHISM)
    // ==========================================
    public class ScoutDrone : RobotBody
    {
        public ScoutDrone(int x, int y, int ts, int ox, int oy) 
            : base("Scout-Drone X", "D", Color.SkyBlue, hp: 45, damage: 15, moveRange: 3, hackDiff: 10, startX: x, startY: y, tileSize: ts, offsetX: ox, offsetY: oy) { }

        public override void ExecuteSpecialSkill(RobotBody target)
        {
            target.TakeDamage(BaseDamage + 10);
        }
    }

    public class CyberNinja : RobotBody
    {
        public CyberNinja(int x, int y, int ts, int ox, int oy) 
            : base("Cyber-Ninja v2", "N", Color.Purple, hp: 85, damage: 25, moveRange: 2, hackDiff: 35, startX: x, startY: y, tileSize: ts, offsetX: ox, offsetY: oy) { }

        public override void ExecuteSpecialSkill(RobotBody target)
        {
            target.TakeDamage(BaseDamage + 20);
        }
    }

    public class HeavyMecha : RobotBody
    {
        public HeavyMecha(int x, int y, int ts, int ox, int oy) 
            : base("Heavy-Mecha Titan", "M", Color.Orange, hp: 160, damage: 35, moveRange: 1, hackDiff: 60, startX: x, startY: y, tileSize: ts, offsetX: ox, offsetY: oy) { }

        public override void ExecuteSpecialSkill(RobotBody target)
        {
            target.TakeDamage(BaseDamage + 30);
        }
    }

    public class DataCore
    {
        public (int X, int Y) GridPos { get; set; }
        public int BonusHack { get; set; } = 25;

        public DataCore(int x, int y)
        {
            GridPos = (x, y);
        }
    }

    // ==========================================
    // 4. PLAYER CORE (COMPOSITION & STRATEGY PATTERN)
    // ==========================================
    public class PlayerCore
    {
        public string AIName { get; private set; }
        public int HackPower { get; private set; }
        public RobotBody? CurrentHost { get; private set; }

        public PlayerCore(string aiName, int hackPower)
        {
            AIName = aiName;
            HackPower = hackPower;
        }

        public void AddHackPower(int amount)
        {
            HackPower += amount;
        }

        public bool Possess(RobotBody targetBot)
        {
            if (targetBot.TryHack(HackPower))
            {
                if (CurrentHost != null) CurrentHost.IsHacked = false;
                CurrentHost = targetBot;
                CurrentHost.OnHacked();
                return true;
            }
            return false;
        }

        public bool MoveTo(int targetX, int targetY, int boardSize, List<RobotBody> allBots, List<DataCore> cores)
        {
            if (CurrentHost == null) return false;

            int dist = Math.Abs(targetX - CurrentHost.GridPos.X) + Math.Abs(targetY - CurrentHost.GridPos.Y);
            if (dist > CurrentHost.MoveRange || dist == 0) return false;

            foreach (var bot in allBots)
            {
                if (bot.IsAlive && bot.GridPos.X == targetX && bot.GridPos.Y == targetY) return false;
            }

            CurrentHost.GridPos = (targetX, targetY);

            for (int i = cores.Count - 1; i >= 0; i--)
            {
                if (cores[i].GridPos.X == targetX && cores[i].GridPos.Y == targetY)
                {
                    AddHackPower(cores[i].BonusHack);
                    cores.RemoveAt(i);
                }
            }

            return true;
        }

        public void Attack(RobotBody target)
        {
            if (CurrentHost == null || !CurrentHost.IsAlive) return;
            CurrentHost.ExecuteSpecialSkill(target);
        }
    }

    // ==========================================
    // 5. MAIN PROGRAM (CYBERPUNK ENGINE)
    // ==========================================
    class Program
    {
        static void SpawnParticles(List<Particle> particles, Vector2 pos, Color color, int count = 20)
        {
            Random rng = new Random();
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float speed = (float)(rng.NextDouble() * 4 + 1);
                particles.Add(new Particle
                {
                    Position = pos,
                    Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Color = color,
                    Size = (float)(rng.NextDouble() * 5 + 3),
                    LifeTime = 1.0f
                });
            }
        }

        static void Main(string[] args)
        {
            const int screenWidth = 900;
            const int screenHeight = 650;
            const int tileSize = 85;
            const int gridOffsetX = 45;
            const int gridOffsetY = 85;

            Raylib.InitWindow(screenWidth, screenHeight, "Cyberpunk: Protocol Possession (SUPER ULTRA EDITION)");
            Raylib.SetTargetFPS(60);

            PlayerCore player = new PlayerCore("NEXUS-AI", hackPower: 40);
            List<RobotBody> robots = new List<RobotBody>
            {
                new ScoutDrone(0, 0, tileSize, gridOffsetX, gridOffsetY),
                new CyberNinja(2, 2, tileSize, gridOffsetX, gridOffsetY),
                new HeavyMecha(4, 4, tileSize, gridOffsetX, gridOffsetY)
            };

            List<DataCore> cores = new List<DataCore>
            {
                new DataCore(1, 1),
                new DataCore(3, 1)
            };

            List<FloatingText> floatingTexts = new List<FloatingText>();
            List<Particle> particles = new List<Particle>();

            player.Possess(robots[0]);

            string statusLog = "Sistem Siap. Klik petak biru untuk gerak atau tombol UI untuk aksi.";
            
            Vector2 laserStart = Vector2.Zero;
            Vector2 laserEnd = Vector2.Zero;
            float laserTimer = 0.0f;
            float screenShake = 0.0f;

            bool isHackingMinigame = false;
            RobotBody? hackTarget = null;
            float hackProgress = 0f;
            float hackTimer = 5.0f;

            Random rng = new Random();

            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                foreach (var bot in robots) bot.UpdateSmoothPosition(tileSize, gridOffsetX, gridOffsetY);

                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    Particle p = particles[i];
                    p.Position += p.Velocity;
                    p.LifeTime -= dt * 2.0f;
                    p.Size = Math.Max(0, p.Size - dt * 2f);
                    particles[i] = p;
                    if (p.LifeTime <= 0) particles.RemoveAt(i);
                }

                for (int i = floatingTexts.Count - 1; i >= 0; i--)
                {
                    FloatingText ft = floatingTexts[i];
                    ft.Position.Y -= dt * 30;
                    ft.LifeTime -= dt;
                    ft.Alpha = Math.Max(0, ft.LifeTime);
                    floatingTexts[i] = ft;
                    if (ft.LifeTime <= 0) floatingTexts.RemoveAt(i);
                }

                if (laserTimer > 0) laserTimer -= dt;
                if (screenShake > 0) screenShake -= dt * 10;

                if (isHackingMinigame && hackTarget != null)
                {
                    hackTimer -= dt;
                    if (Raylib.IsKeyPressed(KeyboardKey.Space)) hackProgress += 15f;

                    if (hackProgress >= 100f)
                    {
                        player.Possess(hackTarget);
                        statusLog = $"HACK SUCCESS! AI Menguasai {hackTarget.ModelName}!";
                        SpawnParticles(particles, hackTarget.WorldPos + new Vector2(40, 40), Color.Lime, 40);
                        isHackingMinigame = false;
                    }
                    else if (hackTimer <= 0)
                    {
                        statusLog = "HACK FAILED: Koneksi ke robot musuh terputus!";
                        isHackingMinigame = false;
                    }
                }

                Vector2 shakeOffset = new Vector2((float)(rng.NextDouble() - 0.5) * screenShake * 10, (float)(rng.NextDouble() - 0.5) * screenShake * 10);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(12, 16, 24, 255));

                Raylib.DrawRectangle(0, 0, screenWidth, 65, new Color(8, 10, 16, 255));
                Raylib.DrawRectangle(0, 63, screenWidth, 2, Color.Lime);
                Raylib.DrawText("CYBERPUNK: PROTOCOL POSSESSION [PRO]", 20, 18, 20, Color.Lime);
                Raylib.DrawText($"CORE: {player.AIName}", 520, 14, 15, Color.Yellow);
                Raylib.DrawText($"HACK POWER: {player.HackPower}", 520, 36, 15, Color.SkyBlue);

                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        int rectX = (int)(gridOffsetX + (x * tileSize) + shakeOffset.X);
                        int rectY = (int)(gridOffsetY + (y * tileSize) + shakeOffset.Y);

                        bool inRange = false;
                        if (player.CurrentHost != null)
                        {
                            int dist = Math.Abs(x - player.CurrentHost.GridPos.X) + Math.Abs(y - player.CurrentHost.GridPos.Y);
                            if (dist <= player.CurrentHost.MoveRange && dist > 0) inRange = true;
                        }

                        Color tileBg = inRange ? new Color(20, 40, 70, 200) : new Color(18, 22, 32, 255);
                        Color tileBorder = inRange ? Color.SkyBlue : new Color(40, 50, 70, 255);

                        Raylib.DrawRectangle(rectX, rectY, tileSize, tileSize, tileBg);
                        Raylib.DrawRectangleLines(rectX, rectY, tileSize, tileSize, tileBorder);
                    }
                }

                foreach (var core in cores)
                {
                    Vector2 pos = new Vector2(gridOffsetX + core.GridPos.X * tileSize + 42 + shakeOffset.X, gridOffsetY + core.GridPos.Y * tileSize + 42 + shakeOffset.Y);
                    Raylib.DrawCircleV(pos, 16, Color.Gold);
                    Raylib.DrawCircleV(pos, 10, Color.Yellow);
                    Raylib.DrawText("*", (int)pos.X - 5, (int)pos.Y - 10, 24, Color.Black);
                }

                foreach (var bot in robots)
                {
                    if (!bot.IsAlive) continue;

                    Vector2 renderPos = bot.WorldPos + shakeOffset;
                    int px = (int)renderPos.X;
                    int py = (int)renderPos.Y;

                    if (player.CurrentHost == bot)
                    {
                        Raylib.DrawRectangle(px + 4, py + 4, tileSize - 8, tileSize - 8, Color.Lime);
                        Raylib.DrawRectangleLines(px + 2, py + 2, tileSize - 4, tileSize - 4, Color.Green);
                    }
                    else
                    {
                        Raylib.DrawRectangle(px + 8, py + 8, tileSize - 16, tileSize - 16, bot.RenderColor);
                    }

                    Raylib.DrawText(bot.Symbol, px + 34, py + 20, 26, Color.White);

                    Raylib.DrawRectangle(px + 10, py + 65, tileSize - 20, 8, Color.DarkGray);
                    float hpPercent = (float)bot.CurrentHP / bot.MaxHP;
                    Raylib.DrawRectangle(px + 10, py + 65, (int)((tileSize - 20) * hpPercent), 8, Color.Green);
                }

                if (laserTimer > 0)
                {
                    Raylib.DrawLineEx(laserStart + shakeOffset, laserEnd + shakeOffset, 5.0f, Color.Red);
                    Raylib.DrawLineEx(laserStart + shakeOffset, laserEnd + shakeOffset, 2.0f, Color.Yellow);
                }

                foreach (var p in particles)
                {
                    Raylib.DrawCircleV(p.Position + shakeOffset, p.Size, p.Color);
                }

                foreach (var ft in floatingTexts)
                {
                    Raylib.DrawText(ft.Text, (int)ft.Position.X, (int)ft.Position.Y, 20, Raylib.ColorAlpha(ft.Color, ft.Alpha));
                }

                int panelX = 500;
                Raylib.DrawRectangle(panelX, gridOffsetY, 360, 440, new Color(22, 28, 40, 255));
                Raylib.DrawRectangleLines(panelX, gridOffsetY, 360, 440, Color.SkyBlue);
                Raylib.DrawText("TACTICAL CONTROL PANEL", panelX + 20, gridOffsetY + 15, 18, Color.SkyBlue);

                if (player.CurrentHost != null)
                {
                    Raylib.DrawText($"ACTIVE HOST : {player.CurrentHost.ModelName}", panelX + 20, gridOffsetY + 50, 15, Color.White);
                    Raylib.DrawText($"HP STATUS   : {player.CurrentHost.CurrentHP}/{player.CurrentHost.MaxHP}", panelX + 20, gridOffsetY + 75, 15, Color.Lime);
                    Raylib.DrawText($"MOVE RANGE  : {player.CurrentHost.MoveRange} Tiles", panelX + 20, gridOffsetY + 100, 15, Color.Yellow);
                }

                Rectangle btnAttackNinja = new Rectangle(panelX + 20, gridOffsetY + 150, 320, 42);
                Raylib.DrawRectangleRec(btnAttackNinja, Color.Purple);
                Raylib.DrawText("SERANG CYBERNINJA", panelX + 75, gridOffsetY + 162, 15, Color.White);

                Rectangle btnHackNinja = new Rectangle(panelX + 20, gridOffsetY + 205, 320, 42);
                Raylib.DrawRectangleRec(btnHackNinja, Color.DarkBlue);
                Raylib.DrawText("RETAS CYBERNINJA (Diff: 35)", panelX + 50, gridOffsetY + 217, 15, Color.White);

                Rectangle btnHackMecha = new Rectangle(panelX + 20, gridOffsetY + 260, 320, 42);
                Raylib.DrawRectangleRec(btnHackMecha, Color.DarkBrown);
                Raylib.DrawText("RETAS HEAVYMECHA (Diff: 60)", panelX + 45, gridOffsetY + 272, 15, Color.White);

                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !isHackingMinigame)
                {
                    Vector2 mousePos = Raylib.GetMousePosition();

                    if (mousePos.X >= gridOffsetX && mousePos.X < gridOffsetX + (5 * tileSize) &&
                        mousePos.Y >= gridOffsetY && mousePos.Y < gridOffsetY + (5 * tileSize))
                    {
                        int clickX = (int)((mousePos.X - gridOffsetX) / tileSize);
                        int clickY = (int)((mousePos.Y - gridOffsetY) / tileSize);

                        if (player.MoveTo(clickX, clickY, 5, robots, cores))
                        {
                            statusLog = $"Robot melangkah ke petak ({clickX}, {clickY}).";
                        }
                    }

                    if (Raylib.CheckCollisionPointRec(mousePos, btnAttackNinja) && robots[1].IsAlive)
                    {
                        if (player.CurrentHost != null)
                        {
                            laserStart = player.CurrentHost.WorldPos + new Vector2(40, 40);
                            laserEnd = robots[1].WorldPos + new Vector2(40, 40);
                            laserTimer = 0.25f;
                            screenShake = 0.5f;

                            player.Attack(robots[1]);
                            SpawnParticles(particles, laserEnd, Color.Red, 25);
                            floatingTexts.Add(new FloatingText { Text = "-35 HP", Position = laserEnd - new Vector2(0, 20), Color = Color.Red, Alpha = 1.0f, LifeTime = 1.2f });
                            statusLog = "Serangan Laser dilancarkan ke CyberNinja!";
                        }
                    }
                    else if (Raylib.CheckCollisionPointRec(mousePos, btnHackNinja))
                    {
                        if (player.HackPower >= robots[1].HackDifficulty)
                        {
                            isHackingMinigame = true;
                            hackTarget = robots[1];
                            hackProgress = 0f;
                            hackTimer = 4.0f;
                        }
                        else statusLog = "Gagal: Hack Power tidak mencukupi!";
                    }
                    else if (Raylib.CheckCollisionPointRec(mousePos, btnHackMecha))
                    {
                        if (player.HackPower >= robots[2].HackDifficulty)
                        {
                            isHackingMinigame = true;
                            hackTarget = robots[2];
                            hackProgress = 0f;
                            hackTimer = 4.0f;
                        }
                        else statusLog = "Gagal: Ambil Data Core (*) emas dulu untuk menaikkan HackPower!";
                    }
                }

                if (isHackingMinigame && hackTarget != null)
                {
                    Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(0, 0, 0, 220));
                    Raylib.DrawRectangle(200, 180, 500, 280, new Color(15, 25, 40, 255));
                    Raylib.DrawRectangleLines(200, 180, 500, 280, Color.Lime);

                    Raylib.DrawText("/// OVERRIDE PROTOCOL INITIATED ///", 230, 210, 20, Color.Lime);
                    Raylib.DrawText($"Target: {hackTarget.ModelName}", 230, 245, 16, Color.White);
                    Raylib.DrawText($"TEKAN [SPASI] BERULANG KALI UNTUK OVERRIDE SYSTEM!", 230, 280, 14, Color.Yellow);

                    Raylib.DrawRectangle(230, 320, 440, 30, Color.DarkGray);
                    Raylib.DrawRectangle(230, 320, (int)(440 * (hackProgress / 100f)), 30, Color.Lime);

                    Raylib.DrawText($"PROGRESS: {(int)hackProgress}%", 390, 326, 16, Color.Black);
                    Raylib.DrawText($"Sisa Waktu: {hackTimer:F1}s", 230, 375, 16, Color.Red);
                }

                Raylib.DrawRectangle(0, screenHeight - 40, screenWidth, 40, Color.Black);
                Raylib.DrawText($"SYSTEM LOG: {statusLog}", 20, screenHeight - 28, 15, Color.Lime);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}