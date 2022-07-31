using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        MenuItem m_SelectedItem = null;
        MenuItem m_FirstVisibleItem = null;
        string m_SearchString = string.Empty;
        DateTime m_LastSearch = DateTime.MinValue;

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
            get { return m_SelectedItem; }
            set
            {
                if (value != m_SelectedItem) {
                    m_SelectedItem = value;
                    SelectedItemChanged?.Invoke(this, m_SelectedItem);
                }
            }
        }

        public MenuItem FirstVisibleItem
        {
            get { return m_FirstVisibleItem; }
            set { m_FirstVisibleItem = value; }
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

            if (!Items.Contains(m_FirstVisibleItem))
                m_FirstVisibleItem = null;

            if (m_FirstVisibleItem == null && Items.Count > 0)
                m_FirstVisibleItem = Items[0];

            screen.Clear(X, Y, Width, Height);
            int row = Y;

            if (!string.IsNullOrEmpty(Title)) {
                string count = Items.Count.ToString("###,###,###,##0");
                string title = string.Format("{0}{1}", Title.PadRight(Width - count.Length), count);
                screen.WriteString(X, row, Width, ColorTheme.Instance.BackgroundTitle, ColorTheme.Instance.ForegroundTitle, title);
                row++;
            }

            if (m_FirstVisibleItem == null) {
                if (!string.IsNullOrEmpty(EmptyText))
                    screen.WriteString(X, row, Width, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, EmptyText);
                return;
            }

            for (int idx = Items.IndexOf(m_FirstVisibleItem); idx < Items.Count; idx++) {
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

            int firstIdx = m_FirstVisibleItem != null ? Items.IndexOf(m_FirstVisibleItem) : 0;
            int idx = 0;
            bool handled = false;
            switch (keyPress.Key) {
                case ConsoleKey.UpArrow:
                    if (SelectedItem == Items.First()) {
                        SelectedItem = Items.Last();
                        if (Items.Count >= Height)
                            m_FirstVisibleItem = Items[Items.Count - Height + delta];
                        else
                            m_FirstVisibleItem = Items[0];
                    } else {
                        SelectedItem = Items[Items.IndexOf(SelectedItem) - 1];
                        if (Items.IndexOf(SelectedItem) < firstIdx)
                            m_FirstVisibleItem = SelectedItem;
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.DownArrow:
                    if (SelectedItem == Items.Last()) {
                        SelectedItem = Items.First();
                        m_FirstVisibleItem = SelectedItem;
                    } else {
                        SelectedItem = Items[Items.IndexOf(SelectedItem) + 1];
                        if (Items.IndexOf(SelectedItem) + delta >= firstIdx + Height)
                            m_FirstVisibleItem = Items[firstIdx + 1];
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.End:
                    SelectedItem = Items.Last();
                    if (Items.Count > Height)
                        m_FirstVisibleItem = Items[Items.Count - Height + delta];
                    else
                        m_FirstVisibleItem = Items[0];
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.Home:
                    SelectedItem = Items.First();
                    m_FirstVisibleItem = SelectedItem;
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.PageDown:
                    idx = Items.IndexOf(SelectedItem);
                    if (idx < Items.Count - 5) {
                        SelectedItem = Items[idx + 5];
                        if (Items.IndexOf(SelectedItem) + delta >= firstIdx + Height)
                            m_FirstVisibleItem = Items[firstIdx + 5];
                    } else {
                        SelectedItem = Items.Last();
                        if (Items.Count > Height)
                            m_FirstVisibleItem = Items[Items.Count - Height + delta];
                        else
                            m_FirstVisibleItem = Items[0];
                    }
                    NeedsRedraw = true;
                    handled = true;
                    break;
                case ConsoleKey.PageUp:
                    idx = Items.IndexOf(SelectedItem);
                    if (idx >= 5) {
                        SelectedItem = Items[idx - 5];
                        if (Items.IndexOf(SelectedItem) < firstIdx)
                            m_FirstVisibleItem = SelectedItem;
                    } else {
                        SelectedItem = Items.First();
                        m_FirstVisibleItem = SelectedItem;
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
                    if (m_LastSearch != DateTime.MinValue && (DateTime.UtcNow - m_LastSearch).TotalMilliseconds >= 500)
                        m_SearchString = string.Empty;
                    m_SearchString = string.Format("{0}{1}", m_SearchString, keyPress.KeyChar);
                    foreach (MenuItem i in Items) {
                        if (i.GetSearchText().StartsWith(m_SearchString, StringComparison.InvariantCultureIgnoreCase)) {
                            m_FirstVisibleItem = i;
                            SelectedItem = i;
                            NeedsRedraw = true;
                            break;
                        }
                    }
                    if (NeedsRedraw)
                        m_LastSearch = DateTime.UtcNow;
                    else {
                        m_SearchString = string.Empty;
                        m_LastSearch = DateTime.MinValue;
                    }
                    handled = true;
                    break;
            }

            return handled;
        }
    }
}
