using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace CAuLi.UI.Pages
{
  class PageReturnValue
  {
    public Type NavigateTo { get; set; }
    public object Parameter { get; set; }
  }

  abstract class PageBase : IDisposable
  {
    protected Screen m_Screen = null;

    public PageBase(Screen screen)
    {
      if (screen == null)
        throw new ArgumentNullException("screen");

      m_Screen = screen;
      m_Screen.SizeChanged += OnSizeChanged;
    }

    public string HelpText
    {
      get;
      set;
    }

    public PageReturnValue ReturnValue
    {
      get;
      protected set;
    }

    public virtual void Dispose()
    {
      m_Screen.SizeChanged -= OnSizeChanged;
    }

    public virtual void Init(object parameter, GenericConfig saveState)
    {
      HelpText = LoadHelp();

      if (m_Screen.Controls == null)
        m_Screen.Controls = new List<Controls.ControlBase>();
      else
        m_Screen.Controls.Clear();
      m_Screen.Clear();
      m_Screen.Draw();
    }

    public virtual PageReturnValue Run()
    {
      while (ReturnValue == null) {
        Thread.Sleep(100);
      }

      return ReturnValue;
    }

    public virtual bool KeyPress(ConsoleKeyInfo keyPress)
    {
      if (keyPress.Key == ConsoleKey.Tab) {
        // TAB changes focus:
        if ((keyPress.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
          m_Screen.FocusPreviousControl();
        else
          m_Screen.FocusNextControl();

        foreach (UI.Controls.ControlBase c in m_Screen.Controls) {
          if (c.NeedsRedraw)
            c.Draw(m_Screen);
        }
        return true;
      } else if (keyPress.Key == ConsoleKey.F1) {
        // F1: Help
        if (!string.IsNullOrEmpty(HelpText)) {
          ReturnValue = new PageReturnValue() { NavigateTo = typeof(HelpPage), Parameter = HelpText };
          return true;
        }
      }
      return false;
    }

    protected virtual void OnSizeChanged(UI.Screen sender, int width, int height)
    {
      if (m_Screen.Width < m_Screen.MinimumWidth || m_Screen.Height < m_Screen.MinimumHeight) {
        m_Screen.Clear();
        m_Screen.WriteString(0, 0, m_Screen.Width, UI.ColorTheme.Instance.Background, ConsoleColor.Yellow, 
                             string.Format("Window too small: {0}x{1}", width, height));
        m_Screen.WriteString(0, 1, m_Screen.Width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, 
                             string.Format("Minimum size: {0}x{1}", m_Screen.MinimumWidth, m_Screen.MinimumHeight));
      }
    }

    public virtual GenericConfig GetSaveState()
    {
      return null;
    }

    protected string LoadHelp()
    {
      string resName = string.Format("CAuLi.Help.{0}.txt", GetType().Name);
      try {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName)) {
          if (stream != null) {
            using (StreamReader reader = new StreamReader(stream)) {
              return reader.ReadToEnd();
            }
          }
        }
      } catch (Exception) { }
      return string.Empty;
    }
  }
}
