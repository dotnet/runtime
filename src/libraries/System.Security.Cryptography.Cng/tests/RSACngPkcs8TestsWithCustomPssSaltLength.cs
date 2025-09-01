// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Cng.Tests
{

    public class RSACngPkcs8TestsWithCustomPssSaltLength : RSACngPkcs8TestsPssSaltLength
    {
        protected override int SaltLength => 200;
    }
}
