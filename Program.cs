// See https://aka.ms/new-console-template for more information

using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class Program {
    public const int SIZE = 24;
    public const int GRIDX = 250;
    public const int GRIDY = 200;
    public const float scrollSpeed = 0.20f;
    public static Square[,] grid = new Square[GRIDX, GRIDY];
    public static string stitchType = "";
    public static Color colour = Color.BLACK;
    public static Vector4 fakeColour = new(0, 0, 0, 1);

    public static Color SLIGHTLYWHITE = new Color(200, 200, 200, 255);

    public static RenderTexture2D minimap;

    public static int scaleX;
    public static int scaleY;

    public static Camera2D camera;
    public static Texture2D unknown;

    public static string text = "Press N to open box";

    public static void Main(string[] args) {
        setup();
        while (!Raylib.WindowShouldClose()) {
            loop();
        }

        shutdown();
    }

    private static void setup() {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_VSYNC_HINT |
                              ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(1280, 800, "Stitch thing");
        Raylib.SetTargetFPS(60);
        rlImGui.Setup();
        unknown = Raylib.LoadTexture("unknown.png");
        camera = new Camera2D {
            Zoom = 0.2f,
            Offset = new Vector2(0, 0)
        };
        for (var i = 0; i < grid.GetLength(0); i++) {
            for (var j = 0; j < grid.GetLength(1); j++) {
                grid[i, j] = new Square();
            }
        }

        minimap = Raylib.LoadRenderTexture(GRIDX, GRIDY);
        scaleX = Raylib.GetRenderWidth() / GRIDX;
        scaleY = Raylib.GetRenderHeight() / GRIDY;
    }

    private static void loop() {
        // inside your game loop, between BeginDrawing() and EndDrawing()
        input();
        var min = getMinVisible();
        var max = getMaxVisible();
        var minGridX = (int)Math.Max(0, min.X / SIZE);
        var minGridY = (int)Math.Max(0, min.Y / SIZE);
        var maxGridX = (int)Math.Min(GRIDX, max.X / SIZE);
        var maxGridY = (int)Math.Min(GRIDY, max.Y / SIZE);

        // draw outlines too!
        if (camera.Zoom > 1f) {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(210, 210, 210, 255));
            Raylib.BeginMode2D(camera);
            for (int i = minGridX; i < maxGridX; i++) {
                for (int j = minGridY; j < maxGridY; j++) {
                    var sq = grid[i, j];
                    drawGraphics(sq, i * SIZE, j * SIZE, 20, sq.colour);
                }
            }
            // get min/max visible world coords

            Raylib.DrawCircleV(min, 5, Color.RED);
            Raylib.DrawCircleV(max, 5, Color.RED);
            for (int x = minGridX; x < maxGridX; x++) {
                Raylib.DrawLineEx(new Vector2(x * SIZE, minGridY * SIZE), new Vector2(x * SIZE, maxGridY * SIZE), 2,
                    Color.RED);
            }

            for (int y = minGridY; y < maxGridY; y++) {
                Raylib.DrawLineEx(new Vector2(minGridX * SIZE, y * SIZE), new Vector2(maxGridX * SIZE, y * SIZE), 2,
                    Color.RED);
            }

            Raylib.EndMode2D();
        }
        // too small, switch to minimap
        else {
            Raylib.BeginTextureMode(minimap);
            Raylib.ClearBackground(new Color(210, 210, 210, 255));
            for (int i = minGridX; i < maxGridX; i++) {
                for (int j = minGridY; j < maxGridY; j++) {
                    var sq = grid[i, j];
                    drawGraphicsMini(sq, i, j, 20, sq.colour);
                }
            }

            Raylib.EndTextureMode();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(210, 210, 210, 255));
            Raylib.BeginMode2D(camera);
            var texture = minimap.Texture;
            var src = new Rectangle(0, 0, texture.Width, -texture.Height);
            var dest = new Rectangle(0, 0, GRIDX * SIZE, GRIDY * SIZE);
            Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0.0f, Color.WHITE);
            Raylib.EndMode2D();
        }
        Raylib.DrawText(text, Raylib.GetScreenWidth() - 300, Raylib.GetScreenHeight() - 50, 20, Color.BLACK);
        rlImGui.Begin(); // starts the ImGui content mode. Make all ImGui calls after this
        //ImGui.ShowDemoWindow();
        if (ImGui.RadioButton("x", stitchType == "x")) {
            stitchType = "x";
        }

        if (ImGui.RadioButton("plus", stitchType == "plus")) {
            stitchType = "plus";
        }

        if (ImGui.RadioButton("dot", stitchType == "dot")) {
            stitchType = "dot";
        }

        if (ImGui.RadioButton("cross1", stitchType == "cross1")) {
            stitchType = "cross1";
        }

        if (ImGui.RadioButton("clear", stitchType == "clear")) {
            stitchType = "clear";
        }

        if (ImGui.ColorPicker4("colour", ref fakeColour, ImGuiColorEditFlags.AlphaBar)) {
            colour = toColor(fakeColour);
        }

        Raylib.DrawFPS(0, 0);

        rlImGui.End(); // ends the ImGui content mode. Make all ImGui calls before this

        Raylib.EndDrawing();
    }

    private static Vector2 getMinVisible() {
        return Raylib.GetScreenToWorld2D(new Vector2(0, 0), camera);
    }

    private static Vector2 getMaxVisible() {
        return Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight()), camera);
    }

    private static void drawGraphics(Square sq, int x, int y, int fontsize, Color color) {
        const int offset = 4;
        if (sq.type == "x") {
            // x cross
            Raylib.DrawLineEx(new Vector2(x, y), new Vector2(x + SIZE, y + SIZE), 2, color);
            Raylib.DrawLineEx(new Vector2(x, y + SIZE), new Vector2(x + SIZE, y), 2, color);
        }
        else if (sq.type == "dot") {
            var centre = new Vector2(x + SIZE / 2f, y + SIZE / 2f);
            Raylib.DrawCircle((int)centre.X, (int)centre.Y, 6, sq.colour);
        }
        else if (sq.type == "plus") {
            var centre = new Vector2(x + SIZE / 2f, y + SIZE / 2f);
            Raylib.DrawLineEx(new Vector2(centre.X, y), new Vector2(centre.X, y + SIZE), 2, color);
            Raylib.DrawLineEx(new Vector2(x, centre.Y), new Vector2(x + SIZE, centre.Y), 2, color);
        }
        else if (sq.type == "clear") {
            return;
        }
        else {
            Raylib.DrawTexture(unknown, x + offset, y + offset, color);
        }
    }

    private static void drawGraphicsMini(Square sq, int x, int y, int fontsize, Color color) {
        Raylib.DrawPixel(x, y, color);
    }

    private static void input() {
        if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {
            camera.Target += new Vector2(2, 0);
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {
            camera.Target -= new Vector2(2, 0);
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {
            camera.Target -= new Vector2(0, 2);
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {
            camera.Target += new Vector2(0, 2);
        }

        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT)) {
            var d = Raylib.GetMouseDelta();
            camera.Target += d;
        }

        var pos = Raylib.GetMousePosition();
        pos = Raylib.GetScreenToWorld2D(pos, camera);
        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && isInGrid(pos)) {
            clickGrid(pos);
        }

        camera.Zoom += Raylib.GetMouseWheelMove() * scrollSpeed;
        camera.Zoom = Math.Clamp(camera.Zoom, 0.05f, 20f);
    }

    private static bool isInGrid(Vector2 pos) {
        return pos.X > 0 && pos.X < GRIDX * SIZE &&
               pos.Y > 0 && pos.Y < GRIDY * SIZE;
    }

    private static void clickGrid(Vector2 pos) {
        var gridX = (int)(pos.X / SIZE);
        var gridY = (int)(pos.Y / SIZE);

        grid[gridX, gridY].type = stitchType;
        grid[gridX, gridY].colour = colour;
    }

    private static void shutdown() {
        // after your game loop is over, before you close the window
        Raylib.UnloadTexture(unknown);
        rlImGui.Shutdown(); // cleans up ImGui#
        Raylib.CloseWindow();
    }

    public static Color toColor(Vector4 vec) {
        return new Color((byte)(vec.X * 255), (byte)(vec.Y * 255), (byte)(vec.Z * 255), (byte)(vec.W * 255));
    }

    public static Vector3 toVec(Color color) {
        return new Vector3(color.R, color.G, color.B);
    }
}

public class Square {
    public string type = "";
    public Color colour = Program.SLIGHTLYWHITE;
}