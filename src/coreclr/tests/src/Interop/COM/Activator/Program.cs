// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Activator
{
    using System;
    using System.Runtime.InteropServices;

    using TestLibrary;

    using Console = Internal.Console;

    class Program
    {
        static void InvalidInterfaceRequest()
        {
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    var notIClassFactory = new Guid("ED53F949-63E4-43B5-A13D-5655478AADD5");
                    var cxt = new ComActivationContext()
                    {
                        InterfaceId = notIClassFactory
                    };
                    ComActivator.GetClassFactoryForType(cxt);
                },
                "Non-IClassFactory request should fail");
        }

        static void ClassNotRegistered()
        {
            COMException e = Assert.Throws<COMException>(
                () =>
                {
                    var CLSID_NotRegistered = new Guid("328FF83E-3F6C-4BE9-A742-752562032925"); // Random GUID
                    var IID_IClassFactory = new Guid("00000001-0000-0000-C000-000000000046");
                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotRegistered,
                        InterfaceId = IID_IClassFactory
                    };
                    ComActivator.GetClassFactoryForType(cxt);
                },
                "Class should not be found");

            const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
            Assert.AreEqual(CLASS_E_CLASSNOTAVAILABLE, e.HResult, "Unexpected HRESULT");
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                InvalidInterfaceRequest();
                ClassNotRegistered();
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
