using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DisplayHelper {

    using static ConditionalLogger;
    using static Screen;

    /// <summary>
    /// Asynchronous helper class to manage any display settings in a reliable and modern way.
    /// With this we do not have to wait in Update loops or use CoRoutines, we just make async calls and get notified, when we were successful...
    /// </summary>
    public class AsyncHelper : Helper {

        /// <summary>
        /// What type of adjustment was done?
        /// </summary>
        public enum AdjustmentType {
            ResolutionChange = 0,
            RefreshRateChange = 1,
            ScreenModeChange = 2,
            DisplayChange = 3,
            All = 4,
        }

        /// <summary>
        /// What is the status of the adjustment operation? Were we successful?
        /// </summary>
        public enum Status {
            Fail = 0,
            Success = 1,
            Aborted = 2,
        }

        private bool shouldAbort = false;
        private bool currentlyAdjusting = false;
        private Action<AdjustmentType, Status> successOrFailAction;
        private Action<AdjustmentType> adjustmentStartedAction;

        /// <summary>
        /// Are we currently executing any adjustment operation?
        /// </summary>
        public bool CurrentlyAdjusting => currentlyAdjusting;

        /// <summary>
        /// Create a new AsyncHelper, you need to setup the callbacks to get notified, when something changes...
        /// </summary>
        /// <param name="successOrFailAction"></param>
        /// <param name="adjustmentStartedAction"></param>
        public AsyncHelper(Action<AdjustmentType, Status> successOrFailAction, Action<AdjustmentType> adjustmentStartedAction) {
            this.successOrFailAction = successOrFailAction;
            this.adjustmentStartedAction = adjustmentStartedAction;
        }

        /// <summary>
        /// Abort any ongoin Adjustment operation. You'll need to handle resetting back yourself afterwards!
        /// </summary>
        public void AbortAdjustment() {
            shouldAbort = true;
        }

        /// <summary>
        /// Change the Resolution by ResolutionInfo and RefreshRate
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="refreshRate"></param>
        /// <returns></returns>
        private async Task SetResolutionAsync(ResolutionInfo resolution, RefreshRate refreshRate, bool notify = true) {
            Func<bool> equalityCheck;
            if (fullScreenMode == FullScreenMode.ExclusiveFullScreen) {
                equalityCheck = () => IsResolutionEqual(ref resolution, ref refreshRate);
            } else {
                equalityCheck = () => IsResolutionEqual(ref resolution);
            }

            void SetResolutionAction() => SetResolution(ref resolution, ref refreshRate);
            Log($"SetResolutionByIDAsync BASE {resolution.width}x{resolution.height} | {refreshRate.value}");
            await DoAdjustment(AdjustmentType.ResolutionChange, SetResolutionAction, equalityCheck, notify);
        }

        /// <summary>
        /// Change the Resolution by ID asynchronously...
        /// </summary>
        /// <param name="resolutionID">What resolution id do we want to change this to?</param>
        /// <returns></returns>
        public async Task SetResolutionByIDAsync(int resolutionID, bool notify = true) {
            ResolutionInfo resolution = resolutionInfos[resolutionInfosLookup[resolutionID]];
            Log($"SetResolutionByIDAsync {resolutionID} | {resolution.width}x{resolution.height}");
            await SetResolutionAsync(resolution, currentResolution.refreshRateRatio, notify);
        }

        /// <summary>
        /// Change the Resolution by ID asynchronously...
        /// </summary>
        /// <param name="resolutionID">What resolution id do we want to change this to?</param>
        /// <param name="refreshIndex">What refresh index should be selected?</param>
        /// <returns></returns>
        public async Task SetResolutionByIDAsync(int resolutionID, int refreshIndex, bool notify = true) {
            ResolutionInfo resolution = resolutionInfos[resolutionInfosLookup[resolutionID]];
            RefreshRateInfo refreshRate = (RefreshRateInfo)resolution.validRefreshRates[refreshIndex];
            Log($"SetResolutionByIDAsync {resolutionID} | {refreshIndex} | {resolution.width}x{resolution.height} | {refreshRate.refreshRate.value}");
            await SetResolutionAsync(resolution, refreshRate.refreshRate, notify);
        }

        /// <summary>
        /// Change the FullscreenMode asynchronously...
        /// </summary>
        /// <param name="newFullScreenMode">what fullscreenmode should we adjust this to?</param>
        /// <returns></returns>
        public async Task SetScreenModeAsync(FullScreenMode newFullScreenMode, bool notify = true) {
            Log($"SetFullscreenModeAsync {newFullScreenMode} | {notify}");
            bool IsFullScreenModeEqual() => fullScreenMode == newFullScreenMode;
            void SetFullScreenMode() => SetScreenMode(newFullScreenMode);
            await DoAdjustment(AdjustmentType.ScreenModeChange, SetFullScreenMode, IsFullScreenModeEqual, notify);
        }

        /// <summary>
        /// Adjust all resolution properties at once
        /// </summary>
        /// <param name="resolutionSettings">The settings object containing all the properties we want to use for the adjustment</param>
        /// <returns></returns>
        public async Task AdjustAll(DisplaySettings resolutionSettings) {
            Log($"AdjustAll {resolutionSettings.displayIndex} | {resolutionSettings.screenMode} | {resolutionSettings.resolutionID} | {resolutionSettings.refreshRateIndex}");
            async Task AdjustAllAction() {
                await ChangeDisplayAsync(resolutionSettings.displayIndex, false);
                await SetScreenModeAsync(resolutionSettings.screenMode, false);
                await SetResolutionByIDAsync(resolutionSettings.resolutionID, resolutionSettings.refreshRateIndex, false);
            }
            await DoAdjustment(AdjustmentType.All, AdjustAllAction, true);
        }

        /// <summary>
        /// Change the users display asynchronously...
        /// </summary>
        /// <param name="index">What display should be displayed?</param>
        /// <returns></returns>
        public async Task ChangeDisplayAsync(int index, bool notify = true) {
            Log($"ChangeDisplayAsync {index} | {notify}");
            // the adjustment action
            async Task ChangeDisplayAdjustment() {
                // get the display by its index...
                DisplayInfo display = displays[index];
                Vector2Int targetCoordinates = new Vector2Int(0, 0);
                if (fullScreenMode != FullScreenMode.Windowed) {
                    // Target the center of the display. Doing it this way shows off
                    // that MoveMainWindow snaps the window to the top left corner
                    // of the display when running in fullscreen mode.
                    targetCoordinates.x += display.width / 2;
                    targetCoordinates.y += display.height / 2;
                }

                // start moving the window to the other display
                AsyncOperation moveOperation = MoveMainWindowTo(display, targetCoordinates);
                // wait one frame and if that is not enough, wait until the move operation reports that it is done...
                await Task.Yield();
                while (!moveOperation.isDone && !shouldAbort) {
                    await Task.Yield();
                }
            }
            // actually start executing our adjustment...
            await DoAdjustment(AdjustmentType.DisplayChange, ChangeDisplayAdjustment, notify);
        }


        /// <summary>
        /// Do a specific adjustment...
        /// </summary>
        /// <param name="changeType">What kind of adjustment is this?</param>
        /// <param name="adjustmentAction">The adjusment action we want to execute</param>
        /// <param name="equalityCheck">Check wether we really need to adjust something...</param>
        /// <returns></returns>
        private async Task DoAdjustment(AdjustmentType changeType, Action adjustmentAction, Func<bool> equalityCheck, bool notify = true) {
            Log($"DoAdjustment {changeType} | {adjustmentAction.Method.Name} | {equalityCheck.Method.Name} | {notify}");
            async Task AdjustmentLogic() {
                // do we actually need to adjust something?
                if (!equalityCheck()) {
                    // ok, execute the adjustment
                    adjustmentAction();
                    // wait one frame and if that it not enough, wait until we are equal to the target or we are prompted to cancel...
                    await Task.Yield();
                    while (!equalityCheck() && !shouldAbort) {
                        await Task.Yield();
                    }
                }
            }
            // actually start our adjustment process...
            await DoAdjustment(changeType, AdjustmentLogic, notify);
        }

        /// <summary>
        /// Do a specific adjustment...
        /// </summary>
        /// <param name="changeType">What kind of adjustment is this?</param>
        /// <param name="adjustmentAction">The adjusment action we want to execute</param>
        /// <returns></returns>
        private async Task DoAdjustment(AdjustmentType changeType, Func<Task> adjustmentAction, bool notify) {
            Log($"DoAdjustment {changeType} | {adjustmentAction.Method.Name} | {notify}");
            // return early if we are already adjusting
            if (!StartAdjustment(changeType, notify)) {
                return;
            }
            Status status = Status.Fail;
            try {
                // await our adjustment logic
                await adjustmentAction();
                status = Status.Success;
            } catch (Exception e) {
                // print any errors that were encountered
                Debug.Log(e.Message);
            } finally {
                // and the adjustment and notify...
                EndAdjustment(changeType, status, notify);
            }
        }

        /// <summary>
        /// Start the adjustment process
        /// </summary>
        /// <returns></returns>
        private bool StartAdjustment(AdjustmentType changeType, bool notify) {
            Log($"StartAdjustment {notify}");
            if (notify) {
                // are we already adjusting? -> return early
                if (currentlyAdjusting) {
                    Log("Already adjusting, this is not allowed!");
                    return false;
                }

                // make sure we flag that we are currently adjusting...
                currentlyAdjusting = true;

                // notify that we started the adjustment process...
                adjustmentStartedAction?.Invoke(changeType);

                shouldAbort = false;
            }
            return true;
        }

        /// <summary>
        /// End the adjustment process
        /// </summary>
        /// <param name="changeType">What type of adjustment was this?</param>
        /// <param name="success">Were we successful?</param>
        private void EndAdjustment(AdjustmentType changeType, Status status, bool notify) {
            Log($"EndAdjustment {changeType} | {status} | {notify}");
            if (notify) {
                // update our list of displays
                UpdateDisplays();
                // update our list of resolutions
                UpdateResolutions();
                // signal we are no longer adjusting something...
                currentlyAdjusting = false;

                if (shouldAbort) {
                    status = Status.Aborted;
                    shouldAbort = false;
                }
                successOrFailAction?.Invoke(changeType, status);
            }
        }
    }
}
