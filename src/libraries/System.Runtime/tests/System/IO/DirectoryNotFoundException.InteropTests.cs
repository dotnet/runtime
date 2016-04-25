// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public static class DirectoryNotFoundExceptionInteropTests
    {
        [Theory]
        [InlineData(HResults.COR_E_DIRECTORYNOTFOUND)]
        [InlineData(HResults.STG_E_PATHNOTFOUND)]
        [InlineData(HResults.CTL_E_PATHNOTFOUND)]
        public static void From_HR(int hr)
        {
            DirectoryNotFoundException exception = Marshal.GetExceptionForHR(hr) as DirectoryNotFoundException;
            Assert.NotNull(exception);

            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: hr, validateMessage: false);
        }
    }
}
