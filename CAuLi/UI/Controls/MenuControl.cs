using System;
using System.Collections.Generic;
using System.Linq;

namespace CAuLi.UI.Controls
{
    class MenuControl : ControlBase
    {
        public class MenuItem
        {
            public delegate string FormatTextHandler(MenuControl menu, MenuItem item);

            public string Id { get; set; }
            public string Text { get; set; }
            public string SearchText { get; set; }
            public object Tag { get; set; }
            public FormatTextHandler FormatText { get; set; }

            public string GetSearchText()
            {
                if (!string.IsNullOrEmpty(SearchText))
                    return SearchText;
                return Text;
            }
        }

        private MenuItem _selectedItem;
        private MenuItem _firstVisibleItem;
        private string _searchString = string.Empty;
        private DateTime _lastSearch = DateTime.MinValue;

        public delegate void ItemTriggeredHandler(MenuControl sender, MenuItem item);
        public ItemTriggeredHandler ItemTriggered;
        public ItemTriggeredHandler SelectedItemChanged;

        public MenuControl()
        {
            Items = new List<MenuItem>();
        }

        public string Title { get; set; }
        public List<MenuItem> Items { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public string EmptyText { get; set; }
        public MenuItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value != _selectedItem) {
                    _selectedItem = value;
                    SelectedItemChanged?.Invoke(this, _selectedItem);
                }
            }
        }

        public MenuItem FirstVisibleItem
        {
            get { return _firstVisibleItem; }
            set { _firstVisibleItem = value; }
        }

        public void SelectMenuItem(string id)
        {
            if (Items != null) {
                foreach (MenuItem mi in Items) {
                    if (mi.Id == id) {
                        SelectedItem = mi;
                        break;
                    }
                }
            }
        }

        public void ScrollToMenuItem(string id)
        {
            if (Items != null) {
                foreach (MenuItem mi in Items) {
                    if (mi.Id == id) {
                        FirstVisibleItem = mi;
                        break;
                    }
                }
            }
        }

        public override void Draw(Screen screen)
        {
            if (Width == 0 || Height == 0)
                return;

            NeedsRedraw = false;
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];

            if (!Items.Contains(_firstVisibleItem))
                _firstVisibleItem = null;

            if (_firstVisibleItem == null && Items.Count > 0)
                _firstVisibleItem = Items[0];

            screen.Clear(X, Y, Width, Height);
            int row = Y;

            if (!string.IsNullOrEmpty(Title)) {
                string count = Items.Count.ToString("###,###,###,##0");
                string title = string.Format("{0}{1}", Title.PadRight(Width - count.Length), count);
                screen.WriteString(X, row, Width, ColorTheme.Instance.BackgroundTitle, ColorTheme.Instance.ForegroundTitle, title);
                row++;
            }

            if (_firstVisibleItem == null) {
                if (!string.IsNullOrEmpty(EmptyText))
                    screen.WriteString(X, row, Width, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, EmptyText);
                return;
            }

            for (int idx = Items.IndexOf(_firstVisibleItem); idx < Items.Count; idx++) {
                if (idx < 0 || idx >= Items.Count)
                    continue;

                MenuItem i = Items[idx];
                string text = i.FormatText == null ? i.Text : i.FormatText(this, i);
                if (string.IsNullOrEmpty(text))
                    continue;

                if (row - Y >= Height)
                    break;

                int col = X;
                if (HorizontalAlignment == UI.HorizontalAlignment.Center) {
                    if (text.Length < Width)
                        col = X + (Width - text.Length) / 2;
                }
                if (i == SelectedItem) {
                    if (HasFocus)
                        screen.WriteString(col, row, Width, ColorTheme.Instance.ReverseBackground, ColorTheme.Instance.ReverseForeground, text.PadRight(Width));
                    else
                        screen.WriteString(col, row, Width, ColorTheme.Instance.Background, ColorTheme.Instance.ForegroundHighlight, text.PadRight(Width));
                } else {
                    screen.WriteString(col, row, Width, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, text);
                }
                row++;
            }
        }

        public override bool KeyPress(ConsoleKeyInfo keyPress)
        {
            base.KeyPress(keyPress);

            if (!IsEnabled || Items == null || Items.Count == 0)
                return false;

            int delta = 0;
            if (!string.IsNullOrEmpty(Title))
                delta = 1;

            int firstIdx = _firstVisibleItem != null ? Items.IndexOf(_firstVisibleItem) : 0;
            int idx = 0;
            bool handled = false;
            switch (keyPress.Key) {
                case ConsoleKey.UpArrow:
                    if (SelectedItem == Items.First()) {
                        SelectedItem = Items.Last();
                        if (Items.Count >= Height)
                            _firstVisibleItem = Items[Items.Count - Height + delta];
                        else
                            _firstVisibleItem = Items[0];
                    } else {
                        SelectedItem = Items[Items.IndexOf(SelectedItem) - 1];
                        if (Items.IndexOf(SelectedItem) < firstIdx)
                            _firstVisibleItem = SelectedItem;
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.DownArrow:
                    if (SelectedItem == Items.Last()) {
                        SelectedItem = Items.First();
                        _firstVisibleItem = SelectedItem;
                    } else {
                        SelectedItem = Items[Items.IndexOf(SelectedItem) + 1];
                        if (Items.IndexOf(SelectedItem) + delta >= firstIdx + Height)
                            _firstVisibleItem = Items[firstIdx + 1];
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.End:
                    SelectedItem = Items.Last();
                    if (Items.Count > Height)
                        _firstVisibleItem = Items[Items.Count - Height + delta];
                    else
                        _firstVisibleItem = Items[0];
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.Home:
                    SelectedItem = Items.First();
                    _firstVisibleItem = SelectedItem;
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.PageDown:
                    idx = Items.IndexOf(SelectedItem);
                    if (idx < Items.Count - 5) {
                        SelectedItem = Items[idx + 5];
                        if (Items.IndexOf(SelectedItem) + delta >= firstIdx + Height)
                            _firstVisibleItem = Items[firstIdx + 5];
                    } else {
                        SelectedItem = Items.Last();
                        if (Items.Count > Height)
                            _firstVisibleItem = Items[Items.Count - Height + delta];
                        else
                            _firstVisibleItem = Items[0];
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.PageUp:
                    idx = Items.IndexOf(SelectedItem);
                    if (idx >= 5) {
                        SelectedItem = Items[idx - 5];
                        if (Items.IndexOf(SelectedItem) < firstIdx)
                            _firstVisibleItem = SelectedItem;
                    } else {
                        SelectedItem = Items.First();
                        _firstVisibleItem = SelectedItem;
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.Enter:
                    if (SelectedItem != null && ItemTriggered != null)
                        ItemTriggered(this, SelectedItem);
                    handled = true;
                    break;
                default:
                    if (_lastSearch != DateTime.MinValue && (DateTime.UtcNow - _lastSearch).TotalMilliseconds >= 500)
                        _searchString = string.Empty;
                    _searchString = string.Format("{0}{1}", _searchString, keyPress.KeyChar);
                    foreach (MenuItem i in Items) {
                        if (i.GetSearchText().StartsWith(_searchString, StringComparison.InvariantCultureIgnoreCase)) {
                            _firstVisibleItem = i;
                            SelectedItem = i;
                            NeedsRedraw = true;
                            break;
                        }
                    }
                    if (NeedsRedraw)
                        _lastSearch = DateTime.UtcNow;
                    else {
                        _searchString = string.Empty;
                        _lastSearch = DateTime.MinValue;
                    }
                    handled = true;
                    break;
            }

            return handled;
        }
    }
}
