// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class FileNotFoundExceptionInteropTests
    {
        [Theory]
        [InlineData(HResults.COR_E_FILENOTFOUND)]
        [InlineData(HResults.CTL_E_FILENOTFOUND)]
        public static void From_HR(int hr)
        {
            FileNotFoundException exception = Assert.IsAssignableFrom<FileNotFoundException>(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));

            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: hr, validateMessage: false);
        }
    }
}
