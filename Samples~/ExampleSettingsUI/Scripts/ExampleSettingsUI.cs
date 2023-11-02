using UnityEngine;
using UnityEngine.UI;
using DisplayHelper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExampleSettingsUI {

    using static ConditionalLogger;

    public class ExampleSettingsUI : MonoBehaviour {
        public Dropdown displayDropdown;
        public Dropdown resolutionDropdown;
        public Dropdown refreshRateDropdown;
        public Dropdown screenModeDropdown;
        public GameObject adjustmentScreen;
        public GameObject root;
        public Button keepSettings;
        public Button revertSettings;
        public Button resetSettings;

        public Text remainingTime;
        private int currentResolutionID;
        private AsyncHelper displayHelper;
        private DisplaySettingsStorage settings;
        private int confirmationDialogWait = 10;

        private (string, FullScreenMode)[] screenModes = new (string, FullScreenMode)[]{
            ("Exclusive", FullScreenMode.ExclusiveFullScreen),
            ("Borderless", FullScreenMode.FullScreenWindow),
            ("Window", FullScreenMode.Windowed),
        };

        private bool initialized = false;
        private bool visible = false;

        private void Start() { }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.F3)) {
                visible = !visible;
                root.SetActive(visible);

                if (!initialized) {
                    displayHelper = new AsyncHelper(OnAdjustmentSuccessOrFail, OnAdjustmentStarted);
                    settings = new DisplaySettingsStorage();

                    // set everything up and capture default values
                    UpdateUI(true);

                    // Add listeners
                    displayDropdown.onValueChanged.AddListener(OnDisplayChanged);
                    resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
                    refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateChanged);
                    screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
                    keepSettings.onClick.AddListener(KeepSettings);
                    revertSettings.onClick.AddListener(RevertSettings);
                    resetSettings.onClick.AddListener(ResetSettings);

                    initialized = true;
                }
            }
        }

        private async void ResetSettings() {
            Log("RESET SETTINGS");

            displayHelper.AbortAdjustment();
            HideConfirmationDialog();

            await displayHelper.AdjustAll(settings.Default);
            settings.Temp.CopyFrom(settings.Default);
            settings.Last.CopyFrom(settings.Default);
        }

        private void HideConfirmationDialog() {
            adjustmentScreen.SetActive(false);
        }

        private async void RevertSettings() {
            Log("REVERT SETTINGS");

            displayHelper.AbortAdjustment();
            HideConfirmationDialog();
            await displayHelper.AdjustAll(settings.Last);
            settings.Temp.CopyFrom(settings.Last);
        }

        private void KeepSettings() {
            Log("KEEP SETTINGS");

            HideConfirmationDialog();
            settings.Last.CopyFrom(settings.Temp);
        }

        private async Task DoConfirmationDialog() {
            await Task.Yield();
            float targetTime = Time.unscaledTime + confirmationDialogWait;
            while (targetTime >= Time.unscaledTime && adjustmentScreen.activeSelf) {
                await Task.Yield();
                int remaining = (int)(targetTime - Time.unscaledTime);
                if (remaining < 0) {
                    remaining = 0;
                }
                remainingTime.text = "" + remaining;
            }
        }

        private async void OnAdjustmentStarted(AsyncHelper.AdjustmentType adjustmentType) {
            if (adjustmentType != AsyncHelper.AdjustmentType.All) {
                adjustmentScreen.SetActive(true);
                await DoConfirmationDialog();
                // time is up, revert...
                if (adjustmentScreen.activeSelf) {
                    RevertSettings();
                }
            }
        }

        private void UpdateUI(bool capture = false) {
            SetupDisplayDropdown(capture);
            SetupResolutionDropdown(capture);
            SetupScreenModeDropdown(capture);
            UpdateRefreshRateDropdown(capture);
        }

        private void OnAdjustmentSuccessOrFail(AsyncHelper.AdjustmentType changeType, AsyncHelper.Status status) {
            Log($"changeType: {changeType} | status: {status}");
            UpdateUI();
        }

        private void SetupDisplayDropdown(bool capture) {
            List<DisplayInfo> displays = displayHelper.GetDisplays(out int currentDisplayIndex);

            // remove exceeding dropdown items
            SetupDropdownEntries(displayDropdown, displays.Count);

            // re-use existing dropdown items and extend if necessary
            for (int i = 0; i < displays.Count; i++) {
                displayDropdown.options[i].text = $"{displays[i].name} ({i})";
            }

            Log($"Display dropdown setup | index:{currentDisplayIndex} | resolvedIndex:{displays[currentDisplayIndex].name} | uielement:{displayDropdown.options[currentDisplayIndex].text} | mainDisplay:{Screen.mainWindowDisplayInfo.name} |");
            // set the current display index in the dropdown accordingly
            displayDropdown.SetValueWithoutNotify(currentDisplayIndex);
            displayDropdown.RefreshShownValue();

            if (capture) {
                settings.Default.displayIndex = settings.Last.displayIndex = settings.Temp.displayIndex = currentDisplayIndex;
            }
        }

        private void SetupResolutionDropdown(bool capture) {
            List<ResolutionInfo> resolutions = displayHelper.GetResolutions(out int currentResolutionID);

            (int width, int height) = displayHelper.DecodeResolution(currentResolutionID);
            Log($"current resolution: {width} {height}");

            // remove exceeding dropdown items
            SetupDropdownEntries(resolutionDropdown, resolutions.Count);

            // re-use existing dropdown items and extend if necessary
            int selectedIndex = 0;

            for (int i = resolutions.Count - 1; i >= 0; i--) {
                int resolutionIndex = resolutionDropdown.options.Count - 1 - i;
                ResolutionInfo resolutionInfo = resolutions[i];
                resolutionDropdown.options[resolutionIndex].text = $"{resolutionInfo.width}x{resolutionInfo.height}";
                if (resolutionInfo.resolutionID == currentResolutionID) {
                    selectedIndex = resolutionIndex;
                }
            }

            // set the current display index in the dropdown accordingly
            resolutionDropdown.SetValueWithoutNotify(selectedIndex);
            resolutionDropdown.RefreshShownValue();
            this.currentResolutionID = currentResolutionID;

            if (capture) {
                settings.Default.resolutionID = settings.Last.resolutionID = settings.Temp.resolutionID = currentResolutionID;
            }
        }

        private void UpdateRefreshRateDropdown(bool capture) {
            Log($"UpdateRefreshRateDropdown actual:{Screen.fullScreenMode} | isExclusive:{displayHelper.IsExclusiveFullScreen}");
            if (!displayHelper.IsExclusiveFullScreen) {
                refreshRateDropdown.gameObject.SetActive(false);
                if (capture) {
                    settings.Default.refreshRateIndex = settings.Last.refreshRateIndex = settings.Temp.refreshRateIndex = 0;
                }
            } else {
                List<IComparableValue<double>> refreshRates = displayHelper.GetRefreshRates(currentResolutionID, out int refreshRateIndex);
                refreshRateDropdown.gameObject.SetActive(true);
                SetupDropdownEntries(refreshRateDropdown, refreshRates.Count);

                for (int i = 0; i < refreshRates.Count; i++) {
                    refreshRateDropdown.options[i].text = refreshRates[i].GetValue().ToString();
                }

                refreshRateDropdown.SetValueWithoutNotify(refreshRateIndex);
                refreshRateDropdown.RefreshShownValue();
                if (capture) {
                    settings.Default.refreshRateIndex = settings.Last.refreshRateIndex = settings.Temp.refreshRateIndex = refreshRateIndex;
                }
            }
        }

        private void SetupScreenModeDropdown(bool capture) {

            SetupDropdownEntries(screenModeDropdown, screenModes.Length);

            int selectedIndex = 0;
            for (int i = 0; i < screenModes.Length; i++) {
                (string, FullScreenMode) screenMode = screenModes[i];
                screenModeDropdown.options[i].text = screenMode.Item1;
                if (displayHelper.ScreenMode == screenMode.Item2) {
                    selectedIndex = i;
                }
            }

            screenModeDropdown.SetValueWithoutNotify(selectedIndex);
            screenModeDropdown.RefreshShownValue();

            if (capture) {
                settings.Default.screenMode = settings.Last.screenMode = settings.Temp.screenMode = displayHelper.ScreenMode;
            }
        }

        private async void OnDisplayChanged(int index) {
            Log($"OnDisplayChanged target:{displayDropdown.options[index].text} | current refreshrate:{Screen.currentResolution.refreshRateRatio.value} | current resolution:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
            Log($"------------------------");

            settings.Temp.displayIndex = index;
            await displayHelper.ChangeDisplayAsync(index);

            Log($"------------------------");
            Log($"After OnDisplayChanged target:{displayDropdown.options[index].text} | resolvedTarget:{screenModes[index].Item2} | current refreshrate:{Screen.currentResolution.refreshRateRatio.value} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
        }

        private async void OnResolutionChanged(int index) {
            string[] currentResolution = resolutionDropdown.options[index].text.Split('x');
            currentResolutionID = Helper.EncodeResolution(int.Parse(currentResolution[0]), int.Parse(currentResolution[1]));

            Log($"OnResolutionChanged target:{resolutionDropdown.options[index].text} | current:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
            Log($"------------------------");

            settings.Temp.resolutionID = currentResolutionID;
            await displayHelper.SetResolutionByIDAsync(currentResolutionID);

            Log($"------------------------");
            Log($"After OnResolutionChanged target:{resolutionDropdown.options[index].text} | current:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
        }

        private async void OnRefreshRateChanged(int index) {
            Log($"OnRefreshRateChanged target:{refreshRateDropdown.options[index].text} | current:{Screen.currentResolution.refreshRateRatio.value} | current resolution:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
            Log($"------------------------");

            settings.Temp.refreshRateIndex = index;
            await displayHelper.SetResolutionByIDAsync(currentResolutionID, index);

            Log($"------------------------");
            Log($"After OnRefreshRateChanged target:{refreshRateDropdown.options[index].text} | current:{Screen.currentResolution.refreshRateRatio.value} | current resolution:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
        }

        private async void OnScreenModeChanged(int index) {
            Log($"OnFullScreenModeChanged target:{screenModeDropdown.options[index].text} | resolvedTarget:{screenModes[index].Item2} | current refreshrate:{Screen.currentResolution.refreshRateRatio.value} | current resolution:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
            Log($"------------------------");

            settings.Temp.screenMode = screenModes[index].Item2;
            await displayHelper.SetScreenModeAsync(screenModes[index].Item2);

            Log($"------------------------");
            Log($"After OnFullScreenModeChanged target:{screenModeDropdown.options[index].text} | resolvedTarget:{screenModes[index].Item2} | current refreshrate:{Screen.currentResolution.refreshRateRatio.value} | current resolution:{displayHelper.CurrentWidth}x{displayHelper.CurrentHeight} | fullscreen:{displayHelper.ScreenMode} | refreshRate {Screen.currentResolution.refreshRateRatio.value}");
        }

        /// <summary>
        /// Set the ui dropdown options and resize them but also re-use existing dropdown elements if possible.
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="referenceCount"></param>
        private void SetupDropdownEntries(Dropdown dropdown, int referenceCount) {
            if (dropdown.options.Count != referenceCount) {
                if (referenceCount == 0) {
                    dropdown.ClearOptions();
                } else {
                    if (dropdown.options.Count > referenceCount) {
                        int difference = dropdown.options.Count - referenceCount;
                        for (int i = 0; i < difference; i++) {
                            dropdown.options.RemoveAt(i);
                        }
                    } else {
                        int difference = referenceCount - dropdown.options.Count;
                        for (int i = 0; i < difference; i++) {
                            dropdown.options.Add(new Dropdown.OptionData());
                        }
                    }
                }

            }
        }
    }
}
