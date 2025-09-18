using System;
using System.Collections.Generic;
using Utility;

namespace CAuLi.UI.Pages
{
  class HelpPage : PageBase
  {
    Controls.TextBlock m_TextBlock = null;
    public HelpPage(Screen screen) :
      base(screen)
    {

    }

    public override void Init(object parameter, GenericConfig saveState)
    {
      base.Init(parameter, saveState);

      string text = VariableExpansion(parameter as string);

      m_TextBlock = new Controls.TextBlock()
      {
        X = 0,
        Y = 2,
        Text = text,
        HasFocus = true
      };

      m_Screen.Controls.Add(m_TextBlock);

      OnSizeChanged(m_Screen, m_Screen.Width, m_Screen.Height);

      DrawTitle();
    }

    private void DrawTitle()
    {
      // Title
      m_Screen.WriteString(0, 1, m_Screen.Width, ColorTheme.Instance.BackgroundTitle, ColorTheme.Instance.ForegroundTitle, "Help".PadRight(m_Screen.Width));
    }

    protected override void OnSizeChanged(Screen sender, int width, int height)
    {
      base.OnSizeChanged(sender, width, height);
      if (!m_Screen.ValidSize)
        return;

      m_TextBlock.Width = m_Screen.Width;
      m_TextBlock.Height = m_Screen.Height - m_TextBlock.Y - 1;

      m_Screen.Clear();
      m_Screen.Draw();
      DrawTitle();
    }

    public override bool KeyPress(ConsoleKeyInfo keyPress)
    {
      if (base.KeyPress(keyPress))
        return true;

      if (keyPress.Key == ConsoleKey.Escape) {
        // ESCAPE: go back
        ReturnValue = new PageReturnValue();
        return true;
      }
      return false;
    }

    private string VariableExpansion(string help)
    {
      string res = help;

      Dictionary<string, string> vars = new Dictionary<string, string>();
      vars["%settings.KeyCodePlayer%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodePlayer);
      vars["%settings.KeyCodeLyrics%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeLyrics);
      vars["%settings.KeyCodeQuit%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeQuit);

      vars["%settings.KeyCodePanRight%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodePanRight);
      vars["%settings.KeyCodePanLeft%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodePanLeft);
      vars["%settings.KeyCodeRepeat%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeRepeat);
      vars["%settings.KeyCodeToggleEq%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeToggleEq);
      vars["%settings.KeyCodeNextEq%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeNextEq);
      vars["%settings.KeyCodeShuffle%"] = string.Format("{0}", (char)AppSettings.Instance.KeyCodeShuffle);

      foreach (KeyValuePair<string, string> v in vars)
        res = res.Replace(v.Key, v.Value);

      return res;
    } // VariableExpansion
  }
}
