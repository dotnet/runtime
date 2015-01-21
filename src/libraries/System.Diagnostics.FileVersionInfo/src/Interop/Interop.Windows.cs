// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode, EntryPoint = "VerLanguageNameW")]
        public static extern int VerLanguageName(uint langID, StringBuilder lpBuffer, uint nSize);

        [DllImport("api-ms-win-core-version-l1-1-0.dll", CharSet = CharSet.Unicode, EntryPoint = "VerQueryValueW")]
        internal extern static bool VerQueryValue(
                    IntPtr pBlock,
                    string lpSubBlock,
                    out IntPtr lplpBuffer,
                    out uint puLen);

        [DllImport("api-ms-win-core-version-l1-1-0.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoExW")]
        internal extern static bool GetFileVersionInfoEx(
                    uint dwFlags,
                    string lpwstrFilename,
                    uint dwHandle,
                    uint dwLen,
                    IntPtr lpData);

        [DllImport("api-ms-win-core-version-l1-1-0.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoSizeExW")]
        internal extern static uint GetFileVersionInfoSizeEx(
                    uint dwFlags,
                    string lpwstrFilename,
                    out uint lpdwHandle);
    }

    internal enum Constants : uint
    {
        ErrorSuccess = 0x0u,
        FileVerGetLocalised = 0x1u,
        VS_FF_Debug = 0x1u,
        MutexModifyState = 0x1u,
        FileVerGetNeutral = 0x2u,
        VS_FF_Prerelease = 0x2u,
        ErrorFileNotFound = 0x2u,
        EventModifyState = 0x2u,
        FileTypeChar = 0x2u,
        ErrorPathNotFound = 0x3u,
        VS_FF_Patched = 0x4u,
        ErrorAccessDenied = 0x5u,
        ErrorInvalidHandle = 0x6u,
        VS_FF_PrivateBuild = 0x8u,
        ErrorInvalidDrive = 0xFu,
        VS_FF_InfoInferred = 0x10u,
        VS_FF_SpecialBuild = 0x20u,
        ErrorSharingViolation = 0x20u,
        ErrorInvalidParameter = 0x57u,
        ErrorInvalidName = 0x7Bu,
        ErrorBadPathname = 0xA1u,
        ErrorAlreadyExists = 0xB7u,
        ErrorFilenameExcedRange = 0xCEu,
        Synchronize = 0x100000u,
        StdErrorHandle = 0xFFFFFFF4u,
        StdOutputHandle = 0xFFFFFFF5u,
        StdInputHandle = 0xFFFFFFF6u,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VS_FIXEDFILEINFO
    {
        public uint dwSignature;
        public uint dwStrucVersion;
        public uint dwFileVersionMS;
        public uint dwFileVersionLS;
        public uint dwProductVersionMS;
        public uint dwProductVersionLS;
        public uint dwFileFlagsMask;
        public uint dwFileFlags;
        public uint dwFileOS;
        public uint dwFileType;
        public uint dwFileSubtype;
        public uint dwFileDateMS;
        public uint dwFileDateLS;
    }
}
