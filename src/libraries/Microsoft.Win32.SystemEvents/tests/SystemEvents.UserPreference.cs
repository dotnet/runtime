// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class UserPreferenceTests : SystemEventsTest
    {

        private void SendMessage(int message, int uiAction, string area, bool freeMemory = true)
        {
            var areaPtr = Marshal.StringToHGlobalUni(area);
            try
            {
                SendMessage((int)message, (IntPtr)uiAction, areaPtr);
            }
            finally
            {
                if (freeMemory)
                {
                    Marshal.FreeHGlobal(areaPtr);
                }
            }
        }

        private void SendReflectedMessage(int message, int uiAction, string area)
        {
            // WM_REFLECT is an internal message where the SystemEvents WndProc will copy
            // the lParam and pass it back in a posted message.  In that case it expects
            // to be the source of the message and will free the memory itself.
            SendMessage(User32.WM_REFLECT + message, uiAction, area, freeMemory:false);
        }

        public static IEnumerable<object[]> PreferenceChangingCases() =>
            new List<object[]>()
            {
                new object[] { User32.WM_SETTINGCHANGE, 0, "Policy", UserPreferenceCategory.Policy },
                new object[] { User32.WM_SETTINGCHANGE, 0, "intl", UserPreferenceCategory.Locale },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETACCESSTIMEOUT, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETFILTERKEYS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETHIGHCONTRAST, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSEKEYS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSCREENREADER, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSERIALKEYS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSHOWSOUNDS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSOUNDSENTRY, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSTICKYKEYS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETTOGGLEKEYS, null, UserPreferenceCategory.Accessibility },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDESKWALLPAPER, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETFONTSMOOTHING, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETCURSORS, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDESKPATTERN, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETGRIDGRANULARITY, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETWORKAREA, null, UserPreferenceCategory.Desktop },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_ICONHORIZONTALSPACING, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_ICONVERTICALSPACING, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETICONMETRICS, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETICONS, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETICONTITLELOGFONT, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETICONTITLEWRAP, null, UserPreferenceCategory.Icon },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDOUBLECLICKTIME, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDOUBLECLKHEIGHT, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDOUBLECLKWIDTH, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSE, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSEBUTTONSWAP, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSEHOVERHEIGHT, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSEHOVERTIME, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSESPEED, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMOUSETRAILS, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSNAPTODEFBUTTON, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETWHEELSCROLLLINES, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETCURSORSHADOW, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETHOTTRACKING, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETTOOLTIPANIMATION, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETTOOLTIPFADE, null, UserPreferenceCategory.Mouse },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETKEYBOARDDELAY, null, UserPreferenceCategory.Keyboard },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETKEYBOARDPREF, null, UserPreferenceCategory.Keyboard },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETKEYBOARDSPEED, null, UserPreferenceCategory.Keyboard },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETLANGTOGGLE, null, UserPreferenceCategory.Keyboard },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMENUDROPALIGNMENT, null, UserPreferenceCategory.Menu },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMENUFADE, null, UserPreferenceCategory.Menu },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMENUSHOWDELAY, null, UserPreferenceCategory.Menu },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMENUANIMATION, null, UserPreferenceCategory.Menu },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSELECTIONFADE, null, UserPreferenceCategory.Menu },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETLOWPOWERACTIVE, null, UserPreferenceCategory.Power },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETLOWPOWERTIMEOUT, null, UserPreferenceCategory.Power },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETPOWEROFFACTIVE, null, UserPreferenceCategory.Power },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETPOWEROFFTIMEOUT, null, UserPreferenceCategory.Power },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSCREENSAVEACTIVE, null, UserPreferenceCategory.Screensaver },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSCREENSAVERRUNNING, null, UserPreferenceCategory.Screensaver },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSCREENSAVETIMEOUT, null, UserPreferenceCategory.Screensaver },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETKEYBOARDCUES, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETCOMBOBOXANIMATION, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETLISTBOXSMOOTHSCROLLING, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETGRADIENTCAPTIONS, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETUIEFFECTS, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETACTIVEWINDOWTRACKING, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETACTIVEWNDTRKZORDER, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETACTIVEWNDTRKTIMEOUT, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETANIMATION, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETBORDER, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETCARETWIDTH, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDRAGFULLWINDOWS, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDRAGHEIGHT, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETDRAGWIDTH, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETFOREGROUNDFLASHCOUNT, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETFOREGROUNDLOCKTIMEOUT, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETMINIMIZEDMETRICS, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETNONCLIENTMETRICS, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SETTINGCHANGE, User32.SPI_SETSHOWIMEUI, null, UserPreferenceCategory.Window },
                new object[] { User32.WM_SYSCOLORCHANGE, 0, null, UserPreferenceCategory.Color },
            };


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(PreferenceChangingCases))]
        public void SignalsUserPreferenceEventsAsynchronously(int message, int uiAction, string area, UserPreferenceCategory expectedCategory)
        {
            var changing = new AutoResetEvent(false);
            var changed = new AutoResetEvent(false);

            UserPreferenceChangingEventArgs changingArgs = null;
            UserPreferenceChangingEventHandler changingHandler = (o, e) =>
            {
                changingArgs = e;
                changing.Set();
            };

            UserPreferenceChangedEventArgs changedArgs = null;
            UserPreferenceChangingEventArgs changingDuringChanged = null;
            UserPreferenceChangedEventHandler changedHandler = (o, e) =>
            {
                changedArgs = e;
                changingDuringChanged = changingArgs;
                changed.Set();
            };

            SystemEvents.UserPreferenceChanging += changingHandler;
            SystemEvents.UserPreferenceChanged += changedHandler;

            try
            {
                SendMessage(message, uiAction, area);
                Assert.True(changing.WaitOne(PostMessageWait));
                Assert.NotNull(changingArgs);
                Assert.Equal(expectedCategory, changingArgs.Category);

                Assert.True(changed.WaitOne(PostMessageWait));
                Assert.NotNull(changedArgs);
                Assert.Equal(expectedCategory, changedArgs.Category);

                // changed must follow changing for the same category
                Assert.NotNull(changingDuringChanged);
                Assert.Equal(expectedCategory, changingDuringChanged.Category);

            }
            finally
            {
                SystemEvents.UserPreferenceChanging -= changingHandler;
                SystemEvents.UserPreferenceChanged -= changedHandler;
                changing.Dispose();
                changed.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsUserPreferenceEventsAsynchronouslyOnThemeChanged()
        {
            var changing = new AutoResetEvent(false);
            var changed = new AutoResetEvent(false);

            UserPreferenceChangingEventArgs changingArgs = null;
            UserPreferenceChangingEventHandler changingHandler = (o, e) =>
            {
                changingArgs = e;
                changing.Set();
            };

            List<UserPreferenceChangedEventArgs> changedArgs = new List<UserPreferenceChangedEventArgs>();
            UserPreferenceChangingEventArgs changingDuringChanged = null;
            UserPreferenceChangedEventHandler changedHandler = (o, e) =>
            {
                changedArgs.Add(e);
                changingDuringChanged = changingArgs;
                // signal test to continue after two events were recieved
                if (changedArgs.Count > 1)
                {
                    changed.Set();
                }
            };

            SystemEvents.UserPreferenceChanging += changingHandler;
            SystemEvents.UserPreferenceChanged += changedHandler;

            try
            {
                SendMessage(User32.WM_THEMECHANGED, 0, null);
                Assert.True(changing.WaitOne(PostMessageWait));
                Assert.NotNull(changingArgs);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changingArgs.Category);

                Assert.True(changed.WaitOne(PostMessageWait));
                Assert.Equal(2, changedArgs.Count);
                Assert.NotNull(changedArgs[0]);
                Assert.Equal(UserPreferenceCategory.Window, changedArgs[0].Category);
                Assert.NotNull(changedArgs[1]);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changedArgs[1].Category);

                // changed must follow changing for VisualStyle
                Assert.NotNull(changingDuringChanged);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changingDuringChanged.Category);

            }
            finally
            {
                SystemEvents.UserPreferenceChanging -= changingHandler;
                SystemEvents.UserPreferenceChanged -= changedHandler;
                changing.Dispose();
                changed.Dispose();
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(PreferenceChangingCases))]
        public void SignalsUserPreferenceEventsSynchronously(int message, int uiAction, string area, UserPreferenceCategory expectedCategory)
        {
            bool changing = false, changed = false;

            UserPreferenceChangingEventArgs changingArgs = null;
            UserPreferenceChangingEventHandler changingHandler = (o, e) =>
            {
                changingArgs = e;
                changing = true;
            };

            UserPreferenceChangedEventArgs changedArgs = null;
            UserPreferenceChangingEventArgs changingDuringChanged = null;
            UserPreferenceChangedEventHandler changedHandler = (o, e) =>
            {
                changedArgs = e;
                changingDuringChanged = changingArgs;
                changed = true;
            };

            SystemEvents.UserPreferenceChanging += changingHandler;
            SystemEvents.UserPreferenceChanged += changedHandler;


            try
            {
                SendReflectedMessage(message, uiAction, area);
                Assert.True(changing);
                Assert.NotNull(changingArgs);
                Assert.Equal(expectedCategory, changingArgs.Category);

                Assert.True(changed);
                Assert.NotNull(changedArgs);
                Assert.Equal(expectedCategory, changedArgs.Category);

                // changed must follow changing for the same category
                Assert.NotNull(changingDuringChanged);
                Assert.Equal(expectedCategory, changingDuringChanged.Category);
            }
            finally
            {
                SystemEvents.UserPreferenceChanging -= changingHandler;
                SystemEvents.UserPreferenceChanged -= changedHandler;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsUserPreferenceEventsSynchronouslyOnReflectedThemeChanged()
        {
            bool changing = false, changed = false;

            UserPreferenceChangingEventArgs changingArgs = null;
            UserPreferenceChangingEventHandler changingHandler = (o, e) =>
            {
                changingArgs = e;
                changing = true;
            };

            List<UserPreferenceChangedEventArgs> changedArgs = new List<UserPreferenceChangedEventArgs>();
            UserPreferenceChangingEventArgs changingDuringChanged = null;
            UserPreferenceChangedEventHandler changedHandler = (o, e) =>
            {
                changedArgs.Add(e);
                changingDuringChanged = changingArgs;
                changed = true;
            };

            SystemEvents.UserPreferenceChanging += changingHandler;
            SystemEvents.UserPreferenceChanged += changedHandler;

            try
            {
                SendReflectedMessage(User32.WM_THEMECHANGED, 0, null);
                Assert.True(changing);
                Assert.NotNull(changingArgs);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changingArgs.Category);

                Assert.True(changed);
                Assert.Equal(2, changedArgs.Count);
                Assert.NotNull(changedArgs[0]);
                Assert.Equal(UserPreferenceCategory.Window, changedArgs[0].Category);
                Assert.NotNull(changedArgs[1]);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changedArgs[1].Category);

                // changed must follow changing for VisualStyle
                Assert.NotNull(changingDuringChanged);
                Assert.Equal(UserPreferenceCategory.VisualStyle, changingDuringChanged.Category);
            }
            finally
            {
                SystemEvents.UserPreferenceChanging -= changingHandler;
                SystemEvents.UserPreferenceChanged -= changedHandler;
            }
        }
    }
}
