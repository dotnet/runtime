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
            /// {724EF170-A42D-4FEF-9F26-B60E846FBA4F}
            /// </summary>
            internal static ReadOnlySpan<byte> AdminTools =>
                [0x70, 0xF1, 0x4E, 0x72, 0x2D, 0xA4, 0xEF, 0x4F, 0x9F, 0x26, 0xB6, 0x0E, 0x84, 0x6F, 0xBA, 0x4F];

            /// <summary>
            /// (CSIDL_CDBURN_AREA) Temporary Burn folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Burn\Burn"
            /// {9E52AB10-F80D-49DF-ACB8-4330F5687855}
            /// </summary>
            internal static ReadOnlySpan<byte> CDBurning =>
                [0x10, 0xAB, 0x52, 0x9E, 0x0D, 0xF8, 0xDF, 0x49, 0xAC, 0xB8, 0x43, 0x30, 0xF5, 0x68, 0x78, 0x55];

            /// <summary>
            /// (CSIDL_COMMON_ADMINTOOLS) Common Administrative Tools
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\Administrative Tools"
            /// {D0384E7D-BAC3-4797-8F14-CBA229B392B5}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonAdminTools =>
                [0x7D, 0x4E, 0x38, 0xD0, 0xC3, 0xBA, 0x97, 0x47, 0x8F, 0x14, 0xCB, 0xA2, 0x29, 0xB3, 0x92, 0xB5];

            /// <summary>
            /// (CSIDL_COMMON_OEM_LINKS) OEM Links folder
            /// "%ALLUSERSPROFILE%\OEM Links"
            /// {C1BAE2D0-10DF-4334-BEDD-7AA20B227A9D}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonOEMLinks =>
                [0xD0, 0xE2, 0xBA, 0xC1, 0xDF, 0x10, 0x34, 0x43, 0xBE, 0xDD, 0x7A, 0xA2, 0x0B, 0x22, 0x7A, 0x9D];

            /// <summary>
            /// (CSIDL_COMMON_PROGRAMS) Common Programs folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs"
            /// {0139D44E-6AFE-49F2-8690-3DAFCAE6FFB8}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonPrograms =>
                [0x4E, 0xD4, 0x39, 0x01, 0xFE, 0x6A, 0xF2, 0x49, 0x86, 0x90, 0x3D, 0xAF, 0xCA, 0xE6, 0xFF, 0xB8];

            /// <summary>
            /// (CSIDL_COMMON_STARTMENU) Common Start Menu folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu"
            /// {A4115719-D62E-491D-AA7C-E74B8BE3B067}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonStartMenu =>
                [0x19, 0x57, 0x11, 0xA4, 0x2E, 0xD6, 0x1D, 0x49, 0xAA, 0x7C, 0xE7, 0x4B, 0x8B, 0xE3, 0xB0, 0x67];

            /// <summary>
            /// (CSIDL_COMMON_STARTUP, CSIDL_COMMON_ALTSTARTUP) Common Startup folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// {82A5EA35-D9CD-47C5-9629-E15D2F714E6E}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonStartup =>
                [0x35, 0xEA, 0xA5, 0x82, 0xCD, 0xD9, 0xC5, 0x47, 0x96, 0x29, 0xE1, 0x5D, 0x2F, 0x71, 0x4E, 0x6E];

            /// <summary>
            /// (CSIDL_COMMON_TEMPLATES) Common Templates folder
            /// "%ALLUSERSPROFILE%\Microsoft\Windows\Templates"
            /// {B94237E7-57AC-4347-9151-B08C6C32D1F7}
            /// </summary>
            internal static ReadOnlySpan<byte> CommonTemplates =>
                [0xE7, 0x37, 0x42, 0xB9, 0xAC, 0x57, 0x47, 0x43, 0x91, 0x51, 0xB0, 0x8C, 0x6C, 0x32, 0xD1, 0xF7];

            /// <summary>
            /// (CSIDL_DRIVES) Computer virtual folder
            /// {0AC0837C-BBF8-452A-850D-79D08E667CA7}
            /// </summary>
            internal static ReadOnlySpan<byte> ComputerFolder =>
                [0x7C, 0x83, 0xC0, 0x0A, 0xF8, 0xBB, 0x2A, 0x45, 0x85, 0x0D, 0x79, 0xD0, 0x8E, 0x66, 0x7C, 0xA7];

            /// <summary>
            /// (CSIDL_CONNECTIONS) Network Connections virtual folder
            /// {6F0CD92B-2E97-45D1-88FF-B0D186B8DEDD}
            /// </summary>
            internal static ReadOnlySpan<byte> ConnectionsFolder =>
                [0x2B, 0xD9, 0x0C, 0x6F, 0x97, 0x2E, 0xD1, 0x45, 0x88, 0xFF, 0xB0, 0xD1, 0x86, 0xB8, 0xDE, 0xDD];

            /// <summary>
            /// (CSIDL_CONTROLS) Control Panel virtual folder
            /// {82A74AEB-AEB4-465C-A014-D097EE346D63}
            /// </summary>
            internal static ReadOnlySpan<byte> ControlPanelFolder =>
                [0xEB, 0x4A, 0xA7, 0x82, 0xB4, 0xAE, 0x5C, 0x46, 0xA0, 0x14, 0xD0, 0x97, 0xEE, 0x34, 0x6D, 0x63];

            /// <summary>
            /// (CSIDL_COOKIES) Cookies folder
            /// "%APPDATA%\Microsoft\Windows\Cookies"
            /// {2B0F765D-C0E9-4171-908E-08A611B84FF6}
            /// </summary>
            internal static ReadOnlySpan<byte> Cookies =>
                [0x5D, 0x76, 0x0F, 0x2B, 0xE9, 0xC0, 0x71, 0x41, 0x90, 0x8E, 0x08, 0xA6, 0x11, 0xB8, 0x4F, 0xF6];

            /// <summary>
            /// (CSIDL_DESKTOP, CSIDL_DESKTOPDIRECTORY) Desktop folder
            /// "%USERPROFILE%\Desktop"
            /// {B4BFCC3A-DB2C-424C-B029-7FE99A87C641}
            /// </summary>
            internal static ReadOnlySpan<byte> Desktop =>
                [0x3A, 0xCC, 0xBF, 0xB4, 0x2C, 0xDB, 0x4C, 0x42, 0xB0, 0x29, 0x7F, 0xE9, 0x9A, 0x87, 0xC6, 0x41];

            /// <summary>
            /// (CSIDL_MYDOCUMENTS, CSIDL_PERSONAL) Documents (My Documents) folder
            /// "%USERPROFILE%\Documents"
            /// {FDD39AD0-238F-46AF-ADB4-6C85480369C7}
            /// </summary>
            internal static ReadOnlySpan<byte> Documents =>
                [0xD0, 0x9A, 0xD3, 0xFD, 0x8F, 0x23, 0xAF, 0x46, 0xAD, 0xB4, 0x6C, 0x85, 0x48, 0x03, 0x69, 0xC7];

            /// <summary>
            /// (CSIDL_FAVORITES, CSIDL_COMMON_FAVORITES) Favorites folder
            /// "%USERPROFILE%\Favorites"
            /// {1777F761-68AD-4D8A-87BD-30B759FA33DD}
            /// </summary>
            internal static ReadOnlySpan<byte> Favorites =>
                [0x61, 0xF7, 0x77, 0x17, 0xAD, 0x68, 0x8A, 0x4D, 0x87, 0xBD, 0x30, 0xB7, 0x59, 0xFA, 0x33, 0xDD];

            /// <summary>
            /// (CSIDL_FONTS) Fonts folder
            /// "%windir%\Fonts"
            /// {FD228CB7-AE11-4AE3-864C-16F3910AB8FE}
            /// </summary>
            internal static ReadOnlySpan<byte> Fonts =>
                [0xB7, 0x8C, 0x22, 0xFD, 0x11, 0xAE, 0xE3, 0x4A, 0x86, 0x4C, 0x16, 0xF3, 0x91, 0x0A, 0xB8, 0xFE];

            /// <summary>
            /// (CSIDL_HISTORY) History folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\History"
            /// {D9DC8A3B-B784-432E-A781-5A1130A75963}
            /// </summary>
            internal static ReadOnlySpan<byte> History =>
                [0x3B, 0x8A, 0xDC, 0xD9, 0x84, 0xB7, 0x2E, 0x43, 0xA7, 0x81, 0x5A, 0x11, 0x30, 0xA7, 0x59, 0x63];

            /// <summary>
            /// (CSIDL_INTERNET_CACHE) Temporary Internet Files folder
            /// "%LOCALAPPDATA%\Microsoft\Windows\Temporary Internet Files"
            /// {352481E8-33BE-4251-BA85-6007CAEDCF9D}
            /// </summary>
            internal static ReadOnlySpan<byte> InternetCache =>
                [0xE8, 0x81, 0x24, 0x35, 0xBE, 0x33, 0x51, 0x42, 0xBA, 0x85, 0x60, 0x07, 0xCA, 0xED, 0xCF, 0x9D];

            /// <summary>
            /// (CSIDL_INTERNET) The Internet virtual folder
            /// {4D9F7874-4E0C-4904-967B-40B0D20C3E4B}
            /// </summary>
            internal static ReadOnlySpan<byte> InternetFolder =>
                [0x74, 0x78, 0x9F, 0x4D, 0x0C, 0x4E, 0x04, 0x49, 0x96, 0x7B, 0x40, 0xB0, 0xD2, 0x0C, 0x3E, 0x4B];

            /// <summary>
            /// (CSIDL_LOCAL_APPDATA) Local folder
            /// "%LOCALAPPDATA%" ("%USERPROFILE%\AppData\Local")
            /// {F1B32785-6FBA-4FCF-9D55-7B8E7F157091}
            /// </summary>
            internal static ReadOnlySpan<byte> LocalAppData =>
                [0x85, 0x27, 0xB3, 0xF1, 0xBA, 0x6F, 0xCF, 0x4F, 0x9D, 0x55, 0x7B, 0x8E, 0x7F, 0x15, 0x70, 0x91];

            /// <summary>
            /// (CSIDL_RESOURCES_LOCALIZED) Fixed localized resources folder
            /// "%windir%\resources\0409" (per active codepage)
            /// {2A00375E-224C-49DE-B8D1-440DF7EF3DDC}
            /// </summary>
            internal static ReadOnlySpan<byte> LocalizedResourcesDir =>
                [0x5E, 0x37, 0x00, 0x2A, 0x4C, 0x22, 0xDE, 0x49, 0xB8, 0xD1, 0x44, 0x0D, 0xF7, 0xEF, 0x3D, 0xDC];

            /// <summary>
            /// (CSIDL_MYMUSIC) Music folder
            /// "%USERPROFILE%\Music"
            /// {4BD8D571-6D19-48D3-BE97-422220080E43}
            /// </summary>
            internal static ReadOnlySpan<byte> Music =>
                [0x71, 0xD5, 0xD8, 0x4B, 0x19, 0x6D, 0xD3, 0x48, 0xBE, 0x97, 0x42, 0x22, 0x20, 0x08, 0x0E, 0x43];

            /// <summary>
            /// (CSIDL_NETHOOD) Network shortcuts folder "%APPDATA%\Microsoft\Windows\Network Shortcuts"
            /// {C5ABBF53-E17F-4121-8900-86626FC2C973}
            /// </summary>
            internal static ReadOnlySpan<byte> NetHood =>
                [0x53, 0xBF, 0xAB, 0xC5, 0x7F, 0xE1, 0x21, 0x41, 0x89, 0x00, 0x86, 0x62, 0x6F, 0xC2, 0xC9, 0x73];

            /// <summary>
            /// (CSIDL_NETWORK, CSIDL_COMPUTERSNEARME) Network virtual folder
            /// {D20BEEC4-5CA8-4905-AE3B-BF251EA09B53}
            /// </summary>
            internal static ReadOnlySpan<byte> NetworkFolder =>
                [0xC4, 0xEE, 0x0B, 0xD2, 0xA8, 0x5C, 0x05, 0x49, 0xAE, 0x3B, 0xBF, 0x25, 0x1E, 0xA0, 0x9B, 0x53];

            /// <summary>
            /// (CSIDL_MYPICTURES) Pictures folder "%USERPROFILE%\Pictures"
            /// {33E28130-4E1E-4676-835A-98395C3BC3BB}
            /// </summary>
            internal static ReadOnlySpan<byte> Pictures =>
                [0x30, 0x81, 0xE2, 0x33, 0x1E, 0x4E, 0x76, 0x46, 0x83, 0x5A, 0x98, 0x39, 0x5C, 0x3B, 0xC3, 0xBB];

            /// <summary>
            /// (CSIDL_PRINTERS) Printers virtual folder
            /// {76FC4E2D-D6AD-4519-A663-37BD56068185}
            /// </summary>
            internal static ReadOnlySpan<byte> PrintersFolder =>
                [0x2D, 0x4E, 0xFC, 0x76, 0xAD, 0xD6, 0x19, 0x45, 0xA6, 0x63, 0x37, 0xBD, 0x56, 0x06, 0x81, 0x85];

            /// <summary>
            /// (CSIDL_PRINTHOOD) Printer Shortcuts folder
            /// "%APPDATA%\Microsoft\Windows\Printer Shortcuts"
            /// {9274BD8D-CFD1-41C3-B35E-B13F55A758F4}
            /// </summary>
            internal static ReadOnlySpan<byte> PrintHood =>
                [0x8D, 0xBD, 0x74, 0x92, 0xD1, 0xCF, 0xC3, 0x41, 0xB3, 0x5E, 0xB1, 0x3F, 0x55, 0xA7, 0x58, 0xF4];

            /// <summary>
            /// (CSIDL_PROFILE) The root users profile folder "%USERPROFILE%"
            /// ("%SystemDrive%\Users\%USERNAME%")
            /// {5E6C858F-0E22-4760-9AFE-EA3317B67173}
            /// </summary>
            internal static ReadOnlySpan<byte> Profile =>
                [0x8F, 0x85, 0x6C, 0x5E, 0x22, 0x0E, 0x60, 0x47, 0x9A, 0xFE, 0xEA, 0x33, 0x17, 0xB6, 0x71, 0x73];

            /// <summary>
            /// (CSIDL_COMMON_APPDATA) ProgramData folder
            /// "%ALLUSERSPROFILE%" ("%ProgramData%", "%SystemDrive%\ProgramData")
            /// {62AB5D82-FDC1-4DC3-A9DD-070D1D495D97}
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramData =>
                [0x82, 0x5D, 0xAB, 0x62, 0xC1, 0xFD, 0xC3, 0x4D, 0xA9, 0xDD, 0x07, 0x0D, 0x1D, 0x49, 0x5D, 0x97];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES) Program Files folder for the current process architecture
            /// "%ProgramFiles%" ("%SystemDrive%\Program Files")
            /// {905e63b6-c1bf-494e-b29c-65b732d3d21a}
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFiles =>
                [0xb6, 0x63, 0x5e, 0x90, 0xbf, 0xc1, 0x4e, 0x49, 0xb2, 0x9c, 0x65, 0xb7, 0x32, 0xd3, 0xd2, 0x1a];

            /// <summary>
            /// (CSIDL_PROGRAM_FILESX86) 32 bit Program Files folder (available to both 32/64 bit processes)
            /// {7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesX86 =>
                [0xEF, 0x40, 0x5A, 0x7C, 0xFB, 0xA0, 0xFC, 0x4B, 0x87, 0x4A, 0xC0, 0xF2, 0xE0, 0xB9, 0xFA, 0x8E];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMON) Common Program Files folder for the current process architecture
            /// "%ProgramFiles%\Common Files"
            /// {F7F1ED05-9F6D-47A2-AAAE-29D317C6F066}
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesCommon =>
                [0x05, 0xED, 0xF1, 0xF7, 0x6D, 0x9F, 0xA2, 0x47, 0xAA, 0xAE, 0x29, 0xD3, 0x17, 0xC6, 0xF0, 0x66];

            /// <summary>
            /// (CSIDL_PROGRAM_FILES_COMMONX86) Common 32 bit Program Files folder (available to both 32/64 bit processes)
            /// {DE974D24-D9C6-4D3E-BF91-F4455120B917}
            /// </summary>
            internal static ReadOnlySpan<byte> ProgramFilesCommonX86 =>
                [0x24, 0x4D, 0x97, 0xDE, 0xC6, 0xD9, 0x3E, 0x4D, 0xBF, 0x91, 0xF4, 0x45, 0x51, 0x20, 0xB9, 0x17];

            /// <summary>
            /// (CSIDL_PROGRAMS) Start menu Programs folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs"
            /// {A77F5D77-2E2B-44C3-A6A2-ABA601054A51}
            /// </summary>
            internal static ReadOnlySpan<byte> Programs =>
                [0x77, 0x5D, 0x7F, 0xA7, 0x2B, 0x2E, 0xC3, 0x44, 0xA6, 0xA2, 0xAB, 0xA6, 0x01, 0x05, 0x4A, 0x51];

            /// <summary>
            /// (CSIDL_COMMON_DESKTOPDIRECTORY) Public Desktop folder
            /// "%PUBLIC%\Desktop"
            /// {C4AA340D-F20F-4863-AFEF-F87EF2E6BA25}
            /// </summary>
            internal static ReadOnlySpan<byte> PublicDesktop =>
                [0x0D, 0x34, 0xAA, 0xC4, 0x0F, 0xF2, 0x63, 0x48, 0xAF, 0xEF, 0xF8, 0x7E, 0xF2, 0xE6, 0xBA, 0x25];

            /// <summary>
            /// (CSIDL_COMMON_DOCUMENTS) Public Documents folder
            /// "%PUBLIC%\Documents"
            /// {ED4824AF-DCE4-45A8-81E2-FC7965083634}
            /// </summary>
            internal static ReadOnlySpan<byte> PublicDocuments =>
                [0xAF, 0x24, 0x48, 0xED, 0xE4, 0xDC, 0xA8, 0x45, 0x81, 0xE2, 0xFC, 0x79, 0x65, 0x08, 0x36, 0x34];

            /// <summary>
            /// (CSIDL_COMMON_MUSIC) Public Music folder
            /// "%PUBLIC%\Music"
            /// {3214FAB5-9757-4298-BB61-92A9DEAA44FF}
            /// </summary>
            internal static ReadOnlySpan<byte> PublicMusic =>
                [0xB5, 0xFA, 0x14, 0x32, 0x57, 0x97, 0x98, 0x42, 0xBB, 0x61, 0x92, 0xA9, 0xDE, 0xAA, 0x44, 0xFF];

            /// <summary>
            /// (CSIDL_COMMON_PICTURES) Public Pictures folder
            /// "%PUBLIC%\Pictures"
            /// {B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5}
            /// </summary>
            internal static ReadOnlySpan<byte> PublicPictures =>
                [0x86, 0xFB, 0xEB, 0xB6, 0x07, 0x69, 0x3C, 0x41, 0x9A, 0xF7, 0x4F, 0xC2, 0xAB, 0xF0, 0x7C, 0xC5];

            /// <summary>
            /// (CSIDL_COMMON_VIDEO) Public Videos folder
            /// "%PUBLIC%\Videos"
            /// {2400183A-6185-49FB-A2D8-4A392A602BA3}
            /// </summary>
            internal static ReadOnlySpan<byte> PublicVideos =>
                [0x3A, 0x18, 0x00, 0x24, 0x85, 0x61, 0xFB, 0x49, 0xA2, 0xD8, 0x4A, 0x39, 0x2A, 0x60, 0x2B, 0xA3];

            /// <summary>
            /// (CSIDL_RECENT) Recent Items folder
            /// "%APPDATA%\Microsoft\Windows\Recent"
            /// {AE50C081-EBD2-438A-8655-8A092E34987A}
            /// </summary>
            internal static ReadOnlySpan<byte> Recent =>
                [0x81, 0xC0, 0x50, 0xAE, 0xD2, 0xEB, 0x8A, 0x43, 0x86, 0x55, 0x8A, 0x09, 0x2E, 0x34, 0x98, 0x7A];

            /// <summary>
            /// (CSIDL_BITBUCKET) Recycle Bin virtual folder
            /// {B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC}
            /// </summary>
            internal static ReadOnlySpan<byte> RecycleBinFolder =>
                [0x46, 0x40, 0x53, 0xB7, 0xCB, 0x3E, 0x18, 0x4C, 0xBE, 0x4E, 0x64, 0xCD, 0x4C, 0xB7, 0xD6, 0xAC];

            /// <summary>
            /// (CSIDL_RESOURCES) Resources fixed folder
            /// "%windir%\Resources"
            /// {8AD10C31-2ADB-4296-A8F7-E4701232C972}
            /// </summary>
            internal static ReadOnlySpan<byte> ResourceDir =>
                [0x31, 0x0C, 0xD1, 0x8A, 0xDB, 0x2A, 0x96, 0x42, 0xA8, 0xF7, 0xE4, 0x70, 0x12, 0x32, 0xC9, 0x72];

            /// <summary>
            /// (CSIDL_APPDATA) Roaming user application data folder
            /// "%APPDATA%" ("%USERPROFILE%\AppData\Roaming")
            /// {3EB685DB-65F9-4CF6-A03A-E3EF65729F3D}
            /// </summary>
            internal static ReadOnlySpan<byte> RoamingAppData =>
                [0xDB, 0x85, 0xB6, 0x3E, 0xF9, 0x65, 0xF6, 0x4C, 0xA0, 0x3A, 0xE3, 0xEF, 0x65, 0x72, 0x9F, 0x3D];

            /// <summary>
            /// (CSIDL_SENDTO) SendTo folder
            /// "%APPDATA%\Microsoft\Windows\SendTo"
            /// {8983036C-27C0-404B-8F08-102D10DCFD74}
            /// </summary>
            internal static ReadOnlySpan<byte> SendTo =>
                [0x6C, 0x03, 0x83, 0x89, 0xC0, 0x27, 0x4B, 0x40, 0x8F, 0x08, 0x10, 0x2D, 0x10, 0xDC, 0xFD, 0x74];

            /// <summary>
            /// (CSIDL_STARTMENU) Start Menu folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu"
            /// {625B53C3-AB48-4EC1-BA1F-A1EF4146FC19}
            /// </summary>
            internal static ReadOnlySpan<byte> StartMenu =>
                [0xC3, 0x53, 0x5B, 0x62, 0x48, 0xAB, 0xC1, 0x4E, 0xBA, 0x1F, 0xA1, 0xEF, 0x41, 0x46, 0xFC, 0x19];

            /// <summary>
            /// (CSIDL_STARTUP, CSIDL_ALTSTARTUP) Startup folder
            /// "%APPDATA%\Microsoft\Windows\Start Menu\Programs\StartUp"
            /// {B97D20BB-F46A-4C97-BA10-5E3608430854}
            /// </summary>
            internal static ReadOnlySpan<byte> Startup =>
                [0xBB, 0x20, 0x7D, 0xB9, 0x6A, 0xF4, 0x97, 0x4C, 0xBA, 0x10, 0x5E, 0x36, 0x08, 0x43, 0x08, 0x54];

            /// <summary>
            /// (CSIDL_SYSTEMX86) X86 System32 folder
            /// "%windir%\system32" or "%windir%\syswow64"
            /// {D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27}
            /// </summary>
            internal static ReadOnlySpan<byte> SystemX86 =>
                [0xB0, 0x31, 0x52, 0xD6, 0xF1, 0xB2, 0x57, 0x48, 0xA4, 0xCE, 0xA8, 0xE7, 0xC6, 0xEA, 0x7D, 0x27];

            /// <summary>
            /// (CSIDL_TEMPLATES) Templates folder
            /// "%APPDATA%\Microsoft\Windows\Templates"
            /// {A63293E8-664E-48DB-A079-DF759E0509F7}
            /// </summary>
            internal static ReadOnlySpan<byte> Templates =>
                [0xE8, 0x93, 0x32, 0xA6, 0x4E, 0x66, 0xDB, 0x48, 0xA0, 0x79, 0xDF, 0x75, 0x9E, 0x05, 0x09, 0xF7];

            /// <summary>
            /// (CSIDL_MYVIDEO) Videos folder
            /// "%USERPROFILE%\Videos"
            /// {18989B1D-99B5-455B-841C-AB7C74E4DDFC}
            /// </summary>
            internal static ReadOnlySpan<byte> Videos =>
                [0x1D, 0x9B, 0x98, 0x18, 0xB5, 0x99, 0x5B, 0x45, 0x84, 0x1C, 0xAB, 0x7C, 0x74, 0xE4, 0xDD, 0xFC];

            /// <summary>
            /// (CSIDL_WINDOWS) Windows folder "%windir%"
            /// {F38BF404-1D43-42F2-9305-67DE0B28FC23}
            /// </summary>
            internal static ReadOnlySpan<byte> Windows =>
                [0x04, 0xF4, 0x8B, 0xF3, 0x43, 0x1D, 0xF2, 0x42, 0x93, 0x05, 0x67, 0xDE, 0x0B, 0x28, 0xFC, 0x23];
        }
    }
}
