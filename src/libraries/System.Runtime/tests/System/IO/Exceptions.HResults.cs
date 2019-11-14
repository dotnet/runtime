// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    public static class HResults
    {
        // DirectoryNotFoundException
        public const int COR_E_DIRECTORYNOTFOUND = unchecked((int)0x80070003);
        public const int STG_E_PATHNOTFOUND = unchecked((int)0x80030003);
        public const int CTL_E_PATHNOTFOUND = unchecked((int)0x800A004C);

        // FileNotFoundException
        public const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);
        public const int CTL_E_FILENOTFOUND = unchecked((int)0x800A0035);

        public const int COR_E_EXCEPTION = unchecked((int)0x80131500);

        public const int COR_E_FILELOAD = unchecked((int)0x80131621);
        public const int FUSION_E_INVALID_PRIVATE_ASM_LOCATION = unchecked((int)0x80131041);
        public const int FUSION_E_SIGNATURE_CHECK_FAILED = unchecked((int)0x80131045);
        public const int FUSION_E_LOADFROM_BLOCKED = unchecked((int)0x80131051);
        public const int FUSION_E_CACHEFILE_FAILED = unchecked((int)0x80131052);
        public const int FUSION_E_ASM_MODULE_MISSING = unchecked((int)0x80131042);
        public const int FUSION_E_INVALID_NAME = unchecked((int)0x80131047);
        public const int FUSION_E_PRIVATE_ASM_DISALLOWED = unchecked((int)0x80131044);
        public const int FUSION_E_HOST_GAC_ASM_MISMATCH = unchecked((int)0x80131050);
        public const int COR_E_MODULE_HASH_CHECK_FAILED = unchecked((int)0x80131039);
        public const int FUSION_E_REF_DEF_MISMATCH = unchecked((int)0x80131040);
        public const int SECURITY_E_INCOMPATIBLE_SHARE = unchecked((int)0x80131401);
        public const int SECURITY_E_INCOMPATIBLE_EVIDENCE = unchecked((int)0x80131403);
        public const int SECURITY_E_UNVERIFIABLE = unchecked((int)0x80131402);
        public const int COR_E_FIXUPSINEXE = unchecked((int)0x80131019);
        public const int ERROR_TOO_MANY_OPEN_FILES = unchecked((int)0x80070004);
        public const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
        public const int ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);
        public const int ERROR_OPEN_FAILED = unchecked((int)0x8007006E);
        public const int ERROR_DISK_CORRUPT = unchecked((int)0x80070571);
        public const int ERROR_UNRECOGNIZED_VOLUME = unchecked((int)0x800703ED);
        public const int ERROR_DLL_INIT_FAILED = unchecked((int)0x8007045A);
        public const int FUSION_E_CODE_DOWNLOAD_DISABLED = unchecked((int)0x80131048);
        public const int CORSEC_E_MISSING_STRONGNAME = unchecked((int)0x8013141b);
        public const int MSEE_E_ASSEMBLYLOADINPROGRESS = unchecked((int)0x80131016);
        public const int ERROR_FILE_INVALID = unchecked((int)0x800703EE);

        public const int COR_E_PATHTOOLONG = unchecked((int)0x800700CE);
    }
}
