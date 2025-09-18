using System;

namespace CAuLi.UI.Controls
{
    abstract class ControlBase
    {
        bool m_IsVisible = true;
        bool m_HasFocus = false;
        bool m_NeedsRedraw = true;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        protected int MinimumWidth { get; set; }
        protected int MinimumHeight { get; set; }
        public bool Focusable { get; set; }
        public bool HasFocus
        {
            get { return m_HasFocus; }
            set
            {
                if (m_HasFocus != value) {
                    m_HasFocus = value;
                    FocusChanged();
                }
            }
        }

        public ControlBase()
        {
            Focusable = true;
            IsEnabled = true;
        }

        public string Name { get; set; }
        public bool NeedsRedraw
        {
            get { return m_NeedsRedraw; }
            set { m_NeedsRedraw = value; }
        }

        public bool IsEnabled { get; set; }
        public bool IsVisible
        {
            get { return m_IsVisible; }
            set
            {
                if (m_IsVisible != value) {
                    m_IsVisible = value;
                    NeedsRedraw = true;
                    IsVisibleChanged();
                }
            }
        }

        public abstract void Draw(Screen screen);

        public virtual bool KeyPress(ConsoleKeyInfo keyPress)
        {
            return false;
        }

        protected virtual void FocusChanged()
        {

        }

        protected virtual void IsVisibleChanged()
        {

        }

    }
}
