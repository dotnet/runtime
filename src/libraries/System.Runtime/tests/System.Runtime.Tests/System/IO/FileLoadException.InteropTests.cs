// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [InlineData(HResults.FUSION_E_INVALID_NAME)]
        [InlineData(HResults.FUSION_E_PRIVATE_ASM_DISALLOWED)]
        [InlineData(HResults.FUSION_E_REF_DEF_MISMATCH)]
        [InlineData(HResults.ERROR_TOO_MANY_OPEN_FILES)]
        [InlineData(HResults.ERROR_SHARING_VIOLATION)]
        [InlineData(HResults.ERROR_LOCK_VIOLATION)]
        [InlineData(HResults.ERROR_OPEN_FAILED)]
        [InlineData(HResults.ERROR_DISK_CORRUPT)]
        [InlineData(HResults.ERROR_UNRECOGNIZED_VOLUME)]
        [InlineData(HResults.ERROR_DLL_INIT_FAILED)]
        [InlineData(HResults.MSEE_E_ASSEMBLYLOADINPROGRESS)]
        [InlineData(HResults.ERROR_FILE_INVALID)]
        public static void Fom_HR(int hr)
        {
            var fileLoadException = Marshal.GetExceptionForHR(hr, new IntPtr(-1)) as FileLoadException;
            Assert.NotNull(fileLoadException);

            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            ExceptionHelpers.ValidateExceptionProperties(fileLoadException, hResult: hr, validateMessage: false);
            Assert.Null(fileLoadException.FileName);
        }
    }
}
