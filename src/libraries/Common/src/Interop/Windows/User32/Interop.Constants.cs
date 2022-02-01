// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class User32
    {
        public const int COLOR_WINDOW = 5;

        public const int CTRL_LOGOFF_EVENT   = 5;
        public const int CTRL_SHUTDOWN_EVENT = 6;

        public const int ENDSESSION_CLOSEAPP = 0x00000001;
        public const int ENDSESSION_CRITICAL = 0x40000000;
        public const int ENDSESSION_LOGOFF = unchecked((int)0x80000000);

        public const int GCL_WNDPROC = (-24);
        public const int GWL_WNDPROC = (-4);

        public const int MWMO_INPUTAVAILABLE = 0x0004;

        public const int PBT_APMQUERYSUSPEND           = 0x0000;
        public const int PBT_APMQUERYSTANDBY           = 0x0001;
        public const int PBT_APMQUERYSUSPENDFAILED     = 0x0002;
        public const int PBT_APMQUERYSTANDBYFAILED     = 0x0003;
        public const int PBT_APMSUSPEND                = 0x0004;
        public const int PBT_APMSTANDBY                = 0x0005;
        public const int PBT_APMRESUMECRITICAL         = 0x0006;
        public const int PBT_APMRESUMESUSPEND          = 0x0007;
        public const int PBT_APMRESUMESTANDBY          = 0x0008;
        public const int PBT_APMBATTERYLOW             = 0x0009;
        public const int PBT_APMPOWERSTATUSCHANGE      = 0x000A;
        public const int PBT_APMOEMEVENT               = 0x000B;

        public const int PM_REMOVE = 0x0001;

        public const int QS_KEY = 0x0001,
        QS_MOUSEMOVE = 0x0002,
        QS_MOUSEBUTTON = 0x0004,
        QS_POSTMESSAGE = 0x0008,
        QS_TIMER = 0x0010,
        QS_PAINT = 0x0020,
        QS_SENDMESSAGE = 0x0040,
        QS_HOTKEY = 0x0080,
        QS_ALLPOSTMESSAGE = 0x0100,
        QS_MOUSE = QS_MOUSEMOVE | QS_MOUSEBUTTON,
        QS_INPUT = QS_MOUSE | QS_KEY,
        QS_ALLEVENTS = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY,
        QS_ALLINPUT = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE;

        public const int SPI_GETBEEP = 1;
        public const int SPI_SETBEEP = 2;
        public const int SPI_GETMOUSE = 3;
        public const int SPI_SETMOUSE = 4;
        public const int SPI_GETBORDER = 5;
        public const int SPI_SETBORDER = 6;
        public const int SPI_GETKEYBOARDSPEED = 10;
        public const int SPI_SETKEYBOARDSPEED = 11;
        public const int SPI_LANGDRIVER = 12;
        public const int SPI_ICONHORIZONTALSPACING = 13;
        public const int SPI_GETSCREENSAVETIMEOUT = 14;
        public const int SPI_SETSCREENSAVETIMEOUT = 15;
        public const int SPI_GETSCREENSAVEACTIVE = 16;
        public const int SPI_SETSCREENSAVEACTIVE = 17;
        public const int SPI_GETGRIDGRANULARITY = 18;
        public const int SPI_SETGRIDGRANULARITY = 19;
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPI_SETDESKPATTERN = 21;
        public const int SPI_GETKEYBOARDDELAY = 22;
        public const int SPI_SETKEYBOARDDELAY = 23;
        public const int SPI_ICONVERTICALSPACING = 24;
        public const int SPI_GETICONTITLEWRAP = 25;
        public const int SPI_SETICONTITLEWRAP = 26;
        public const int SPI_GETMENUDROPALIGNMENT = 27;
        public const int SPI_SETMENUDROPALIGNMENT = 28;
        public const int SPI_SETDOUBLECLKWIDTH = 29;
        public const int SPI_SETDOUBLECLKHEIGHT = 30;
        public const int SPI_GETICONTITLELOGFONT = 31;
        public const int SPI_SETDOUBLECLICKTIME = 32;
        public const int SPI_SETMOUSEBUTTONSWAP = 33;
        public const int SPI_SETICONTITLELOGFONT = 34;
        public const int SPI_GETFASTTASKSWITCH = 35;
        public const int SPI_SETFASTTASKSWITCH = 36;
        public const int SPI_SETDRAGFULLWINDOWS = 37;
        public const int SPI_GETDRAGFULLWINDOWS = 38;
        public const int SPI_GETNONCLIENTMETRICS = 41;
        public const int SPI_SETNONCLIENTMETRICS = 42;
        public const int SPI_GETMINIMIZEDMETRICS = 43;
        public const int SPI_SETMINIMIZEDMETRICS = 44;
        public const int SPI_GETICONMETRICS = 45;
        public const int SPI_SETICONMETRICS = 46;
        public const int SPI_SETWORKAREA = 47;
        public const int SPI_GETWORKAREA = 48;
        public const int SPI_SETPENWINDOWS = 49;
        public const int SPI_GETHIGHCONTRAST = 66;
        public const int SPI_SETHIGHCONTRAST = 67;
        public const int SPI_GETKEYBOARDPREF = 68;
        public const int SPI_SETKEYBOARDPREF = 69;
        public const int SPI_GETSCREENREADER = 70;
        public const int SPI_SETSCREENREADER = 71;
        public const int SPI_GETANIMATION = 72;
        public const int SPI_SETANIMATION = 73;
        public const int SPI_GETFONTSMOOTHING = 74;
        public const int SPI_SETFONTSMOOTHING = 75;
        public const int SPI_SETDRAGWIDTH = 76;
        public const int SPI_SETDRAGHEIGHT = 77;
        public const int SPI_SETHANDHELD = 78;
        public const int SPI_GETLOWPOWERTIMEOUT = 79;
        public const int SPI_GETPOWEROFFTIMEOUT = 80;
        public const int SPI_SETLOWPOWERTIMEOUT = 81;
        public const int SPI_SETPOWEROFFTIMEOUT = 82;
        public const int SPI_GETLOWPOWERACTIVE = 83;
        public const int SPI_GETPOWEROFFACTIVE = 84;
        public const int SPI_SETLOWPOWERACTIVE = 85;
        public const int SPI_SETPOWEROFFACTIVE = 86;
        public const int SPI_SETCURSORS = 87;
        public const int SPI_SETICONS = 88;
        public const int SPI_GETDEFAULTINPUTLANG = 89;
        public const int SPI_SETDEFAULTINPUTLANG = 90;
        public const int SPI_SETLANGTOGGLE = 91;
        public const int SPI_GETWINDOWSEXTENSION = 92;
        public const int SPI_SETMOUSETRAILS = 93;
        public const int SPI_GETMOUSETRAILS = 94;
        public const int SPI_SETSCREENSAVERRUNNING = 97;
        public const int SPI_SCREENSAVERRUNNING = SPI_SETSCREENSAVERRUNNING;
        public const int SPI_GETFILTERKEYS = 50;
        public const int SPI_SETFILTERKEYS = 51;
        public const int SPI_GETTOGGLEKEYS = 52;
        public const int SPI_SETTOGGLEKEYS = 53;
        public const int SPI_GETMOUSEKEYS = 54;
        public const int SPI_SETMOUSEKEYS = 55;
        public const int SPI_GETSHOWSOUNDS = 56;
        public const int SPI_SETSHOWSOUNDS = 57;
        public const int SPI_GETSTICKYKEYS = 58;
        public const int SPI_SETSTICKYKEYS = 59;
        public const int SPI_GETACCESSTIMEOUT = 60;
        public const int SPI_SETACCESSTIMEOUT = 61;
        public const int SPI_GETSERIALKEYS = 62;
        public const int SPI_SETSERIALKEYS = 63;
        public const int SPI_GETSOUNDSENTRY = 64;
        public const int SPI_SETSOUNDSENTRY = 65;
        public const int SPI_GETSNAPTODEFBUTTON = 95;
        public const int SPI_SETSNAPTODEFBUTTON = 96;
        public const int SPI_GETMOUSEHOVERWIDTH = 98;
        public const int SPI_SETMOUSEHOVERWIDTH = 99;
        public const int SPI_GETMOUSEHOVERHEIGHT = 100;
        public const int SPI_SETMOUSEHOVERHEIGHT = 101;
        public const int SPI_GETMOUSEHOVERTIME = 102;
        public const int SPI_SETMOUSEHOVERTIME = 103;
        public const int SPI_GETWHEELSCROLLLINES = 104;
        public const int SPI_SETWHEELSCROLLLINES = 105;
        public const int SPI_GETMENUSHOWDELAY = 106;
        public const int SPI_SETMENUSHOWDELAY = 107;
        public const int SPI_GETSHOWIMEUI = 110;
        public const int SPI_SETSHOWIMEUI = 111;
        public const int SPI_GETMOUSESPEED = 112;
        public const int SPI_SETMOUSESPEED = 113;
        public const int SPI_GETSCREENSAVERRUNNING = 114;
        public const int SPI_GETDESKWALLPAPER = 115;
        public const int SPI_GETACTIVEWINDOWTRACKING = 0x1000;
        public const int SPI_SETACTIVEWINDOWTRACKING = 0x1001;
        public const int SPI_GETMENUANIMATION = 0x1002;
        public const int SPI_SETMENUANIMATION = 0x1003;
        public const int SPI_GETCOMBOBOXANIMATION = 0x1004;
        public const int SPI_SETCOMBOBOXANIMATION = 0x1005;
        public const int SPI_GETLISTBOXSMOOTHSCROLLING = 0x1006;
        public const int SPI_SETLISTBOXSMOOTHSCROLLING = 0x1007;
        public const int SPI_GETGRADIENTCAPTIONS = 0x1008;
        public const int SPI_SETGRADIENTCAPTIONS = 0x1009;
        public const int SPI_GETKEYBOARDCUES = 0x100A;
        public const int SPI_SETKEYBOARDCUES = 0x100B;
        public const int SPI_GETMENUUNDERLINES = SPI_GETKEYBOARDCUES;
        public const int SPI_SETMENUUNDERLINES = SPI_SETKEYBOARDCUES;
        public const int SPI_GETACTIVEWNDTRKZORDER = 0x100C;
        public const int SPI_SETACTIVEWNDTRKZORDER = 0x100D;
        public const int SPI_GETHOTTRACKING = 0x100E;
        public const int SPI_SETHOTTRACKING = 0x100F;
        public const int SPI_GETMENUFADE = 0x1012;
        public const int SPI_SETMENUFADE = 0x1013;
        public const int SPI_GETSELECTIONFADE = 0x1014;
        public const int SPI_SETSELECTIONFADE = 0x1015;
        public const int SPI_GETTOOLTIPANIMATION = 0x1016;
        public const int SPI_SETTOOLTIPANIMATION = 0x1017;
        public const int SPI_GETTOOLTIPFADE = 0x1018;
        public const int SPI_SETTOOLTIPFADE = 0x1019;
        public const int SPI_GETCURSORSHADOW = 0x101A;
        public const int SPI_SETCURSORSHADOW = 0x101B;
        public const int SPI_GETUIEFFECTS = 0x103E;
        public const int SPI_SETUIEFFECTS = 0x103F;
        public const int SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        public const int SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        public const int SPI_GETACTIVEWNDTRKTIMEOUT = 0x2002;
        public const int SPI_SETACTIVEWNDTRKTIMEOUT = 0x2003;
        public const int SPI_GETFOREGROUNDFLASHCOUNT = 0x2004;
        public const int SPI_SETFOREGROUNDFLASHCOUNT = 0x2005;
        public const int SPI_GETCARETWIDTH = 0x2006;
        public const int SPI_SETCARETWIDTH = 0x2007;

        public const int WAIT_TIMEOUT = 0x00000102;

        public const int WM_CLOSE = 0x0010;
        public const int WM_QUERYENDSESSION = 0x0011;
        public const int WM_QUIT = 0x0012;
        public const int WM_SYSCOLORCHANGE = 0x0015;
        public const int WM_ENDSESSION = 0x0016;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_FONTCHANGE = 0x001D;
        public const int WM_TIMECHANGE = 0x001E;
        public const int WM_COMPACTING = 0x0041;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int WM_TIMER = 0x0113;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int WM_WTSSESSION_CHANGE = 0x02B1;
        public const int WM_PALETTECHANGED = 0x0311;
        public const int WM_THEMECHANGED = 0x031A;
        public const int WM_USER = 0x0400;
        public const int WM_CREATETIMER = WM_USER + 1;
        public const int WM_KILLTIMER = WM_USER + 2;
        public const int WM_REFLECT = WM_USER + 0x1C00;

        public const int WS_POPUP = unchecked((int)0x80000000);

        public const int WSF_VISIBLE = 0x0001;

        public const int UOI_FLAGS = 1;

        public const int HWND_BROADCAST = 0xffff;

    }
}
