namespace DisplayHelper {
    /// <summary>
    /// Storage facility to hold all our display settings in their different states
    /// </summary>
    public class DisplaySettingsStorage {
        private DisplaySettings defaultSettings = new DisplaySettings();
        private DisplaySettings lastSettings = new DisplaySettings();
        private DisplaySettings temporarySettings = new DisplaySettings();

        public DisplaySettings Last => lastSettings;
        public DisplaySettings Default => defaultSettings;
        public DisplaySettings Temp => temporarySettings;

        /// <summary>
        /// Shorthand for the copyFrom method in DisplaySettings
        /// Copy settings from one object to the other..
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public DisplaySettings CopyFrom(DisplaySettings from, DisplaySettings to) {
            return to.CopyFrom(from);
        }
    }
}
