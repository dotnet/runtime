// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    public sealed partial class FileVersionInfo
    {
        private unsafe FileVersionInfo(string fileName)
        {
            _fileName = fileName;

            uint handle;  // This variable is not used, but we need an out variable.
            uint infoSize = Interop.Version.GetFileVersionInfoSizeEx(
                (uint)Interop.Version.FileVersionInfoType.FILE_VER_GET_LOCALISED, _fileName, out handle);

            if (infoSize != 0)
            {
                byte[] mem = new byte[infoSize];
                fixed (byte* memPtr = mem)
                {
                    IntPtr memIntPtr = new IntPtr((void*)memPtr);
                    if (Interop.Version.GetFileVersionInfoEx(
                            (uint)Interop.Version.FileVersionInfoType.FILE_VER_GET_LOCALISED | (uint)Interop.Version.FileVersionInfoType.FILE_VER_GET_NEUTRAL,
                            _fileName,
                            0U,
                            infoSize,
                            memIntPtr))
                    {
                        uint langid = GetVarEntry(memIntPtr);
                        if (!GetVersionInfoForCodePage(memIntPtr, ConvertTo8DigitHex(langid)))
                        {
                            // Some DLLs might not contain correct codepage information. In these cases we will fail during lookup.
                            // Explorer will take a few shots in dark by trying several specific lang-codepages
                            // (Explorer also randomly guesses 041D04B0=Swedish+CP_UNICODE and 040704B0=German+CP_UNICODE sometimes).
                            // We will try to simulate similar behavior here.
                            foreach (uint id in s_fallbackLanguageCodePages)
                            {
                                if (id != langid)
                                {
                                    if (GetVersionInfoForCodePage(memIntPtr, ConvertTo8DigitHex(id)))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // Some dlls might not contain correct codepage information,
        // in which case the lookup will fail. Explorer will take
        // a few shots in dark. We'll simulate similar behavior by
        // falling back to the following lang-codepages:
        private static readonly uint[] s_fallbackLanguageCodePages = new uint[]
        {
            0x040904B0, // US English + CP_UNICODE
            0x040904E4, // US English + CP_USASCII
            0x04090000  // US English + unknown codepage
        };

        private static string ConvertTo8DigitHex(uint value)
        {
            return value.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static Interop.Version.VS_FIXEDFILEINFO GetFixedFileInfo(IntPtr memPtr)
        {
            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.Version.VerQueryValue(memPtr, "\\", out memRef, out memLen))
            {
                return (Interop.Version.VS_FIXEDFILEINFO)Marshal.PtrToStructure<Interop.Version.VS_FIXEDFILEINFO>(memRef);
            }

            return new Interop.Version.VS_FIXEDFILEINFO();
        }

        private static string GetFileVersionLanguage(IntPtr memPtr)
        {
            uint langid = GetVarEntry(memPtr) >> 16;

            var lang = new StringBuilder(256);
            Interop.Kernel32.VerLanguageName(langid, lang, (uint)lang.Capacity);
            return lang.ToString();
        }

        private static string GetFileVersionString(IntPtr memPtr, string name)
        {
            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.Version.VerQueryValue(memPtr, name, out memRef, out memLen))
            {
                if (memRef != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(memRef);
                }
            }

            return string.Empty;
        }

        private static uint GetVarEntry(IntPtr memPtr)
        {
            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.Version.VerQueryValue(memPtr, "\\VarFileInfo\\Translation", out memRef, out memLen))
            {
                return (uint)((Marshal.ReadInt16(memRef) << 16) + Marshal.ReadInt16((IntPtr)((long)memRef + 2)));
            }

            return 0x040904E4;
        }

        //
        // This function tries to find version information for a specific codepage.
        // Returns true when version information is found.
        //
        private bool GetVersionInfoForCodePage(IntPtr memIntPtr, string codepage)
        {
            string template = "\\\\StringFileInfo\\\\{0}\\\\{1}";

            _companyName = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "CompanyName"));
            _fileDescription = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "FileDescription"));
            _fileVersion = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "FileVersion"));
            _internalName = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "InternalName"));
            _legalCopyright = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "LegalCopyright"));
            _originalFilename = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "OriginalFilename"));
            _productName = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "ProductName"));
            _productVersion = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "ProductVersion"));
            _comments = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "Comments"));
            _legalTrademarks = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "LegalTrademarks"));
            _privateBuild = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "PrivateBuild"));
            _specialBuild = GetFileVersionString(memIntPtr, string.Format(CultureInfo.InvariantCulture, template, codepage, "SpecialBuild"));

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

            _isDebug = (ffi.dwFileFlags & (uint)Interop.Version.FileVersionInfo.VS_FF_DEBUG) != 0;
            _isPatched = (ffi.dwFileFlags & (uint)Interop.Version.FileVersionInfo.VS_FF_PATCHED) != 0;
            _isPrivateBuild = (ffi.dwFileFlags & (uint)Interop.Version.FileVersionInfo.VS_FF_PRIVATEBUILD) != 0;
            _isPreRelease = (ffi.dwFileFlags & (uint)Interop.Version.FileVersionInfo.VS_FF_PRERELEASE) != 0;
            _isSpecialBuild = (ffi.dwFileFlags & (uint)Interop.Version.FileVersionInfo.VS_FF_SPECIALBUILD) != 0;

            // fileVersion is chosen based on best guess. Other fields can be used if appropriate.
            return (_fileVersion != string.Empty);
        }

        private static uint HIWORD(uint dword)
        {
            return (dword >> 16) & 0xffff;
        }

        private static uint LOWORD(uint dword)
        {
            return dword & 0xffff;
        }
    }
}
