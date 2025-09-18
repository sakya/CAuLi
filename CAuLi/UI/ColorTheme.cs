using System;
using System.Xml.Serialization;

namespace CAuLi.UI
{
  public class ColorTheme
  {
    public ColorTheme()
    {
      // Default color theme
      OverAccentColor = ConsoleColor.White;
      AccentColor = ConsoleColor.DarkGreen;
      Background = ConsoleColor.Black;
      Foreground = ConsoleColor.Gray;
      ForegroundHighlight = ConsoleColor.White;
      BackgroundTitle = ConsoleColor.DarkBlue;
      ForegroundTitle = ConsoleColor.White;

      ReverseBackground = ConsoleColor.Gray;
      ReverseForeground = ConsoleColor.Black;
    }

    [XmlAttribute]
    public string Name
    {
      get;
      set;
    }

    public static ColorTheme Instance
    {
      get;
      set;
    }

    public ConsoleColor AccentColor
    {
      get;
      set;
    }

    public ConsoleColor OverAccentColor
    {
      get;
      set;
    }

    public ConsoleColor Background
    {
      get;
      set;
    }

    public ConsoleColor ReverseBackground
    {
      get;
      set;
    }

    public ConsoleColor Foreground
    {
      get;
      set;
    }
    public ConsoleColor ReverseForeground
    {
      get;
      set;
    }

    public ConsoleColor ForegroundHighlight
    {
      get;
      set;
    }

    public ConsoleColor BackgroundTitle
    {
      get;
      set;
    }

    public ConsoleColor ForegroundTitle
    {
      get;
      set;
    }

  }
}
