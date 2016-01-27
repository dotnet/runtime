// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

using Xunit;

public partial class PathTooLongException_Interop_40100_Tests
{
    [Fact]
    public static void PathTooLongException_from_HR()
    {
        int hr = HResults.COR_E_PATHTOOLONG;
        var e = Marshal.GetExceptionForHR(hr);
        var ptle = e as PathTooLongException;
        Assert.NotNull(ptle);
        Utility.ValidateExceptionProperties(ptle, hResult: hr, validateMessage: false);
    }
}
