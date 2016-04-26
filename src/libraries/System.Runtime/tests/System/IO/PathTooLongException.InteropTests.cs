// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public static class PathTooLongExceptionInteropTests
    {
        [Fact]
        public static void From_HR()
        {
            int hr = HResults.COR_E_PATHTOOLONG;
            PathTooLongException exception = Marshal.GetExceptionForHR(hr) as PathTooLongException;
            Assert.NotNull(exception);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: hr, validateMessage: false);
        }
    }
}
