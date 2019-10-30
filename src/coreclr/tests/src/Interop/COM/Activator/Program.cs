// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Activator
{
    using Internal.Runtime.InteropServices;

    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using TestLibrary;

    using Console = Internal.Console;

    class Program
    {
        static void InvalidInterfaceRequest()
        {
            Console.WriteLine($"Running {nameof(InvalidInterfaceRequest)}...");

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

        static void NonrootedAssemblyPath()
        {
            Console.WriteLine($"Running {nameof(NonrootedAssemblyPath)}...");

            ArgumentException e = Assert.Throws<ArgumentException>(
                () =>
                {
                    var cxt = new ComActivationContext()
                    {
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = "foo.dll"
                    };
                    ComActivator.GetClassFactoryForType(cxt);
                },
                "Non-root assembly path should not be valid");
        }

        static void ClassNotRegistered()
        {
            Console.WriteLine($"Running {nameof(ClassNotRegistered)}...");

            COMException e = Assert.Throws<COMException>(
                () =>
                {
                    var CLSID_NotRegistered = new Guid("328FF83E-3F6C-4BE9-A742-752562032925"); // Random GUID
                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotRegistered,
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = @"C:\foo.dll"
                    };
                    ComActivator.GetClassFactoryForType(cxt);
                },
                "Class should not be found");

            const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
            Assert.AreEqual(CLASS_E_CLASSNOTAVAILABLE, e.HResult, "Unexpected HRESULT");
        }

        static void ValidateAssemblyIsolation()
        {
            Console.WriteLine($"Running {nameof(ValidateAssemblyIsolation)}...");

            string assemblySubPath = Path.Combine(Environment.CurrentDirectory, "Servers");
            string assemblyAPath = Path.Combine(assemblySubPath, "AssemblyA.dll");
            string assemblyBPath = Path.Combine(assemblySubPath, "AssemblyB.dll");
            string assemblyCPath = Path.Combine(assemblySubPath, "AssemblyC.dll");
            string assemblyPaths = $"{assemblyAPath}{Path.PathSeparator}{assemblyBPath}{Path.PathSeparator}{assemblyCPath}";

            HostPolicyMock.Initialize(Environment.CurrentDirectory, null);

            var CLSID_NotUsed = Guid.Empty; // During this phase of activation the GUID is not used.
            Guid iid = typeof(IGetTypeFromC).GUID;
            Type typeCFromAssemblyA;
            Type typeCFromAssemblyB;

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                0,
                assemblyPaths,
                string.Empty,
                string.Empty))
            {
                var cxt = new ComActivationContext()
                {
                    ClassId = CLSID_NotUsed,
                    InterfaceId = typeof(IClassFactory).GUID,
                    AssemblyPath = assemblyAPath,
                    AssemblyName = "AssemblyA",
                    TypeName = "ClassFromA"
                };

                var factory = (IClassFactory)ComActivator.GetClassFactoryForType(cxt);

                object svr;
                factory.CreateInstance(null, ref iid, out svr);
                typeCFromAssemblyA = (Type)((IGetTypeFromC)svr).GetTypeFromC();
            }

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                0,
                assemblyPaths,
                string.Empty,
                string.Empty))
            {
                var cxt = new ComActivationContext()
                {
                    ClassId = CLSID_NotUsed,
                    InterfaceId = typeof(IClassFactory).GUID,
                    AssemblyPath = assemblyBPath,
                    AssemblyName = "AssemblyB",
                    TypeName = "ClassFromB"
                };

                var factory = (IClassFactory)ComActivator.GetClassFactoryForType(cxt);

                object svr;
                factory.CreateInstance(null, ref iid, out svr);
                typeCFromAssemblyB = (Type)((IGetTypeFromC)svr).GetTypeFromC();
            }

            Assert.AreNotEqual(typeCFromAssemblyA, typeCFromAssemblyB, "Types should be from different AssemblyLoadContexts");
        }

        static void ValidateUserDefinedRegistrationCallbacks()
        {
            Console.WriteLine($"Running {nameof(ValidateUserDefinedRegistrationCallbacks)}...");

            string assemblySubPath = Path.Combine(Environment.CurrentDirectory, "Servers");
            string assemblyAPath = Path.Combine(assemblySubPath, "AssemblyA.dll");
            string assemblyBPath = Path.Combine(assemblySubPath, "AssemblyB.dll");
            string assemblyCPath = Path.Combine(assemblySubPath, "AssemblyC.dll");
            string assemblyPaths = $"{assemblyAPath}{Path.PathSeparator}{assemblyBPath}{Path.PathSeparator}{assemblyCPath}";

            HostPolicyMock.Initialize(Environment.CurrentDirectory, null);

            var CLSID_NotUsed = Guid.Empty; // During this phase of activation the GUID is not used.
            Guid iid = typeof(IValidateRegistrationCallbacks).GUID;

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                0,
                assemblyPaths,
                string.Empty,
                string.Empty))
            {
                foreach (string typename in new[] { "ValidRegistrationTypeCallbacks",  "ValidRegistrationStringCallbacks" })
                {
                    Console.WriteLine($"Validating {typename}...");

                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotUsed,
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = assemblyAPath,
                        AssemblyName = "AssemblyA",
                        TypeName = typename
                    };

                    var factory = (IClassFactory)ComActivator.GetClassFactoryForType(cxt);

                    object svr;
                    factory.CreateInstance(null, ref iid, out svr);

                    var inst = (IValidateRegistrationCallbacks)svr;
                    Assert.IsFalse(inst.DidRegister());
                    Assert.IsFalse(inst.DidUnregister());

                    cxt.InterfaceId = Guid.Empty;
                    ComActivator.ClassRegistrationScenarioForType(cxt, register: true);
                    ComActivator.ClassRegistrationScenarioForType(cxt, register: false);

                    Assert.IsTrue(inst.DidRegister());
                    Assert.IsTrue(inst.DidUnregister());
                }
            }

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                0,
                assemblyPaths,
                string.Empty,
                string.Empty))
            {
                foreach (string typename in new[] { "NoRegistrationCallbacks",  "InvalidArgRegistrationCallbacks", "InvalidInstanceRegistrationCallbacks", "MultipleRegistrationCallbacks" })
                {
                    Console.WriteLine($"Validating {typename}...");

                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotUsed,
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = assemblyAPath,
                        AssemblyName = "AssemblyA",
                        TypeName = typename
                    };

                    var factory = (IClassFactory)ComActivator.GetClassFactoryForType(cxt);

                    object svr;
                    factory.CreateInstance(null, ref iid, out svr);

                    var inst = (IValidateRegistrationCallbacks)svr;
                    cxt.InterfaceId = Guid.Empty;
                    bool exceptionThrown = false;
                    try
                    {
                        ComActivator.ClassRegistrationScenarioForType(cxt, register: true);
                    }
                    catch
                    {
                        exceptionThrown = true;
                    }

                    Assert.IsTrue(exceptionThrown || !inst.DidRegister());

                    exceptionThrown = false;
                    try
                    {
                        ComActivator.ClassRegistrationScenarioForType(cxt, register: false);
                    }
                    catch
                    {
                        exceptionThrown = true;
                    }

                    Assert.IsTrue(exceptionThrown || !inst.DidUnregister());
                }
            }
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                InvalidInterfaceRequest();
                ClassNotRegistered();
                NonrootedAssemblyPath();
                ValidateAssemblyIsolation();
                ValidateUserDefinedRegistrationCallbacks();
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
