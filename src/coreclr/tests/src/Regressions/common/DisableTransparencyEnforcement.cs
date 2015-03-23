// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// This test is verifying that security transparency violations are ignored in CoreCLR

using System;
using System.Security;

[assembly:AllowPartiallyTrustedCallers]

[SecurityCritical]
class CriticalType
{
    [SecurityCritical]
    public CriticalType()
    {
    }
}

class My {
    static int Main() {

        new CriticalType().ToString();
        
        // GC.Collect(int) is marked as security critical in CoreCLR for historic reasons.
        // Verify that the transparency violations are ignored by calling it from here.
        GC.Collect(1);

        TestLibrary.TestFramework.LogInformation("PASS");
   
        return 100;
    }
}
