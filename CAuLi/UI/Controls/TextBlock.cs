using System;
using System.Collections.Generic;

namespace CAuLi.UI.Controls
{
  class TextBlock : ControlBase
  {
    private string _text = string.Empty;
    private int _firstLine = 0;

    private List<string> _lines = [];

    public override void Draw(Screen screen)
    {
      NeedsRedraw = false;
      _lines = Utility.String.SplitStringInLines(_text, Width);

      int width = Width;
      if (VScrollbarVisible)
        width -= 1;

      int row = Y;
      for (int i = _firstLine; i<_firstLine+Height; i++) {
        if (i >= _lines.Count)
          screen.WriteString(X, row, width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(width));
        else
          screen.WriteString(X, row, width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, _lines[i].Trim().PadRight(width));
        row++;
      }

      // Vertical scrollbar:
      if (VScrollbarVisible) {
        int chars =  Y + (int)Math.Round((double)Height / 100.0 * ((double)Height / (double)_lines.Count * 100.0));
        int start = Y + (int)Math.Round((double)Height / 100.0 * ((double)_firstLine / (double)_lines.Count * 100.0));
        if (start > Y + Height - chars - 1)
          start = Y + Height - chars - 1;
        int end = start + chars;

        for (int i = Y; i < Y + Height; i++) {
          if (i >= start && i <= end)
            screen.WriteString(Width - 1, i, 1, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "▓");
          else
            screen.WriteString(Width - 1, i, 1, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "░");
        }
      }
    }

    public string Text {
      get { return _text; }
      set
      {
        _text = value;
      }
    }

    public int FirstLine
    {
      get { return _firstLine; }
      set { _firstLine = value; }
    }

    public bool VScrollbarVisible
    {
      get {
        return _lines != null && _lines.Count > Height;
      }
    }

    public override bool KeyPress(ConsoleKeyInfo keyPress)
    {
      base.KeyPress(keyPress);

      switch (keyPress.Key) {
        case ConsoleKey.UpArrow:
          if (_firstLine > 0) {
            _firstLine--;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.DownArrow:
          if (_firstLine + 1 < _lines.Count) {
            _firstLine++;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.PageUp:
          if (_firstLine > 0) {
            _firstLine -= 10;
            if (_firstLine < 0)
              _firstLine = 0;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.PageDown:
          if (_firstLine + 10 < _lines.Count) {
            _firstLine += 10;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.Home:
          _firstLine = 0;
          NeedsRedraw = true;
          return true;
        case ConsoleKey.End:
          _firstLine = _lines.Count - Height;
          if (_firstLine < 0)
            _firstLine = 0;
          NeedsRedraw = true;
          return true;
      }
      return false;
    }
  }
}
