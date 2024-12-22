using System.Runtime.InteropServices;
namespace UnityExplorer.UI
{
    public class SecondaryDisplayHelper
    {
        public static Dictionary<int, int> DisplayTohWnd = new();
        public static HashSet<int> PreviouslyActiatedDisplays = new();
        public enum ShowStates
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
            ShowNoActivateRecentPosition = 4,
            Show = 5,
            MinimizeActivateNext = 6,
            MinimizeNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        [DllImport("User32")]
        private static extern int ShowWindowAsync(int hwnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static void DeactivateAdditionalDisplay()
        {
            PreviouslyActiatedDisplays.Add(DisplayManager.ActiveDisplayIndex);
            var hWnd = FindWindow(null, "Unity Secondary Display").ToInt32();
            if (hWnd != 0)
            {
                DisplayTohWnd[DisplayManager.ActiveDisplayIndex] = hWnd;
                ShowWindowAsync(hWnd, SW_HIDE);
            }
        }
        public static void ActivateAdditionalDisplay()
        {
            if (PreviouslyActiatedDisplays.Contains(DisplayManager.ActiveDisplayIndex))
            {
                if (DisplayTohWnd.TryGetValue(DisplayManager.ActiveDisplayIndex, out var hWnd))
                {
                    ShowWindowAsync(hWnd, SW_SHOW);
                }
            }
            else
            {
                DisplayManager.ActiveDisplay.Activate();
            }
        }
    }
}
