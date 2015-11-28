// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;

using Xunit;

public partial class DirectoryNotFoundException_Interop_40100_Tests
{
    [Fact]
    public static void DirectoryNotFoundException_from_HR()
    {
        int[] hrs = { HResults.COR_E_DIRECTORYNOTFOUND, HResults.STG_E_PATHNOTFOUND, HResults.CTL_E_PATHNOTFOUND };
        foreach (var hr in hrs)
        {
            var e = Marshal.GetExceptionForHR(hr);
            var dnfe = e as DirectoryNotFoundException;
            Assert.NotNull(dnfe);

            // Don't validate the message.  Currently .NET Native does not produce HR-specific messages
            Utility.ValidateExceptionProperties(dnfe, hResult: hr, validateMessage: false);
        }
    }
}
