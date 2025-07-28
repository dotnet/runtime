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
            internal static Guid AdminTools =>
                new(0x724EF170, 0xA42D, 0x4FEF, 0x9F, 0x26, 0xB6, 0x0E, 0x84, 0x6F, 0xBA, 0x4F);

            /// <summary>
            /// (CSIDL_CDBURN_AREA) Temporary Burn folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Burn\Burn"
            /// </summary>
            internal static Guid CDBurning =>
                new(0x9E52AB10, 0xF80D, 0x49DF, 0xAC, 0xB8, 0x43, 0x30, 0xF5, 0x68, 0x78, 0x55);

            /// <summary>
            /// (CSIDL_COMMON_ADMINTOOLS) Common Administrative Tools
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\Administrative Tools"
            /// </summary>
            internal static Guid CommonAdminTools =>
                new(0xD0384E7D, 0xBAC3, 0x4797, 0x8F, 0x14, 0xCB, 0xA2, 0x29, 0xB3, 0x92, 0xB5);

            /// <summary>
            /// (CSIDL_COMMON_OEM_LINKS) OEM Links folder
            /// "%ALLUSERSPROFILE%\OEM Links"
            /// </summary>
            internal static Guid CommonOEMLinks =>
                new(0xC1BAE2D0, 0x10DF, 0x4334, 0xBE, 0xDD, 0x7A, 0xA2, 0x0B, 0x22, 0x7A, 0x9D);

            /// <summary>
            /// (CSIDL_COMMON_PROGRAMS) Common Programs folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs"
            /// </summary>
            internal static Guid CommonPrograms =>
                new(0x0139D44E, 0x6AFE, 0x49F2, 0x86, 0x90, 0x3D, 0xAF, 0xCA, 0xE6, 0xFF, 0xB8);

            /// <summary>
            /// (CSIDL_COMMON_STARTMENU) Common Start Menu folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu"
            /// </summary>
            internal static Guid CommonStartMenu =>
                new(0xA4115719, 0xD62E, 0x491D, 0xAA, 0x7C, 0xE7, 0x4B, 0x8B, 0xE3, 0xB0, 0x67);

            /// <summary>
            /// (CSIDL_COMMON_STARTUP, CSIDL_COMMON_ALTSTARTUP) Common Startup folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// </summary>
            internal static Guid CommonStartup =>
                new(0x82A5EA35, 0xD9CD, 0x47C5, 0x96, 0x29, 0xE1, 0x5D, 0x2F, 0x71, 0x4E, 0x6E);

            /// <summary>
            /// (CSIDL_COMMON_TEMPLATES) Common Templates folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Templates"
            /// </summary>
            internal static Guid CommonTemplates =>
                new(0xB94237E7, 0x57AC, 0x4347, 0x91, 0x51, 0xB0, 0x8C, 0x6C, 0x32, 0xD1, 0xF7);

            /// <summary>
            /// (CSIDL_DRIVES) Computer virtual folder
            /// </summary>
            internal static Guid ComputerFolder =>
                new(0x0AC0837C, 0xBBF8, 0x452A, 0x85, 0x0D, 0x79, 0xD0, 0x8E, 0x66, 0x7C, 0xA7);

            /// <summary>
            /// (CSIDL_CONNECTIONS) Network Connections virtual folder
            /// </summary>
            internal static Guid ConnectionsFolder =>
                new(0x6F0CD92B, 0x2E97, 0x45D1, 0x88, 0xFF, 0xB0, 0xD1, 0x86, 0xB8, 0xDE, 0xDD);

            /// <summary>
            /// (CSIDL_CONTROLS) Control Panel virtual folder
            /// </summary>
            internal static Guid ControlPanelFolder =>
                new(0x82A74AEB, 0xAEB4, 0x465C, 0xA0, 0x14, 0xD0, 0x97, 0xEE, 0x34, 0x6D, 0x63);

            /// <summary>
            /// (CSIDL_COOKIES) Cookies folder
            /// "%APPDATA%\Microsoft\Windows\Cookies"
            /// </summary>
            internal static Guid Cookies =>
                new(0x2B0F765D, 0xC0E9, 0x4171, 0x90, 0x8E, 0x08, 0xA6, 0x11, 0xB8, 0x4F, 0xF6);

            /// <summary>
            /// (CSIDL_DESKTOP, CSIDL_DESKTOPDIRECTORY) Desktop folder
            /// "%USERPROFILE%\Desktop"
            /// </summary>
            internal static Guid Desktop =>
                new(0xB4BFCC3A, 0xDB2C, 0x424C, 0xB0, 0x29, 0x7F, 0xE9, 0x9A, 0x87, 0xC6, 0x41);

            /// <summary>
            /// (CSIDL_MYDOCUMENTS, CSIDL_PERSONAL) Documents (My Documents) folder
            /// "%USERPROFILE%\Documents"
            /// </summary>
            internal static Guid Documents =>
                new(0xFDD39AD0, 0x238F, 0x46AF, 0xAD, 0xB4, 0x6C, 0x85, 0x48, 0x03, 0x69, 0xC7);

            /// <summary>
            /// (CSIDL_FAVORITES, CSIDL_COMMON_FAVORITES) Favorites folder
            /// "%USERPROFILE%\Favorites"
            /// </summary>
            internal static Guid Favorites =>
                new(0x1777F761, 0x68AD, 0x4D8A, 0x87, 0xBD, 0x30, 0xB7, 0x59, 0xFA, 0x33, 0xDD);

            /// <summary>
            /// (CSIDL_FONTS) Fonts folder
            /// "%windir%\Fonts"
            /// </summary>
            internal static Guid Fonts =>
                new(0xFD228CB7, 0xAE11, 0x4AE3, 0x86, 0x4C, 0x16, 0xF3, 0x91, 0x0A, 0xB8, 0xFE);

            /// <summary>
            /// (CSIDL_HISTORY) History folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\History"
            /// </summary>
            internal static Guid History =>
                new(0xD9DC8A3B, 0xB784, 0x432E, 0xA7, 0x81, 0x5A, 0x11, 0x30, 0xA7, 0x59, 0x63);

            /// <summary>
            /// (CSIDL_INTERNET_CACHE) Temporary Internet Files folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Temporary Internet Files"
            /// </summary>
            internal static Guid InternetCache =>
                new(0x352481E8, 0x33BE, 0x4251, 0xBA, 0x85, 0x60, 0x07, 0xCA, 0xED, 0xCF, 0x9D);

            /// <summary>
            /// (CSIDL_INTERNET) The Internet virtual folder
            /// </summary>
            internal static Guid InternetFolder =>
                new(0x4D9F7874, 0x4E0C, 0x4904, 0x96, 0x7B, 0x40, 0xB0, 0xD2, 0x0C, 0x3E, 0x4B);

            /// <summary>
            /// (CSIDL_LOCAL_APPDATA) Local folder
            /// "%LOCALAPPDATA%" ("%USERPROFILE%\AppData\Local")
            /// </summary>
            internal static Guid LocalAppData =>
                new(0xF1B32785, 0x6FBA, 0x4FCF, 0x9D, 0x55, 0x7B, 0x8E, 0x7F, 0x15, 0x70, 0x91);

            /// <summary>
            /// (CSIDL_RESOURCES_LOCALIZED) Fixed localized resources folder
            /// "%windir%\resources\0409" (per active codepage)
            /// </summary>
            internal static Guid LocalizedResourcesDir =>
                new(0x2A00375E, 0x224C, 0x49DE, 0xB8, 0xD1, 0x44, 0x0D, 0xF7, 0xEF, 0x3D, 0xDC);

            /// <summary>
            /// (CSIDL_MYMUSIC) Music folder
            /// "%USERPROFILE%\Music"
            /// </summary>
            internal static Guid Music =>
                new(0x4BD8D571, 0x6D19, 0x48D3, 0xBE, 0x97, 0x42, 0x22, 0x20, 0x08, 0x0E, 0x43);

            /// <summary>
            /// (CSIDL_NETHOOD) Network shortcuts folder "%APPDATA%\Microsoft\Windows\Network Shortcuts"
            /// </summary>
            internal static Guid NetHood =>
                new(0xC5ABBF53, 0xE17F, 0x4121, 0x89, 0x00, 0x86, 0x62, 0x6F, 0xC2, 0xC9, 0x73);

            /// <summary>
            /// (CSIDL_NETWORK, CSIDL_COMPUTERSNEARME) Network virtual folder
            /// </summary>
            internal static Guid NetworkFolder =>
                new(0xD20BEEC4, 0x5CA8, 0x4905, 0xAE, 0x3B, 0xBF, 0x25, 0x1E, 0xA0, 0x9B, 0x53);

            /// <summary>
            /// (CSIDL_MYPICTURES) Pictures folder "%USERPROFILE%\Pictures"
            /// </summary>
            internal static Guid Pictures =>
                new(0x33E28130, 0x4E1E, 0x4676, 0x83, 0x5A, 0x98, 0x39, 0x5C, 0x3B, 0xC3, 0xBB);

            /// <summary>
            /// (CSIDL_PRINTERS) Printers virtual folder
            /// </summary>
            internal static Guid PrintersFolder =>
                new(0x76FC4E2D, 0xD6AD, 0x4519, 0xA6, 0x63, 0x37, 0xBD, 0x56, 0x06, 0x81, 0x85);

            /// <summary>
            /// (CSIDL_PRINTHOOD) Printer Shortcuts folder
            /// "%APPDATA%\Microsoft\Windows\Printer Shortcuts"
            /// </summary>
            internal static Guid PrintHood =>
                new(0x9274BD8D, 0xCFD1, 0x41C3, 0xB3, 0x5E, 0xB1, 0x3F, 0x55, 0xA7, 0x58, 0xF4);

            /// <summary>
            /// (CSIDL_PROFILE) The root users profile folder "%USERPROFILE%"
            /// ("%SystemDrive%\Users\%USERNAME%")
            /// </summary>
            internal static Guid Profile =>
                new(0x5E6C858F, 0x0E22, 0x4760, 0x9A, 0xFE, 0xEA, 0x33, 0x17, 0xB6, 0x71, 0x73);

            /// <summary>
            /// (CSIDL_COMMON_APPDATA) ProgramData folder
            /// "%ALLUSERSPROFILE%" ("%ProgramData%", "%SystemDrive%\ProgramData")
            /// </summary>
            internal static Guid ProgramData =>
                new(0x62AB5D82, 0xFDC1, 0x4DC3, 0xA9, 0xDD, 0x07, 0x0D, 0x1D, 0x49, 0x5D, 0x97);

            /// <summary>
            /// (CSIDL_PROGRAM_FILES) Program Files folder for the current process architecture
            /// "%ProgramFiles%" ("%SystemDrive%\Program Files")
            /// </summary>
            internal static Guid ProgramFiles =>
                new(0x905e63b6, 0xc1bf, 0x494e, 0xb2, 0x9c, 0x65, 0xb7, 0x32, 0xd3, 0xd2, 0x1a);

            /// <summary>
            /// (CSIDL_PROGRAM_FILESX86) 32 bit Program Files folder (available to both 32/64 bit processes)
            /// </summary>
            internal static Guid ProgramFilesX86 =>
                new(0x7C5A40EF, 0xA0FB, 0x4BFC, 0x87, 0x4A, 0xC0, 0xF2, 0xE0, 0xB9, 0xFA, 0x8E);

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMON) Common Program Files folder for the current process architecture
            /// "%ProgramFiles%\Common Files"
            /// </summary>
            internal static Guid ProgramFilesCommon =>
                new(0xF7F1ED05, 0x9F6D, 0x47A2, 0xAA, 0xAE, 0x29, 0xD3, 0x17, 0xC6, 0xF0, 0x66);

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMONX86) Common 32 bit Program Files folder (available to both 32/64 bit processes)
            /// </summary>
            internal static Guid ProgramFilesCommonX86 =>
                new(0xDE974D24, 0xD9C6, 0x4D3E, 0xBF, 0x91, 0xF4, 0x45, 0x51, 0x20, 0xB9, 0x17);

            /// <summary>
            /// (CSIDL_PROGRAMS) Start menu Programs folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs"
            /// </summary>
            internal static Guid Programs =>
                new(0xA77F5D77, 0x2E2B, 0x44C3, 0xA6, 0xA2, 0xAB, 0xA6, 0x01, 0x05, 0x4A, 0x51);

            /// <summary>
            /// (CSIDL_COMMON_DESKTOPDIRECTORY) Public Desktop folder
            /// "%PUBLIC%\Desktop"
            /// </summary>
            internal static Guid PublicDesktop =>
                new(0xC4AA340D, 0xF20F, 0x4863, 0xAF, 0xEF, 0xF8, 0x7E, 0xF2, 0xE6, 0xBA, 0x25);

            /// <summary>
            /// (CSIDL_COMMON_DOCUMENTS) Public Documents folder
            /// "%PUBLIC%\Documents"
            /// </summary>
            internal static Guid PublicDocuments =>
                new(0xED4824AF, 0xDCE4, 0x45A8, 0x81, 0xE2, 0xFC, 0x79, 0x65, 0x08, 0x36, 0x34);

            /// <summary>
            /// (CSIDL_COMMON_MUSIC) Public Music folder
            /// "%PUBLIC%\Music"
            /// </summary>
            internal static Guid PublicMusic =>
                new(0x3214FAB5, 0x9757, 0x4298, 0xBB, 0x61, 0x92, 0xA9, 0xDE, 0xAA, 0x44, 0xFF);

            /// <summary>
            /// (CSIDL_COMMON_PICTURES) Public Pictures folder
            /// "%PUBLIC%\Pictures"
            /// </summary>
            internal static Guid PublicPictures =>
                new(0xB6EBFB86, 0x6907, 0x413C, 0x9A, 0xF7, 0x4F, 0xC2, 0xAB, 0xF0, 0x7C, 0xC5);

            /// <summary>
            /// (CSIDL_COMMON_VIDEO) Public Videos folder
            /// "%PUBLIC%\Videos"
            /// </summary>
            internal static Guid PublicVideos =>
                new(0x2400183A, 0x6185, 0x49FB, 0xA2, 0xD8, 0x4A, 0x39, 0x2A, 0x60, 0x2B, 0xA3);

            /// <summary>
            /// (CSIDL_RECENT) Recent Items folder
            /// "%APPDATA%\Microsoft\Windows\Recent"
            /// </summary>
            internal static Guid Recent =>
                new(0xAE50C081, 0xEBD2, 0x438A, 0x86, 0x55, 0x8A, 0x09, 0x2E, 0x34, 0x98, 0x7A);

            /// <summary>
            /// (CSIDL_BITBUCKET) Recycle Bin virtual folder
            /// </summary>
            internal static Guid RecycleBinFolder =>
                new(0xB7534046, 0x3ECB, 0x4C18, 0xBE, 0x4E, 0x64, 0xCD, 0x4C, 0xB7, 0xD6, 0xAC);

            /// <summary>
            /// (CSIDL_RESOURCES) Resources fixed folder
            /// "%windir%\Resources"
            /// </summary>
            internal static Guid ResourceDir =>
                new(0x8AD10C31, 0x2ADB, 0x4296, 0xA8, 0xF7, 0xE4, 0x70, 0x12, 0x32, 0xC9, 0x72);

            /// <summary>
            /// (CSIDL_APPDATA) Roaming user application data folder
            /// "%APPDATA%" ("%USERPROFILE%\AppData\Roaming")
            /// </summary>
            internal static Guid RoamingAppData =>
                new(0x3EB685DB, 0x65F9, 0x4CF6, 0xA0, 0x3A, 0xE3, 0xEF, 0x65, 0x72, 0x9F, 0x3D);

            /// <summary>
            /// (CSIDL_SENDTO) SendTo folder
            /// "%APPDATA%\Microsoft\Windows\SendTo"
            /// </summary>
            internal static Guid SendTo =>
                new(0x8983036C, 0x27C0, 0x404B, 0x8F, 0x08, 0x10, 0x2D, 0x10, 0xDC, 0xFD, 0x74);

            /// <summary>
            /// (CSIDL_STARTMENU) Start Menu folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu"
            /// </summary>
            internal static Guid StartMenu =>
                new(0x625B53C3, 0xAB48, 0x4EC1, 0xBA, 0x1F, 0xA1, 0xEF, 0x41, 0x46, 0xFC, 0x19);

            /// <summary>
            /// (CSIDL_STARTUP, CSIDL_ALTSTARTUP) Startup folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// </summary>
            internal static Guid Startup =>
                new(0xB97D20BB, 0xF46A, 0x4C97, 0xBA, 0x10, 0x5E, 0x36, 0x08, 0x43, 0x08, 0x54);

            /// <summary>
            /// (CSIDL_SYSTEMX86) X86 System32 folder
            /// "%windir%\system32" or "%windir%\syswow64"
            /// </summary>
            internal static Guid SystemX86 =>
                new(0xD65231B0, 0xB2F1, 0x4857, 0xA4, 0xCE, 0xA8, 0xE7, 0xC6, 0xEA, 0x7D, 0x27);

            /// <summary>
            /// (CSIDL_TEMPLATES) Templates folder
            /// "%APPDATA%\Microsoft\Windows\Templates"
            /// </summary>
            internal static Guid Templates =>
                new(0xA63293E8, 0x664E, 0x48DB, 0xA0, 0x79, 0xDF, 0x75, 0x9E, 0x05, 0x09, 0xF7);

            /// <summary>
            /// (CSIDL_MYVIDEO) Videos folder
            /// "%USERPROFILE%\Videos"
            /// </summary>
            internal static Guid Videos =>
                new(0x18989B1D, 0x99B5, 0x455B, 0x84, 0x1C, 0xAB, 0x7C, 0x74, 0xE4, 0xDD, 0xFC);

            /// <summary>
            /// (CSIDL_WINDOWS) Windows folder "%windir%"
            /// </summary>
            internal static Guid Windows =>
                new(0xF38BF404, 0x1D43, 0x42F2, 0x93, 0x05, 0x67, 0xDE, 0x0B, 0x28, 0xFC, 0x23);
        }
    }
}
