using Microsoft.Win32;
using Raylib_cs;
using Color = Raylib_cs.Color;

namespace Background_Desktop_Player;

public static class ColorHelper
{
  public static bool IsDarkMode => IsDarkTheme();
  public static Color InactiveColor => IsDarkMode ? new Color(66, 66, 66, 255) : new Color(189, 189, 189, 255);
  public static Color AccentColor = RlColorFromWinArgb(GetAccentColor());
  public static Color FinalThemeColor => Raylib.IsWindowFocused() ? AccentColor : InactiveColor;
  
  public static bool IsDarkTheme()
  {
    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
    if (key?.GetValue("AppsUseLightTheme") is int value)
      return value == 0;
    return false;
  }

  public static int GetAccentColor()
  {
    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
    if (key?.GetValue("ColorizationColor") is int color)
      return color;
    return unchecked((int)0xFF0078D7);
  }
  
  static Color RlColorFromWinArgb(int argbColor)
  {
    byte a = (byte)((argbColor >> 24) & 0xFF);
    byte r = (byte)((argbColor >> 16) & 0xFF);
    byte g = (byte)((argbColor >> 8)  & 0xFF);
    byte b = (byte)(argbColor & 0xFF);

    return new Color(r, g, b, a);
  }
}