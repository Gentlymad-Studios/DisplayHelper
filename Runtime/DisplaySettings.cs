using UnityEngine;

namespace DisplayHelper {
    /// <summary>
    /// All settings relevant to adjust something display specific...
    /// </summary>
    public class DisplaySettings {
        public int displayIndex;
        public int refreshRateIndex;
        public FullScreenMode screenMode;
        public int resolutionID;

        /// <summary>
        /// Copy data from one DisplaySettings object to the other
        /// </summary>
        /// <param name="otherSettings"></param>
        /// <returns></returns>
        public DisplaySettings CopyFrom(DisplaySettings otherSettings) {
            screenMode = otherSettings.screenMode;
            resolutionID = otherSettings.resolutionID;
            refreshRateIndex = otherSettings.refreshRateIndex;
            displayIndex = otherSettings.displayIndex;
            return this;
        }
    }
}

