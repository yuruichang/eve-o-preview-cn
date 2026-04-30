using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace EveOPreview.Configuration.Implementation
{
    public class LayoutProfile
    {
        [JsonProperty("PerClientLayout")]
        public Dictionary<string, Dictionary<string, Point>> PerClientLayout { get; set; } = new Dictionary<string, Dictionary<string, Point>>();

        [JsonProperty("FlatLayout")]
        public Dictionary<string, Point> FlatLayout { get; set; } = new Dictionary<string, Point>();
    }

    sealed class ThumbnailConfiguration : IThumbnailConfiguration
    {
        #region Private fields
        private bool _enablePerClientThumbnailLayouts;
        private bool _enableClientLayoutTracking;

        // Cache KeysConverter to avoid per-call allocations
        private static readonly KeysConverter _keysConverter = new KeysConverter();
        #endregion

        public ThumbnailConfiguration()
        {
            this.ConfigVersion = 2;

            this.CycleGroups = new List<CycleGroup>();
            this.ClientNotes = new Dictionary<string, string>();
            this.SavedLayoutProfiles = new Dictionary<string, LayoutProfile>();  

            this.PerClientActiveClientHighlightColor = new Dictionary<string, Color>
            {
                {"EVE - Example Toon 1", Color.Red},
                {"EVE - Example Toon 2", Color.Green}
            };

            this.PerClientLayout = new Dictionary<string, Dictionary<string, Point>>();
            this.FlatLayout = new Dictionary<string, Point>();
            this.ClientLayout = new Dictionary<string, ClientLayout>();
            this.ClientHotkey = new Dictionary<string, string>();
            this.DisableThumbnail = new Dictionary<string, bool>();
            this.PriorityClients = new List<string>();

            this.MinimizeToTray = false;
            this.ThumbnailRefreshPeriod = 500;

            this.EnableCompatibilityMode = false;

            this.ThumbnailOpacity = 0.5;

            this.EnableClientLayoutTracking = false;
            this.HideActiveClientThumbnail = false;
            this.MinimizeInactiveClients = false;
            this.ShowThumbnailsAlwaysOnTop = true;
            this.EnablePerClientThumbnailLayouts = false;

            this.HideThumbnailsOnLostFocus = false;
            this.HideThumbnailsDelay = 2;

            this.ThumbnailSize = new Size(384, 216);
            this.ThumbnailMinimumSize = new Size(192, 108);
            this.ThumbnailMaximumSize = new Size(960, 540);

            this.EnableThumbnailSnap = true;

            this.ThumbnailZoomEnabled = false;
            this.ThumbnailZoomFactor = 2;
            this.ThumbnailZoomAnchor = ZoomAnchor.NW;

            this.ShowThumbnailOverlays = true;
            this.ShowThumbnailFrames = false;

            this.EnableActiveClientHighlight = false;
            this.ActiveClientHighlightColor = Color.GreenYellow;
            this.ActiveClientHighlightThickness = 3;

            this.TitleFontSettings = new FontSettings();
            this.TitleFontSettings.Name = "Arial";
            this.TitleFontSettings.Size = 14.25f;
            this.TitleFontSettings.ForeColor = Color.FromArgb(255, 255, 165, 0);
            this.TitleFontSettings.Style = FontStyle.Regular;
            this.TitleFontSettings.OutlineColor = Color.Black;
            this.TitleFontSettings.OutlineWidth = 3.0f;
            this.TitleFontSettings.PositionOffsetFromLeft = 10;
            this.TitleFontSettings.PositionOffsetFromTop = 5;

            this.LoginThumbnailLocation = new Point(5, 5);
        }

        [JsonProperty("ConfigVersion")]
        public int ConfigVersion { get; set; }

        [JsonProperty("CycleGroups")]
        public List<CycleGroup> CycleGroups { get; set; } = new List<CycleGroup>();

        [JsonProperty("SavedLayoutProfiles")]
        public Dictionary<string, LayoutProfile> SavedLayoutProfiles { get; set; }

        [JsonProperty("PerClientActiveClientHighlightColor")]
        public Dictionary<string, Color> PerClientActiveClientHighlightColor { get; set; }

        public bool MinimizeToTray { get; set; }
        public int ThumbnailRefreshPeriod { get; set; }

        [JsonProperty("CompatibilityMode")]
        public bool EnableCompatibilityMode { get; set; }

        [JsonProperty("ThumbnailsOpacity")]
        public double ThumbnailOpacity { get; set; }

        [JsonProperty("ClientNotes")]
        public Dictionary<string, string> ClientNotes { get; set; }

        public string GetClientNote(string currentClient)
        {
            return this.ClientNotes.TryGetValue(currentClient, out string note) ? note : string.Empty;
        }

        public void SetClientNote(string currentClient, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                this.ClientNotes.Remove(currentClient);
            }
            else
            {
                this.ClientNotes[currentClient] = note;
            }
        }

        public bool EnableClientLayoutTracking
        {
            get => this._enableClientLayoutTracking;
            set
            {
                if (!value)
                {
                    this.ClientLayout.Clear();
                }

                this._enableClientLayoutTracking = value;
            }
        }

        public bool HideActiveClientThumbnail { get; set; }
        public bool MinimizeInactiveClients { get; set; }
        public bool ShowThumbnailsAlwaysOnTop { get; set; }

        public bool EnablePerClientThumbnailLayouts
        {
            get => this._enablePerClientThumbnailLayouts;
            set
            {
                if (!value)
                {
                    this.PerClientLayout.Clear();
                }

                this._enablePerClientThumbnailLayouts = value;
            }
        }

        public bool HideThumbnailsOnLostFocus { get; set; }
        public int HideThumbnailsDelay { get; set; }

        public Size ThumbnailSize { get; set; }
        public Size ThumbnailMaximumSize { get; set; }
        public Size ThumbnailMinimumSize { get; set; }

        public bool EnableThumbnailSnap { get; set; }

        [JsonProperty("EnableThumbnailZoom")]
        public bool ThumbnailZoomEnabled { get; set; }
        public int ThumbnailZoomFactor { get; set; }
        public ZoomAnchor ThumbnailZoomAnchor { get; set; }

        public bool ShowThumbnailOverlays { get; set; }
        public bool ShowThumbnailFrames { get; set; }

        public bool EnableActiveClientHighlight { get; set; }

        public Color ActiveClientHighlightColor { get; set; }

        public int ActiveClientHighlightThickness { get; set; }

        public FontSettings TitleFontSettings { get; set; }

        [JsonProperty("LoginThumbnailLocation")]
        public Point LoginThumbnailLocation { get; set; }

        [JsonProperty]
        private Dictionary<string, Dictionary<string, Point>> PerClientLayout { get; set; }
        [JsonProperty]
        private Dictionary<string, Point> FlatLayout { get; set; }
        [JsonProperty]
        private Dictionary<string, ClientLayout> ClientLayout { get; set; }
        [JsonProperty]
        private Dictionary<string, string> ClientHotkey { get; set; }
        [JsonProperty]
        private Dictionary<string, bool> DisableThumbnail { get; set; }
        [JsonProperty]
        private List<string> PriorityClients { get; set; }

        public Point GetThumbnailLocation(string currentClient, string activeClient, Point defaultLocation)
        {
            if (this.EnablePerClientThumbnailLayouts && !string.IsNullOrEmpty(activeClient))
            {
                Dictionary<string, Point> layoutSource;
                if (this.PerClientLayout.TryGetValue(activeClient, out layoutSource) && layoutSource.TryGetValue(currentClient, out Point location))
                {
                    return location;
                }
            }

            return this.FlatLayout.TryGetValue(currentClient, out Point locationFlat) ? locationFlat : defaultLocation;
        }

        public void SetThumbnailLocation(string currentClient, string activeClient, Point location)
        {
            Dictionary<string, Point> layoutSource;

            if (this.EnablePerClientThumbnailLayouts)
            {
                if (string.IsNullOrEmpty(activeClient))
                {
                    return;
                }

                if (!this.PerClientLayout.TryGetValue(activeClient, out layoutSource))
                {
                    layoutSource = new Dictionary<string, Point>();
                    this.PerClientLayout[activeClient] = layoutSource;
                }
            }
            else
            {
                layoutSource = this.FlatLayout;
            }

            layoutSource[currentClient] = location;
        }

        public ClientLayout GetClientLayout(string currentClient)
        {
            ClientLayout layout;
            this.ClientLayout.TryGetValue(currentClient, out layout);
            return layout;
        }

        public void SetClientLayout(string currentClient, ClientLayout layout)
        {
            this.ClientLayout[currentClient] = layout;
        }

        public Keys GetClientHotkey(string currentClient)
        {
            string hotkey;
            if (this.ClientHotkey.TryGetValue(currentClient, out hotkey))
            {
                object rawValue = _keysConverter.ConvertFromInvariantString(hotkey);
                return rawValue != null ? (Keys)rawValue : Keys.None;
            }

            return Keys.None;
        }

        public void SetClientHotkey(string currentClient, Keys hotkey)
        {
            this.ClientHotkey[currentClient] = _keysConverter.ConvertToInvariantString(hotkey);
        }

        public Keys StringToKey(string hotkey)
        {
            object rawValue = _keysConverter.ConvertFromInvariantString(hotkey);
            return rawValue != null ? (Keys)rawValue : Keys.None;
        }

        public bool IsPriorityClient(string currentClient)
        {
            return this.PriorityClients.Contains(currentClient);
        }

        public bool IsThumbnailDisabled(string currentClient)
        {
            return this.DisableThumbnail.TryGetValue(currentClient, out bool isDisabled) && isDisabled;
        }

        public void ToggleThumbnail(string currentClient, bool isDisabled)
        {
            this.DisableThumbnail[currentClient] = isDisabled;
        }

        public void ApplyRestrictions()
        {
            this.ThumbnailRefreshPeriod = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailRefreshPeriod, 300, 1000);
            this.ThumbnailSize = new Size(ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailSize.Width, this.ThumbnailMinimumSize.Width, this.ThumbnailMaximumSize.Width),
                ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailSize.Height, this.ThumbnailMinimumSize.Height, this.ThumbnailMaximumSize.Height));
            this.ThumbnailOpacity = ThumbnailConfiguration.ApplyRestrictions((int)(this.ThumbnailOpacity * 100.00), 20, 100) / 100.00;
            this.ThumbnailZoomFactor = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailZoomFactor, 2, 10);
            this.ActiveClientHighlightThickness = ThumbnailConfiguration.ApplyRestrictions(this.ActiveClientHighlightThickness, 1, 6);
        }

        private static int ApplyRestrictions(int value, int minimum, int maximum)
        {
            if (value <= minimum)
            {
                return minimum;
            }

            if (value >= maximum)
            {
                return maximum;
            }

            return value;
        }

        public List<string> GetSavedLayoutProfiles()
        {
            if (this.SavedLayoutProfiles == null) this.SavedLayoutProfiles = new Dictionary<string, LayoutProfile>();
            return this.SavedLayoutProfiles.Keys.ToList();
        }

        public void SaveLayoutProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;
            if (this.SavedLayoutProfiles == null) this.SavedLayoutProfiles = new Dictionary<string, LayoutProfile>();

            var profile = new LayoutProfile();

            foreach (var kvp in this.FlatLayout)
            {
                profile.FlatLayout[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in this.PerClientLayout)
            {
                var innerDict = new Dictionary<string, Point>();
                foreach (var innerKvp in kvp.Value)
                {
                    innerDict[innerKvp.Key] = innerKvp.Value;
                }
                profile.PerClientLayout[kvp.Key] = innerDict;
            }

            this.SavedLayoutProfiles[profileName] = profile;
        }

        public void LoadLayoutProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName) || this.SavedLayoutProfiles == null) return;
            if (!this.SavedLayoutProfiles.TryGetValue(profileName, out var profile)) return;

            this.FlatLayout.Clear();
            foreach (var kvp in profile.FlatLayout)
            {
                this.FlatLayout[kvp.Key] = kvp.Value;
            }

            this.PerClientLayout.Clear();
            foreach (var kvp in profile.PerClientLayout)
            {
                var innerDict = new Dictionary<string, Point>();
                foreach (var innerKvp in kvp.Value)
                {
                    innerDict[innerKvp.Key] = innerKvp.Value;
                }
                this.PerClientLayout[kvp.Key] = innerDict;
            }
        }

        public void DeleteLayoutProfile(string profileName)
        {
            if (this.SavedLayoutProfiles != null && this.SavedLayoutProfiles.ContainsKey(profileName))
            {
                this.SavedLayoutProfiles.Remove(profileName);
            }
        }
    }
}