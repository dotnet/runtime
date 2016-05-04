// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Test.Cryptography
{
    internal abstract partial class CertLoader
    {
        //
        // This helper is for loading test certificates with private keys. 
        //
        // If the test doesn't need a private key to function, don't use this. Under normal circumstances, it will return null.
        //
        // You can change the value of TestMode to run the test manually. 
        //
        //    TestMode = CertLoadMode.Disable
        //
        //        Disable all tests that rely on private keys. Unfortunately, this has to be the default for inner-loop tests to avoid cluttering
        //        people's machines with leaked keys on disk every time they build.
        //
        //
        //    TestMode = CertLoadMode.LoadFromPfx
        //
        //        Load certs from PFX data. This is convenient as it requires no preparatory steps. The downside is that every time you open a CNG .PFX,
        //        a temporarily key is permanently leaked to your disk. (And every time you open a CAPI PFX, a key is leaked if the test aborts before
        //        Disposing the certificate.) 
        //
        //        Only use if you're testing on a VM or if you just don't care about your machine accumulating leaked keys.
        //
        //
        //     TestMode = CetLoadMode.LoadFromStore
        //
        //        Load certs from the certificate store (set StoreName to the name you want to use.) This requires that you preinstall the certificates
        //        into the cert store (say by File.WriteAllByte()'ing the PFX blob into a "foo.pfx" file, then launching it and following the wizard.)
        //        but then you don't need to worry about key leaks. 
        //        LoadFromStore = 3,
        //
        //
        // These fields are nominally private and readonly but intentionally not declared as such so that an ad-hoc test host can change them for convenience.
        //
        public static CertLoadMode TestMode = CertLoadMode.LoadFromPfx;
        public static string StoreName = "DotNetCoreFxTestCerts";  // Do not use "MY" here as that can break many test assumptions.
    }
}

