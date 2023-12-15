// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;
    using Server.Contract.Servers;

    public class Program
    {
        static readonly string DefaultLicKey = "__MOCK_LICENSE_KEY__";
        static void ActivateLicensedObject()
        {
            Console.WriteLine($"Calling {nameof(ActivateLicensedObject)}...");

            // Validate activation
            var licenseTesting = (LicenseTesting)new LicenseTestingClass();

            // Validate license denial
            licenseTesting.SetNextDenyLicense(true);
            try
            {
                var tmp = (LicenseTesting)new LicenseTestingClass();
                Assert.Fail("Activation of licensed class should fail");
            }
            catch (COMException e)
            {
                const int CLASS_E_NOTLICENSED = unchecked((int)0x80040112);
                Assert.Equal(CLASS_E_NOTLICENSED, e.HResult);
            }
            finally
            {
                licenseTesting.SetNextDenyLicense(false);
            }
        }

        class MockLicenseContext : LicenseContext
        {
            private readonly Type _type;
            private string _key;

            public MockLicenseContext(Type type, LicenseUsageMode mode)
            {
                UsageMode = mode;
                _type = type;
            }

            public override LicenseUsageMode UsageMode { get; }

            public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
            {
                if (type == _type)
                {
                    return _key;
                }

                return null;
            }

            public override void SetSavedLicenseKey(Type type, string key)
            {
                if (type == _type)
                {
                    _key = key;
                }
            }
        }

        static void ActivateUnderDesigntimeContext()
        {
            Console.WriteLine($"Calling {nameof(ActivateUnderDesigntimeContext)}...");

            LicenseContext prev = LicenseManager.CurrentContext;
            try
            {
                string licKey = "__TEST__";
                LicenseManager.CurrentContext = new MockLicenseContext(typeof(LicenseTestingClass), LicenseUsageMode.Designtime);
                LicenseManager.CurrentContext.SetSavedLicenseKey(typeof(LicenseTestingClass), licKey);

                var licenseTesting = (LicenseTesting)new LicenseTestingClass();

                // During design time the IClassFactory::CreateInstance will be called - no license
                Assert.Null(licenseTesting.GetLicense());

                // Verify the value retrieved from the IClassFactory2::RequestLicKey was what was set
                Assert.Equal(DefaultLicKey, LicenseManager.CurrentContext.GetSavedLicenseKey(typeof(LicenseTestingClass), resourceAssembly: null));
            }
            finally
            {
                LicenseManager.CurrentContext = prev;
            }
        }

        static void ActivateUnderRuntimeContext()
        {
            Console.WriteLine($"Calling {nameof(ActivateUnderRuntimeContext)}...");

            LicenseContext prev = LicenseManager.CurrentContext;
            try
            {
                string licKey = "__TEST__";
                LicenseManager.CurrentContext = new MockLicenseContext(typeof(LicenseTestingClass), LicenseUsageMode.Runtime);
                LicenseManager.CurrentContext.SetSavedLicenseKey(typeof(LicenseTestingClass), licKey);

                var licenseTesting = (LicenseTesting)new LicenseTestingClass();

                // During runtime the IClassFactory::CreateInstance2 will be called with license from context
                Assert.Equal(licKey, licenseTesting.GetLicense());
            }
            finally
            {
                LicenseManager.CurrentContext = prev;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                ActivateLicensedObject();
                ActivateUnderDesigntimeContext();
                ActivateUnderRuntimeContext();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
