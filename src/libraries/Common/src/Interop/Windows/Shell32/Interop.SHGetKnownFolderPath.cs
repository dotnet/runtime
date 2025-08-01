// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Shell32
    {
        internal const int COR_E_PLATFORMNOTSUPPORTED = unchecked((int)0x80131539);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb762188.aspx
        [LibraryImport(Libraries.Shell32, SetLastError = false, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHGetKnownFolderPath(
            in Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out string ppszPath);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd378457.aspx
        internal static class KnownFolders
        {
            /// <summary>
            /// (CSIDL_ADMINTOOLS) Per user Administrative Tools
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Administrative Tools"
            /// </summary>
            internal static ReadOnlySpan<byte> AdminTools =>
                [0x72, 0x4E, 0xF1, 0x70, 0xA4, 0x2D, 0x4F, 0xEF, 0x9F, 0x26, 0xB6, 0x0E, 0x84, 0x6F, 0xBA, 0x4F];

            /// <summary>
            /// (CSIDL_CDBURN_AREA) Temporary Burn folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Burn\Burn"
            /// </summary>
            internal static ReadOnlySpan<byte> CDBurning =>
                [0x9E, 0x52, 0xAB, 0x10, 0xF8, 0x0D, 0x49, 0xDF, 0xAC, 0xB8, 0x43, 0x30, 0xF5, 0x68, 0x78, 0x55];

            /// <summary>
            /// (CSIDL_COMMON_ADMINTOOLS) Common Administrative Tools
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\Administrative Tools"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonAdminTools =>
                [0xD0, 0x38, 0x4E, 0x7D, 0xBA, 0xC3, 0x47, 0x97, 0x8F, 0x14, 0xCB, 0xA2, 0x29, 0xB3, 0x92, 0xB5];

            /// <summary>
            /// (CSIDL_COMMON_OEM_LINKS) OEM Links folder
            /// "%ALLUSERSPROFILE%\OEM Links"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonOEMLinks =>
                [0xC1, 0xBA, 0xE2, 0xD0, 0x10, 0xDF, 0x43, 0x34, 0xBE, 0xDD, 0x7A, 0xA2, 0x0B, 0x22, 0x7A, 0x9D];

            /// <summary>
            /// (CSIDL_COMMON_PROGRAMS) Common Programs folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonPrograms =>
                [0x01, 0x39, 0xD4, 0x4E, 0x6A, 0xFE, 0x49, 0xF2, 0x86, 0x90, 0x3D, 0xAF, 0xCA, 0xE6, 0xFF, 0xB8];

            /// <summary>
            /// (CSIDL_COMMON_STARTMENU) Common Start Menu folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonStartMenu =>
                [0xA4, 0x11, 0x57, 0x19, 0xD6, 0x2E, 0x49, 0x1D, 0xAA, 0x7C, 0xE7, 0x4B, 0x8B, 0xE3, 0xB0, 0x67];

            /// <summary>
            /// (CSIDL_COMMON_STARTUP, CSIDL_COMMON_ALTSTARTUP) Common Startup folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonStartup =>
                [0x82, 0xA5, 0xEA, 0x35, 0xD9, 0xCD, 0x47, 0xC5, 0x96, 0x29, 0xE1, 0x5D, 0x2F, 0x71, 0x4E, 0x6E];

            /// <summary>
            /// (CSIDL_COMMON_TEMPLATES) Common Templates folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Templates"
            /// </summary>
            internal static ReadOnlySpan<byte> CommonTemplates =>
                [0xB9, 0x42, 0x37, 0xE7, 0x57, 0xAC, 0x43, 0x47, 0x91, 0x51, 0xB0, 0x8C, 0x6C, 0x32, 0xD1, 0xF7];

            /// <summary>
            /// (CSIDL_DRIVES) Computer virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> ComputerFolder =>
                [0x0A, 0xC0, 0x83, 0x7C, 0xBB, 0xF8, 0x45, 0x2A, 0x85, 0x0D, 0x79, 0xD0, 0x8E, 0x66, 0x7C, 0xA7];

            /// <summary>
            /// (CSIDL_CONNECTIONS) Network Connections virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> ConnectionsFolder =>
                [0x6F, 0x0C, 0xD9, 0x2B, 0x2E, 0x97, 0x45, 0xD1, 0x88, 0xFF, 0xB0, 0xD1, 0x86, 0xB8, 0xDE, 0xDD];

            /// <summary>
            /// (CSIDL_CONTROLS) Control Panel virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> ControlPanelFolder =>
                [0x82, 0xA7, 0x4A, 0xEB, 0xAE, 0xB4, 0x46, 0x5C, 0xA0, 0x14, 0xD0, 0x97, 0xEE, 0x34, 0x6D, 0x63];

            /// <summary>
            /// (CSIDL_COOKIES) Cookies folder
            /// "%APPDATA%\Microsoft\Windows\Cookies"
            /// </summary>
            internal static ReadOnlySpan<byte> Cookies =>
                [0x2B, 0x0F, 0x76, 0x5D, 0xC0, 0xE9, 0x41, 0x71, 0x90, 0x8E, 0x08, 0xA6, 0x11, 0xB8, 0x4F, 0xF6];

            /// <summary>
            /// (CSIDL_DESKTOP, CSIDL_DESKTOPDIRECTORY) Desktop folder
            /// "%USERPROFILE%\Desktop"
            /// </summary>
            internal static ReadOnlySpan<byte> Desktop =>
                [0xB4, 0xBF, 0xCC, 0x3A, 0xDB, 0x2C, 0x42, 0x4C, 0xB0, 0x29, 0x7F, 0xE9, 0x9A, 0x87, 0xC6, 0x41];

            /// <summary>
            /// (CSIDL_MYDOCUMENTS, CSIDL_PERSONAL) Documents (My Documents) folder
            /// "%USERPROFILE%\Documents"
            /// </summary>
            internal static ReadOnlySpan<byte> Documents =>
                [0xFD, 0xD3, 0x9A, 0xD0, 0x23, 0x8F, 0x46, 0xAF, 0xAD, 0xB4, 0x6C, 0x85, 0x48, 0x03, 0x69, 0xC7];

            /// <summary>
            /// (CSIDL_FAVORITES, CSIDL_COMMON_FAVORITES) Favorites folder
            /// "%USERPROFILE%\Favorites"
            /// </summary>
            internal static ReadOnlySpan<byte> Favorites =>
                [0x17, 0x77, 0xF7, 0x61, 0x68, 0xAD, 0x4D, 0x8A, 0x87, 0xBD, 0x30, 0xB7, 0x59, 0xFA, 0x33, 0xDD];

            /// <summary>
            /// (CSIDL_FONTS) Fonts folder
            /// "%windir%\Fonts"
            /// </summary>
            internal static ReadOnlySpan<byte> Fonts =>
                [0xFD, 0x22, 0x8C, 0xB7, 0xAE, 0x11, 0x4A, 0xE3, 0x86, 0x4C, 0x16, 0xF3, 0x91, 0x0A, 0xB8, 0xFE];

            /// <summary>
            /// (CSIDL_HISTORY) History folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\History"
            /// </summary>
            internal static ReadOnlySpan<byte> History =>
                [0xD9, 0xDC, 0x8A, 0x3B, 0xB7, 0x84, 0x43, 0x2E, 0xA7, 0x81, 0x5A, 0x11, 0x30, 0xA7, 0x59, 0x63];

            /// <summary>
            /// (CSIDL_INTERNET_CACHE) Temporary Internet Files folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Temporary Internet Files"
            /// </summary>
            internal static ReadOnlySpan<byte> InternetCache =>
                [0x35, 0x24, 0x81, 0xE8, 0x33, 0xBE, 0x42, 0x51, 0xBA, 0x85, 0x60, 0x07, 0xCA, 0xED, 0xCF, 0x9D];

            /// <summary>
            /// (CSIDL_INTERNET) The Internet virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> InternetFolder =>
                [0x4D, 0x9F, 0x78, 0x74, 0x4E, 0x0C, 0x49, 0x04, 0x96, 0x7B, 0x40, 0xB0, 0xD2, 0x0C, 0x3E, 0x4B];

            /// <summary>
            /// (CSIDL_LOCAL_APPDATA) Local folder
            /// "%LOCALAPPDATA%" ("%USERPROFILE%\AppData\Local")
            /// </summary>
            internal static ReadOnlySpan<byte> LocalAppData =>
                [0xF1, 0xB3, 0x27, 0x85, 0x6F, 0xBA, 0x4F, 0xCF, 0x9D, 0x55, 0x7B, 0x8E, 0x7F, 0x15, 0x70, 0x91];

            /// <summary>
            /// (CSIDL_RESOURCES_LOCALIZED) Fixed localized resources folder
            /// "%windir%\resources\0409" (per active codepage)
            /// </summary>
            internal static ReadOnlySpan<byte> LocalizedResourcesDir =>
                [0x2A, 0x00, 0x37, 0x5E, 0x22, 0x4C, 0x49, 0xDE, 0xB8, 0xD1, 0x44, 0x0D, 0xF7, 0xEF, 0x3D, 0xDC];

            /// <summary>
            /// (CSIDL_MYMUSIC) Music folder
            /// "%USERPROFILE%\Music"
            /// </summary>
            internal static ReadOnlySpan<byte> Music =>
                [0x4B, 0xD8, 0xD5, 0x71, 0x6D, 0x19, 0x48, 0xD3, 0xBE, 0x97, 0x42, 0x22, 0x20, 0x08, 0x0E, 0x43];

            /// <summary>
            /// (CSIDL_NETHOOD) Network shortcuts folder "%APPDATA%\Microsoft\Windows\Network Shortcuts"
            /// </summary>
            internal static ReadOnlySpan<byte> NetHood =>
                [0xC5, 0xAB, 0xBF, 0x53, 0xE1, 0x7F, 0x41, 0x21, 0x89, 0x00, 0x86, 0x62, 0x6F, 0xC2, 0xC9, 0x73];

            /// <summary>
            /// (CSIDL_NETWORK, CSIDL_COMPUTERSNEARME) Network virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> NetworkFolder =>
                [0xD2, 0x0B, 0xEE, 0xC4, 0x5C, 0xA8, 0x49, 0x05, 0xAE, 0x3B, 0xBF, 0x25, 0x1E, 0xA0, 0x9B, 0x53];

            /// <summary>
            /// (CSIDL_MYPICTURES) Pictures folder "%USERPROFILE%\Pictures"
            /// </summary>
            internal static ReadOnlySpan<byte> Pictures =>
                [0x33, 0xE2, 0x81, 0x30, 0x4E, 0x1E, 0x46, 0x76, 0x83, 0x5A, 0x98, 0x39, 0x5C, 0x3B, 0xC3, 0xBB];

            /// <summary>
            /// (CSIDL_PRINTERS) Printers virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> PrintersFolder =>
                [0x76, 0xFC, 0x4E, 0x2D, 0xD6, 0xAD, 0x45, 0x19, 0xA6, 0x63, 0x37, 0xBD, 0x56, 0x06, 0x81, 0x85];

            /// <summary>
            /// (CSIDL_PRINTHOOD) Printer Shortcuts folder
            /// "%APPDATA%\Microsoft\Windows\Printer Shortcuts"
            /// </summary>
            internal static ReadOnlySpan<byte> PrintHood =>
                [0x92, 0x74, 0xBD, 0x8D, 0xCF, 0xD1, 0x41, 0xC3, 0xB3, 0x5E, 0xB1, 0x3F, 0x55, 0xA7, 0x58, 0xF4];

            /// <summary>
            /// (CSIDL_PROFILE) The root users profile folder "%USERPROFILE%"
            /// ("%SystemDrive%\Users\%USERNAME%")
            /// </summary>
            internal static ReadOnlySpan<byte> Profile =>
                [0x5E, 0x6C, 0x85, 0x8F, 0x0E, 0x22, 0x47, 0x60, 0x9A, 0xFE, 0xEA, 0x33, 0x17, 0xB6, 0x71, 0x73];

            /// <summary>
            /// (CSIDL_COMMON_APPDATA) ProgramData folder
            /// "%ALLUSERSPROFILE%" ("%ProgramData%", "%SystemDrive%\ProgramData")
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramData =>
                [0x62, 0xAB, 0x5D, 0x82, 0xFD, 0xC1, 0x4D, 0xC3, 0xA9, 0xDD, 0x07, 0x0D, 0x1D, 0x49, 0x5D, 0x97];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES) Program Files folder for the current process architecture
            /// "%ProgramFiles%" ("%SystemDrive%\Program Files")
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFiles =>
                [0x90, 0x5e, 0x63, 0xb6, 0xc1, 0xbf, 0x49, 0x4e, 0xb2, 0x9c, 0x65, 0xb7, 0x32, 0xd3, 0xd2, 0x1a];

            /// <summary>
            /// (CSIDL_PROGRAM_FILESX86) 32 bit Program Files folder (available to both 32/64 bit processes)
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesX86 =>
                [0x7C, 0x5A, 0x40, 0xEF, 0xA0, 0xFB, 0x4B, 0xFC, 0x87, 0x4A, 0xC0, 0xF2, 0xE0, 0xB9, 0xFA, 0x8E];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMON) Common Program Files folder for the current process architecture
            /// "%ProgramFiles%\Common Files"
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesCommon =>
                [0xF7, 0xF1, 0xED, 0x05, 0x9F, 0x6D, 0x47, 0xA2, 0xAA, 0xAE, 0x29, 0xD3, 0x17, 0xC6, 0xF0, 0x66];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMONX86) Common 32 bit Program Files folder (available to both 32/64 bit processes)
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesCommonX86 =>
                [0xDE, 0x97, 0x4D, 0x24, 0xD9, 0xC6, 0x4D, 0x3E, 0xBF, 0x91, 0xF4, 0x45, 0x51, 0x20, 0xB9, 0x17];

            /// <summary>
            /// (CSIDL_PROGRAMS) Start menu Programs folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs"
            /// </summary>
            internal static ReadOnlySpan<byte> Programs =>
                [0xA7, 0x7F, 0x5D, 0x77, 0x2E, 0x2B, 0x44, 0xC3, 0xA6, 0xA2, 0xAB, 0xA6, 0x01, 0x05, 0x4A, 0x51];

            /// <summary>
            /// (CSIDL_COMMON_DESKTOPDIRECTORY) Public Desktop folder
            /// "%PUBLIC%\Desktop"
            /// </summary>
            internal static ReadOnlySpan<byte> PublicDesktop =>
                [0xC4, 0xAA, 0x34, 0x0D, 0xF2, 0x0F, 0x48, 0x63, 0xAF, 0xEF, 0xF8, 0x7E, 0xF2, 0xE6, 0xBA, 0x25];

            /// <summary>
            /// (CSIDL_COMMON_DOCUMENTS) Public Documents folder
            /// "%PUBLIC%\Documents"
            /// </summary>
            internal static ReadOnlySpan<byte> PublicDocuments =>
                [0xED, 0x48, 0x24, 0xAF, 0xDC, 0xE4, 0x45, 0xA8, 0x81, 0xE2, 0xFC, 0x79, 0x65, 0x08, 0x36, 0x34];

            /// <summary>
            /// (CSIDL_COMMON_MUSIC) Public Music folder
            /// "%PUBLIC%\Music"
            /// </summary>
            internal static ReadOnlySpan<byte> PublicMusic =>
                [0x32, 0x14, 0xFA, 0xB5, 0x97, 0x57, 0x42, 0x98, 0xBB, 0x61, 0x92, 0xA9, 0xDE, 0xAA, 0x44, 0xFF];

            /// <summary>
            /// (CSIDL_COMMON_PICTURES) Public Pictures folder
            /// "%PUBLIC%\Pictures"
            /// </summary>
            internal static ReadOnlySpan<byte> PublicPictures =>
                [0xB6, 0xEB, 0xFB, 0x86, 0x69, 0x07, 0x41, 0x3C, 0x9A, 0xF7, 0x4F, 0xC2, 0xAB, 0xF0, 0x7C, 0xC5];

            /// <summary>
            /// (CSIDL_COMMON_VIDEO) Public Videos folder
            /// "%PUBLIC%\Videos"
            /// </summary>
            internal static ReadOnlySpan<byte> PublicVideos =>
                [0x24, 0x00, 0x18, 0x3A, 0x61, 0x85, 0x49, 0xFB, 0xA2, 0xD8, 0x4A, 0x39, 0x2A, 0x60, 0x2B, 0xA3];

            /// <summary>
            /// (CSIDL_RECENT) Recent Items folder
            /// "%APPDATA%\Microsoft\Windows\Recent"
            /// </summary>
            internal static ReadOnlySpan<byte> Recent =>
                [0xAE, 0x50, 0xC0, 0x81, 0xEB, 0xD2, 0x43, 0x8A, 0x86, 0x55, 0x8A, 0x09, 0x2E, 0x34, 0x98, 0x7A];

            /// <summary>
            /// (CSIDL_BITBUCKET) Recycle Bin virtual folder
            /// </summary>
            internal static ReadOnlySpan<byte> RecycleBinFolder =>
                [0xB7, 0x53, 0x40, 0x46, 0x3E, 0xCB, 0x4C, 0x18, 0xBE, 0x4E, 0x64, 0xCD, 0x4C, 0xB7, 0xD6, 0xAC];

            /// <summary>
            /// (CSIDL_RESOURCES) Resources fixed folder
            /// "%windir%\Resources"
            /// </summary>
            internal static ReadOnlySpan<byte> ResourceDir =>
                [0x8A, 0xD1, 0x0C, 0x31, 0x2A, 0xDB, 0x42, 0x96, 0xA8, 0xF7, 0xE4, 0x70, 0x12, 0x32, 0xC9, 0x72];

            /// <summary>
            /// (CSIDL_APPDATA) Roaming user application data folder
            /// "%APPDATA%" ("%USERPROFILE%\AppData\Roaming")
            /// </summary>
            internal static ReadOnlySpan<byte> RoamingAppData =>
                [0x3E, 0xB6, 0x85, 0xDB, 0x65, 0xF9, 0x4C, 0xF6, 0xA0, 0x3A, 0xE3, 0xEF, 0x65, 0x72, 0x9F, 0x3D];

            /// <summary>
            /// (CSIDL_SENDTO) SendTo folder
            /// "%APPDATA%\Microsoft\Windows\SendTo"
            /// </summary>
            internal static ReadOnlySpan<byte> SendTo =>
                [0x89, 0x83, 0x03, 0x6C, 0x27, 0xC0, 0x40, 0x4B, 0x8F, 0x08, 0x10, 0x2D, 0x10, 0xDC, 0xFD, 0x74];

            /// <summary>
            /// (CSIDL_STARTMENU) Start Menu folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu"
            /// </summary>
            internal static ReadOnlySpan<byte> StartMenu =>
                [0x62, 0x5B, 0x53, 0xC3, 0xAB, 0x48, 0x4E, 0xC1, 0xBA, 0x1F, 0xA1, 0xEF, 0x41, 0x46, 0xFC, 0x19];

            /// <summary>
            /// (CSIDL_STARTUP, CSIDL_ALTSTARTUP) Startup folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// </summary>
            internal static ReadOnlySpan<byte> Startup =>
                [0xB9, 0x7D, 0x20, 0xBB, 0xF4, 0x6A, 0x4C, 0x97, 0xBA, 0x10, 0x5E, 0x36, 0x08, 0x43, 0x08, 0x54];

            /// <summary>
            /// (CSIDL_SYSTEMX86) X86 System32 folder
            /// "%windir%\system32" or "%windir%\syswow64"
            /// </summary>
            internal static ReadOnlySpan<byte> SystemX86 =>
                [0xD6, 0x52, 0x31, 0xB0, 0xB2, 0xF1, 0x48, 0x57, 0xA4, 0xCE, 0xA8, 0xE7, 0xC6, 0xEA, 0x7D, 0x27];

            /// <summary>
            /// (CSIDL_TEMPLATES) Templates folder
            /// "%APPDATA%\Microsoft\Windows\Templates"
            /// </summary>
            internal static ReadOnlySpan<byte> Templates =>
                [0xA6, 0x32, 0x93, 0xE8, 0x66, 0x4E, 0x48, 0xDB, 0xA0, 0x79, 0xDF, 0x75, 0x9E, 0x05, 0x09, 0xF7];

            /// <summary>
            /// (CSIDL_MYVIDEO) Videos folder
            /// "%USERPROFILE%\Videos"
            /// </summary>
            internal static ReadOnlySpan<byte> Videos =>
                [0x18, 0x98, 0x9B, 0x1D, 0x99, 0xB5, 0x45, 0x5B, 0x84, 0x1C, 0xAB, 0x7C, 0x74, 0xE4, 0xDD, 0xFC];

            /// <summary>
            /// (CSIDL_WINDOWS) Windows folder "%windir%"
            /// </summary>
            internal static ReadOnlySpan<byte> Windows =>
                [0xF3, 0x8B, 0xF4, 0x04, 0x1D, 0x43, 0x42, 0xF2, 0x93, 0x05, 0x67, 0xDE, 0x0B, 0x28, 0xFC, 0x23];
        }
    }
}
