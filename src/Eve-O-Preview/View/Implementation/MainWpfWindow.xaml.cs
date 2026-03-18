using EveOPreview.Configuration.Implementation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
// 【关键修复】：使用别名解决 WPF 和 WinForms 的命名空间冲突
using WinForms = System.Windows.Forms;
using WpfControls = System.Windows.Controls;

namespace EveOPreview.View.Implementation
{
    public class ClientItem
    {
        public string Title { get; set; }
        public bool IsEnabled { get; set; }
    }

    public partial class MainWpfWindow : System.Windows.Window, IMainFormView, WinForms.IWin32Window
    {
        public IntPtr Handle => new WindowInteropHelper(this).EnsureHandle();

        private bool _suppressEvents = false;

        // 【核心修复】：补齐缺失的托盘图标变量定义
        private WinForms.NotifyIcon _notifyIcon;

        // 【核心修复】：显式指定使用 WPF 的 RadioButton
        private readonly Dictionary<ViewZoomAnchor, WpfControls.RadioButton> _zoomAnchorMap;
        private System.Drawing.Color _activeClientHighlightColor = System.Drawing.Color.Green;
        private FontSettings _titleFontSettings = new FontSettings { Name = "Microsoft YaHei", Size = 10, Style = System.Drawing.FontStyle.Regular };

        public ObservableCollection<ClientItem> ActiveClientsList { get; set; } = new ObservableCollection<ClientItem>();
        private Dictionary<string, IThumbnailDescription> _thumbnailsMap = new Dictionary<string, IThumbnailDescription>();

        public MainWpfWindow()
        {
            InitializeComponent();
            ClientsListBox.DataContext = ActiveClientsList;

            // 【核心修复】：指定 Dictionary 的类型为 WPF 的 RadioButton
            _zoomAnchorMap = new Dictionary<ViewZoomAnchor, WpfControls.RadioButton>
            {
                { ViewZoomAnchor.NW, RadioNW }, { ViewZoomAnchor.N, RadioN }, { ViewZoomAnchor.NE, RadioNE },
                { ViewZoomAnchor.W, RadioW },   { ViewZoomAnchor.C, RadioC }, { ViewZoomAnchor.E, RadioE },
                { ViewZoomAnchor.SW, RadioSW }, { ViewZoomAnchor.S, RadioS }, { ViewZoomAnchor.SE, RadioSE }
            };

            MinimizeToTrayCheckBox.Click += SettingChanged_Handler;
            ShowThumbnailsAlwaysOnTopCheckBox.Click += SettingChanged_Handler;
            HideThumbnailsOnLostFocusCheckBox.Click += SettingChanged_Handler;
            EnableClientLayoutTrackingCheckBox.Click += SettingChanged_Handler;
            HideActiveClientThumbnailCheckBox.Click += SettingChanged_Handler;
            MinimizeInactiveClientsCheckBox.Click += SettingChanged_Handler;
            EnablePerClientThumbnailLayoutsCheckBox.Click += SettingChanged_Handler;
            OpacitySlider.ValueChanged += SettingChanged_Handler;
            EnableThumbnailZoomCheckBox.Click += SettingChanged_Handler;
            foreach (var rb in _zoomAnchorMap.Values) rb.Checked += SettingChanged_Handler;

            ShowThumbnailOverlaysCheckBox.Click += SettingChanged_Handler;
            ShowThumbnailFramesCheckBox.Click += SettingChanged_Handler;
            EnableActiveClientHighlightCheckBox.Click += SettingChanged_Handler;
            HighlightColorButton.Click += HighlightColorButton_Click;

            ThumbWidthText.LostFocus += ThumbnailSizeChanged_Handler;
            ThumbHeightText.LostFocus += ThumbnailSizeChanged_Handler;
            ZoomFactorText.LostFocus += SettingChanged_Handler;

            this.Closing += (s, e) =>
            {
                var request = new ViewCloseRequest();
                this.FormCloseRequested?.Invoke(request);
                e.Cancel = !request.Allow;
            };

            this.StateChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    if (this.MinimizeToTray)
                    {
                        this.Hide();
                    }
                    this.FormMinimized?.Invoke();
                }
            };

            this.Closed += (s, e) =>
            {
                // 清理托盘图标，防止残留
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                WinForms.Application.ExitThread();
            };

            this.Loaded += (s, e) => { this.FormActivated?.Invoke(); };

            InitializeNotifyIcon();
        }

        private void SettingChanged_Handler(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;
            ApplicationSettingsChanged?.Invoke();
        }

        private void ThumbnailSizeChanged_Handler(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;
            ApplicationSettingsChanged?.Invoke();
            ThumbnailsSizeChanged?.Invoke();
        }

        private void HighlightColorButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.ColorDialog())
            {
                dialog.Color = this.ActiveClientHighlightColor;
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    this.ActiveClientHighlightColor = dialog.Color;
                    if (!_suppressEvents) ApplicationSettingsChanged?.Invoke();
                }
            }
        }

        public bool MinimizeToTray { get => MinimizeToTrayCheckBox.IsChecked ?? false; set { _suppressEvents = true; MinimizeToTrayCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool ShowThumbnailsAlwaysOnTop { get => ShowThumbnailsAlwaysOnTopCheckBox.IsChecked ?? false; set { _suppressEvents = true; ShowThumbnailsAlwaysOnTopCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool HideThumbnailsOnLostFocus { get => HideThumbnailsOnLostFocusCheckBox.IsChecked ?? false; set { _suppressEvents = true; HideThumbnailsOnLostFocusCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool EnableClientLayoutTracking { get => EnableClientLayoutTrackingCheckBox.IsChecked ?? false; set { _suppressEvents = true; EnableClientLayoutTrackingCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool HideActiveClientThumbnail { get => HideActiveClientThumbnailCheckBox.IsChecked ?? false; set { _suppressEvents = true; HideActiveClientThumbnailCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool MinimizeInactiveClients { get => MinimizeInactiveClientsCheckBox.IsChecked ?? false; set { _suppressEvents = true; MinimizeInactiveClientsCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool EnablePerClientThumbnailLayouts { get => EnablePerClientThumbnailLayoutsCheckBox.IsChecked ?? false; set { _suppressEvents = true; EnablePerClientThumbnailLayoutsCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool EnableThumbnailZoom { get => EnableThumbnailZoomCheckBox.IsChecked ?? false; set { _suppressEvents = true; EnableThumbnailZoomCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool ShowThumbnailOverlays { get => ShowThumbnailOverlaysCheckBox.IsChecked ?? false; set { _suppressEvents = true; ShowThumbnailOverlaysCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool ShowThumbnailFrames { get => ShowThumbnailFramesCheckBox.IsChecked ?? false; set { _suppressEvents = true; ShowThumbnailFramesCheckBox.IsChecked = value; _suppressEvents = false; } }
        public bool EnableActiveClientHighlight { get => EnableActiveClientHighlightCheckBox.IsChecked ?? false; set { _suppressEvents = true; EnableActiveClientHighlightCheckBox.IsChecked = value; _suppressEvents = false; } }

        public double ThumbnailOpacity
        {
            get => OpacitySlider.Value / 100.0;
            set { _suppressEvents = true; OpacitySlider.Value = value * 100.0; _suppressEvents = false; }
        }

        public System.Drawing.Size ThumbnailSize
        {
            get
            {
                int w = int.TryParse(ThumbWidthText.Text, out int parsedW) ? parsedW : 256;
                int h = int.TryParse(ThumbHeightText.Text, out int parsedH) ? parsedH : 144;
                return new System.Drawing.Size(w, h);
            }
            set
            {
                _suppressEvents = true;
                ThumbWidthText.Text = value.Width.ToString();
                ThumbHeightText.Text = value.Height.ToString();
                _suppressEvents = false;
            }
        }

        public int ThumbnailZoomFactor
        {
            get => int.TryParse(ZoomFactorText.Text, out int z) ? z : 2;
            set { _suppressEvents = true; ZoomFactorText.Text = value.ToString(); _suppressEvents = false; }
        }

        public ViewZoomAnchor ThumbnailZoomAnchor
        {
            get { foreach (var kvp in _zoomAnchorMap) if (kvp.Value.IsChecked == true) return kvp.Key; return ViewZoomAnchor.NW; }
            set { _suppressEvents = true; if (_zoomAnchorMap.TryGetValue(value, out var rb)) rb.IsChecked = true; _suppressEvents = false; }
        }

        public System.Drawing.Color ActiveClientHighlightColor
        {
            get => _activeClientHighlightColor;
            set
            {
                _suppressEvents = true;
                _activeClientHighlightColor = value;
                HighlightColorButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B));
                _suppressEvents = false;
            }
        }

        public FontSettings TitleFontSettings
        {
            get
            {
                if (_titleFontSettings == null)
                {
                    _titleFontSettings = new FontSettings();
                    _titleFontSettings.Name = "Microsoft YaHei";
                    _titleFontSettings.Size = 10;
                    _titleFontSettings.Style = System.Drawing.FontStyle.Regular;
                    _titleFontSettings.ForeColor = System.Drawing.Color.White;
                    _titleFontSettings.OutlineColor = System.Drawing.Color.Black;
                    _titleFontSettings.OutlineWidth = 1;
                    _titleFontSettings.PositionOffsetFromTop = 4;
                    _titleFontSettings.PositionOffsetFromLeft = 4;
                }
                return _titleFontSettings;
            }
            set
            {
                if (value != null)
                {
                    _suppressEvents = true;
                    _titleFontSettings = value;

                    if (_titleFontSettings.ForeColor == System.Drawing.Color.Empty || _titleFontSettings.ForeColor.A == 0 || _titleFontSettings.ForeColor == System.Drawing.Color.Transparent)
                    {
                        _titleFontSettings.ForeColor = System.Drawing.Color.White;
                    }

                    Dispatcher.Invoke(() => FontPreviewText.Text = $"当前字体: {_titleFontSettings.Name}, {_titleFontSettings.Size}pt");
                    _suppressEvents = false;
                }
            }
        }

        public void AddThumbnails(IList<IThumbnailDescription> thumbnails)
        {
            Dispatcher.Invoke(() =>
            {
                _suppressEvents = true;
                foreach (var t in thumbnails)
                {
                    _thumbnailsMap[t.Title] = t;
                    ActiveClientsList.Add(new ClientItem { Title = t.Title, IsEnabled = !t.IsDisabled });
                }
                _suppressEvents = false;
            });
        }

        public void RemoveThumbnails(IList<IThumbnailDescription> thumbnails)
        {
            Dispatcher.Invoke(() =>
            {
                _suppressEvents = true;
                foreach (var t in thumbnails)
                {
                    _thumbnailsMap.Remove(t.Title);
                    var item = ActiveClientsList.FirstOrDefault(x => x.Title == t.Title);
                    if (item != null) ActiveClientsList.Remove(item);
                }
                _suppressEvents = false;
            });
        }

        private void ClientCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;
            // 【核心修复】：指定使用 WPF 的 CheckBox
            if (sender is WpfControls.CheckBox cb && cb.DataContext is ClientItem item)
            {
                item.IsEnabled = cb.IsChecked ?? false;
                ClientsListBox.SelectedItem = item;

                if (_thumbnailsMap.TryGetValue(item.Title, out var thumb))
                {
                    thumb.IsDisabled = !item.IsEnabled;
                }

                ThumbnailStateChanged?.Invoke(item.Title);
                ApplicationSettingsChanged?.Invoke();
            }
        }

        private void ClientsListBox_SelectionChanged(object sender, WpfControls.SelectionChangedEventArgs e)
        {
            if (ClientsListBox.SelectedItem is ClientItem item)
            {
                ClientNotePanel.IsEnabled = true;
                SelectedClientChanged?.Invoke(item.Title);
            }
            else ClientNotePanel.IsEnabled = false;
        }

        private void ClientNoteText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ClientsListBox.SelectedItem is ClientItem item) ClientNoteUpdated?.Invoke(item.Title, ClientNoteText.Text);
        }

        public void ChangeFontButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FontDialog())
            {
                try
                {
                    dialog.Font = new System.Drawing.Font(_titleFontSettings.Name, _titleFontSettings.Size, _titleFontSettings.Style);
                    dialog.Color = _titleFontSettings.ForeColor;
                }
                catch { }

                dialog.ShowColor = true;

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _titleFontSettings.Name = dialog.Font.Name;
                    _titleFontSettings.Size = (int)Math.Round(dialog.Font.Size);
                    _titleFontSettings.Style = dialog.Font.Style;
                    _titleFontSettings.ForeColor = dialog.Color;

                    if (!_suppressEvents)
                    {
                        ApplicationSettingsChanged?.Invoke();
                        ThumbnailsSizeChanged?.Invoke();
                    }
                    Dispatcher.Invoke(() => FontPreviewText.Text = $"当前字体: {_titleFontSettings.Name}, {_titleFontSettings.Size}pt");
                }
            }
        }

        public string ClientNote { get => ClientNoteText.Text ?? ""; set { _suppressEvents = true; ClientNoteText.Text = value ?? ""; _suppressEvents = false; } }
        public void SetVersionInfo(string version) => Dispatcher.Invoke(() => VersionTextBlock.Text = $"版本: {version}");
        public void DocsButton_Click(object sender, RoutedEventArgs e) => DocumentationLinkActivated?.Invoke();
        public void Minimize() { }
        public void RefreshZoomSettings() { }
        public void SetDocumentationUrl(string url) { }
        public void SetThumbnailSizeLimitations(System.Drawing.Size minimumSize, System.Drawing.Size maximumSize) { }
        public Func<string> GetClientNameFromInput { get; set; }
        public Action<string> SelectedClientChanged { get; set; }
        public Action<string, string> ClientNoteUpdated { get; set; }
        public Action ApplicationExitRequested { get; set; }
        public Action FormActivated { get; set; }
        public Action FormMinimized { get; set; }
        public Action<ViewCloseRequest> FormCloseRequested { get; set; }
        public Action ApplicationSettingsChanged { get; set; }
        public Action ThumbnailsSizeChanged { get; set; }
        public Action<string> ThumbnailStateChanged { get; set; }
        public Action DocumentationLinkActivated { get; set; }
        public List<CycleGroup> CycleGroups { get; set; } = new List<CycleGroup>();

        void EveOPreview.IView.Show()
        {
            base.Show();
            this.FormActivated?.Invoke();

            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Run(this);
            }
            else
            {
                new System.Windows.Application().Run(this);
            }
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon();
            _notifyIcon.Text = "EVE-O Preview";

            try { _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(WinForms.Application.ExecutablePath); } catch { }
            _notifyIcon.Visible = true;

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("显示主界面", null, (s, e) => RestoreFromTray());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出程序", null, (s, e) => {
                _notifyIcon.Visible = false;
                ApplicationExitRequested?.Invoke();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseDoubleClick += (s, e) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        private void ListBoxItem_Selected(object sender, RoutedEventArgs e) { }
    }
}