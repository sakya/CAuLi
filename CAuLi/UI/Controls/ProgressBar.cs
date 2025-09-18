using System;
using System.Text;

namespace CAuLi.UI.Controls
{
  class ProgressBar : ControlBase
  {
    double m_Value = 0;
    int m_Chars = 0;

    public ProgressBar() :
      base()
    {
      Focusable = false;
    }

    public double Value {
      get { return m_Value; }
      set
      {
        if (m_Value != value) {
          m_Value = value;

          double perc = 0;
          if (Maximum - Minimum > 0)
            perc = (Value - Minimum) / (Maximum - Minimum) * 100.0;
          int chars = (int)Math.Round(perc / (100.0 / (double)Width));
          if (m_Chars != chars) {
            m_Chars = chars;
            NeedsRedraw = true;
          }
        }
      }
    }

    public double Minimum { get; set; }
    public double Maximum { get; set; }

    public override void Draw(Screen screen)
    {
      NeedsRedraw = false;
      double perc = 0;
      if (Maximum - Minimum > 0)
        perc = (Value - Minimum) / (Maximum - Minimum) * 100.0;

      StringBuilder sb = new StringBuilder(Width);
      sb.Append(new string('░', Width));

      int chars = (int)Math.Round(perc / (100.0 / (double)Width));
      for (int i = 0; i < chars; i++) {
        if (i < sb.Length)
          sb[i] = '▓';
      }
      screen.WriteString(X, Y, Width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, sb.ToString());
    }
  }
}
