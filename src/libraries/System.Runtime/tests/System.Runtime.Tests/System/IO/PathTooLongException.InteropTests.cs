// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class PathTooLongExceptionInteropTests
    {
        [Fact]
        public static void From_HR()
        {
            int hr = HResults.COR_E_PATHTOOLONG;
            PathTooLongException exception = Assert.IsAssignableFrom<PathTooLongException>(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: hr, validateMessage: false);
        }
    }
}
