using System.Collections.Generic;
using UnityEngine;

namespace DisplayHelper {
    /// <summary>
    /// Struct to hold all the information relevant for every resolution we detect
    /// </summary>
    public struct ResolutionInfo : IComparableValue<int> {
        public int resolutionID;
        public int width;
        public int height;
        /// <summary>
        /// List of valid refresh rates
        /// </summary>
        public List<IComparableValue<double>> validRefreshRates;

        public ResolutionInfo(int width, int height, RefreshRate refreshRate) {
            resolutionID = Helper.EncodeResolution(width, height);
            this.width = width;
            this.height = height;
            validRefreshRates = new List<IComparableValue<double>>() { new RefreshRateInfo(refreshRate) };
        }

        /// <summary>
        /// Add a new refreshrate to this resolution...
        /// </summary>
        /// <param name="refreshRate"></param>
        public void AddRefreshRate(RefreshRate refreshRate) {
            IComparableValue<double>.InsertSorted(validRefreshRates, refreshRate.value, () => new RefreshRateInfo(refreshRate), out _);
        }

        /// <summary>
        /// Get the comparable value to sort and find entries fast
        /// </summary>
        /// <returns></returns>
        public int GetValue() {
            return resolutionID;
        }
    }
}
