// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32
{
    /// <summary>
    ///  Provides a set of global system events to callers.
    /// </summary>
    public sealed class SystemEvents
    {
        // Almost all of our data is static.  We keep a single instance of
        // SystemEvents around so we can bind delegates to it.
        // Non-static methods in this class will only be called through
        // one of the delegates.
        private static readonly object s_eventLockObject = new object();
        private static readonly object s_procLockObject = new object();
        private static volatile SystemEvents? s_systemEvents;
        private static volatile Thread? s_windowThread;
        private static volatile ManualResetEvent? s_eventWindowReady;
        private static readonly Random s_randomTimerId = new Random();
        private static volatile bool s_registeredSessionNotification;
        private static volatile IntPtr s_defWindowProc;

        private static volatile string? s_className;

        // cross-thread marshaling
        private static volatile Queue<Delegate>? s_threadCallbackList; // list of Delegates
        private static volatile int s_threadCallbackMessage;

        // Per-instance data that is isolated to the window thread.
        private volatile IntPtr _windowHandle;
        private Interop.User32.WndProc? _windowProc;
        private Interop.Kernel32.ConsoleCtrlHandlerRoutine? _consoleHandler;

        // The set of events we respond to.
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
        // match a delegate with a SynchronizationContext so we can fire on the proper thread.
        private static Dictionary<object, List<SystemEventInvokeInfo>>? s_handlers;

        private SystemEvents()
        {
            // This class is intended to be static, but predates static classes (which were introduced in C# 2.0).
        }

        // stole from SystemInformation... if we get SystemInformation moved
        // to somewhere that we can use it... rip this!
        private static volatile IntPtr s_processWinStation = IntPtr.Zero;
        private static volatile bool s_isUserInteractive;
        private static unsafe bool UserInteractive
        {
            get
            {
                IntPtr hwinsta = Interop.User32.GetProcessWindowStation();
                if (hwinsta != IntPtr.Zero && s_processWinStation != hwinsta)
                {
                    s_isUserInteractive = true;

                    uint dummy = 0;
                    Interop.User32.USEROBJECTFLAGS flags = default;

                    if (Interop.User32.GetUserObjectInformationW(hwinsta, Interop.User32.UOI_FLAGS, &flags, (uint)sizeof(Interop.User32.USEROBJECTFLAGS), ref dummy))
                    {
                        if ((flags.dwFlags & Interop.User32.WSF_VISIBLE) == 0)
                        {
                            s_isUserInteractive = false;
                        }
                    }
                    s_processWinStation = hwinsta;
                }

                return s_isUserInteractive;
            }
        }

        /// <summary>
        ///  Occurs when the display settings are changing.
        /// </summary>
        public static event EventHandler? DisplaySettingsChanging
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

        /// <summary>
        ///  Occurs when the user changes the display settings.
        /// </summary>
        public static event EventHandler? DisplaySettingsChanged
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

        /// <summary>
        ///  Occurs before the thread that listens for system events is terminated.
        ///  Delegates will be invoked on the events thread.
        /// </summary>
        public static event EventHandler? EventsThreadShutdown
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

        /// <summary>
        ///  Occurs when the user adds fonts to or removes fonts from the system.
        /// </summary>
        public static event EventHandler? InstalledFontsChanged
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

        /// <summary>
        ///  Occurs when the system is running out of available RAM.
        /// </summary>
        [Obsolete("The LowMemory event has been deprecated and is not supported.")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static event EventHandler? LowMemory
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onLowMemoryEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onLowMemoryEvent, value);
            }
        }

        /// <summary>
        ///  Occurs when the user switches to an application that uses a different
        ///  palette.
        /// </summary>
        public static event EventHandler? PaletteChanged
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


        /// <summary>
        ///  Occurs when the user suspends or resumes the system.
        /// </summary>
        public static event PowerModeChangedEventHandler? PowerModeChanged
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onPowerModeChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onPowerModeChangedEvent, value);
            }
        }

        /// <summary>
        ///  Occurs when the user is logging off or shutting down the system.
        /// </summary>
        public static event SessionEndedEventHandler? SessionEnded
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onSessionEndedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionEndedEvent, value);
            }
        }

        /// <summary>
        ///  Occurs when the user is trying to log off or shutdown the system.
        /// </summary>
        public static event SessionEndingEventHandler? SessionEnding
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onSessionEndingEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionEndingEvent, value);
            }
        }

        /// <summary>
        ///  Occurs when a user session switches.
        /// </summary>
        public static event SessionSwitchEventHandler? SessionSwitch
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                EnsureRegisteredSessionNotification();
                AddEventHandler(s_onSessionSwitchEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onSessionSwitchEvent, value);
            }
        }

        /// <summary>
        ///   Occurs when the user changes the time on the system clock.
        /// </summary>
        public static event EventHandler? TimeChanged
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onTimeChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onTimeChangedEvent, value);
            }
        }

        /// <summary>
        ///  Occurs when a windows timer interval has expired.
        /// </summary>
        public static event TimerElapsedEventHandler? TimerElapsed
        {
            add
            {
                EnsureSystemEvents(requireHandle: true);
                AddEventHandler(s_onTimerElapsedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onTimerElapsedEvent, value);
            }
        }


        /// <summary>
        ///  Occurs when a user preference has changed.
        /// </summary>
        public static event UserPreferenceChangedEventHandler? UserPreferenceChanged
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

        /// <summary>
        ///  Occurs when a user preference is changing.
        /// </summary>
        public static event UserPreferenceChangingEventHandler? UserPreferenceChanging
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

        private static void AddEventHandler(object key, Delegate? value)
        {
            if (value is null)
            {
                return;
            }

            lock (s_eventLockObject)
            {
                if (s_handlers == null)
                {
                    s_handlers = new Dictionary<object, List<SystemEventInvokeInfo>>();
                    EnsureSystemEvents(requireHandle: false);
                }

                if (!s_handlers.TryGetValue(key, out List<SystemEventInvokeInfo>? invokeItems))
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

        /// <summary>
        ///  Console handler we add in case we are a console application or a service.
        ///  Without this we will not get end session events.
        /// </summary>
        private bool ConsoleHandlerProc(int signalType)
        {
            switch (signalType)
            {
                case Interop.User32.CTRL_LOGOFF_EVENT:
                    OnSessionEnded((IntPtr)1, (IntPtr)Interop.User32.ENDSESSION_LOGOFF);
                    break;

                case Interop.User32.CTRL_SHUTDOWN_EVENT:
                    OnSessionEnded((IntPtr)1, (IntPtr)0);
                    break;
            }

            return false;
        }

        private static IntPtr DefWndProc
        {
            get
            {
                if (s_defWindowProc == IntPtr.Zero)
                {
                    s_defWindowProc = Interop.Kernel32.GetProcAddress(Interop.Kernel32.GetModuleHandle("user32.dll"), "DefWindowProcW");
                }
                return s_defWindowProc;
            }
        }

        /// <summary>
        ///  Creates a new window timer associated with the system events window.
        /// </summary>
        public static IntPtr CreateTimer(int interval)
        {
            if (interval <= 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgument, nameof(interval), interval.ToString(System.Threading.Thread.CurrentThread.CurrentCulture), "0"));
            }

            EnsureSystemEvents(requireHandle: true);
            IntPtr timerId = Interop.User32.SendMessageW(s_systemEvents!._windowHandle,
                                                        Interop.User32.WM_CREATETIMER, (IntPtr)interval, IntPtr.Zero);
            GC.KeepAlive(s_systemEvents);

            if (timerId == IntPtr.Zero)
            {
                throw new ExternalException(SR.ErrorCreateTimer);
            }
            return timerId;
        }

        private void Dispose()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                if (s_registeredSessionNotification)
                {
                    Interop.Wtsapi32.WTSUnRegisterSessionNotification(s_systemEvents!._windowHandle);
                    GC.KeepAlive(s_systemEvents);
                }

                IntPtr handle = _windowHandle;
                _windowHandle = IntPtr.Zero;

                // We check IsWindow because our broadcast window may have been destroyed.

                if (Interop.User32.IsWindow(handle) && DefWndProc != IntPtr.Zero)
                {
                    // We used to use this as a sentinel to identify window classes we created on
                    // other appdomains. We still want to set to the default WNDPROC to prevent
                    // messages coming back to managed code if our callback gets collected.

                    if (IntPtr.Size == 4)
                    {
                        // In a 32-bit process we must call the non-'ptr' version of these APIs
                        Interop.User32.SetWindowLongW(handle, Interop.User32.GWL_WNDPROC, DefWndProc);
                        Interop.User32.SetClassLongW(handle, Interop.User32.GCL_WNDPROC, DefWndProc);
                    }
                    else
                    {
                        Interop.User32.SetWindowLongPtrW(handle, Interop.User32.GWL_WNDPROC, DefWndProc);
                        Interop.User32.SetClassLongPtrW(handle, Interop.User32.GCL_WNDPROC, DefWndProc);
                    }
                }

                if (Interop.User32.IsWindow(handle) && !Interop.User32.DestroyWindow(handle))
                {
                    // We may not have been able to destroy the window if we're shutdown from another thread.
                    // Attempt to close the window by posting a WM_CLOSE message instead. (Messages always
                    // fire on the same thread.)
                    Interop.User32.PostMessageW(handle, Interop.User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    IntPtr hInstance = Interop.Kernel32.GetModuleHandle(null);
                    Interop.User32.UnregisterClassW(s_className!, hInstance);
                }
            }

            if (_consoleHandler != null)
            {
                Interop.Kernel32.SetConsoleCtrlHandler(_consoleHandler, false);
                _consoleHandler = null;
            }
        }

        /// <summary>
        ///  Creates the static resources needed by system events.
        /// </summary>
        private static void EnsureSystemEvents(bool requireHandle)
        {
            if (s_systemEvents == null)
            {
                lock (s_procLockObject)
                {
                    if (s_systemEvents == null)
                    {
                        // Create a new pumping thread.  We always create one even if the current thread
                        // is STA, as there are no guarantees this thread will pump nor still be alive
                        // for the desired duration.

                        s_eventWindowReady = new ManualResetEvent(false);
                        SystemEvents systemEvents = new SystemEvents();
                        s_windowThread = new Thread(new ThreadStart(systemEvents.WindowThreadProc))
                        {
                            IsBackground = true,
                            Name = ".NET System Events"
                        };
                        s_windowThread.Start();
                        s_eventWindowReady.WaitOne();

                        // ensure this is initialized last as that will force concurrent threads calling
                        // this method to block until after we've initialized.
                        s_systemEvents = systemEvents;

                        if (requireHandle && s_systemEvents._windowHandle == IntPtr.Zero)
                        {
                            // In theory, it's not the end of the world that
                            // we don't get system events.  Unfortunately, the main reason windowHandle == 0
                            // is CreateWindowEx failed for mysterious reasons, and when that happens,
                            // subsequent (and more important) CreateWindowEx calls also fail.
                            throw new ExternalException(SR.ErrorCreateSystemEvents);
                        }
                    }
                }
            }
        }

        private static void EnsureRegisteredSessionNotification()
        {
            if (!s_registeredSessionNotification)
            {
                IntPtr retval = Interop.Kernel32.LoadLibrary(Interop.Libraries.Wtsapi32);

                if (retval != IntPtr.Zero)
                {
                    Interop.Wtsapi32.WTSRegisterSessionNotification(s_systemEvents!._windowHandle, Interop.Wtsapi32.NOTIFY_FOR_THIS_SESSION);
                    GC.KeepAlive(s_systemEvents);
                    s_registeredSessionNotification = true;
                    Interop.Kernel32.FreeLibrary(retval);
                }
            }
        }

        private static UserPreferenceCategory GetUserPreferenceCategory(int msg, IntPtr wParam, IntPtr lParam)
        {
            UserPreferenceCategory pref = UserPreferenceCategory.General;

            if (msg == Interop.User32.WM_SETTINGCHANGE)
            {
                if (lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam)!.Equals("Policy"))
                {
                    pref = UserPreferenceCategory.Policy;
                }
                else if (lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam)!.Equals("intl"))
                {
                    pref = UserPreferenceCategory.Locale;
                }
                else
                {
                    switch ((int)wParam)
                    {
                        case Interop.User32.SPI_SETACCESSTIMEOUT:
                        case Interop.User32.SPI_SETFILTERKEYS:
                        case Interop.User32.SPI_SETHIGHCONTRAST:
                        case Interop.User32.SPI_SETMOUSEKEYS:
                        case Interop.User32.SPI_SETSCREENREADER:
                        case Interop.User32.SPI_SETSERIALKEYS:
                        case Interop.User32.SPI_SETSHOWSOUNDS:
                        case Interop.User32.SPI_SETSOUNDSENTRY:
                        case Interop.User32.SPI_SETSTICKYKEYS:
                        case Interop.User32.SPI_SETTOGGLEKEYS:
                            pref = UserPreferenceCategory.Accessibility;
                            break;

                        case Interop.User32.SPI_SETDESKWALLPAPER:
                        case Interop.User32.SPI_SETFONTSMOOTHING:
                        case Interop.User32.SPI_SETCURSORS:
                        case Interop.User32.SPI_SETDESKPATTERN:
                        case Interop.User32.SPI_SETGRIDGRANULARITY:
                        case Interop.User32.SPI_SETWORKAREA:
                            pref = UserPreferenceCategory.Desktop;
                            break;

                        case Interop.User32.SPI_ICONHORIZONTALSPACING:
                        case Interop.User32.SPI_ICONVERTICALSPACING:
                        case Interop.User32.SPI_SETICONMETRICS:
                        case Interop.User32.SPI_SETICONS:
                        case Interop.User32.SPI_SETICONTITLELOGFONT:
                        case Interop.User32.SPI_SETICONTITLEWRAP:
                            pref = UserPreferenceCategory.Icon;
                            break;

                        case Interop.User32.SPI_SETDOUBLECLICKTIME:
                        case Interop.User32.SPI_SETDOUBLECLKHEIGHT:
                        case Interop.User32.SPI_SETDOUBLECLKWIDTH:
                        case Interop.User32.SPI_SETMOUSE:
                        case Interop.User32.SPI_SETMOUSEBUTTONSWAP:
                        case Interop.User32.SPI_SETMOUSEHOVERHEIGHT:
                        case Interop.User32.SPI_SETMOUSEHOVERTIME:
                        case Interop.User32.SPI_SETMOUSESPEED:
                        case Interop.User32.SPI_SETMOUSETRAILS:
                        case Interop.User32.SPI_SETSNAPTODEFBUTTON:
                        case Interop.User32.SPI_SETWHEELSCROLLLINES:
                        case Interop.User32.SPI_SETCURSORSHADOW:
                        case Interop.User32.SPI_SETHOTTRACKING:
                        case Interop.User32.SPI_SETTOOLTIPANIMATION:
                        case Interop.User32.SPI_SETTOOLTIPFADE:
                            pref = UserPreferenceCategory.Mouse;
                            break;

                        case Interop.User32.SPI_SETKEYBOARDDELAY:
                        case Interop.User32.SPI_SETKEYBOARDPREF:
                        case Interop.User32.SPI_SETKEYBOARDSPEED:
                        case Interop.User32.SPI_SETLANGTOGGLE:
                            pref = UserPreferenceCategory.Keyboard;
                            break;

                        case Interop.User32.SPI_SETMENUDROPALIGNMENT:
                        case Interop.User32.SPI_SETMENUFADE:
                        case Interop.User32.SPI_SETMENUSHOWDELAY:
                        case Interop.User32.SPI_SETMENUANIMATION:
                        case Interop.User32.SPI_SETSELECTIONFADE:
                            pref = UserPreferenceCategory.Menu;
                            break;

                        case Interop.User32.SPI_SETLOWPOWERACTIVE:
                        case Interop.User32.SPI_SETLOWPOWERTIMEOUT:
                        case Interop.User32.SPI_SETPOWEROFFACTIVE:
                        case Interop.User32.SPI_SETPOWEROFFTIMEOUT:
                            pref = UserPreferenceCategory.Power;
                            break;

                        case Interop.User32.SPI_SETSCREENSAVEACTIVE:
                        case Interop.User32.SPI_SETSCREENSAVERRUNNING:
                        case Interop.User32.SPI_SETSCREENSAVETIMEOUT:
                            pref = UserPreferenceCategory.Screensaver;
                            break;

                        case Interop.User32.SPI_SETKEYBOARDCUES:
                        case Interop.User32.SPI_SETCOMBOBOXANIMATION:
                        case Interop.User32.SPI_SETLISTBOXSMOOTHSCROLLING:
                        case Interop.User32.SPI_SETGRADIENTCAPTIONS:
                        case Interop.User32.SPI_SETUIEFFECTS:
                        case Interop.User32.SPI_SETACTIVEWINDOWTRACKING:
                        case Interop.User32.SPI_SETACTIVEWNDTRKZORDER:
                        case Interop.User32.SPI_SETACTIVEWNDTRKTIMEOUT:
                        case Interop.User32.SPI_SETANIMATION:
                        case Interop.User32.SPI_SETBORDER:
                        case Interop.User32.SPI_SETCARETWIDTH:
                        case Interop.User32.SPI_SETDRAGFULLWINDOWS:
                        case Interop.User32.SPI_SETDRAGHEIGHT:
                        case Interop.User32.SPI_SETDRAGWIDTH:
                        case Interop.User32.SPI_SETFOREGROUNDFLASHCOUNT:
                        case Interop.User32.SPI_SETFOREGROUNDLOCKTIMEOUT:
                        case Interop.User32.SPI_SETMINIMIZEDMETRICS:
                        case Interop.User32.SPI_SETNONCLIENTMETRICS:
                        case Interop.User32.SPI_SETSHOWIMEUI:
                            pref = UserPreferenceCategory.Window;
                            break;
                    }
                }
            }
            else if (msg == Interop.User32.WM_SYSCOLORCHANGE)
            {
                pref = UserPreferenceCategory.Color;
            }
            else
            {
                Debug.Fail("Unrecognized message passed to UserPreferenceCategory");
            }

            return pref;
        }

        private unsafe void Initialize()
        {
            _consoleHandler = new Interop.Kernel32.ConsoleCtrlHandlerRoutine(ConsoleHandlerProc);

            if (!Interop.Kernel32.SetConsoleCtrlHandler(_consoleHandler, true))
            {
                Debug.Fail("Failed to install console handler.");
                _consoleHandler = null;
            }

            IntPtr hInstance = Interop.Kernel32.GetModuleHandle(null);

            s_className = $".NET-BroadcastEventWindow.{AppDomain.CurrentDomain.GetHashCode():x}.0";

            fixed (char* className = s_className)
            {
                // It is important that we stash the delegate to ensure it doesn't
                // get collected by the GC.

                _windowProc = WindowProc;

                Interop.User32.WNDCLASS windowClass = new Interop.User32.WNDCLASS
                {
                    hbrBackground = (IntPtr)(Interop.User32.COLOR_WINDOW + 1),
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_windowProc),
                    lpszClassName = className,
                    hInstance = hInstance
                };

                if (Interop.User32.RegisterClassW(ref windowClass) == 0)
                {
                    _windowProc = null;
                    Debug.WriteLine("Unable to register broadcast window class: {0}", Marshal.GetLastWin32Error());
                }
                else
                {
                    // And create an instance of the window.
                    _windowHandle = Interop.User32.CreateWindowExW(
                        0,
                        s_className,
                        s_className,
                        Interop.User32.WS_POPUP,
                        0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero,
                        hInstance, IntPtr.Zero);
                }
            }

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Shutdown);
        }

        /// <summary>
        ///  Called on the control's owning thread to perform the actual callback.
        ///  This empties this control's callback queue, propagating any exceptions
        ///  back as needed.
        /// </summary>
        private static void InvokeMarshaledCallbacks()
        {
            Debug.Assert(s_threadCallbackList != null, "Invoking marshaled callbacks before there are any");

            Delegate? current = null;
            lock (s_threadCallbackList!)
            {
                if (s_threadCallbackList.Count > 0)
                {
                    current = s_threadCallbackList.Dequeue();
                }
            }

            // Now invoke on all the queued items.
            while (current != null)
            {
                try
                {
                    // Optimize a common case of using EventHandler. This allows us to invoke
                    // early bound, which is a bit more efficient.
                    if (current is EventHandler c)
                    {
                        c(null, EventArgs.Empty);
                    }
                    else
                    {
                        current.DynamicInvoke(Array.Empty<object>());
                    }
                }
                catch (Exception t)
                {
                    Debug.Fail("SystemEvents marshaled callback failed:" + t);
                }

                lock (s_threadCallbackList)
                {
                    current = s_threadCallbackList.Count > 0 ?
                        s_threadCallbackList.Dequeue() :
                        null;
                }
            }
        }

        /// <summary>
        ///  Executes the given delegate asynchronously on the thread that listens for system events.  Similar to Control.BeginInvoke().
        /// </summary>
        public static void InvokeOnEventsThread(Delegate method)
        {
            // This method is really only here for GDI+ initialization/shutdown
            EnsureSystemEvents(requireHandle: true);

#if DEBUG
            unsafe
            {
                int pid;
                int thread = Interop.User32.GetWindowThreadProcessId(s_systemEvents!._windowHandle, &pid);
                GC.KeepAlive(s_systemEvents);
                Debug.Assert(s_windowThread == null || thread != Interop.Kernel32.GetCurrentThreadId(), "Don't call MarshaledInvoke on the system events thread");
            }
#endif

            if (s_threadCallbackList == null)
            {
                lock (s_eventLockObject)
                {
                    if (s_threadCallbackList == null)
                    {
                        s_threadCallbackMessage = Interop.User32.RegisterWindowMessageW("SystemEventsThreadCallbackMessage");
                        s_threadCallbackList = new Queue<Delegate>();
                    }
                }
            }

            Debug.Assert(s_threadCallbackMessage != 0, "threadCallbackList initialized but threadCallbackMessage not?");

            lock (s_threadCallbackList)
            {
                s_threadCallbackList.Enqueue(method);
            }

            Interop.User32.PostMessageW(s_systemEvents!._windowHandle, s_threadCallbackMessage, IntPtr.Zero, IntPtr.Zero);
            GC.KeepAlive(s_systemEvents);
        }

        /// <summary>
        ///  Kills the timer specified by the given id.
        /// </summary>
        public static void KillTimer(IntPtr timerId)
        {
            EnsureSystemEvents(requireHandle: true);
            if (s_systemEvents!._windowHandle != IntPtr.Zero)
            {
                int res = (int)Interop.User32.SendMessageW(s_systemEvents._windowHandle,
                                                                Interop.User32.WM_KILLTIMER, timerId, IntPtr.Zero);
                GC.KeepAlive(s_systemEvents);

                if (res == 0)
                    throw new ExternalException(SR.ErrorKillTimer);
            }
        }

        /// <summary>
        ///  Callback that handles the create timer
        ///  user message.
        /// </summary>
        private IntPtr OnCreateTimer(IntPtr wParam)
        {
            IntPtr timerId = (IntPtr)s_randomTimerId.Next();
            IntPtr res = Interop.User32.SetTimer(_windowHandle, timerId, (int)wParam, IntPtr.Zero);
            return (res == IntPtr.Zero ? IntPtr.Zero : timerId);
        }

        /// <summary>
        ///  Handler that raises the DisplaySettings changing event
        /// </summary>
        private void OnDisplaySettingsChanging()
        {
            RaiseEvent(s_onDisplaySettingsChangingEvent, this, EventArgs.Empty);
        }

        /// <summary>
        ///  Handler that raises the DisplaySettings changed event
        /// </summary>
        private void OnDisplaySettingsChanged()
        {
            RaiseEvent(s_onDisplaySettingsChangedEvent, this, EventArgs.Empty);
        }

        /// <summary>
        ///  Handler for any event that fires a standard EventHandler delegate.
        /// </summary>
        private void OnGenericEvent(object eventKey)
        {
            RaiseEvent(eventKey, this, EventArgs.Empty);
        }

        private void OnShutdown(object eventKey)
        {
            RaiseEvent(false, eventKey, this, EventArgs.Empty);
        }

        /// <summary>
        ///  Callback that handles the KillTimer user message.
        /// </summary>
        private bool OnKillTimer(IntPtr wParam)
        {
            bool res = Interop.User32.KillTimer(_windowHandle, wParam);
            return res;
        }

        /// <summary>
        ///  Handler for WM_POWERBROADCAST.
        /// </summary>
        private void OnPowerModeChanged(IntPtr wParam)
        {
            PowerModes mode;

            switch ((int)wParam)
            {
                case Interop.User32.PBT_APMSUSPEND:
                case Interop.User32.PBT_APMSTANDBY:
                    mode = PowerModes.Suspend;
                    break;

                case Interop.User32.PBT_APMRESUMECRITICAL:
                case Interop.User32.PBT_APMRESUMESUSPEND:
                case Interop.User32.PBT_APMRESUMESTANDBY:
                    mode = PowerModes.Resume;
                    break;

                case Interop.User32.PBT_APMBATTERYLOW:
                case Interop.User32.PBT_APMPOWERSTATUSCHANGE:
                case Interop.User32.PBT_APMOEMEVENT:
                    mode = PowerModes.StatusChange;
                    break;

                default:
                    return;
            }

            RaiseEvent(s_onPowerModeChangedEvent, this, new PowerModeChangedEventArgs(mode));
        }

        /// <summary>
        ///  Handler for WM_ENDSESSION.
        /// </summary>
        private void OnSessionEnded(IntPtr wParam, IntPtr lParam)
        {
            // wParam will be nonzero if the session is actually ending.  If
            // it was canceled then we do not want to raise the event.
            if (wParam != (IntPtr)0)
            {
                SessionEndReasons reason = SessionEndReasons.SystemShutdown;

                if (((unchecked((int)(long)lParam)) & Interop.User32.ENDSESSION_LOGOFF) != 0)
                {
                    reason = SessionEndReasons.Logoff;
                }

                SessionEndedEventArgs endEvt = new SessionEndedEventArgs(reason);

                RaiseEvent(s_onSessionEndedEvent, this, endEvt);
            }
        }

        /// <summary>
        ///  Handler for WM_QUERYENDSESSION.
        /// </summary>
        private int OnSessionEnding(IntPtr lParam)
        {
            int endOk;

            SessionEndReasons reason = SessionEndReasons.SystemShutdown;

            // Casting to (int) is bad if we're 64-bit; casting to (long) is ok whether we're 64- or 32-bit.
            if ((((long)lParam) & Interop.User32.ENDSESSION_LOGOFF) != 0)
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

        /// <summary>
        ///  Handler for WM_THEMECHANGED
        ///  VS 2005 note: Before VS 2005, we used to fire UserPreferenceChanged with category
        ///  set to Window. In VS 2005, we support visual styles and need a new category Theme
        ///  since Window is too general. We fire UserPreferenceChanged with this category, but
        ///  for backward compat, we also fire it with category set to Window.
        /// </summary>
        private void OnThemeChanged()
        {
            // we need to fire a changing event handler for Themes.
            // note that it needs to be documented that accessing theme information during the changing event is forbidden.
            RaiseEvent(s_onUserPreferenceChangingEvent, this, new UserPreferenceChangingEventArgs(UserPreferenceCategory.VisualStyle));

            UserPreferenceCategory pref = UserPreferenceCategory.Window;

            RaiseEvent(s_onUserPreferenceChangedEvent, this, new UserPreferenceChangedEventArgs(pref));

            pref = UserPreferenceCategory.VisualStyle;

            RaiseEvent(s_onUserPreferenceChangedEvent, this, new UserPreferenceChangedEventArgs(pref));
        }

        /// <summary>
        ///  Handler for WM_SETTINGCHANGE and WM_SYSCOLORCHANGE.
        /// </summary>
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

        /// <summary>
        ///  Handler for WM_TIMER.
        /// </summary>
        private void OnTimerElapsed(IntPtr wParam)
        {
            RaiseEvent(s_onTimerElapsedEvent, this, new TimerElapsedEventArgs(wParam));
        }

        private static void RaiseEvent(object key, params object[] args)
        {
            RaiseEvent(true, key, args);
        }

        private static void RaiseEvent(bool checkFinalization, object key, params object[] args)
        {
            Debug.Assert(args != null && args.Length == 2);

            // If the AppDomain's unloading, we shouldn't fire SystemEvents other than Shutdown.
            if (checkFinalization && AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                return;
            }

            SystemEventInvokeInfo?[]? invokeItemArray = null;

            lock (s_eventLockObject)
            {
                if (s_handlers != null && s_handlers.ContainsKey(key))
                {
                    List<SystemEventInvokeInfo> invokeItems = s_handlers[key];

                    // clone the list so we don't have this type locked and cause
                    // a deadlock if someone tries to modify handlers during an invoke.
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
                        SystemEventInvokeInfo info = invokeItemArray[i]!;
                        info.Invoke(checkFinalization, args!);
                        invokeItemArray[i] = null; // clear it if it's valid
                    }
                    catch (Exception)
                    {
                        // Eat exceptions (Everett compat)
                    }
                }

                // clean out any that are dead.
                lock (s_eventLockObject)
                {
                    List<SystemEventInvokeInfo>? invokeItems = null;

                    for (int i = 0; i < invokeItemArray.Length; i++)
                    {
                        SystemEventInvokeInfo? info = invokeItemArray[i];
                        if (info != null)
                        {
                            if (invokeItems == null)
                            {
                                if (!s_handlers!.TryGetValue(key, out invokeItems))
                                {
                                    // weird.  just to be safe.
                                    return;
                                }
                            }

                            invokeItems.Remove(info);
                        }
                    }
                }
            }
        }

        private static void RemoveEventHandler(object key, Delegate? value)
        {
            if (value is null)
            {
                return;
            }

            lock (s_eventLockObject)
            {
                if (s_handlers != null && s_handlers.ContainsKey(key))
                {
                    List<SystemEventInvokeInfo> invokeItems = s_handlers[key];

                    invokeItems.Remove(new SystemEventInvokeInfo(value));
                }
            }
        }

        private static void Shutdown()
        {
            if (s_systemEvents != null)
            {
                lock (s_procLockObject)
                {
                    if (s_systemEvents != null)
                    {
                        // If we are using system events from another thread, request that it terminate
                        if (s_windowThread != null)
                        {
#if DEBUG
                            unsafe
                            {
                                int pid;
                                int thread = Interop.User32.GetWindowThreadProcessId(s_systemEvents._windowHandle, &pid);
                                Debug.Assert(thread != Interop.Kernel32.GetCurrentThreadId(), "Don't call Shutdown on the system events thread");

                            }
#endif
                            // The handle could be valid, Zero or invalid depending on the state of the thread
                            // that is processing the messages. We optimistically expect it to be valid to
                            // notify the thread to shutdown. The Zero or invalid values should be present
                            // only when the thread is already shutting down due to external factors.
                            if (s_systemEvents._windowHandle != IntPtr.Zero)
                            {
                                Interop.User32.PostMessageW(s_systemEvents._windowHandle, Interop.User32.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                                GC.KeepAlive(s_systemEvents);
                            }

                            s_windowThread.Join();
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

#if FEATURE_CER
        [PrePrepareMethod]
#endif
        private static void Shutdown(object? sender, EventArgs e)
        {
            Shutdown();
        }

        /// <summary>
        ///  A standard Win32 window proc for our broadcast window.
        /// </summary>
        private IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case Interop.User32.WM_SETTINGCHANGE:
                    string? newString;
                    IntPtr newStringPtr = lParam;
                    if (lParam != IntPtr.Zero)
                    {
                        newString = Marshal.PtrToStringUni(lParam);
                        if (newString != null)
                        {
                            newStringPtr = Marshal.StringToHGlobalUni(newString);
                        }
                    }
                    Interop.User32.PostMessageW(_windowHandle, Interop.User32.WM_REFLECT + msg, wParam, newStringPtr);
                    break;
                case Interop.User32.WM_WTSSESSION_CHANGE:
                    OnSessionSwitch((int)wParam);
                    break;
                case Interop.User32.WM_SYSCOLORCHANGE:
                case Interop.User32.WM_COMPACTING:
                case Interop.User32.WM_DISPLAYCHANGE:
                case Interop.User32.WM_FONTCHANGE:
                case Interop.User32.WM_PALETTECHANGED:
                case Interop.User32.WM_TIMECHANGE:
                case Interop.User32.WM_TIMER:
                case Interop.User32.WM_THEMECHANGED:
                    Interop.User32.PostMessageW(_windowHandle, Interop.User32.WM_REFLECT + msg, wParam, lParam);
                    break;

                case Interop.User32.WM_CREATETIMER:
                    return OnCreateTimer(wParam);

                case Interop.User32.WM_KILLTIMER:
                    return (IntPtr)(OnKillTimer(wParam) ? 1 : 0);

                case Interop.User32.WM_REFLECT + Interop.User32.WM_SETTINGCHANGE:
                    try
                    {
                        OnUserPreferenceChanging(msg - Interop.User32.WM_REFLECT, wParam, lParam);
                        OnUserPreferenceChanged(msg - Interop.User32.WM_REFLECT, wParam, lParam);
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
                            Debug.Fail("Exception occurred while freeing memory: " + e.ToString());
                        }
                    }
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_SYSCOLORCHANGE:
                    OnUserPreferenceChanging(msg - Interop.User32.WM_REFLECT, wParam, lParam);
                    OnUserPreferenceChanged(msg - Interop.User32.WM_REFLECT, wParam, lParam);
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_THEMECHANGED:
                    OnThemeChanged();
                    break;

                case Interop.User32.WM_QUERYENDSESSION:
                    return (IntPtr)OnSessionEnding(lParam);

                case Interop.User32.WM_ENDSESSION:
                    OnSessionEnded(wParam, lParam);
                    break;

                case Interop.User32.WM_POWERBROADCAST:
                    OnPowerModeChanged(wParam);
                    break;

                // WM_HIBERNATE on WinCE
                case Interop.User32.WM_REFLECT + Interop.User32.WM_COMPACTING:
                    OnGenericEvent(s_onLowMemoryEvent);
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_DISPLAYCHANGE:
                    OnDisplaySettingsChanging();
                    OnDisplaySettingsChanged();
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_FONTCHANGE:
                    OnGenericEvent(s_onInstalledFontsChangedEvent);
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_PALETTECHANGED:
                    OnGenericEvent(s_onPaletteChangedEvent);
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_TIMECHANGE:
                    OnGenericEvent(s_onTimeChangedEvent);
                    break;

                case Interop.User32.WM_REFLECT + Interop.User32.WM_TIMER:
                    OnTimerElapsed(wParam);
                    break;

                case Interop.User32.WM_DESTROY:
                    Interop.User32.PostQuitMessage(0);
                    _windowHandle = IntPtr.Zero;
                    break;

                default:
                    // If we received a thread execute message, then execute it.
                    if (msg == s_threadCallbackMessage && msg != 0)
                    {
                        InvokeMarshaledCallbacks();
                        return IntPtr.Zero;
                    }
                    break;
            }

            return Interop.User32.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        ///  This is the method that runs our window thread.  This method
        ///  creates a window and spins up a message loop.  The window
        ///  is made visible with a size of 0, 0, so that it will trap
        ///  global broadcast messages.
        /// </summary>
        private void WindowThreadProc()
        {
            try
            {
                Initialize();
                s_eventWindowReady!.Set();

                if (_windowHandle != IntPtr.Zero)
                {
                    Interop.User32.MSG msg = default(Interop.User32.MSG);

                    while (Interop.User32.GetMessageW(ref msg, _windowHandle, 0, 0) > 0)
                    {
                        Interop.User32.TranslateMessage(ref msg);
                        Interop.User32.DispatchMessageW(ref msg);
                    }
                }

                OnShutdown(s_onEventsThreadShutdownEvent);
            }
            catch (Exception e)
            {
                // In case something very very wrong happend during the creation action.
                // This will unblock the calling thread.
                s_eventWindowReady!.Set();

                if (!((e is ThreadInterruptedException) || (e is ThreadAbortException)))
                {
                    Debug.Fail("Unexpected thread exception in system events window thread proc", e.ToString());
                }
            }

            Dispose();
        }

        // A class that helps fire events on the right thread.
        private sealed class SystemEventInvokeInfo
        {
            private readonly SynchronizationContext _syncContext; // the context that we'll use to fire against.
            private readonly Delegate _delegate;     // the delegate we'll fire.  This is a weak ref so we don't hold object in memory.
            public SystemEventInvokeInfo(Delegate d)
            {
                _delegate = d;
                _syncContext = AsyncOperationManager.SynchronizationContext;
            }

            // fire the given event with the given params.
            public void Invoke(bool checkFinalization, params object[] args)
            {
                try
                {
                    // If we didn't get call back invoke directly.
                    if (_syncContext == null)
                    {
                        InvokeCallback(args);
                    }
                    else
                    {
                        // otherwise tell the context to do it for us.
                        _syncContext.Send(new SendOrPostCallback(InvokeCallback), args);
                    }
                }
                catch (InvalidAsynchronousStateException)
                {
                    // if the synch context is invalid -- do the invoke directly for app compat.
                    // If the app's shutting down, don't fire the event (unless it's shutdown).
                    if (!checkFinalization || !AppDomain.CurrentDomain.IsFinalizingForUnload())
                    {
                        InvokeCallback(args);
                    }
                }
            }

            // our delegate method that the SyncContext will call on.
            private void InvokeCallback(object? arg)
            {
                _delegate.DynamicInvoke((object[]?)arg);
            }

            public override bool Equals([NotNullWhen(true)] object? other)
            {
                return other is SystemEventInvokeInfo otherInvoke && otherInvoke._delegate.Equals(_delegate);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
