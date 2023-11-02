using System.Diagnostics;


namespace DisplayHelper {
    public class ConditionalLogger {
        public const string DISPLAY_DEBUG = nameof(DISPLAY_DEBUG);
        public const string logColorHex = "#AAAA00";


        [Conditional(DISPLAY_DEBUG)]
        public static void Log(object message) {
            UnityEngine.Debug.Log($"[<color={logColorHex}>{DISPLAY_DEBUG}</color>] {message}");
        }
    }
}
