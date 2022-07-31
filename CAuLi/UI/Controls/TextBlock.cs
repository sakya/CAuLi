using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAuLi.UI.Controls
{
  class TextBlock : ControlBase
  {
    string m_Text = string.Empty;
    int m_FirstLine = 0;

    List<string> m_Lines = new List<string>();
    public override void Draw(Screen screen)
    {
      NeedsRedraw = false;
      m_Lines = Utility.String.SplitStringInLines(m_Text, Width);

      int width = Width;
      if (VScrollbarVisible)
        width -= 1;

      int row = Y;
      for (int i = m_FirstLine; i<m_FirstLine+Height; i++) {
        if (i >= m_Lines.Count)
          screen.WriteString(X, row, width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(width));
        else
          screen.WriteString(X, row, width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, m_Lines[i].Trim().PadRight(width));
        row++;
      }

      // Vertical scrollbar:
      if (VScrollbarVisible) {
        int chars =  Y + (int)Math.Round((double)Height / 100.0 * ((double)Height / (double)m_Lines.Count * 100.0));
        int start = Y + (int)Math.Round((double)Height / 100.0 * ((double)m_FirstLine / (double)m_Lines.Count * 100.0));
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
      get { return m_Text; }
      set
      {
        m_Text = value;        
      }
    }

    public int FirstLine
    {
      get { return m_FirstLine; }
      set { m_FirstLine = value; }
    }

    public bool VScrollbarVisible
    {
      get {
        return m_Lines != null && m_Lines.Count > Height;
      }
    }

    public override bool KeyPress(ConsoleKeyInfo keyPress)
    {
      base.KeyPress(keyPress);

      switch (keyPress.Key) {
        case ConsoleKey.UpArrow:
          if (m_FirstLine > 0) {
            m_FirstLine--;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.DownArrow:
          if (m_FirstLine + 1 < m_Lines.Count) { 
            m_FirstLine++;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.PageUp:
          if (m_FirstLine > 0) {
            m_FirstLine -= 10;
            if (m_FirstLine < 0)
              m_FirstLine = 0;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.PageDown:
          if (m_FirstLine + 10 < m_Lines.Count) {
            m_FirstLine += 10;
            NeedsRedraw = true;
            return true;
          }
          break;
        case ConsoleKey.Home:
          m_FirstLine = 0;
          NeedsRedraw = true;
          return true;
        case ConsoleKey.End:
          m_FirstLine = m_Lines.Count - Height;
          if (m_FirstLine < 0)
            m_FirstLine = 0;
          NeedsRedraw = true;
          return true;
      }
      return false;
    }
  }
}
