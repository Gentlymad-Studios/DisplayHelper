using UnityEngine;

namespace DisplayHelper {
    /// <summary>
    /// Mocked refresh rate data struct that can be compared by its value fast
    /// </summary>
    public struct RefreshRateInfo : IComparableValue<double> {
        public RefreshRate refreshRate;

        public RefreshRateInfo(RefreshRate refreshRate) {
            this.refreshRate = refreshRate;
        }

        public double GetValue() {
            return refreshRate.value;
        }
    }
}
