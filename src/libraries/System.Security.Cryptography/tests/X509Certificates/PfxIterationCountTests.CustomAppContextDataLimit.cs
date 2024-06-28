// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public class PfxIterationCountTests_CustomAppContextDataLimit
    {
        private static readonly Dictionary<string, PfxInfo> s_certificatesDictionary
            = PfxIterationCountTests.s_certificates.ToDictionary((c) => c.Name);

        // We need to use virtual in a non-abstract class because RemoteExecutor can't work on abstract classes.
        internal virtual X509Certificate Import(byte[] blob) => new X509Certificate(blob);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(memberName: nameof(PfxIterationCountTests.GetCertsWith_IterationCountNotExceedingDefaultLimit_AndNullOrEmptyPassword_MemberData), MemberType = typeof(PfxIterationCountTests))]
        public void Import_AppContextDataWithValueMinusTwo_ActsAsDefaultLimit_IterationCountNotExceedingDefaultLimit(string name, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            _ = iterationCount;
            _ = blob;

            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2, which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            RemoteExecutor.Invoke((certName) =>
            {
                AppContext.SetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit", -2);

                PfxInfo pfxInfo = s_certificatesDictionary[certName];

                X509Certificate cert = Import(pfxInfo.Blob);
                Assert.True(cert.Subject == "CN=test" || cert.Subject == "CN=potato");
            }, name).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(memberName: nameof(PfxIterationCountTests.GetCertsWith_IterationCountExceedingDefaultLimit_MemberData), MemberType = typeof(PfxIterationCountTests))]
        public void Import_AppContextDataWithValueMinusTwo_ActsAsDefaultLimit_IterationCountLimitExceeded_Throws(string name, string password, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            _ = password;
            _ = iterationCount;
            _ = blob;

            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2, which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            RemoteExecutor.Invoke((certName) =>
            {
                AppContext.SetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit", -2);

                PfxInfo pfxInfo = s_certificatesDictionary[certName];

                CryptographicException ce = Assert.Throws<CryptographicException>(() => Import(pfxInfo.Blob));
                Assert.Contains(PfxIterationCountTests.FwlinkId, ce.Message);
            }, name).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(memberName: nameof(PfxIterationCountTests.GetCertsWith_IterationCountNotExceedingDefaultLimit_AndNullOrEmptyPassword_MemberData), MemberType = typeof(PfxIterationCountTests))]
        public void Import_AppContextDataWithValueZero_IterationCountNotExceedingDefaultLimit_Throws(string name, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            _ = iterationCount;
            _ = blob;

            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2, which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            RemoteExecutor.Invoke((certName) =>
            {
                AppContext.SetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit", 0);

                PfxInfo pfxInfo = s_certificatesDictionary[certName];

                CryptographicException ce = Assert.Throws<CryptographicException>(() => Import(pfxInfo.Blob));
                Assert.Contains(PfxIterationCountTests.FwlinkId, ce.Message);
            }, name).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(memberName: nameof(PfxIterationCountTests.GetCertsWith_IterationCountExceedingDefaultLimit_MemberData), MemberType = typeof(PfxIterationCountTests))]
        public void Import_AppContextDataWithValueMinusOne_IterationCountExceedingDefaultLimit(string name, string password, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            _ = password;
            _ = blob;
            _ = iterationCount;

            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2, which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            RemoteExecutor.Invoke((certName) =>
            {
                AppContext.SetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit", -1);

                PfxInfo pfxInfo = s_certificatesDictionary[certName];

                // The total iteration count filter is disabled, but individual items are
                // still limited to 600k.
                CryptographicException ce = Assert.Throws<CryptographicException>(() => Import(pfxInfo.Blob));
                Assert.Contains(PfxIterationCountTests.FwlinkId, ce.Message);
            }, name).Dispose();
        }
    }

    public class PfxIterationCountTests_CustomLimit_X509Certificate2 : PfxIterationCountTests_CustomAppContextDataLimit
    {
        internal override X509Certificate Import(byte[] blob) => new X509Certificate2(blob);
    }

    public class PfxIterationCountTests_CustomLimit_X509Certificate2Collection : PfxIterationCountTests_CustomAppContextDataLimit
    {
        internal override X509Certificate Import(byte[] blob)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(blob);
            return collection[0];
        }
    }
}
