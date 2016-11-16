// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Environment
    {
        public enum SpecialFolder
        {
            ApplicationData = SpecialFolderValues.CSIDL_APPDATA,
            CommonApplicationData = SpecialFolderValues.CSIDL_COMMON_APPDATA,
            LocalApplicationData = SpecialFolderValues.CSIDL_LOCAL_APPDATA,
            Cookies = SpecialFolderValues.CSIDL_COOKIES,
            Desktop = SpecialFolderValues.CSIDL_DESKTOP,
            Favorites = SpecialFolderValues.CSIDL_FAVORITES,
            History = SpecialFolderValues.CSIDL_HISTORY,
            InternetCache = SpecialFolderValues.CSIDL_INTERNET_CACHE,
            Programs = SpecialFolderValues.CSIDL_PROGRAMS,
            MyComputer = SpecialFolderValues.CSIDL_DRIVES,
            MyMusic = SpecialFolderValues.CSIDL_MYMUSIC,
            MyPictures = SpecialFolderValues.CSIDL_MYPICTURES,
            MyVideos = SpecialFolderValues.CSIDL_MYVIDEO,
            Recent = SpecialFolderValues.CSIDL_RECENT,
            SendTo = SpecialFolderValues.CSIDL_SENDTO,
            StartMenu = SpecialFolderValues.CSIDL_STARTMENU,
            Startup = SpecialFolderValues.CSIDL_STARTUP,
            System = SpecialFolderValues.CSIDL_SYSTEM,
            Templates = SpecialFolderValues.CSIDL_TEMPLATES,
            DesktopDirectory = SpecialFolderValues.CSIDL_DESKTOPDIRECTORY,
            Personal = SpecialFolderValues.CSIDL_PERSONAL,
            MyDocuments = SpecialFolderValues.CSIDL_PERSONAL,
            ProgramFiles = SpecialFolderValues.CSIDL_PROGRAM_FILES,
            CommonProgramFiles = SpecialFolderValues.CSIDL_PROGRAM_FILES_COMMON,
            AdminTools = SpecialFolderValues.CSIDL_ADMINTOOLS,
            CDBurning = SpecialFolderValues.CSIDL_CDBURN_AREA,
            CommonAdminTools = SpecialFolderValues.CSIDL_COMMON_ADMINTOOLS,
            CommonDocuments = SpecialFolderValues.CSIDL_COMMON_DOCUMENTS,
            CommonMusic = SpecialFolderValues.CSIDL_COMMON_MUSIC,
            CommonOemLinks = SpecialFolderValues.CSIDL_COMMON_OEM_LINKS,
            CommonPictures = SpecialFolderValues.CSIDL_COMMON_PICTURES,
            CommonStartMenu = SpecialFolderValues.CSIDL_COMMON_STARTMENU,
            CommonPrograms = SpecialFolderValues.CSIDL_COMMON_PROGRAMS,
            CommonStartup = SpecialFolderValues.CSIDL_COMMON_STARTUP,
            CommonDesktopDirectory = SpecialFolderValues.CSIDL_COMMON_DESKTOPDIRECTORY,
            CommonTemplates = SpecialFolderValues.CSIDL_COMMON_TEMPLATES,
            CommonVideos = SpecialFolderValues.CSIDL_COMMON_VIDEO,
            Fonts = SpecialFolderValues.CSIDL_FONTS,
            NetworkShortcuts = SpecialFolderValues.CSIDL_NETHOOD,
            PrinterShortcuts = SpecialFolderValues.CSIDL_PRINTHOOD,
            UserProfile = SpecialFolderValues.CSIDL_PROFILE,
            CommonProgramFilesX86 = SpecialFolderValues.CSIDL_PROGRAM_FILES_COMMONX86,
            ProgramFilesX86 = SpecialFolderValues.CSIDL_PROGRAM_FILESX86,
            Resources = SpecialFolderValues.CSIDL_RESOURCES,
            LocalizedResources = SpecialFolderValues.CSIDL_RESOURCES_LOCALIZED,
            SystemX86 = SpecialFolderValues.CSIDL_SYSTEMX86,
            Windows = SpecialFolderValues.CSIDL_WINDOWS,
        }

        // These values are specific to Windows and are known to SHGetFolderPath, however they are
        // also the values used in the SpecialFolder enum.  As such, we keep them as constants
        // with their Win32 names, but keep them here rather than in Interop.Kernel32 as they're
        // used on all platforms.
        private static class SpecialFolderValues
        {
            internal const int CSIDL_APPDATA = 0x001a;
            internal const int CSIDL_COMMON_APPDATA = 0x0023;
            internal const int CSIDL_LOCAL_APPDATA = 0x001c;
            internal const int CSIDL_COOKIES = 0x0021;
            internal const int CSIDL_FAVORITES = 0x0006;
            internal const int CSIDL_HISTORY = 0x0022;
            internal const int CSIDL_INTERNET_CACHE = 0x0020;
            internal const int CSIDL_PROGRAMS = 0x0002;
            internal const int CSIDL_RECENT = 0x0008;
            internal const int CSIDL_SENDTO = 0x0009;
            internal const int CSIDL_STARTMENU = 0x000b;
            internal const int CSIDL_STARTUP = 0x0007;
            internal const int CSIDL_SYSTEM = 0x0025;
            internal const int CSIDL_TEMPLATES = 0x0015;
            internal const int CSIDL_DESKTOPDIRECTORY = 0x0010;
            internal const int CSIDL_PERSONAL = 0x0005;
            internal const int CSIDL_PROGRAM_FILES = 0x0026;
            internal const int CSIDL_PROGRAM_FILES_COMMON = 0x002b;
            internal const int CSIDL_DESKTOP = 0x0000;
            internal const int CSIDL_DRIVES = 0x0011;
            internal const int CSIDL_MYMUSIC = 0x000d;
            internal const int CSIDL_MYPICTURES = 0x0027;

            internal const int CSIDL_ADMINTOOLS = 0x0030; // <user name>\Start Menu\Programs\Administrative Tools
            internal const int CSIDL_CDBURN_AREA = 0x003b; // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
            internal const int CSIDL_COMMON_ADMINTOOLS = 0x002f; // All Users\Start Menu\Programs\Administrative Tools
            internal const int CSIDL_COMMON_DOCUMENTS = 0x002e; // All Users\Documents
            internal const int CSIDL_COMMON_MUSIC = 0x0035; // All Users\My Music
            internal const int CSIDL_COMMON_OEM_LINKS = 0x003a; // Links to All Users OEM specific apps
            internal const int CSIDL_COMMON_PICTURES = 0x0036; // All Users\My Pictures
            internal const int CSIDL_COMMON_STARTMENU = 0x0016; // All Users\Start Menu
            internal const int CSIDL_COMMON_PROGRAMS = 0X0017; // All Users\Start Menu\Programs
            internal const int CSIDL_COMMON_STARTUP = 0x0018; // All Users\Startup
            internal const int CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019; // All Users\Desktop
            internal const int CSIDL_COMMON_TEMPLATES = 0x002d; // All Users\Templates
            internal const int CSIDL_COMMON_VIDEO = 0x0037; // All Users\My Video
            internal const int CSIDL_FONTS = 0x0014; // windows\fonts
            internal const int CSIDL_MYVIDEO = 0x000e; // "My Videos" folder
            internal const int CSIDL_NETHOOD = 0x0013; // %APPDATA%\Microsoft\Windows\Network Shortcuts
            internal const int CSIDL_PRINTHOOD = 0x001b; // %APPDATA%\Microsoft\Windows\Printer Shortcuts
            internal const int CSIDL_PROFILE = 0x0028; // %USERPROFILE% (%SystemDrive%\Users\%USERNAME%)
            internal const int CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c; // x86 Program Files\Common on RISC
            internal const int CSIDL_PROGRAM_FILESX86 = 0x002a; // x86 C:\Program Files on RISC
            internal const int CSIDL_RESOURCES = 0x0038; // %windir%\Resources
            internal const int CSIDL_RESOURCES_LOCALIZED = 0x0039; // %windir%\resources\0409 (code page)
            internal const int CSIDL_SYSTEMX86 = 0x0029; // %windir%\system32
            internal const int CSIDL_WINDOWS = 0x0024; // GetWindowsDirectory()
        }
    }
}
