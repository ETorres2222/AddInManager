﻿using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RevitAddinManager.Model
{
    public static class ProcessManager
    {
        private static FormControl FrmControl => FormControl.Instance;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void SetRevitAsWindowOwner(this Window window)
        {
            if (null == window) { return; }
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner = GetActivateWindow();
            window.Loaded += delegate { FrmControl.IsOpened = true; };
            window.Closed += delegate { FrmControl.IsOpened = false; };
            window.Closing += SetActivateWindow;
        }

        private static void SetActivateWindow(object sender, CancelEventArgs e)
        {
            SetActivateWindow();
        }

        /// <summary>
        /// Set process revert use revit
        /// </summary>
        /// <returns></returns>
        private static void SetActivateWindow()
        {
            IntPtr ptr = GetActivateWindow();
            if (ptr != IntPtr.Zero)
            {
                SetForegroundWindow(ptr);
            }
        }

        /// <summary>
        /// return active windows is active
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetActivateWindow()
        {
            return Process.GetCurrentProcess().MainWindowHandle;
        }
    }
}