using System;
using System.Collections.Generic;
using UnityEngine;

namespace DisplayHelper {

    using static Screen;

    public class Helper {
        private const int BitShift = 16;
        private const int LowerBitsMask = 0xFFFF;

        protected List<ResolutionInfo> resolutionInfos = new List<ResolutionInfo>();
        protected Dictionary<int, int> resolutionInfosLookup = new Dictionary<int, int>();
        protected List<DisplayInfo> displays = new List<DisplayInfo>();

        /// <summary>
        /// Get the current width of the active resolution
        /// </summary>
        public int CurrentWidth => fullScreen ? currentResolution.width : width;
        /// <summary>
        /// Get the current height of the resolution
        /// </summary>
        public int CurrentHeight => fullScreen ? currentResolution.height : height;
        /// <summary>
        /// Get the currenttly active screenmode
        /// </summary>
        public FullScreenMode ScreenMode => fullScreenMode;
        /// <summary>
        /// Are we in exclusive fullscreen mode?
        /// </summary>
        public bool IsExclusiveFullScreen => fullScreenMode == FullScreenMode.ExclusiveFullScreen;
        
        /// <summary>
        /// Create the helper and immediatly update the list of displays and resolutions
        /// </summary>
        public Helper() {
            UpdateDisplays();
            UpdateResolutions();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public List<DisplayInfo> GetDisplays(out int currentIndex) {
            Debug.Log("main display: "+ mainWindowDisplayInfo.name);
            for (int i=0; i< displays.Count; i++) {
                if (displays[i].Equals(mainWindowDisplayInfo)) {
                    Debug.Log("display found " + displays[i].name + " index:"+i);
                    currentIndex = i;
                    return displays;
                }
            }
            Debug.Log("display NOT found assuming index:" + 0);
            currentIndex = 0;
            return displays;
        }

        /// <summary>
        /// Get a list of all detected resolutions
        /// Returns an index for the given encoded resolution, to highlight its position in the list
        /// </summary>
        /// <param name="currentResolutionID"></param>
        /// <returns></returns>
        public List<ResolutionInfo> GetResolutions(out int currentResolutionID) {
            currentResolutionID = ResolveResolution(CurrentWidth, CurrentHeight, true);
            return resolutionInfos;
        }

        /// <summary>
        /// Return all refresh rates for a given resolution ID
        /// </summary>
        /// <param name="resolutionID"></param>
        /// <param name="refreshRateIndex"></param>
        /// <returns></returns>
        public List<IComparableValue<double>> GetRefreshRates(int resolutionID, out int refreshRateIndex) {
            ResolutionInfo resolution = resolutionInfos[resolutionInfosLookup[resolutionID]];
            refreshRateIndex = -1;
            for (int i=0; i< resolution.validRefreshRates.Count; i++) {
                if (resolution.validRefreshRates[i].GetValue() == currentResolution.refreshRateRatio.value) {
                    refreshRateIndex = i;
                    break;
                }
            }
            return resolution.validRefreshRates;
        }

        /// <summary>
        /// Resolove the current detected resolution
        /// </summary>
        /// <param name="getNearest"></param>
        /// <returns></returns>
        public int ResolveCurrentResolution(bool getNearest = false) {
            return ResolveResolution(CurrentWidth, CurrentHeight, getNearest);
        }

        /// <summary>
        /// Resolve a resolution by looking it up in our lookup table,
        /// or if it could not be found, return the nearest resolution if getNearest is set
        /// otherwise returns -1
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="getNearest"></param>
        /// <returns></returns>
        public int ResolveResolution(int width, int height, bool getNearest = false) {
            int resolutionID = EncodeResolution(width, height);

            if (resolutionInfosLookup.ContainsKey(resolutionID)) {
                return resolutionID;
            } else if (getNearest) {
                return GetNearestResolutionID(width, height);
            }

            return -1;
        }

        /// <summary>
        /// Encode a resolution into a unique & sortable integer value.
        /// This grants us a fast lookup into resolutions as well as an efficient way of sorting them.
        /// </summary>
        /// <param name="width">the width of the resolution to encode</param>
        /// <param name="height">the height of the resolution to encode</param>
        /// <returns></returns>
        public static int EncodeResolution(int width, int height) {
            return (width << BitShift) | height;
        }

        /// <summary>
        /// Decode a resolution ID into width and height dimensions
        /// </summary>
        /// <param name="resolutionID">The encoded resolution ID</param>
        /// <returns></returns>
        public (int width, int height) DecodeResolution(int resolutionID) {
            int width = resolutionID >> BitShift;
            int height = resolutionID & LowerBitsMask;
            return (width, height);
        }

        /// <summary>
        /// Get the nearest resolution in the list of resolutions.
        /// </summary>
        /// <param name="width">width to search for</param>
        /// <param name="height">height to search for</param>
        /// <returns></returns>
        private int GetNearestResolutionID(int width, int height) {
            int smallestDiff = int.MaxValue;
            int closest = resolutionInfos[0].GetValue();
            ResolutionInfo resolution;

            for (int i=0; i< resolutionInfos.Count; i++) {
                resolution = resolutionInfos[i];
                int diff = Math.Abs(width - resolution.width) + Math.Abs(height - resolution.height);
                if (diff < smallestDiff) {
                    smallestDiff = diff;
                    closest = resolution.resolutionID;
                }
            }

            return closest;
        }

        /// <summary>
        /// Update the list of displays
        /// </summary>
        protected void UpdateDisplays() {
            GetDisplayLayout(displays);

            // make sure we always have at least one display...
            if (displays.Count == 0) {
                DisplayInfo fakeDisplay = new DisplayInfo() {name = "Fake display"};
                fakeDisplay.width = currentResolution.width;
                fakeDisplay.height = currentResolution.height;
                fakeDisplay.workArea = new RectInt(0, 0, currentResolution.width, currentResolution.height);
                fakeDisplay.refreshRate.denominator = 1;
                displays.Add(fakeDisplay);
            }
        }

        /// <summary>
        /// Update the list of resolutions.
        /// This might change after the display connection changed.
        /// </summary>
        public void UpdateResolutions() {
            resolutionInfos.Clear();
            resolutionInfosLookup.Clear();

            List<Resolution> unityResolutions = new List<Resolution>(resolutions) {
                currentResolution, // we add the current resolution, because unity sometimes forgets that...
#if UNITY_EDITOR
                // test resolutions for the editor
                new Resolution() { width = currentResolution.width, height = currentResolution.height, refreshRateRatio = currentResolution.refreshRateRatio  },
                new Resolution() { width = currentResolution.width, height = currentResolution.height, refreshRateRatio = new RefreshRate(){ numerator=60, denominator=1 } },
                new Resolution() { width = currentResolution.width, height = currentResolution.height, refreshRateRatio = new RefreshRate(){ numerator=59, denominator=1 } },
#endif
            };

            for (int i = 0; i < unityResolutions.Count; i++) {
                Resolution unityRes = unityResolutions[i];
                int resolutionID = EncodeResolution(unityRes.width, unityRes.height);
                // add the new resolution if it does not exist yet
                int resolutionIndex = IComparableValue<int>.InsertSorted(resolutionInfos, resolutionID, () => new ResolutionInfo(unityRes.width, unityRes.height, unityRes.refreshRateRatio), out bool existed);
                // if the resolution already exists...
                if (existed) {
                    // add the refreshRate (checks internally if the refreshrate already exists)
                    resolutionInfos[resolutionIndex].AddRefreshRate(unityRes.refreshRateRatio);
                    // update the resolutions lookup
                    resolutionInfosLookup[resolutionID] = resolutionIndex;
                } else {
                    // add an easy lookup for our resolution
                    resolutionInfosLookup.Add(resolutionID, resolutionIndex);
                }
            }
        }

        /// <summary>
        /// is the given resolution equal to the current resolution?
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="refreshRate"></param>
        /// <returns></returns>
        public bool IsResolutionEqual(ref ResolutionInfo resolution, ref RefreshRate refreshRate) {
            return IsResolutionEqual(ref resolution) && currentResolution.refreshRateRatio.value == refreshRate.value;
        }

        /// <summary>
        /// is the given resolution equal to the current resolution?
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public bool IsResolutionEqual(ref ResolutionInfo resolution) {
            return CurrentWidth == resolution.width && CurrentHeight == resolution.height;
        }

        /// <summary>
        /// Set the screen mode...
        /// </summary>
        /// <param name="newScreenMode"></param>
        public void SetScreenMode(FullScreenMode newScreenMode) {
            if (newScreenMode == FullScreenMode.Windowed) {
                fullScreenMode = newScreenMode;
            } else {
                DisplayInfo display = mainWindowDisplayInfo;
                Screen.SetResolution(display.width, display.height, newScreenMode);
            }
        }

        /// <summary>
        /// Set the resolution
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="refreshRate"></param>
        public void SetResolution(ref ResolutionInfo resolution, ref RefreshRate refreshRate) {
            if (IsExclusiveFullScreen) {
                Screen.SetResolution(resolution.width, resolution.height, fullScreenMode, refreshRate);
            } else {
                Screen.SetResolution(resolution.width, resolution.height, fullScreenMode);
            }
        }
    }
}
