// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class FileLoadExceptionInteropTests
    {
        [Theory]
        [InlineData(HResults.COR_E_FILELOAD)]
        [InlineData(HResults.FUSION_E_INVALID_PRIVATE_ASM_LOCATION)]
        [InlineData(HResults.FUSION_E_SIGNATURE_CHECK_FAILED)]
        [InlineData(HResults.FUSION_E_LOADFROM_BLOCKED)]
        [InlineData(HResults.FUSION_E_CACHEFILE_FAILED)]
        [InlineData(HResults.FUSION_E_ASM_MODULE_MISSING)]
        [InlineData(HResults.FUSION_E_INVALID_NAME)]
        [InlineData(HResults.FUSION_E_PRIVATE_ASM_DISALLOWED)]
        [InlineData(HResults.FUSION_E_HOST_GAC_ASM_MISMATCH)]
        [InlineData(HResults.COR_E_MODULE_HASH_CHECK_FAILED)]
        [InlineData(HResults.FUSION_E_REF_DEF_MISMATCH)]
        [InlineData(HResults.SECURITY_E_INCOMPATIBLE_SHARE)]
        [InlineData(HResults.SECURITY_E_INCOMPATIBLE_EVIDENCE)]
        [InlineData(HResults.SECURITY_E_UNVERIFIABLE)]
        [InlineData(HResults.COR_E_FIXUPSINEXE)]
        [InlineData(HResults.ERROR_TOO_MANY_OPEN_FILES)]
        [InlineData(HResults.ERROR_SHARING_VIOLATION)]
        [InlineData(HResults.ERROR_LOCK_VIOLATION)]
        [InlineData(HResults.ERROR_OPEN_FAILED)]
        [InlineData(HResults.ERROR_DISK_CORRUPT)]
        [InlineData(HResults.ERROR_UNRECOGNIZED_VOLUME)]
        [InlineData(HResults.ERROR_DLL_INIT_FAILED)]
        [InlineData(HResults.FUSION_E_CODE_DOWNLOAD_DISABLED)]
        [InlineData(HResults.CORSEC_E_MISSING_STRONGNAME)]
        [InlineData(HResults.MSEE_E_ASSEMBLYLOADINPROGRESS)]
        [InlineData(HResults.ERROR_FILE_INVALID)]
        public static void Fom_HR(int hr)
        {
            var fileLoadException = Marshal.GetExceptionForHR(hr, new IntPtr(-1)) as FileLoadException;
            Assert.NotNull(fileLoadException);

            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            ExceptionUtility.ValidateExceptionProperties(fileLoadException, hResult: hr, validateMessage: false);
            Assert.Null(fileLoadException.FileName);
        }
    }
}
