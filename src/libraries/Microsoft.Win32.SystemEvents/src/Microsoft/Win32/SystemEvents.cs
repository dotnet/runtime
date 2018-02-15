// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32
{
    using System;
    using System.Diagnostics;
    using System.Security;
    using System.Security.Permissions;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;

    /// <devdoc>
    ///    <para> 
    ///       Provides a
    ///       set of global system events to callers. This
    ///       class cannot be inherited.</para>
    /// </devdoc>
    [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
    public sealed class SystemEvents
    {
        // Almost all of our data is static.  We keep a single instance of
        // SystemEvents around so we can bind delegates to it.
        // Non-static methods in this class will only be called through
        // one of the delegates.
        //
        private static readonly object s_eventLockObject = new object();
        private static readonly object s_procLockObject = new object();
        private static volatile SystemEvents s_systemEvents;
        private static volatile Thread s_windowThread;
        private static volatile ManualResetEvent s_eventWindowReady;
        private static Random s_randomTimerId = new Random();
        private static volatile bool s_startupRecreates;
        private static volatile bool s_registeredSessionNotification = false;
        private static volatile int s_domainQualifier;
        private static volatile NativeMethods.WNDCLASS s_staticwndclass;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static volatile IntPtr s_defWindowProc;

        private static volatile string s_className = null;

        // cross-thread marshaling
        private static volatile Queue s_threadCallbackList; // list of Delegates
        private static volatile int s_threadCallbackMessage = 0;
        private static volatile ManualResetEvent s_eventThreadTerminated;

        //Decide whether to marshal or use Everett-style non-marshaled calls
        private static volatile bool s_checkedThreadAffinity = false;
        private static volatile bool s_useEverettThreadAffinity = false;
        private const string everettThreadAffinityValue = "EnableSystemEventsThreadAffinityCompatibility";

        // Per-instance data that is isolated to the window thread.
        //
        private volatile IntPtr _windowHandle;
        private NativeMethods.WndProc _windowProc;
        private NativeMethods.ConHndlr _consoleHandler;

        // The set of events we respond to.  
        //
        private static readonly object s_onUserPreferenceChangingEvent = new object();
        private static readonly object s_onUserPreferenceChangedEvent = new object();
        private static readonly object s_onSessionEndingEvent = new object();
        private static readonly object s_onSessionEndedEvent = new object();
        private static readonly object s_onPowerModeChangedEvent = new object();
        private static readonly object s_onLowMemoryEvent = new object();
        private static readonly object s_onDisplaySettingsChangingEvent = new object();
        private static readonly object s_onDisplaySettingsChangedEvent = new object();
        private static readonly object s_onInstalledFontsChangedEvent = new object();
        private static readonly object s_onTimeChangedEvent = new object();
        private static readonly object s_onTimerElapsedEvent = new object();
        private static readonly object s_onPaletteChangedEvent = new object();
        private static readonly object s_onEventsThreadShutdownEvent = new object();
        private static readonly object s_onSessionSwitchEvent = new object();


        // Our list of handler information.  This is a lookup of the above keys and objects that
        // match a delegate with a SyncronizationContext so we can fire on the proper thread.
        //
        private static Dictionary<object, List<SystemEventInvokeInfo>> s_handlers;


        /// <devdoc>
        ///     This class is static, there is no need to ever create it.
        /// </devdoc>
        private SystemEvents()
        {
        }


        // stole from SystemInformation... if we get SystemInformation moved
        // to somewhere that we can use it... rip this!
        //
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static volatile IntPtr s_processWinStation = IntPtr.Zero;
        private static volatile bool s_isUserInteractive = false;
        private static bool UserInteractive
        {
            get
            {
                if (Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                {
                    IntPtr hwinsta = IntPtr.Zero;

                    hwinsta = UnsafeNativeMethods.GetProcessWindowStation();
                    if (hwinsta != IntPtr.Zero && s_processWinStation != hwinsta)
                    {
                        s_isUserInteractive = true;

                        int lengthNeeded = 0;
                        NativeMethods.USEROBJECTFLAGS flags = new NativeMethods.USEROBJECTFLAGS();

                        if (UnsafeNativeMethods.GetUserObjectInformation(new HandleRef(null, hwinsta), NativeMethods.UOI_FLAGS, flags, Marshal.SizeOf(flags), ref lengthNeeded))
                        {
                            if ((flags.dwFlags & NativeMethods.WSF_VISIBLE) == 0)
                            {
                                s_isUserInteractive = false;
                            }
                        }
                        s_processWinStation = hwinsta;
                    }
                }
                else
                {
                    s_isUserInteractive = true;
                }
                return s_isUserInteractive;
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the display settings are changing.</para>
        /// </devdoc>
        public static event EventHandler DisplaySettingsChanging
        {
            add
            {
                AddEventHandler(s_onDisplaySettingsChangingEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onDisplaySettingsChangingEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user changes the display settings.</para>
        /// </devdoc>
        public static event EventHandler DisplaySettingsChanged
        {
            add
            {
                AddEventHandler(s_onDisplaySettingsChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onDisplaySettingsChangedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs before the thread that listens for system events is terminated.
        ///           Delegates will be invoked on the events thread.</para>
        /// </devdoc>
        public static event EventHandler EventsThreadShutdown
        {
            // Really only here for GDI+ initialization and shut down
            add
            {
                AddEventHandler(s_onEventsThreadShutdownEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onEventsThreadShutdownEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user adds fonts to or removes fonts from the system.</para>
        /// </devdoc>
        public static event EventHandler InstalledFontsChanged
        {
            add
            {
                AddEventHandler(s_onInstalledFontsChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onInstalledFontsChangedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the system is running out of available RAM.</para>
        /// </devdoc>
        [Obsolete("This event has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static event EventHandler LowMemory
        {
            add
            {
                EnsureSystemEvents(true, true);
                AddEventHandler(s_onLowMemoryEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onLowMemoryEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user switches to an application that uses a different 
        ///       palette.</para>
        /// </devdoc>
        public static event EventHandler PaletteChanged
        {
            add
            {
                AddEventHandler(s_onPaletteChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onPaletteChangedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user suspends or resumes the system.</para>
        /// </devdoc>
        public static event PowerModeChangedEventHandler PowerModeChanged
        {
            add
            {
                EnsureSystemEvents(true, true);
                AddEventHandler(s_onPowerModeChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onPowerModeChangedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user is logging off or shutting down the system.</para>
        /// </devdoc>
        public static event SessionEndedEventHandler SessionEnded
        {
            add
            {
                EnsureSystemEvents(true, false);
                AddEventHandler(s_onSessionEndedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionEndedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user is trying to log off or shutdown the system.</para>
        /// </devdoc>
        public static event SessionEndingEventHandler SessionEnding
        {
            add
            {
                EnsureSystemEvents(true, false);
                AddEventHandler(s_onSessionEndingEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionEndingEvent, value);
            }
        }

        /// <devdoc>
        ///    <para>Occurs when a user session switches.</para>
        /// </devdoc>
        public static event SessionSwitchEventHandler SessionSwitch
        {
            add
            {
                EnsureSystemEvents(true, true);
                EnsureRegisteredSessionNotification();
                AddEventHandler(s_onSessionSwitchEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionSwitchEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when the user changes the time on the system clock.</para>
        /// </devdoc>
        public static event EventHandler TimeChanged
        {
            add
            {
                EnsureSystemEvents(true, false);
                AddEventHandler(s_onTimeChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onTimeChangedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when a windows timer interval has expired.</para>
        /// </devdoc>
        public static event TimerElapsedEventHandler TimerElapsed
        {
            add
            {
                EnsureSystemEvents(true, false);
                AddEventHandler(s_onTimerElapsedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onTimerElapsedEvent, value);
            }
        }


        /// <devdoc>
        ///    <para>Occurs when a user preference has changed.</para>
        /// </devdoc>
        public static event UserPreferenceChangedEventHandler UserPreferenceChanged
        {
            add
            {
                AddEventHandler(s_onUserPreferenceChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onUserPreferenceChangedEvent, value);
            }
        }

        /// <devdoc>
        ///    <para>Occurs when a user preference is changing.</para>
        /// </devdoc>
        public static event UserPreferenceChangingEventHandler UserPreferenceChanging
        {
            add
            {
                AddEventHandler(s_onUserPreferenceChangingEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onUserPreferenceChangingEvent, value);
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
        private static void AddEventHandler(object key, Delegate value)
        {
            lock (s_eventLockObject)
            {
                if (s_handlers == null)
                {
                    s_handlers = new Dictionary<object, List<SystemEventInvokeInfo>>();
                    EnsureSystemEvents(false, false);
                }

                List<SystemEventInvokeInfo> invokeItems;

                if (!s_handlers.TryGetValue(key, out invokeItems))
                {
                    invokeItems = new List<SystemEventInvokeInfo>();
                    s_handlers[key] = invokeItems;
                }
                else
                {
                    invokeItems = s_handlers[key];
                }

                invokeItems.Add(new SystemEventInvokeInfo(value));
            }
        }

        /// <devdoc>
        ///      Console handler we add in case we are a console application or a service.
        ///      Without this we will not get end session events.
        /// </devdoc>
        private int ConsoleHandlerProc(int signalType)
        {
            switch (signalType)
            {
                case NativeMethods.CTRL_LOGOFF_EVENT:
                    OnSessionEnded((IntPtr)1, (IntPtr)NativeMethods.ENDSESSION_LOGOFF);
                    break;

                case NativeMethods.CTRL_SHUTDOWN_EVENT:
                    OnSessionEnded((IntPtr)1, (IntPtr)0);
                    break;
            }

            return 0;
        }

        private NativeMethods.WNDCLASS WndClass
        {
            get
            {
                if (s_staticwndclass == null)
                {
                    const string classNameFormat = ".NET-BroadcastEventWindow.{0}.{1}.{2}";

                    IntPtr hInstance = UnsafeNativeMethods.GetModuleHandle(null);

                    s_className = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        classNameFormat,
                        ThisAssembly.Version,
                        Convert.ToString(AppDomain.CurrentDomain.GetHashCode(), 16),
                        s_domainQualifier);

                    NativeMethods.WNDCLASS tempwndclass = new NativeMethods.WNDCLASS();
                    tempwndclass.hbrBackground = (IntPtr)(NativeMethods.COLOR_WINDOW + 1);
                    tempwndclass.style = 0;

                    _windowProc = new NativeMethods.WndProc(this.WindowProc);
                    tempwndclass.lpszClassName = s_className;
                    tempwndclass.lpfnWndProc = _windowProc;
                    tempwndclass.hInstance = hInstance;
                    s_staticwndclass = tempwndclass;
                }
                return s_staticwndclass;
            }
        }

        private IntPtr DefWndProc
        {
            get
            {
                if (s_defWindowProc == IntPtr.Zero)
                {
                    string defproc = (Marshal.SystemDefaultCharSize == 1 ? "DefWindowProcA" : "DefWindowProcW");

                    s_defWindowProc = UnsafeNativeMethods.GetProcAddress(new HandleRef(this, UnsafeNativeMethods.GetModuleHandle("user32.dll")), defproc);
                }
                return s_defWindowProc;
            }
        }

        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Only called on a single thread")]
        private void BumpQualifier()
        {
            s_staticwndclass = null;
            s_domainQualifier++;
        }

        /// <include file='doc\SystemEvents.uex' path='docs/doc[@for="SystemEvents.CreateBroadcastWindow"]/*' />
        /// <devdoc>
        ///      Goes through the work to register and create a window.
        /// </devdoc>
        private IntPtr CreateBroadcastWindow()
        {
            // Register the window class.
            //
            NativeMethods.WNDCLASS_I wndclassi = new NativeMethods.WNDCLASS_I();
            IntPtr hInstance = UnsafeNativeMethods.GetModuleHandle(null);

            if (!UnsafeNativeMethods.GetClassInfo(new HandleRef(this, hInstance), WndClass.lpszClassName, wndclassi))
            {
                if (UnsafeNativeMethods.RegisterClass(WndClass) == 0)
                {
                    _windowProc = null;
                    Debug.Fail("Unable to register broadcast window class");
                    return IntPtr.Zero;
                }
            }
            else
            {
                //lets double check the wndproc returned by getclassinfo for defwndproc.
                if (wndclassi.lpfnWndProc == DefWndProc)
                {
                    //if we are in there, it means className belongs to an unloaded appdomain.
                    short atom = 0;

                    //try to unregister it.
                    if (0 != UnsafeNativeMethods.UnregisterClass(WndClass.lpszClassName, new HandleRef(null, UnsafeNativeMethods.GetModuleHandle(null))))
                    {
                        atom = UnsafeNativeMethods.RegisterClass(WndClass);
                    }

                    if (atom == 0)
                    {
                        do
                        {
                            BumpQualifier();
                            atom = UnsafeNativeMethods.RegisterClass(WndClass);
                        } while (atom == 0 && Marshal.GetLastWin32Error() == NativeMethods.ERROR_CLASS_ALREADY_EXISTS);
                    }
                }
            }

            // And create an instance of the window.
            //
            IntPtr hwnd = UnsafeNativeMethods.CreateWindowEx(
                                                            0,
                                                            WndClass.lpszClassName,
                                                            WndClass.lpszClassName,
                                                            NativeMethods.WS_POPUP,
                                                            0, 0, 0, 0, NativeMethods.NullHandleRef, NativeMethods.NullHandleRef,
                                                            new HandleRef(this, hInstance), null);
            return hwnd;
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Creates a new window timer asociated with the
        ///       system events window.</para>
        /// </devdoc>
        public static IntPtr CreateTimer(int interval)
        {
            if (interval <= 0)
            {
                throw new ArgumentException(SR.GetString(SR.InvalidLowBoundArgument, "interval", interval.ToString(System.Threading.Thread.CurrentThread.CurrentCulture), "0"));
            }

            EnsureSystemEvents(true, true);
            IntPtr timerId = UnsafeNativeMethods.SendMessage(new HandleRef(s_systemEvents, s_systemEvents._windowHandle),
                                                             NativeMethods.WM_CREATETIMER, (IntPtr)interval, IntPtr.Zero);

            if (timerId == IntPtr.Zero)
            {
                throw new ExternalException(SR.GetString(SR.ErrorCreateTimer));
            }
            return timerId;
        }

        private void Dispose()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                if (s_registeredSessionNotification)
                {
                    UnsafeNativeMethods.WTSUnRegisterSessionNotification(new HandleRef(s_systemEvents, s_systemEvents._windowHandle));
                }

                IntPtr handle = _windowHandle;
                _windowHandle = IntPtr.Zero;

                HandleRef href = new HandleRef(this, handle);

                //we check IsWindow because Application may have rudely destroyed our broadcast window.
                //if this were true, we want to unregister the class.
                if (UnsafeNativeMethods.IsWindow(href) && DefWndProc != IntPtr.Zero)
                {
                    UnsafeNativeMethods.SetWindowLong(href, NativeMethods.GWL_WNDPROC, new HandleRef(this, DefWndProc));

                    //set our sentinel value that we will look for upon initialization to indicate
                    //the window class belongs to an unloaded appdomain and therefore should not be used.
                    UnsafeNativeMethods.SetClassLong(href, NativeMethods.GCL_WNDPROC, DefWndProc);
                }

                // If DestroyWindow failed, it is because we're being
                // shutdown from another thread.  In this case, locate the
                // DefWindowProc call in User32, sling the window back to it,
                // and post a nice fat WM_CLOSE
                //
                if (UnsafeNativeMethods.IsWindow(href) && !UnsafeNativeMethods.DestroyWindow(href))
                {
                    UnsafeNativeMethods.PostMessage(href, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    IntPtr hInstance = UnsafeNativeMethods.GetModuleHandle(null);
                    UnsafeNativeMethods.UnregisterClass(s_className, new HandleRef(this, hInstance));
                }
            }

            if (_consoleHandler != null)
            {
                UnsafeNativeMethods.SetConsoleCtrlHandler(_consoleHandler, 0);
                _consoleHandler = null;
            }
        }

        /// <devdoc>
        ///  Creates the static resources needed by 
        ///  system events.
        /// </devdoc>
        private static void EnsureSystemEvents(bool requireHandle, bool throwOnRefusal)
        {
            // The secondary check here is to detect asp.net.  Asp.net uses multiple
            // app domains to field requests and we do not want to gobble up an 
            // additional thread per domain.  So under this scenario SystemEvents
            // becomes a nop.
            //
            if (s_systemEvents == null)
            {
                lock (s_procLockObject)
                {
                    if (s_systemEvents == null)
                    {
                        if (Thread.GetDomain().GetData(".appDomain") != null)
                        {
                            if (throwOnRefusal)
                            {
                                throw new InvalidOperationException(SR.GetString(SR.ErrorSystemEventsNotSupported));
                            }
                            return;
                        }

                        // If we are creating system events on a thread declared as STA, then
                        // just share the thread.
                        //
                        if (!UserInteractive || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                        {
                            s_systemEvents = new SystemEvents();
                            s_systemEvents.Initialize();
                        }
                        else
                        {
                            s_eventWindowReady = new ManualResetEvent(false);
                            s_systemEvents = new SystemEvents();
                            s_windowThread = new Thread(new ThreadStart(s_systemEvents.WindowThreadProc));
                            s_windowThread.IsBackground = true;
                            s_windowThread.Name = ".NET SystemEvents";
                            s_windowThread.Start();
                            s_eventWindowReady.WaitOne();
                        }

                        if (requireHandle && s_systemEvents._windowHandle == IntPtr.Zero)
                        {
                            // In theory, it's not the end of the world that
                            // we don't get system events.  Unfortunately, the main reason windowHandle == 0
                            // is CreateWindowEx failed for mysterious reasons, and when that happens,
                            // subsequent (and more important) CreateWindowEx calls also fail.
                            throw new ExternalException(SR.GetString(SR.ErrorCreateSystemEvents));
                        }

                        s_startupRecreates = false;
                    }
                }
            }
        }

        private static void EnsureRegisteredSessionNotification()
        {
            if (!s_registeredSessionNotification)
            {
                IntPtr retval = SafeNativeMethods.LoadLibrary(ExternDll.Wtsapi32);

                if (retval != IntPtr.Zero)
                {
                    UnsafeNativeMethods.WTSRegisterSessionNotification(new HandleRef(s_systemEvents, s_systemEvents._windowHandle), NativeMethods.NOTIFY_FOR_THIS_SESSION);
                    s_registeredSessionNotification = true;
                    SafeNativeMethods.FreeLibrary(new HandleRef(null, retval));
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
        private UserPreferenceCategory GetUserPreferenceCategory(int msg, IntPtr wParam, IntPtr lParam)
        {
            UserPreferenceCategory pref = UserPreferenceCategory.General;

            if (msg == NativeMethods.WM_SETTINGCHANGE)
            {
                if (lParam != IntPtr.Zero && Marshal.PtrToStringAuto(lParam).Equals("Policy"))
                {
                    pref = UserPreferenceCategory.Policy;
                }
                else if (lParam != IntPtr.Zero && Marshal.PtrToStringAuto(lParam).Equals("intl"))
                {
                    pref = UserPreferenceCategory.Locale;
                }
                else
                {
                    switch ((int)wParam)
                    {
                        case NativeMethods.SPI_SETACCESSTIMEOUT:
                        case NativeMethods.SPI_SETFILTERKEYS:
                        case NativeMethods.SPI_SETHIGHCONTRAST:
                        case NativeMethods.SPI_SETMOUSEKEYS:
                        case NativeMethods.SPI_SETSCREENREADER:
                        case NativeMethods.SPI_SETSERIALKEYS:
                        case NativeMethods.SPI_SETSHOWSOUNDS:
                        case NativeMethods.SPI_SETSOUNDSENTRY:
                        case NativeMethods.SPI_SETSTICKYKEYS:
                        case NativeMethods.SPI_SETTOGGLEKEYS:
                            pref = UserPreferenceCategory.Accessibility;
                            break;

                        case NativeMethods.SPI_SETDESKWALLPAPER:
                        case NativeMethods.SPI_SETFONTSMOOTHING:
                        case NativeMethods.SPI_SETCURSORS:
                        case NativeMethods.SPI_SETDESKPATTERN:
                        case NativeMethods.SPI_SETGRIDGRANULARITY:
                        case NativeMethods.SPI_SETWORKAREA:
                            pref = UserPreferenceCategory.Desktop;
                            break;

                        case NativeMethods.SPI_ICONHORIZONTALSPACING:
                        case NativeMethods.SPI_ICONVERTICALSPACING:
                        case NativeMethods.SPI_SETICONMETRICS:
                        case NativeMethods.SPI_SETICONS:
                        case NativeMethods.SPI_SETICONTITLELOGFONT:
                        case NativeMethods.SPI_SETICONTITLEWRAP:
                            pref = UserPreferenceCategory.Icon;
                            break;

                        case NativeMethods.SPI_SETDOUBLECLICKTIME:
                        case NativeMethods.SPI_SETDOUBLECLKHEIGHT:
                        case NativeMethods.SPI_SETDOUBLECLKWIDTH:
                        case NativeMethods.SPI_SETMOUSE:
                        case NativeMethods.SPI_SETMOUSEBUTTONSWAP:
                        case NativeMethods.SPI_SETMOUSEHOVERHEIGHT:
                        case NativeMethods.SPI_SETMOUSEHOVERTIME:
                        case NativeMethods.SPI_SETMOUSESPEED:
                        case NativeMethods.SPI_SETMOUSETRAILS:
                        case NativeMethods.SPI_SETSNAPTODEFBUTTON:
                        case NativeMethods.SPI_SETWHEELSCROLLLINES:
                        case NativeMethods.SPI_SETCURSORSHADOW:
                        case NativeMethods.SPI_SETHOTTRACKING:
                        case NativeMethods.SPI_SETTOOLTIPANIMATION:
                        case NativeMethods.SPI_SETTOOLTIPFADE:
                            pref = UserPreferenceCategory.Mouse;
                            break;

                        case NativeMethods.SPI_SETKEYBOARDDELAY:
                        case NativeMethods.SPI_SETKEYBOARDPREF:
                        case NativeMethods.SPI_SETKEYBOARDSPEED:
                        case NativeMethods.SPI_SETLANGTOGGLE:
                            pref = UserPreferenceCategory.Keyboard;
                            break;

                        case NativeMethods.SPI_SETMENUDROPALIGNMENT:
                        case NativeMethods.SPI_SETMENUFADE:
                        case NativeMethods.SPI_SETMENUSHOWDELAY:
                        case NativeMethods.SPI_SETMENUANIMATION:
                        case NativeMethods.SPI_SETSELECTIONFADE:
                            pref = UserPreferenceCategory.Menu;
                            break;

                        case NativeMethods.SPI_SETLOWPOWERACTIVE:
                        case NativeMethods.SPI_SETLOWPOWERTIMEOUT:
                        case NativeMethods.SPI_SETPOWEROFFACTIVE:
                        case NativeMethods.SPI_SETPOWEROFFTIMEOUT:
                            pref = UserPreferenceCategory.Power;
                            break;

                        case NativeMethods.SPI_SETSCREENSAVEACTIVE:
                        case NativeMethods.SPI_SETSCREENSAVERRUNNING:
                        case NativeMethods.SPI_SETSCREENSAVETIMEOUT:
                            pref = UserPreferenceCategory.Screensaver;
                            break;

                        case NativeMethods.SPI_SETKEYBOARDCUES:
                        case NativeMethods.SPI_SETCOMBOBOXANIMATION:
                        case NativeMethods.SPI_SETLISTBOXSMOOTHSCROLLING:
                        case NativeMethods.SPI_SETGRADIENTCAPTIONS:
                        case NativeMethods.SPI_SETUIEFFECTS:
                        case NativeMethods.SPI_SETACTIVEWINDOWTRACKING:
                        case NativeMethods.SPI_SETACTIVEWNDTRKZORDER:
                        case NativeMethods.SPI_SETACTIVEWNDTRKTIMEOUT:
                        case NativeMethods.SPI_SETANIMATION:
                        case NativeMethods.SPI_SETBORDER:
                        case NativeMethods.SPI_SETCARETWIDTH:
                        case NativeMethods.SPI_SETDRAGFULLWINDOWS:
                        case NativeMethods.SPI_SETDRAGHEIGHT:
                        case NativeMethods.SPI_SETDRAGWIDTH:
                        case NativeMethods.SPI_SETFOREGROUNDFLASHCOUNT:
                        case NativeMethods.SPI_SETFOREGROUNDLOCKTIMEOUT:
                        case NativeMethods.SPI_SETMINIMIZEDMETRICS:
                        case NativeMethods.SPI_SETNONCLIENTMETRICS:
                        case NativeMethods.SPI_SETSHOWIMEUI:
                            pref = UserPreferenceCategory.Window;
                            break;
                    }
                }
            }
            else if (msg == NativeMethods.WM_SYSCOLORCHANGE)
            {
                pref = UserPreferenceCategory.Color;
            }
            else
            {
                Debug.Fail("Unrecognized message passed to UserPreferenceCategory");
            }

            return pref;
        }

        private void Initialize()
        {
            _consoleHandler = new NativeMethods.ConHndlr(this.ConsoleHandlerProc);
            if (!UnsafeNativeMethods.SetConsoleCtrlHandler(_consoleHandler, 1))
            {
                Debug.Fail("Failed to install console handler.");
                _consoleHandler = null;
            }

            _windowHandle = CreateBroadcastWindow();
            Debug.Assert(_windowHandle != IntPtr.Zero, "CreateBroadcastWindow failed");

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(SystemEvents.Shutdown);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(SystemEvents.Shutdown);
        }

        /// <devdoc>
        ///     Called on the control's owning thread to perform the actual callback.
        ///     This empties this control's callback queue, propagating any excpetions
        ///     back as needed.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void InvokeMarshaledCallbacks()
        {
            Debug.Assert(s_threadCallbackList != null, "Invoking marshaled callbacks before there are any");

            Delegate current = null;
            lock (s_threadCallbackList)
            {
                if (s_threadCallbackList.Count > 0)
                {
                    current = (Delegate)s_threadCallbackList.Dequeue();
                }
            }

            // Now invoke on all the queued items.
            //
            while (current != null)
            {
                try
                {
                    // Optimize a common case of using EventHandler. This allows us to invoke
                    // early bound, which is a bit more efficient.
                    //
                    EventHandler c = current as EventHandler;
                    if (c != null)
                    {
                        c(null, EventArgs.Empty);
                    }
                    else
                    {
                        current.DynamicInvoke(new object[0]);
                    }
                }
                catch (Exception t)
                {
                    Debug.Fail("SystemEvents marshaled callback failed:" + t);
                }
                lock (s_threadCallbackList)
                {
                    if (s_threadCallbackList.Count > 0)
                    {
                        current = (Delegate)s_threadCallbackList.Dequeue();
                    }
                    else
                    {
                        current = null;
                    }
                }
            }
        }

        /// <devdoc>
        ///     Executes the given delegate on the thread that listens for system events.  Similar to Control.Invoke().
        /// </devdoc>
        public static void InvokeOnEventsThread(Delegate method)
        {
            // This method is really only here for GDI+ initialization/shutdown
            EnsureSystemEvents(true, true);

#if DEBUG
            int pid;
            int thread = SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(s_systemEvents, s_systemEvents._windowHandle), out pid);
            Debug.Assert(s_windowThread == null || thread != SafeNativeMethods.GetCurrentThreadId(), "Don't call MarshaledInvoke on the system events thread");
#endif

            if (s_threadCallbackList == null)
            {
                lock (s_eventLockObject)
                {
                    if (s_threadCallbackList == null)
                    {
                        s_threadCallbackMessage = SafeNativeMethods.RegisterWindowMessage("SystemEventsThreadCallbackMessage");
                        s_threadCallbackList = new Queue();
                    }
                }
            }

            Debug.Assert(s_threadCallbackMessage != 0, "threadCallbackList initialized but threadCallbackMessage not?");

            lock (s_threadCallbackList)
            {
                s_threadCallbackList.Enqueue(method);
            }

            UnsafeNativeMethods.PostMessage(new HandleRef(s_systemEvents, s_systemEvents._windowHandle), s_threadCallbackMessage, IntPtr.Zero, IntPtr.Zero);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Kills the timer specified by the given id.</para>
        /// </devdoc>
        public static void KillTimer(IntPtr timerId)
        {
            EnsureSystemEvents(true, true);
            if (s_systemEvents._windowHandle != IntPtr.Zero)
            {
                int res = (int)UnsafeNativeMethods.SendMessage(new HandleRef(s_systemEvents, s_systemEvents._windowHandle),
                                                                NativeMethods.WM_KILLTIMER, timerId, IntPtr.Zero);

                if (res == 0)
                    throw new ExternalException(SR.GetString(SR.ErrorKillTimer));
            }
        }

        /// <devdoc>
        ///      Callback that handles the create timer
        ///      user message.
        /// </devdoc>
        private IntPtr OnCreateTimer(IntPtr wParam)
        {
            IntPtr timerId = (IntPtr)s_randomTimerId.Next();
            IntPtr res = UnsafeNativeMethods.SetTimer(new HandleRef(this, _windowHandle), new HandleRef(this, timerId), (int)wParam, NativeMethods.NullHandleRef);
            return (res == IntPtr.Zero ? IntPtr.Zero : timerId);
        }

        /// <devdoc>
        ///      Handler that raises the DisplaySettings changing event
        /// </devdoc>
        private void OnDisplaySettingsChanging()
        {
            RaiseEvent(s_onDisplaySettingsChangingEvent, this, EventArgs.Empty);
        }

        /// <devdoc>
        ///      Handler that raises the DisplaySettings changed event
        /// </devdoc>
        private void OnDisplaySettingsChanged()
        {
            RaiseEvent(s_onDisplaySettingsChangedEvent, this, EventArgs.Empty);
        }

        /// <devdoc>
        ///      Handler for any event that fires a standard EventHandler delegate.
        /// </devdoc>
        private void OnGenericEvent(object eventKey)
        {
            RaiseEvent(eventKey, this, EventArgs.Empty);
        }

        private void OnShutdown(object eventKey)
        {
            RaiseEvent(false, eventKey, this, EventArgs.Empty);
        }

        /// <devdoc>
        ///      Callback that handles the KillTimer
        ///      user message.        
        /// </devdoc>
        private bool OnKillTimer(IntPtr wParam)
        {
            bool res = UnsafeNativeMethods.KillTimer(new HandleRef(this, _windowHandle), new HandleRef(this, wParam));
            return res;
        }

        /// <devdoc>
        ///      Handler for WM_POWERBROADCAST.
        /// </devdoc>
        private void OnPowerModeChanged(IntPtr wParam)
        {
            PowerModes mode;

            switch ((int)wParam)
            {
                case NativeMethods.PBT_APMSUSPEND:
                case NativeMethods.PBT_APMSTANDBY:
                    mode = PowerModes.Suspend;
                    break;

                case NativeMethods.PBT_APMRESUMECRITICAL:
                case NativeMethods.PBT_APMRESUMESUSPEND:
                case NativeMethods.PBT_APMRESUMESTANDBY:
                    mode = PowerModes.Resume;
                    break;

                case NativeMethods.PBT_APMBATTERYLOW:
                case NativeMethods.PBT_APMPOWERSTATUSCHANGE:
                case NativeMethods.PBT_APMOEMEVENT:
                    mode = PowerModes.StatusChange;
                    break;

                default:
                    return;
            }

            RaiseEvent(s_onPowerModeChangedEvent, this, new PowerModeChangedEventArgs(mode));
        }

        /// <devdoc>
        ///      Handler for WM_ENDSESSION.
        /// </devdoc>
        private void OnSessionEnded(IntPtr wParam, IntPtr lParam)
        {
            // wParam will be nonzero if the session is actually ending.  If
            // it was canceled then we do not want to raise the event.
            //
            if (wParam != (IntPtr)0)
            {
                SessionEndReasons reason = SessionEndReasons.SystemShutdown;

                if (((unchecked((int)(long)lParam)) & NativeMethods.ENDSESSION_LOGOFF) != 0)
                {
                    reason = SessionEndReasons.Logoff;
                }

                SessionEndedEventArgs endEvt = new SessionEndedEventArgs(reason);

                RaiseEvent(s_onSessionEndedEvent, this, endEvt);
            }
        }

        /// <devdoc>
        ///      Handler for WM_QUERYENDSESSION.
        /// </devdoc>
        private int OnSessionEnding(IntPtr lParam)
        {
            int endOk = 1;

            SessionEndReasons reason = SessionEndReasons.SystemShutdown;

            //Casting to (int) is bad if we're 64-bit; casting to (long) is ok whether we're 64- or 32-bit.
            if ((((long)lParam) & NativeMethods.ENDSESSION_LOGOFF) != 0)
            {
                reason = SessionEndReasons.Logoff;
            }

            SessionEndingEventArgs endEvt = new SessionEndingEventArgs(reason);

            RaiseEvent(s_onSessionEndingEvent, this, endEvt);
            endOk = (endEvt.Cancel ? 0 : 1);

            return endOk;
        }

        private void OnSessionSwitch(int wParam)
        {
            SessionSwitchEventArgs switchEventArgs = new SessionSwitchEventArgs((SessionSwitchReason)wParam);

            RaiseEvent(s_onSessionSwitchEvent, this, switchEventArgs);
        }

        /// <devdoc>
        ///      Handler for WM_THEMECHANGED
        ///      Whidbey note: Before Whidbey, we used to fire UserPreferenceChanged with category
        ///      set to Window. In Whidbey, we support visual styles and need a new category Theme 
        ///      since Window is too general. We fire UserPreferenceChanged with this category, but
        ///      for backward compat, we also fire it with category set to Window.
        /// </devdoc>
        private void OnThemeChanged()
        {
            //we need to fire a changing event handler for Themes.
            //note that it needs to be documented that accessing theme information during the changing event is forbidden.
            RaiseEvent(s_onUserPreferenceChangingEvent, this, new UserPreferenceChangingEventArgs(UserPreferenceCategory.VisualStyle));

            UserPreferenceCategory pref = UserPreferenceCategory.Window;

            RaiseEvent(s_onUserPreferenceChangedEvent, this, new UserPreferenceChangedEventArgs(pref));

            pref = UserPreferenceCategory.VisualStyle;

            RaiseEvent(s_onUserPreferenceChangedEvent, this, new UserPreferenceChangedEventArgs(pref));
        }

        /// <devdoc>
        ///      Handler for WM_SETTINGCHANGE and WM_SYSCOLORCHANGE.
        /// </devdoc>
        private void OnUserPreferenceChanged(int msg, IntPtr wParam, IntPtr lParam)
        {
            UserPreferenceCategory pref = GetUserPreferenceCategory(msg, wParam, lParam);

            RaiseEvent(s_onUserPreferenceChangedEvent, this, new UserPreferenceChangedEventArgs(pref));
        }

        private void OnUserPreferenceChanging(int msg, IntPtr wParam, IntPtr lParam)
        {
            UserPreferenceCategory pref = GetUserPreferenceCategory(msg, wParam, lParam);

            RaiseEvent(s_onUserPreferenceChangingEvent, this, new UserPreferenceChangingEventArgs(pref));
        }

        /// <devdoc>
        ///      Handler for WM_TIMER.
        /// </devdoc>
        private void OnTimerElapsed(IntPtr wParam)
        {
            RaiseEvent(s_onTimerElapsedEvent, this, new TimerElapsedEventArgs(wParam));
        }

        #region EverettThreadAffinity
        //VSWhidbey 470990: we need a backdoor to allow applications to enable the old, broken
        //behavior for SystemEvents, where we fire them on whichever thread they end up on.
        //It's unlikely that someone's depending on this behavior, but we want to avoid a QFE if 
        //they are.  Unfortunately, all of CommonAppDataRegistry's friends are on 
        //System.Windows.Forms.Application, and we can't take a dependency to windows forms from here.
        internal static bool UseEverettThreadAffinity
        {
            get
            {
                if (!s_checkedThreadAffinity)
                {
                    //No point in locking if we don't have to...
                    lock (s_eventLockObject)
                    {
                        //...but now that we have the lock, make sure nobody else just beat us here.
                        if (!s_checkedThreadAffinity)
                        {
                            s_checkedThreadAffinity = true;
                            string template = @"Software\{0}\{1}\{2}";
                            try
                            {
                                //We need access to be able to read from the registry here.  We're not creating a 
                                //registry key, nor are we returning information from the registry to the user.
                                new RegistryPermission(PermissionState.Unrestricted).Assert();
                                RegistryKey key = Registry.LocalMachine.OpenSubKey(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                    template, CompanyNameInternal, ProductNameInternal, ProductVersionInternal));
                                if (key != null)
                                {
                                    object value = key.GetValue(everettThreadAffinityValue);
                                    if (value != null && (int)value != 0)
                                    {
                                        s_useEverettThreadAffinity = true;
                                    }
                                }
                            }
                            catch (SecurityException)
                            {
                                // Can't read the key: use default value (false)
                            }
                            catch (InvalidCastException)
                            {
                                // Key is of wrong type: use default value (false)
                            }
                        }
                    }
                }
                return s_useEverettThreadAffinity;
            }
        }

        private static string CompanyNameInternal
        {
            //No point in caching the value: we're only using it once.
            get
            {
                string companyName = null;
                // custom attribute
                //
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        companyName = ((AssemblyCompanyAttribute)attrs[0]).Company;
                    }
                }

                // win32 version
                //
                if (companyName == null || companyName.Length == 0)
                {
                    companyName = GetAppFileVersionInfo().CompanyName;
                    if (companyName != null)
                    {
                        companyName = companyName.Trim();
                    }
                }

                // fake it with a namespace
                // won't work with MC++ see GetAppMainType.
                if (companyName == null || companyName.Length == 0)
                {
                    Type t = GetAppMainType();

                    if (t != null)
                    {
                        string ns = t.Namespace;

                        if (!string.IsNullOrEmpty(ns))
                        {
                            int firstDot = ns.IndexOf(".", StringComparison.Ordinal);
                            if (firstDot != -1)
                            {
                                companyName = ns.Substring(0, firstDot);
                            }
                            else
                            {
                                companyName = ns;
                            }
                        }
                        else
                        {
                            // last ditch... no namespace, use product name...
                            //
                            companyName = ProductNameInternal;
                        }
                    }
                }
                return companyName;
            }
        }

        private static string ProductNameInternal
        {
            get
            {
                string productName = null;
                // custom attribute
                //
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        productName = ((AssemblyProductAttribute)attrs[0]).Product;
                    }
                }

                // win32 version info
                //
                if (productName == null || productName.Length == 0)
                {
                    productName = GetAppFileVersionInfo().ProductName;
                    if (productName != null)
                    {
                        productName = productName.Trim();
                    }
                }

                // fake it with namespace
                // won't work with MC++ see GetAppMainType.
                if (productName == null || productName.Length == 0)
                {
                    Type t = GetAppMainType();

                    if (t != null)
                    {
                        string ns = t.Namespace;

                        if (!string.IsNullOrEmpty(ns))
                        {
                            int lastDot = ns.LastIndexOf(".", StringComparison.Ordinal);
                            if (lastDot != -1 && lastDot < ns.Length - 1)
                            {
                                productName = ns.Substring(lastDot + 1);
                            }
                            else
                            {
                                productName = ns;
                            }
                        }
                        else
                        {
                            // last ditch... use the main type
                            //
                            productName = t.Name;
                        }
                    }
                }
                return productName;
            }
        }

        private static string ProductVersionInternal
        {
            get
            {
                string productVersion = null;
                // custom attribute
                //
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        productVersion = ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion;
                    }
                }

                // win32 version info
                //
                if (productVersion == null || productVersion.Length == 0)
                {
                    productVersion = GetAppFileVersionInfo().ProductVersion;
                    if (productVersion != null)
                    {
                        productVersion = productVersion.Trim();
                    }
                }

                // fake it
                //
                if (productVersion == null || productVersion.Length == 0)
                {
                    productVersion = "1.0.0.0";
                }
                return productVersion;
            }
        }

        private static volatile object s_appFileVersion;
        private static FileVersionInfo GetAppFileVersionInfo()
        {
            if (s_appFileVersion == null)
            {
                Type t = GetAppMainType();
                if (t != null)
                {
                    // SECREVIEW : This Assert is ok, getting the module's version is a safe operation, 
                    //             the result is provided by the system.
                    //
                    FileIOPermission fiop = new FileIOPermission(PermissionState.None);
                    fiop.AllFiles = FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read;
                    fiop.Assert();

                    try
                    {
                        s_appFileVersion = FileVersionInfo.GetVersionInfo(t.Module.FullyQualifiedName);
                    }
                    finally
                    {
                        CodeAccessPermission.RevertAssert();
                    }
                }
                else
                {
                    s_appFileVersion = FileVersionInfo.GetVersionInfo(ExecutablePath);
                }
            }

            return (FileVersionInfo)s_appFileVersion;
        }

        /// <include file='doc\Application.uex' path='docs/doc[@for="Application.GetAppMainType"]/*' />
        /// <devdoc>
        ///     Retrieves the Type that contains the "Main" method.
        /// </devdoc>
        private static volatile Type s_mainType;
        private static Type GetAppMainType()
        {
            if (s_mainType == null)
            {
                Assembly exe = Assembly.GetEntryAssembly();

                // Get Main type...This doesn't work in MC++ because Main is a global function and not
                // a class static method (it doesn't belong to a Type).
                if (exe != null)
                {
                    s_mainType = exe.EntryPoint.ReflectedType;
                }
            }

            return s_mainType;
        }

        private static volatile string s_executablePath = null;
        private static string ExecutablePath
        {
            [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
            get
            {
                if (s_executablePath == null)
                {
                    Assembly asm = Assembly.GetEntryAssembly();
                    if (asm == null)
                    {
                        StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH);
                        UnsafeNativeMethods.GetModuleFileName(NativeMethods.NullHandleRef, sb, sb.Capacity);

                        s_executablePath = IntSecurity.UnsafeGetFullPath(sb.ToString());
                    }
                    else
                    {
                        String ecb = asm.EscapedCodeBase;
                        Uri codeBase = new Uri(ecb);
                        if (codeBase.Scheme == "file")
                        {
                            s_executablePath = NativeMethods.GetLocalPath(ecb);
                        }
                        else
                        {
                            s_executablePath = codeBase.ToString();
                        }
                    }
                }
                Uri exeUri = new Uri(s_executablePath);
                if (exeUri.Scheme == "file")
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, s_executablePath).Demand();
                }
                return s_executablePath;
            }
        }
        #endregion

        private static void RaiseEvent(object key, params object[] args)
        {
            RaiseEvent(true, key, args);
        }

        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void RaiseEvent(bool checkFinalization, object key, params object[] args)
        {
            //If the AppDomain's unloading, we shouldn't fire SystemEvents other than Shutdown.
            if (checkFinalization && AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                return;
            }

            SystemEventInvokeInfo[] invokeItemArray = null;

            lock (s_eventLockObject)
            {
                if (s_handlers != null && s_handlers.ContainsKey(key))
                {
                    List<SystemEventInvokeInfo> invokeItems = s_handlers[key];

                    // clone the list so we don't have this type locked and cause
                    // a deadlock if someone tries to modify handlers during an invoke.
                    //
                    if (invokeItems != null)
                    {
                        invokeItemArray = invokeItems.ToArray();
                    }
                }
            }

            if (invokeItemArray != null)
            {
                for (int i = 0; i < invokeItemArray.Length; i++)
                {
                    try
                    {
                        SystemEventInvokeInfo info = invokeItemArray[i];
                        info.Invoke(checkFinalization, args);
                        invokeItemArray[i] = null; // clear it if it's valid
                    }
                    catch (Exception)
                    {
                        //Eat exceptions (Everett compat)
                    }
                }

                // clean out any that are dead.
                //
                lock (s_eventLockObject)
                {
                    List<SystemEventInvokeInfo> invokeItems = null;

                    for (int i = 0; i < invokeItemArray.Length; i++)
                    {
                        SystemEventInvokeInfo info = invokeItemArray[i];
                        if (info != null)
                        {
                            if (invokeItems == null)
                            {
                                if (!s_handlers.TryGetValue(key, out invokeItems))
                                {
                                    // weird.  just to be safe.
                                    //
                                    return;
                                }
                            }

                            invokeItems.Remove(info);
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
        private static void RemoveEventHandler(object key, Delegate value)
        {
            lock (s_eventLockObject)
            {
                if (s_handlers != null && s_handlers.ContainsKey(key))
                {
                    List<SystemEventInvokeInfo> invokeItems = (List<SystemEventInvokeInfo>)s_handlers[key];

                    invokeItems.Remove(new SystemEventInvokeInfo(value));
                }
            }
        }

        /// <devdoc>
        ///     This method is invoked via reflection from windows forms.  Why?  Because when the runtime is hosted in IE,
        ///     IE doesn't tell it when to shut down.  The first notification the runtime gets is 
        ///     DLL_PROCESS_DETACH, at which point it is too late for us to run any managed code.  But,
        ///     if we don't destroy our system events window the HWND will fault if it
        ///     receives a message after the runtime shuts down.  So it is imparative that
        ///     we destroy the window, but it is also necessary to recreate the window on demand.
        ///     That's hard to do, because we originally created it in response to an event
        ///     wire-up, but that event is still bound so technically we should still have the
        ///     window around.  To work around this crashing fiasco, we have special code
        ///     in the ActiveXImpl class within Control.  This code checks to see if it is running
        ///     inside of IE, and if so, it will invoke these methods via private reflection.
        ///     It will invoke Shutdown when the last active X control is destroyed, and then
        ///     call Startup with the first activeX control is recreated.  
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void Startup()
        {
            if (s_startupRecreates)
            {
                EnsureSystemEvents(false, false);
            }
        }

        /// <devdoc>
        ///     This method is invoked via reflection from windows forms.  Why?  Because when the runtime is hosted in IE,
        ///     IE doesn't tell it when to shut down.  The first notification the runtime gets is 
        ///     DLL_PROCESS_DETACH, at which point it is too late for us to run any managed code.  But,
        ///     if we don't destroy our system events window the HWND will fault if it
        ///     receives a message after the runtime shuts down.  So it is imparative that
        ///     we destroy the window, but it is also necessary to recreate the window on demand.
        ///     That's hard to do, because we originally created it in response to an event
        ///     wire-up, but that event is still bound so technically we should still have the
        ///     window around.  To work around this crashing fiasco, we have special code
        ///     in the ActiveXImpl class within Control.  This code checks to see if it is running
        ///     inside of IE, and if so, it will invoke these methods via private reflection.
        ///     It will invoke Shutdown when the last active X control is destroyed, and then
        ///     call Startup with the first activeX control is recreated.  
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void Shutdown()
        {
            if (s_systemEvents != null && s_systemEvents._windowHandle != IntPtr.Zero)
            {
                lock (s_procLockObject)
                {
                    if (s_systemEvents != null)
                    {
                        s_startupRecreates = true;

                        // If we are using system events from another thread, request that it terminate
                        //
                        if (s_windowThread != null)
                        {
                            s_eventThreadTerminated = new ManualResetEvent(false);

#if DEBUG
                            int pid;
                            int thread = SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(s_systemEvents, s_systemEvents._windowHandle), out pid);
                            Debug.Assert(thread != SafeNativeMethods.GetCurrentThreadId(), "Don't call Shutdown on the system events thread");
#endif
                            UnsafeNativeMethods.PostMessage(new HandleRef(s_systemEvents, s_systemEvents._windowHandle), NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);

                            s_eventThreadTerminated.WaitOne();
                            s_windowThread.Join(); //avoids an AppDomainUnloaded exception on our background thread.
                        }
                        else
                        {
                            s_systemEvents.Dispose();
                            s_systemEvents = null;
                        }
                    }
                }
            }
        }

        [PrePrepareMethod]
        private static void Shutdown(object sender, EventArgs e)
        {
            Shutdown();
        }

        /// <devdoc>
        ///      A standard Win32 window proc for our broadcast window.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeMethods.WM_SETTINGCHANGE:
                    string newString;
                    IntPtr newStringPtr = lParam;
                    if (lParam != IntPtr.Zero)
                    {
                        newString = Marshal.PtrToStringAuto(lParam);
                        if (newString != null)
                        {
                            newStringPtr = Marshal.StringToHGlobalAuto(newString);
                        }
                    }
                    UnsafeNativeMethods.PostMessage(new HandleRef(this, _windowHandle), NativeMethods.WM_REFLECT + msg, wParam, newStringPtr);
                    break;
                case NativeMethods.WM_WTSSESSION_CHANGE:
                    OnSessionSwitch((int)wParam);
                    break;
                case NativeMethods.WM_SYSCOLORCHANGE:
                case NativeMethods.WM_COMPACTING:
                case NativeMethods.WM_DISPLAYCHANGE:
                case NativeMethods.WM_FONTCHANGE:
                case NativeMethods.WM_PALETTECHANGED:
                case NativeMethods.WM_TIMECHANGE:
                case NativeMethods.WM_TIMER:
                case NativeMethods.WM_THEMECHANGED:
                    UnsafeNativeMethods.PostMessage(new HandleRef(this, _windowHandle), NativeMethods.WM_REFLECT + msg, wParam, lParam);
                    break;

                case NativeMethods.WM_CREATETIMER:
                    return OnCreateTimer(wParam);

                case NativeMethods.WM_KILLTIMER:
                    return (IntPtr)(OnKillTimer(wParam) ? 1 : 0);

                case NativeMethods.WM_REFLECT + NativeMethods.WM_SETTINGCHANGE:
                    try
                    {
                        OnUserPreferenceChanging(msg - NativeMethods.WM_REFLECT, wParam, lParam);
                        OnUserPreferenceChanged(msg - NativeMethods.WM_REFLECT, wParam, lParam);
                    }
                    finally
                    {
                        try
                        {
                            if (lParam != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(lParam);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Assert(false, "Exception occurred while freeing memory: " + e.ToString());
                        }
                    }
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_SYSCOLORCHANGE:
                    OnUserPreferenceChanging(msg - NativeMethods.WM_REFLECT, wParam, lParam);
                    OnUserPreferenceChanged(msg - NativeMethods.WM_REFLECT, wParam, lParam);
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_THEMECHANGED:
                    OnThemeChanged();
                    break;

                case NativeMethods.WM_QUERYENDSESSION:
                    return (IntPtr)OnSessionEnding(lParam);

                case NativeMethods.WM_ENDSESSION:
                    OnSessionEnded(wParam, lParam);
                    break;

                case NativeMethods.WM_POWERBROADCAST:
                    OnPowerModeChanged(wParam);
                    break;

                // WM_HIBERNATE on WinCE
                case NativeMethods.WM_REFLECT + NativeMethods.WM_COMPACTING:
                    OnGenericEvent(s_onLowMemoryEvent);
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_DISPLAYCHANGE:
                    OnDisplaySettingsChanging();
                    OnDisplaySettingsChanged();
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_FONTCHANGE:
                    OnGenericEvent(s_onInstalledFontsChangedEvent);
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_PALETTECHANGED:
                    OnGenericEvent(s_onPaletteChangedEvent);
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_TIMECHANGE:
                    OnGenericEvent(s_onTimeChangedEvent);
                    break;

                case NativeMethods.WM_REFLECT + NativeMethods.WM_TIMER:
                    OnTimerElapsed(wParam);
                    break;

                default:
                    // If we received a thread execute message, then execute it.
                    //
                    if (msg == s_threadCallbackMessage && msg != 0)
                    {
                        InvokeMarshaledCallbacks();
                        return IntPtr.Zero;
                    }
                    break;
            }

            return UnsafeNativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <devdoc>
        ///      This is the method that runs our window thread.  This method
        ///      creates a window and spins up a message loop.  The window
        ///      is made visible with a size of 0, 0, so that it will trap
        ///      global broadcast messages.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void WindowThreadProc()
        {
            try
            {
                Initialize();
                s_eventWindowReady.Set();

                if (_windowHandle != IntPtr.Zero)
                {
                    NativeMethods.MSG msg = new NativeMethods.MSG();

                    bool keepRunning = true;

                    // Blocking on a GetMessage() call prevents the EE from being able to unwind
                    // this thread properly (e.g. during AppDomainUnload). So, we use PeekMessage()
                    // and sleep so we always block in managed code instead.
                    //
                    while (keepRunning)
                    {
                        int ret = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 100, NativeMethods.QS_ALLINPUT, NativeMethods.MWMO_INPUTAVAILABLE);

                        if (ret == NativeMethods.WAIT_TIMEOUT)
                        {
                            Thread.Sleep(1);
                        }
                        else
                        {
                            while (UnsafeNativeMethods.PeekMessage(ref msg, NativeMethods.NullHandleRef, 0, 0, NativeMethods.PM_REMOVE))
                            {
                                if (msg.message == NativeMethods.WM_QUIT)
                                {
                                    keepRunning = false;
                                    break;
                                }

                                UnsafeNativeMethods.TranslateMessage(ref msg);
                                UnsafeNativeMethods.DispatchMessage(ref msg);
                            }
                        }
                    }
                }

                OnShutdown(s_onEventsThreadShutdownEvent);
            }
            catch (Exception e)
            {
                // In case something very very wrong happend during the creation action.
                // This will unblock the calling thread.
                //
                s_eventWindowReady.Set();

                if (!((e is ThreadInterruptedException) || (e is ThreadAbortException)))
                {
                    Debug.Fail("Unexpected thread exception in system events window thread proc", e.ToString());
                }
            }

            Dispose();
            if (s_eventThreadTerminated != null)
            {
                s_eventThreadTerminated.Set();
            }
        }

        // A class that helps fire events on the right thread.
        //
        private class SystemEventInvokeInfo
        {
            private SynchronizationContext _syncContext; // the context that we'll use to fire against.
            private Delegate _delegate;     // the delegate we'll fire.  This is a weak ref so we don't hold object in memory.
            public SystemEventInvokeInfo(Delegate d)
            {
                _delegate = d;
                _syncContext = AsyncOperationManager.SynchronizationContext;
            }

            // fire the given event with the given params.
            //
            public void Invoke(bool checkFinalization, params object[] args)
            {
                try
                {
                    // If we didn't get call back, or if we're using Everett threading, invoke directly.
                    //
                    if (_syncContext == null || SystemEvents.UseEverettThreadAffinity)
                    {
                        InvokeCallback(args);
                    }
                    else
                    {
                        // otherwise tell the context to do it for us.
                        //
                        _syncContext.Send(new SendOrPostCallback(InvokeCallback), args);
                    }
                }
                catch (InvalidAsynchronousStateException)
                {
                    //if the synch context is invalid -- do the invoke directly for app compat.
                    //If the app's shutting down, don't fire the event (unless it's shutdown).
                    if (!checkFinalization || !AppDomain.CurrentDomain.IsFinalizingForUnload())
                    {
                        InvokeCallback(args);
                    }
                }
            }

            // our delegate method that the SyncContext will call on.
            //
            private void InvokeCallback(object arg)
            {
                _delegate.DynamicInvoke((object[])arg);
            }

            public override bool Equals(object other)
            {
                SystemEventInvokeInfo otherInvoke = other as SystemEventInvokeInfo;

                if (otherInvoke == null)
                {
                    return false;
                }
                return otherInvoke._delegate.Equals(_delegate);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
