// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

public partial class FileLoadException_Interop_40100_Tests
{
    [Fact]
    public static void FileLoadException_from_HR()
    {
        int[] hrs =
            {
                HResults.COR_E_FILELOAD,
                HResults.FUSION_E_INVALID_PRIVATE_ASM_LOCATION,
                HResults.FUSION_E_SIGNATURE_CHECK_FAILED,
                HResults.FUSION_E_LOADFROM_BLOCKED,
                HResults.FUSION_E_CACHEFILE_FAILED,
                HResults.FUSION_E_ASM_MODULE_MISSING,
                HResults.FUSION_E_INVALID_NAME,
                HResults.FUSION_E_PRIVATE_ASM_DISALLOWED,
                HResults.FUSION_E_HOST_GAC_ASM_MISMATCH,
                HResults.COR_E_MODULE_HASH_CHECK_FAILED,
                HResults.FUSION_E_REF_DEF_MISMATCH,
                HResults.SECURITY_E_INCOMPATIBLE_SHARE,
                HResults.SECURITY_E_INCOMPATIBLE_EVIDENCE,
                HResults.SECURITY_E_UNVERIFIABLE,
                HResults.COR_E_FIXUPSINEXE,
                HResults.ERROR_TOO_MANY_OPEN_FILES,
                HResults.ERROR_SHARING_VIOLATION,
                HResults.ERROR_LOCK_VIOLATION,
                HResults.ERROR_OPEN_FAILED,
                HResults.ERROR_DISK_CORRUPT,
                HResults.ERROR_UNRECOGNIZED_VOLUME,
                HResults.ERROR_DLL_INIT_FAILED,
                HResults.FUSION_E_CODE_DOWNLOAD_DISABLED,
                HResults.CORSEC_E_MISSING_STRONGNAME,
                HResults.MSEE_E_ASSEMBLYLOADINPROGRESS,
                HResults.ERROR_FILE_INVALID,
            };

        foreach (var hr in hrs)
        {
            var e = Marshal.GetExceptionForHR(hr);
            Assert.IsType<FileLoadException>(e);
            var fle = e as FileLoadException;
            if (fle == null)
            {
                Assert.True(false, String.Format("Expected FileLoadException for hr 0x{0:X8} but got {1}.", hr, e.GetType()));
            }
            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            Utility.ValidateExceptionProperties(fle, hResult: hr, validateMessage: false);
            Assert.Equal(null, fle.FileName);
        }
    }
}
