// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32
{
    public partial class PowerModeChangedEventArgs : System.EventArgs
    {
        public PowerModeChangedEventArgs(Microsoft.Win32.PowerModes mode) { }
        public Microsoft.Win32.PowerModes Mode { get { throw null; } }
    }
    public delegate void PowerModeChangedEventHandler(object sender, Microsoft.Win32.PowerModeChangedEventArgs e);
    public enum PowerModes
    {
        Resume = 1,
        StatusChange = 2,
        Suspend = 3,
    }
    public partial class SessionEndedEventArgs : System.EventArgs
    {
        public SessionEndedEventArgs(Microsoft.Win32.SessionEndReasons reason) { }
        public Microsoft.Win32.SessionEndReasons Reason { get { throw null; } }
    }
    public delegate void SessionEndedEventHandler(object sender, Microsoft.Win32.SessionEndedEventArgs e);
    public partial class SessionEndingEventArgs : System.EventArgs
    {
        public SessionEndingEventArgs(Microsoft.Win32.SessionEndReasons reason) { }
        public bool Cancel { get { throw null; } set { } }
        public Microsoft.Win32.SessionEndReasons Reason { get { throw null; } }
    }
    public delegate void SessionEndingEventHandler(object sender, Microsoft.Win32.SessionEndingEventArgs e);
    public enum SessionEndReasons
    {
        Logoff = 1,
        SystemShutdown = 2,
    }
    public partial class SessionSwitchEventArgs : System.EventArgs
    {
        public SessionSwitchEventArgs(Microsoft.Win32.SessionSwitchReason reason) { }
        public Microsoft.Win32.SessionSwitchReason Reason { get { throw null; } }
    }
    public delegate void SessionSwitchEventHandler(object sender, Microsoft.Win32.SessionSwitchEventArgs e);
    public enum SessionSwitchReason
    {
        ConsoleConnect = 1,
        ConsoleDisconnect = 2,
        RemoteConnect = 3,
        RemoteDisconnect = 4,
        SessionLogon = 5,
        SessionLogoff = 6,
        SessionLock = 7,
        SessionUnlock = 8,
        SessionRemoteControl = 9,
    }
    public sealed partial class SystemEvents
    {
        internal SystemEvents() { }
        public static event System.EventHandler DisplaySettingsChanged { add { } remove { } }
        public static event System.EventHandler DisplaySettingsChanging { add { } remove { } }
        public static event System.EventHandler EventsThreadShutdown { add { } remove { } }
        public static event System.EventHandler InstalledFontsChanged { add { } remove { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This event has been deprecated. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static event System.EventHandler LowMemory { add { } remove { } }
        public static event System.EventHandler PaletteChanged { add { } remove { } }
        public static event Microsoft.Win32.PowerModeChangedEventHandler PowerModeChanged { add { } remove { } }
        public static event Microsoft.Win32.SessionEndedEventHandler SessionEnded { add { } remove { } }
        public static event Microsoft.Win32.SessionEndingEventHandler SessionEnding { add { } remove { } }
        public static event Microsoft.Win32.SessionSwitchEventHandler SessionSwitch { add { } remove { } }
        public static event System.EventHandler TimeChanged { add { } remove { } }
        public static event Microsoft.Win32.TimerElapsedEventHandler TimerElapsed { add { } remove { } }
        public static event Microsoft.Win32.UserPreferenceChangedEventHandler UserPreferenceChanged { add { } remove { } }
        public static event Microsoft.Win32.UserPreferenceChangingEventHandler UserPreferenceChanging { add { } remove { } }
        public static System.IntPtr CreateTimer(int interval) { throw null; }
        public static void InvokeOnEventsThread(System.Delegate method) { }
        public static void KillTimer(System.IntPtr timerId) { }
    }
    public partial class TimerElapsedEventArgs : System.EventArgs
    {
        public TimerElapsedEventArgs(System.IntPtr timerId) { }
        public System.IntPtr TimerId { get { throw null; } }
    }
    public delegate void TimerElapsedEventHandler(object sender, Microsoft.Win32.TimerElapsedEventArgs e);
    public enum UserPreferenceCategory
    {
        Accessibility = 1,
        Color = 2,
        Desktop = 3,
        General = 4,
        Icon = 5,
        Keyboard = 6,
        Menu = 7,
        Mouse = 8,
        Policy = 9,
        Power = 10,
        Screensaver = 11,
        Window = 12,
        Locale = 13,
        VisualStyle = 14,
    }
    public partial class UserPreferenceChangedEventArgs : System.EventArgs
    {
        public UserPreferenceChangedEventArgs(Microsoft.Win32.UserPreferenceCategory category) { }
        public Microsoft.Win32.UserPreferenceCategory Category { get { throw null; } }
    }
    public delegate void UserPreferenceChangedEventHandler(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e);
    public partial class UserPreferenceChangingEventArgs : System.EventArgs
    {
        public UserPreferenceChangingEventArgs(Microsoft.Win32.UserPreferenceCategory category) { }
        public Microsoft.Win32.UserPreferenceCategory Category { get { throw null; } }
    }
    public delegate void UserPreferenceChangingEventHandler(object sender, Microsoft.Win32.UserPreferenceChangingEventArgs e);
}
