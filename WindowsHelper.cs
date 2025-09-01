using System.Numerics;
using System.Runtime.InteropServices;
using static Raylib_cs.Raylib;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Background_Desktop_Player;

public static class WindowsHelper
{
  const int GWL_EXSTYLE      = -20;
  const int WS_EX_TOOLWINDOW = 0x00000080;
  const int WS_EX_APPWINDOW  = 0x00040000;
  const int GWL_WNDPROC      = -4;
  const int WM_CLOSE         = 0x0010;
  const int WM_SYSCOMMAND = 0x0112;
  const int SC_MINIMIZE = 0xF020;
  
  const int SW_HIDE = 0;
  const int SW_SHOW = 5;
  const int SWP_NOMOVE = 0x0002;
  const int SWP_NOSIZE = 0x0001;
  const int SWP_NOZORDER = 0x0004;
  const int SWP_FRAMECHANGED = 0x0020;

  public const int VK_ALT  = 0x12;
  public const int VK_BACK = 0x08;

  private static MouseCursor hidden_latestCursor = MouseCursor.Default;
  public static MouseCursor LatestCursor
  {
    get => hidden_latestCursor;
    set
    {
      hidden_latestCursor = value;
      CursorChanged = true;
    }
  }
  public static bool CursorChanged;
  
  private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
  private static WndProc _newProc;
  private static IntPtr _oldProc;

  [DllImport("user32.dll")] 
  static extern IntPtr GetActiveWindow();
  [DllImport("user32.dll")] 
  static extern int GetWindowLong(IntPtr hWnd, int nIndex);
  [DllImport("user32.dll")] 
  static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
  [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] 
  static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProc newProc);
  [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
  static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] 
  static extern short GetAsyncKeyState(int vKey);
  [DllImport("user32.dll")]
  static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
  [DllImport("user32.dll")] 
  static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  

  public static bool IsKeyPressedGlobal(int key) => 
    (GetAsyncKeyState(key) & 0x8000) != 0;

  public static void HideFromTaskbar(bool value)
  {
    IntPtr hwnd = GetActiveWindow(); 
    
    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
    if (Program.Global_SmallMode)
      exStyle = (exStyle & ~WS_EX_TOOLWINDOW) | WS_EX_APPWINDOW;
    else 
      exStyle = (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
    
    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle); 
  }
  
  public static void SetupSpecialWindowsStuff()
  {
    IntPtr hwnd = GetActiveWindow();
    
    _newProc = (hWnd, msg, wParam, lParam) =>
    {
      switch (msg)
      {
        case WM_CLOSE:
          Program.Global_ShouldClose = true;
          return IntPtr.Zero;
        case WM_SYSCOMMAND when (wParam.ToInt32() & 0xFFF0) == SC_MINIMIZE:
          Program.Global_SmallMode = true;
          return IntPtr.Zero;
        default:
          return CallWindowProc(_oldProc, hWnd, msg, wParam, lParam);
      }
    };
    
    _oldProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, _newProc);
  }
  
  public static NotifyIcon SetupTray()
  {
    NotifyIcon tray = new();
    tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    tray.Visible = true;
    tray.ContextMenuStrip = new();
    
    tray.ContextMenuStrip.Items.Add("Show/Hide", null, (_, _) => Program.Global_SmallMode = !Program.Global_SmallMode);
    tray.ContextMenuStrip.Items.Add("Close", null, (_, _) => Program.Global_ShouldClose = true);

    return tray;
  }

  private static bool _dragging;
  private static Vector2 _offset = Vector2.Zero;
  private static Point _mouseGlobalPos;
  public static void DraggingUpdate()
  {
    if (!Program.Global_SmallMode) return;

    if (IsMouseButtonPressed(MouseButton.Left))
    {
      _offset = GetMousePosition();
      _dragging = true;
    }
    if (IsMouseButtonReleased(MouseButton.Left))
      _dragging = false;

    if (!_dragging)
    {
      hidden_latestCursor = MouseCursor.Default;
      return;
    }
    _mouseGlobalPos = Cursor.Position;
    SetWindowPosition(_mouseGlobalPos.X - (int)_offset.X, _mouseGlobalPos.Y - (int)_offset.Y);
    LatestCursor = MouseCursor.ResizeAll;
  }

  public static void SizeUpdate()
  {
    Program.Global_WindowSize.X = GetScreenWidth();
    Program.Global_WindowSize.Y = GetScreenHeight();
    Program.Global_Rec = new Rectangle(Vector2.Zero, Program.Global_WindowSize);
  }

  public static void TransparencyUpdate()
  {
    if (!Program.Global_SmallMode)
    {
      SetWindowOpacity(1);
      return;
    }
    
    _mouseGlobalPos = Cursor.Position;
    const int loose = 14;
    int currentMonitor = GetCurrentMonitor();
    Vector2 currentMonitorPosition = GetMonitorPosition(currentMonitor);
    Vector2 currentWindowPosition = GetWindowPosition();
    bool verdict = CheckCollisionPointRec(
      new Vector2(_mouseGlobalPos.X, _mouseGlobalPos.Y),
      new Rectangle(
        currentMonitorPosition.X + currentWindowPosition.X - loose,
        currentMonitorPosition.Y + currentWindowPosition.Y - loose,
        Program.Global_Rec.Size + new Vector2(loose) * 2)) || IsMouseButtonDown(MouseButton.Left);
    
    SetWindowOpacity(verdict ? 1 : 0.2f);
    if (verdict)
      LatestCursor = MouseCursor.PointingHand;
  }

  public static void ApplyCursor()
  {
    if (!CursorChanged) return;
    SetMouseCursor(LatestCursor);
    CursorChanged = false;
  }
  
  private static double _lastClickTime;
  private static Vector2 _lastClickPos;
  public static bool CheckDoubleClick(MouseButton button)
  {
    const double maxInterval = 0.4;
    const float maxDistance = 5f;

    if (Raylib.IsMouseButtonPressed(button))
    {
      double now = Raylib.GetTime();
      Vector2 pos = Raylib.GetMousePosition();

      if (now - _lastClickTime < maxInterval &&
          (pos - _lastClickPos).Length() < maxDistance)
      {
        _lastClickTime = now;
        _lastClickPos = pos;
        return true;
      }

      _lastClickTime = now;
      _lastClickPos = pos;
    }

    return false;
  }
}