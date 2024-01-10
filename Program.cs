// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace CrossStitch;

public class Program {
    public const int SIZE = 16;
    public const int EDGE = 1;
    public const int GRIDX = 250;
    public const int GRIDY = 200;
    public const float scrollSpeed = 0.20f;
    public const int tileOffset = 2;
    public const string PATTERNDIR = "Patterns";
    public const string SYMBOLDIR = "Symbols";


    public const float MINZOOM = 1f;
    public const float DEFAULTZOOM = 1f; // / 3f;


    public static Grid grid = new();
    public static string stitchType = "";
    public static Color colour = Color.BLACK;
    public static Vector4 fakeColour = new(0, 0, 0, 1);

    public static Color SLIGHTLYWHITE = new(200, 200, 200, 255);

    public static RenderTexture2D minimap;

    public static bool initModal = true;

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
            Zoom = DEFAULTZOOM,
            Offset = new Vector2(0, 0)
        };

        minimap = Raylib.LoadRenderTexture(GRIDX, GRIDY);
    }

    private static void loop() {
        // inside your game loop, between BeginDrawing() and EndDrawing()
        input();
        render();
        UI();
        Raylib.EndDrawing();
    }

    private static void render() {
        var min = getMinVisible();
        var max = getMaxVisible();
        var minGridX = (int)Math.Max(0, min.X / SIZE);
        var minGridY = (int)Math.Max(0, min.Y / SIZE);
        var maxGridX = (int)Math.Min(GRIDX, max.X / SIZE + 1);
        var maxGridY = (int)Math.Min(GRIDY, max.Y / SIZE + 1);

        // draw outlines too!
        if (camera.Zoom >= MINZOOM) {
            Raylib.BeginDrawing();
            // background
            Raylib.ClearBackground(SLIGHTLYWHITE);
            Raylib.BeginMode2D(camera);
            Raylib.DrawRectangle(minGridX * SIZE, minGridY * SIZE, (maxGridX - minGridX) * SIZE,
                (maxGridY - minGridY) * SIZE, Color.RAYWHITE);

            for (int i = minGridX; i < maxGridX; i++) {
                for (int j = minGridY; j < maxGridY; j++) {
                    var sq = grid[i, j];
                    drawGraphics(sq, i * SIZE, j * SIZE, 20, sq.colour);
                }
            }

            // get min/max visible world coords
            //Raylib.DrawCircleV(min, 5, Color.RED);
            //Raylib.DrawCircleV(max, 5, Color.RED);
            for (int x = minGridX; x < maxGridX; x++) {
                Raylib.DrawLineEx(new Vector2(x * SIZE, minGridY * SIZE), new Vector2(x * SIZE, maxGridY * SIZE), EDGE,
                    Color.RED);
            }

            for (int y = minGridY; y < maxGridY; y++) {
                Raylib.DrawLineEx(new Vector2(minGridX * SIZE, y * SIZE), new Vector2(maxGridX * SIZE, y * SIZE), EDGE,
                    Color.RED);
            }

            Raylib.EndMode2D();
        }
        // too small, switch to minimap
        else {
            Raylib.BeginTextureMode(minimap);
            Raylib.ClearBackground(Color.RAYWHITE);
            for (int i = minGridX; i < maxGridX; i++) {
                for (int j = minGridY; j < maxGridY; j++) {
                    var sq = grid[i, j];
                    drawGraphicsMini(sq, i, j, 20, sq.colour);
                }
            }

            Raylib.EndTextureMode();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(SLIGHTLYWHITE);
            Raylib.BeginMode2D(camera);
            var texture = minimap.Texture;
            var src = new Rectangle(0, 0, texture.Width, -texture.Height);
            var dest = new Rectangle(0, 0, GRIDX * SIZE, GRIDY * SIZE);
            Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0.0f, Color.WHITE);
            Raylib.EndMode2D();
        }
    }

    private static void UI() {
        Raylib.DrawText(text, Raylib.GetScreenWidth() - 300, Raylib.GetScreenHeight() - 50, 20, Color.BLACK);
        rlImGui.Begin(); // starts the ImGui content mode. Make all ImGui calls after this
        // RETARDATION:
        //if ((bool) Raylib.IsWindowFullscreen() || (bool) Raylib.IsWindowMaximized())
        // {
        //   int currentMonitor = Raylib.GetCurrentMonitor();
        //   io.DisplaySize = new Vector2((float) Raylib.GetMonitorWidth(currentMonitor), (float) Raylib.GetMonitorHeight(currentMonitor));
        // }
        // else
        //   io.DisplaySize = new Vector2((float) Raylib.GetScreenWidth(), (float) Raylib.GetScreenHeight());
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        if (initModal) {
            ImGui.OpenPopup("Loaded symbols");
        }

        // centre the modal
        ImGui.SetNextWindowSize(new Vector2(400, 100));
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.8f));
        if (ImGui.BeginPopupModal("Loaded symbols", ref initModal, ImGuiWindowFlags.NoResize)) {
            ImGui.Text("Loaded 0 symbols. Do you want to open the directory?");
            ImGui.Separator();

            if (ImGui.Button("Continue")) {
                initModal = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SetItemDefaultFocus();
            ImGui.SameLine();
            if (ImGui.Button("Open directory")) {
                openDirectory();
                initModal = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

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

        if (ImGui.BeginMainMenuBar()) {
            if (ImGui.BeginMenu("File")) {
                var filename = "save.cst";
                var file = Path.Combine(PATTERNDIR, filename);
                if (ImGui.MenuItem("New...")) {
                }

                if (ImGui.MenuItem("Open...", "CTRL+O")) {
                    grid = FileIO.fromFile(file);
                }

                if (ImGui.MenuItem("Save", "CTRL+S")) {
                    FileIO.toFile(file, grid);
                }

                if (ImGui.MenuItem("Save as...", "CTRL+SHIFT+S")) {
                }

                if (ImGui.MenuItem("Exit", "ALT+F4")) {
                    Environment.Exit(0);
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("test2")) {
                if (ImGui.MenuItem("Undo", "CTRL+Z")) {
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        Raylib.DrawFPS(0, 0);

        rlImGui.End(); // ends the ImGui content mode. Make all ImGui calls before this
    }

    private static void openDirectory() {
        var cwd = Directory.GetCurrentDirectory();
        if (OperatingSystem.IsLinux()) {
            Process.Start("xdg-open", $"{cwd}");
        }
        else if (OperatingSystem.IsWindows()) {
            Process.Start($"{cwd}");
        }
        else {
            throw new NotSupportedException("Unable to open file manager.");
        }
    }

    private static Vector2 getMinVisible() {
        return Raylib.GetScreenToWorld2D(new Vector2(0, 0), camera);
    }

    private static Vector2 getMaxVisible() {
        return Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight()), camera);
    }

    private static void drawGraphics(Square sq, int x, int y, int fontsize, Color color) {
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
            Raylib.DrawTexture(unknown, x + tileOffset, y + tileOffset, color);
        }
    }

    private static void drawGraphicsMini(Square sq, int x, int y, int fontsize, Color color) {
        if (sq.type == "clear") {
            return;
        }

        Raylib.DrawPixel(x, y, color);
    }

    private static void input() {
        // if imgui, don't handle anything
        var io = ImGui.GetIO();

        if (io.WantCaptureKeyboard) {
            return;
        }

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

        if (io.WantCaptureMouse) {
            return;
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
        return pos.X is > 0 and < GRIDX * SIZE &&
               pos.Y is > 0 and < GRIDY * SIZE;
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
    public Color colour = Color.RAYWHITE;
}

public class Grid {
    public int width = Program.GRIDX;
    public int height = Program.GRIDY;

    public Square[,] squares;

    public Grid() {
        squares = new Square[width, height];
        for (var i = 0; i < width; i++) {
            for (var j = 0; j < height; j++) {
                squares[i, j] = new Square();
            }
        }
    }

    public Square this[int x, int y] {
        get => squares[x, y];
        set => squares[x, y] = value;
    }
}