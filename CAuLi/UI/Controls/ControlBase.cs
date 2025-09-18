using System;

namespace CAuLi.UI.Controls
{
    abstract class ControlBase
    {
        private bool _isVisible = true;
        private bool _hasFocus;
        private bool _needsRedraw = true;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        protected int MinimumWidth { get; set; }
        protected int MinimumHeight { get; set; }
        public bool Focusable { get; set; }
        public bool HasFocus
        {
            get { return _hasFocus; }
            set
            {
                if (_hasFocus != value) {
                    _hasFocus = value;
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
            get { return _needsRedraw; }
            set { _needsRedraw = value; }
        }

        public bool IsEnabled { get; set; }
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value) {
                    _isVisible = value;
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
