using System.Numerics;
using static Raylib_cs.Raylib;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Font = Raylib_cs.Font;
using Rectangle = Raylib_cs.Rectangle;

namespace Background_Desktop_Player;

class Program
{
  public const int WINDOW_WIDTH = 800; 
  public const int WINDOW_HEIGHT = 600; 
  
  public static NotifyIcon Global_Tray = null!;
  public static bool Global_ShouldClose;
  private static bool hidden_SmallMode; 
  public static bool Global_SmallMode
  {
    get => hidden_SmallMode;
    set
    {
      if (Global_SmallMode) // going to selector
      {
        SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);
        ClearWindowState(ConfigFlags.TopmostWindow
                         | ConfigFlags.UndecoratedWindow
                         );
        WindowsHelper.SizeUpdate();

        int currentMonitor = GetCurrentMonitor();
        Vector2 currentMonitorPosition = GetMonitorPosition(currentMonitor);
        int currentMonitorSizeX = GetMonitorWidth(currentMonitor);
        int currentMonitorSizeY = GetMonitorHeight(currentMonitor);
        SetWindowPosition(
          (int)(currentMonitorPosition.X + (currentMonitorSizeX / 2 - Global_WindowSize.X / 2)), 
          (int)(currentMonitorPosition.Y + (currentMonitorSizeY / 2 - Global_WindowSize.Y / 2)));
      }
      else // going to small mode
      {
        Vector2 textSize = MeasureTextEx(Global_Font, Global_CurrentText, Global_Fontsize, 1);
        SetWindowSize(textSize.X <= 400 ? (int)textSize.X + Global_TextPadding*2 : 400, (int)textSize.Y + Global_TextPadding*2);
        SetWindowState(ConfigFlags.TopmostWindow
                       | ConfigFlags.UndecoratedWindow
                       );
        WindowsHelper.SizeUpdate();
        
        int currentMonitor = GetCurrentMonitor();
        Vector2 currentMonitorPosition = GetMonitorPosition(currentMonitor);
        SetWindowPosition(
          (int)(currentMonitorPosition.X + Global_StartSmallModePos.X), 
          (int)(currentMonitorPosition.Y + Global_StartSmallModePos.Y));
      }

      WindowsHelper.HideFromTaskbar(hidden_SmallMode);
      hidden_SmallMode = value;
    }
  }
  public static Vector2 Global_WindowSize;
  public static Vector2 Global_StartSmallModePos = new(32);
  public static string Global_CurrentText = "Test Music Text - ItsMeFtk";
  public static Font Global_Font;
  public static float Global_Fontsize = 18;
  public static int Global_TextPadding = 4;
  public static Music? Global_CurrentMusic;
  public static Rectangle Global_Rec;


  static void Setup()
  {
    SetConfigFlags(ConfigFlags.AlwaysRunWindow | ConfigFlags.TransparentWindow | ConfigFlags.VSyncHint);
    InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Background Desktop Player");
    WindowsHelper.SizeUpdate();
    InitAudioDevice();
    SetWindowIcon(GenImageColor(1, 1, Color.Blank));
    WindowsHelper.SetupSpecialWindowsStuff();
    Global_Tray = WindowsHelper.SetupTray();
  }

  static void Begin()
  {
    Global_Font = GetFontDefault();
  }

  static void Update()
  {
    if ((IsKeyDown(KeyboardKey.LeftAlt) && IsKeyPressed(KeyboardKey.F4)) ||
        (WindowsHelper.IsKeyPressedGlobal(WindowsHelper.VK_SHIFT) && WindowsHelper.IsKeyPressedGlobal(WindowsHelper.VK_ESCAPE)))
      Global_ShouldClose = true;
    if (!Global_SmallMode && IsKeyPressed(KeyboardKey.Escape))
      Global_SmallMode = true;
    if (Global_SmallMode && WindowsHelper.CheckDoubleClick(MouseButton.Left))
      Global_SmallMode = false;
    if (IsMouseButtonPressed(MouseButton.Middle))
      Global_ShouldClose = true;
    
    WindowsHelper.TransparencyUpdate();
    WindowsHelper.DraggingUpdate();
    
    if (Global_CurrentMusic is not null)
      UpdateMusicStream(Global_CurrentMusic.Value);
    WindowsHelper.ApplyCursor();
  }

  static void Render()
  {
    BeginDrawing();
    ClearBackground(Color.Blank);
    
    float lineThick;
    switch (Global_SmallMode)
    { 
      case false:
        Color bg_color = ColorHelper.IsDarkMode ? new Color(20, 20, 20, 255) : new Color(210, 210, 210, 255);
        lineThick = 1;
        
        DrawRectangleRec(Global_Rec, bg_color);
        
        Vector2 lines_pos = Global_Rec.Position;
        DrawLineEx(lines_pos, lines_pos + Vector2.UnitX * Global_Rec.Width, lineThick, ColorHelper.FinalThemeColor);
        lines_pos += Vector2.UnitX * 310;
        DrawLineEx(lines_pos, lines_pos + Vector2.UnitY * Global_Rec.Height, lineThick, ColorHelper.FinalThemeColor);
        
        break;
      case true:
        DrawRectangleRounded(Global_Rec, 0.2f, 3, new Color(72, 72, 72, 187));
        lineThick = 2;
        DrawRectangleRoundedLinesEx(
          new Rectangle(Global_Rec.X + lineThick, Global_Rec.Y + lineThick, Global_Rec.Width - lineThick*2, Global_Rec.Height - lineThick*2), 
          0.1f, 3, lineThick, ColorHelper.AccentColor);
        DrawTextEx(Global_Font, Global_CurrentText, new Vector2(Global_Rec.X + Global_TextPadding, Global_Rec.Y + Global_TextPadding), Global_Fontsize, 1, Color.White);
        break;
    }
      
    EndDrawing();
  }
  
  [STAThread]
  static void Main()
  {
    Setup();
    Begin();

    while (!Global_ShouldClose)
    {
      Update();
      Render();
    }
    
    CloseAudioDevice();
    CloseWindow();
  }
}