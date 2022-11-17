// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public sealed partial class FileVersionInfo
    {
        private unsafe FileVersionInfo(string fileName)
        {
            _fileName = fileName;

            uint infoSize = Interop.Version.GetFileVersionInfoSizeEx(Interop.Version.FileVersionInfoType.FILE_VER_GET_LOCALISED, _fileName, out _);
            if (infoSize != 0)
            {
                void* memPtr = NativeMemory.Alloc(infoSize);
                try
                {
                    if (Interop.Version.GetFileVersionInfoEx(
                        Interop.Version.FileVersionInfoType.FILE_VER_GET_LOCALISED | Interop.Version.FileVersionInfoType.FILE_VER_GET_NEUTRAL,
                        _fileName,
                        0U,
                        infoSize,
                        memPtr))
                    {
                        // Some dlls might not contain correct codepage information, in which case the lookup will fail. Explorer will take
                        // a few shots in dark. We'll simulate similar behavior by falling back to the following lang-codepages.
                        uint lcp = GetLanguageAndCodePage(memPtr);
                        _ = GetVersionInfoForCodePage(memPtr, lcp.ToString("X8")) ||
                            (lcp != 0x040904B0 && GetVersionInfoForCodePage(memPtr, "040904B0")) || // US English + CP_UNICODE
                            (lcp != 0x040904E4 && GetVersionInfoForCodePage(memPtr, "040904E4")) || // US English + CP_USASCII
                            (lcp != 0x04090000 && GetVersionInfoForCodePage(memPtr, "04090000"));   // US English + unknown codepage
                    }
                }
                finally
                {
                    NativeMemory.Free(memPtr);
                }
            }
        }

        private static unsafe Interop.Version.VS_FIXEDFILEINFO GetFixedFileInfo(void* memPtr)
        {
            if (Interop.Version.VerQueryValue(memPtr, "\\", out void* memRef, out _))
            {
                return *(Interop.Version.VS_FIXEDFILEINFO*)memRef;
            }

            return default;
        }

        private static unsafe string GetFileVersionLanguage(void* memPtr)
        {
            uint langid = GetLanguageAndCodePage(memPtr) >> 16;

            const int MaxLength = 256;
            char* lang = stackalloc char[MaxLength];
            int charsWritten = Interop.Kernel32.VerLanguageName(langid, lang, MaxLength);
            return new string(lang, 0, charsWritten);
        }

        private static unsafe string GetFileVersionString(void* memPtr, string name)
        {
            if (Interop.Version.VerQueryValue(memPtr, name, out void* memRef, out _) &&
                memRef is not null)
            {
                return Marshal.PtrToStringUni((IntPtr)memRef)!;
            }

            return string.Empty;
        }

        private static unsafe uint GetLanguageAndCodePage(void* memPtr)
        {
            if (Interop.Version.VerQueryValue(memPtr, "\\VarFileInfo\\Translation", out void* memRef, out _))
            {
                return
                    (uint)((*(ushort*)memRef << 16) +
                    *((ushort*)memRef + 1));
            }

            return 0x040904E4; // US English + CP_USASCII
        }

        //
        // This function tries to find version information for a specific codepage.
        // Returns true when version information is found.
        //
        private unsafe bool GetVersionInfoForCodePage(void* memIntPtr, string codepage)
        {
            Span<char> stackBuffer = stackalloc char[256];

            _companyName = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\CompanyName"));
            _fileDescription = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\FileDescription"));
            _fileVersion = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\FileVersion"));
            _internalName = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\InternalName"));
            _legalCopyright = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\LegalCopyright"));
            _originalFilename = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\OriginalFilename"));
            _productName = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\ProductName"));
            _productVersion = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\ProductVersion"));
            _comments = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\Comments"));
            _legalTrademarks = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\LegalTrademarks"));
            _privateBuild = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\PrivateBuild"));
            _specialBuild = GetFileVersionString(memIntPtr, string.Create(null, stackBuffer, $"\\\\StringFileInfo\\\\{codepage}\\\\SpecialBuild"));

            _language = GetFileVersionLanguage(memIntPtr);

            Interop.Version.VS_FIXEDFILEINFO ffi = GetFixedFileInfo(memIntPtr);
            _fileMajor = (int)HIWORD(ffi.dwFileVersionMS);
            _fileMinor = (int)LOWORD(ffi.dwFileVersionMS);
            _fileBuild = (int)HIWORD(ffi.dwFileVersionLS);
            _filePrivate = (int)LOWORD(ffi.dwFileVersionLS);
            _productMajor = (int)HIWORD(ffi.dwProductVersionMS);
            _productMinor = (int)LOWORD(ffi.dwProductVersionMS);
            _productBuild = (int)HIWORD(ffi.dwProductVersionLS);
            _productPrivate = (int)LOWORD(ffi.dwProductVersionLS);

            _isDebug = (ffi.dwFileFlags & Interop.Version.FileVersionInfo.VS_FF_DEBUG) != 0;
            _isPatched = (ffi.dwFileFlags & Interop.Version.FileVersionInfo.VS_FF_PATCHED) != 0;
            _isPrivateBuild = (ffi.dwFileFlags & Interop.Version.FileVersionInfo.VS_FF_PRIVATEBUILD) != 0;
            _isPreRelease = (ffi.dwFileFlags & Interop.Version.FileVersionInfo.VS_FF_PRERELEASE) != 0;
            _isSpecialBuild = (ffi.dwFileFlags & Interop.Version.FileVersionInfo.VS_FF_SPECIALBUILD) != 0;

            // fileVersion is chosen based on best guess. Other fields can be used if appropriate.
            return (_fileVersion != string.Empty);
        }

        private static uint HIWORD(uint dword) => (dword >> 16) & 0xffff;

        private static uint LOWORD(uint dword) => dword & 0xffff;
    }
}
