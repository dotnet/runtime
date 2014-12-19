// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Globalization;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides version information for a physical file on disk.
    /// </summary>
    public sealed class FileVersionInfo
    {
        private readonly string _fileName;
        private string _companyName;
        private string _fileDescription;
        private string _fileVersion;
        private string _internalName;
        private string _legalCopyright;
        private string _originalFilename;
        private string _productName;
        private string _productVersion;
        private string _comments;
        private string _legalTrademarks;
        private string _privateBuild;
        private string _specialBuild;
        private string _language;
        private uint _fileMajor;
        private uint _fileMinor;
        private uint _fileBuild;
        private uint _filePrivate;
        private uint _productMajor;
        private uint _productMinor;
        private uint _productBuild;
        private uint _productPrivate;
        private uint _fileFlags;

        private FileVersionInfo(string fileName)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Gets the comments associated with the file.
        /// </summary>
        public string Comments
        {
            get
            {
                return _comments;
            }
        }

        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        public string CompanyName
        {
            get
            {
                return _companyName;
            }
        }

        /// <summary>
        /// Gets the build number of the file.
        /// </summary>
        public int FileBuildPart
        {
            get
            {
                return (int)_fileBuild;
            }
        }

        /// <summary>
        /// Gets the description of the file.
        /// </summary>
        public string FileDescription
        {
            get
            {
                return _fileDescription;
            }
        }

        /// <summary>
        /// Gets the major part of the version number.
        /// </summary>
        public int FileMajorPart
        {
            get
            {
                return (int)_fileMajor;
            }
        }

        /// <summary>
        /// Gets the minor part of the version number of the file.
        /// </summary>
        public int FileMinorPart
        {
            get
            {
                return (int)_fileMinor;
            }
        }

        /// <summary>
        /// Gets the name of the file that this instance of System.Windows.Forms.FileVersionInfo describes.
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
        }

        /// <summary>
        /// Gets the file private part number.
        /// </summary>
        public int FilePrivatePart
        {
            get
            {
                return (int)_filePrivate;
            }
        }

        /// <summary>
        /// Gets the file version number.
        /// </summary>
        public string FileVersion
        {
            get
            {
                return _fileVersion;
            }
        }

        /// <summary>
        /// Gets the internal name of the file, if one exists.
        /// </summary>
        public string InternalName
        {
            get
            {
                return _internalName;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the file contains debugging information
        /// or is compiled with debugging features enabled.
        /// </summary>
        public bool IsDebug
        {
            get
            {
                return (_fileFlags & (uint)Interop.Constants.VS_FF_Debug) != 0;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the file has been modified and is not identical to
        /// the original shipping file of the same version number.
        /// </summary>
        public bool IsPatched
        {
            get
            {
                return (_fileFlags & (uint)Interop.Constants.VS_FF_Patched) != 0;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the file was built using standard release procedures.
        /// </summary>
        public bool IsPrivateBuild
        {
            get
            {
                return (_fileFlags & (uint)Interop.Constants.VS_FF_PrivateBuild) != 0;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the file
        /// is a development version, rather than a commercially released product.
        /// </summary>
        public bool IsPreRelease
        {
            get
            {
                return (_fileFlags & (uint)Interop.Constants.VS_FF_Prerelease) != 0;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the file is a special build.
        /// </summary>
        public bool IsSpecialBuild
        {
            get
            {
                return (_fileFlags & (uint)Interop.Constants.VS_FF_SpecialBuild) != 0;
            }
        }

        /// <summary>
        /// Gets the default language string for the version info block.
        /// </summary>
        public string Language
        {
            get
            {
                return _language;
            }
        }

        /// <summary>
        /// Gets all copyright notices that apply to the specified file.
        /// </summary>
        public string LegalCopyright
        {
            get
            {
                return _legalCopyright;
            }
        }

        /// <summary>
        /// Gets the trademarks and registered trademarks that apply to the file.
        /// </summary>
        public string LegalTrademarks
        {
            get
            {
                return _legalTrademarks;
            }
        }

        /// <summary>
        /// Gets the name the file was created with.
        /// </summary>
        public string OriginalFilename
        {
            get
            {
                return _originalFilename;
            }
        }

        /// <summary>
        /// Gets information about a private version of the file.
        /// </summary>
        public string PrivateBuild
        {
            get
            {
                return _privateBuild;
            }
        }

        /// <summary>
        /// Gets the build number of the product this file is associated with.
        /// </summary>
        public int ProductBuildPart
        {
            get
            {
                return (int)_productBuild;
            }
        }

        /// <summary>
        /// Gets the major part of the version number for the product this file is associated with.
        /// </summary>
        public int ProductMajorPart
        {
            get
            {
                return (int)_productMajor;
            }
        }

        /// <summary>
        /// Gets the minor part of the version number for the product the file is associated with.
        /// </summary>
        public int ProductMinorPart
        {
            get
            {
                return (int)_productMinor;
            }
        }

        /// <summary>
        /// Gets the name of the product this file is distributed with.
        /// </summary>
        public string ProductName
        {
            get
            {
                return _productName;
            }
        }

        /// <summary>
        /// Gets the private part number of the product this file is associated with.
        /// </summary>
        public int ProductPrivatePart
        {
            get
            {
                return (int)_productPrivate;
            }
        }

        /// <summary>
        /// Gets the version of the product this file is distributed with.
        /// </summary>
        public string ProductVersion
        {
            get
            {
                return _productVersion;
            }
        }

        /// <summary>
        /// Gets the special build information for the file.
        /// </summary>
        public string SpecialBuild
        {
            get
            {
                return _specialBuild;
            }
        }

        private static string ConvertTo8DigitHex(uint value)
        {
            string s = Convert.ToString(value, 16);
            s = s.ToUpperInvariant();
            if (s.Length == 8)
            {
                return s;
            }
            else
            {
                StringBuilder b = new StringBuilder(8);
                for (int l = s.Length; l < 8; l++)
                {
                    b.Append("0");
                }
                b.Append(s);
                return b.ToString();
            }
        }

        private static Interop.VS_FIXEDFILEINFO GetFixedFileInfo(IntPtr memPtr)
        {
            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.mincore.VerQueryValue(memPtr, "\\", out memRef, out memLen))
            {
                Interop.VS_FIXEDFILEINFO fixedFileInfo =
                    (Interop.VS_FIXEDFILEINFO)Marshal.PtrToStructure<Interop.VS_FIXEDFILEINFO>(memRef);
                return fixedFileInfo;
            }

            return new Interop.VS_FIXEDFILEINFO();
        }

        private static string GetFileVersionLanguage(IntPtr memPtr)
        {
            uint langid = GetVarEntry(memPtr) >> 16;

            StringBuilder lang = new StringBuilder(256);
            Interop.mincore.VerLanguageName(langid, lang, (uint)lang.Capacity);
            return lang.ToString();
        }

        private static string GetFileVersionString(IntPtr memPtr, string name)
        {
            string data = "";

            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.mincore.VerQueryValue(memPtr, name, out memRef, out memLen))
            {
                if (memRef != IntPtr.Zero)
                {
                    data = Marshal.PtrToStringUni(memRef);
                }
            }
            return data;
        }

        private static uint GetVarEntry(IntPtr memPtr)
        {
            IntPtr memRef = IntPtr.Zero;
            uint memLen;

            if (Interop.mincore.VerQueryValue(memPtr, "\\VarFileInfo\\Translation", out memRef, out memLen))
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

            Interop.VS_FIXEDFILEINFO ffi = GetFixedFileInfo(memIntPtr);
            _fileMajor = HIWORD(ffi.dwFileVersionMS);
            _fileMinor = LOWORD(ffi.dwFileVersionMS);
            _fileBuild = HIWORD(ffi.dwFileVersionLS);
            _filePrivate = LOWORD(ffi.dwFileVersionLS);
            _productMajor = HIWORD(ffi.dwProductVersionMS);
            _productMinor = LOWORD(ffi.dwProductVersionMS);
            _productBuild = HIWORD(ffi.dwProductVersionLS);
            _productPrivate = LOWORD(ffi.dwProductVersionLS);
            _fileFlags = ffi.dwFileFlags;

            // fileVersion is chosen based on best guess. Other fields can be used if appropriate. 
            return (_fileVersion != string.Empty);
        }

        /// <summary>
        /// Returns a System.Windows.Forms.FileVersionInfo representing the version information associated with the specified file.
        /// </summary>
        public unsafe static FileVersionInfo GetVersionInfo(string fileName)
        {
            // Check for the existence of the file. File.Exists returns false
            // if Read permission is denied.
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            uint handle;  // This variable is not used, but we need an out variable.
            uint infoSize = Interop.mincore.GetFileVersionInfoSizeEx(
                (uint)Interop.Constants.FileVerGetLocalised, fileName, out handle);
            FileVersionInfo versionInfo = new FileVersionInfo(fileName);

            if (infoSize != 0)
            {
                byte[] mem = new byte[infoSize];
                fixed (byte* memPtr = mem)
                {
                    IntPtr memIntPtr = new IntPtr((void*)memPtr);
                    if (Interop.mincore.GetFileVersionInfoEx(
                            (uint)Interop.Constants.FileVerGetLocalised | (uint)Interop.Constants.FileVerGetNeutral,
                            fileName,
                            0U,
                            infoSize,
                            memIntPtr))
                    {
                        uint langid = GetVarEntry(memIntPtr);
                        if (!versionInfo.GetVersionInfoForCodePage(memIntPtr, ConvertTo8DigitHex(langid)))
                        {
                            // Some dlls might not contain correct codepage information. In this case we will fail during lookup. 
                            // Explorer will take a few shots in dark by trying following ID:
                            //
                            // 040904B0 // US English + CP_UNICODE
                            // 040904E4 // US English + CP_USASCII
                            // 04090000 // US English + unknown codepage
                            // Explorer also randomly guess 041D04B0=Swedish+CP_UNICODE and 040704B0=German+CP_UNICODE) sometimes.
                            // We will try to simulate similiar behavior here.            
                            uint[] ids = new uint[] { 0x040904B0, 0x040904E4, 0x04090000 };
                            foreach (uint id in ids)
                            {
                                if (id != langid)
                                {
                                    if (versionInfo.GetVersionInfoForCodePage(memIntPtr, ConvertTo8DigitHex(id)))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return versionInfo;
        }

        private static uint HIWORD(uint dword)
        {
            return (dword >> 16) & 0xffff;
        }

        private static uint LOWORD(uint dword)
        {
            return dword & 0xffff;
        }

        /// <summary>
        /// Returns a partial list of properties in System.Windows.Forms.FileVersionInfo
        /// and their values.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(128);
            String nl = "\r\n";
            sb.Append("File:             "); sb.Append(FileName); sb.Append(nl);
            sb.Append("InternalName:     "); sb.Append(InternalName); sb.Append(nl);
            sb.Append("OriginalFilename: "); sb.Append(OriginalFilename); sb.Append(nl);
            sb.Append("FileVersion:      "); sb.Append(FileVersion); sb.Append(nl);
            sb.Append("FileDescription:  "); sb.Append(FileDescription); sb.Append(nl);
            sb.Append("Product:          "); sb.Append(ProductName); sb.Append(nl);
            sb.Append("ProductVersion:   "); sb.Append(ProductVersion); sb.Append(nl);
            sb.Append("Debug:            "); sb.Append(IsDebug.ToString()); sb.Append(nl);
            sb.Append("Patched:          "); sb.Append(IsPatched.ToString()); sb.Append(nl);
            sb.Append("PreRelease:       "); sb.Append(IsPreRelease.ToString()); sb.Append(nl);
            sb.Append("PrivateBuild:     "); sb.Append(IsPrivateBuild.ToString()); sb.Append(nl);
            sb.Append("SpecialBuild:     "); sb.Append(IsSpecialBuild.ToString()); sb.Append(nl);
            sb.Append("Language:         "); sb.Append(Language); sb.Append(nl);
            return sb.ToString();
        }
    }
}
