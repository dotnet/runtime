// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Internal.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace Internal.Runtime.InteropServices
{
    [ComImport]
    [ComVisible(false)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClassFactory
    {
        void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject);

        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }
}

sealed class ClassFactoryWrapper
{
    private static readonly MethodInfo IClassFactory_Create = typeof(object).Assembly.GetType("Internal.Runtime.InteropServices.IClassFactory").GetMethod("CreateInstance");
    private readonly object _obj;

    public ClassFactoryWrapper(object obj)
    {
        _obj = obj;
    }

    public void CreateInstance(
            object pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject)
    {
        object[] args = new object[] { pUnkOuter, riid, null };
        IClassFactory_Create.Invoke(_obj, BindingFlags.DoNotWrapExceptions, binder: null, args, culture: null);
        riid = (Guid)args[1];
        ppvObject = (IntPtr)args[2];
    }
}

namespace Activator
{
    public unsafe class Program
    {
        private const string SharedLoadContextIdentifier = $"{nameof(Program)}.{nameof(ValidateAssemblyLoadContext)}";
        private const nint IsolatedContext = -1;

        private enum LoadContextKind
        {
            Default,
            Isolated,
            Named
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LoadContext
        {
            public nuint Size;
            public IntPtr Identifier;
        }

        private static delegate*<ComActivationContext, object> GetClassFactoryForTypeMethod = (delegate*<ComActivationContext, object>)typeof(object).Assembly.GetType("Internal.Runtime.InteropServices.ComActivator", throwOnError: true).GetMethod("GetClassFactoryForType", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();
        private static delegate*<ComActivationContext, bool, void> ClassRegistrationScenarioForType = (delegate*<ComActivationContext, bool, void>)typeof(object).Assembly.GetType("Internal.Runtime.InteropServices.ComActivator", throwOnError: true).GetMethod("ClassRegistrationScenarioForType", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();

        private static ClassFactoryWrapper GetClassFactoryForType(ComActivationContext context)
        {
            return new ClassFactoryWrapper(GetClassFactoryForTypeMethod(context));
        }

        private static ClassFactoryWrapper GetClassFactoryForType(ComActivationContext context, LoadContextKind loadContextKind)
        {
            if (loadContextKind is not LoadContextKind.Named)
            {
                context.LoadContext = loadContextKind switch
                {
                    LoadContextKind.Default => IntPtr.Zero,
                    LoadContextKind.Isolated => IsolatedContext,
                    _ => throw new ArgumentOutOfRangeException(nameof(loadContextKind))
                };
                return GetClassFactoryForType(context);
            }

            fixed (char* identifier = SharedLoadContextIdentifier)
            {
                LoadContext loadContext = new()
                {
                    Size = (nuint)sizeof(LoadContext),
                    Identifier = (IntPtr)identifier
                };
                context.LoadContext = (IntPtr)(&loadContext);
                return GetClassFactoryForType(context);
            }
        }

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
                    GetClassFactoryForType(cxt);
                });
        }

        static void NonrootedAssemblyPath(bool builtInComDisabled)
        {
            Console.WriteLine($"Running {nameof(NonrootedAssemblyPath)}...");

            Action action = () =>
                {
                    var cxt = new ComActivationContext()
                    {
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = "foo.dll"
                    };
                    GetClassFactoryForType(cxt);
                };

            if (!builtInComDisabled)
            {
                Assert.Throws<ArgumentException>(action);
            }
            else
            {
                Assert.Throws<NotSupportedException>(action);
            }
        }

        static void ClassNotRegistered(bool builtInComDisabled)
        {
            Console.WriteLine($"Running {nameof(ClassNotRegistered)}...");

            Action action = () =>
                {
                    var CLSID_NotRegistered = new Guid("328FF83E-3F6C-4BE9-A742-752562032925"); // Random GUID
                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotRegistered,
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = @"C:\foo.dll"
                    };
                    GetClassFactoryForType(cxt);
                };

            if (!builtInComDisabled)
            {
                COMException e = Assert.Throws<COMException>(action);
                const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
                Assert.Equal(CLASS_E_CLASSNOTAVAILABLE, e.HResult);
            }
            else
            {
                Assert.Throws<NotSupportedException>(action);
            }
        }

        static void ValidateAssemblyLoadContext(bool builtInComDisabled, LoadContextKind loadContextKind)
        {
            Console.WriteLine($"Running {nameof(ValidateAssemblyLoadContext)}({nameof(loadContextKind)}={loadContextKind})...");

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

                if (builtInComDisabled)
                {
                    Assert.Throws<NotSupportedException>(
                        () => GetClassFactoryForType(cxt, loadContextKind));
                    return;
                }

                var factory = GetClassFactoryForType(cxt, loadContextKind);

                IntPtr svrRaw;
                factory.CreateInstance(null, ref iid, out svrRaw);
                var svr = (IGetTypeFromC)Marshal.GetObjectForIUnknown(svrRaw);
                Marshal.Release(svrRaw);
                typeCFromAssemblyA = (Type)svr.GetTypeFromC();
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

                var factory = GetClassFactoryForType(cxt, loadContextKind);

                IntPtr svrRaw;
                factory.CreateInstance(null, ref iid, out svrRaw);
                var svr = (IGetTypeFromC)Marshal.GetObjectForIUnknown(svrRaw);
                Marshal.Release(svrRaw);
                typeCFromAssemblyB = (Type)svr.GetTypeFromC();
            }

            if (loadContextKind is LoadContextKind.Isolated)
            {
                Assert.NotEqual(typeCFromAssemblyA, typeCFromAssemblyB);
            }
            else
            {
                Assert.Equal(typeCFromAssemblyA, typeCFromAssemblyB);
            }

            if (loadContextKind is LoadContextKind.Named)
            {
                AssemblyLoadContext alcA = AssemblyLoadContext.GetLoadContext(typeCFromAssemblyA.Assembly);
                AssemblyLoadContext alcB = AssemblyLoadContext.GetLoadContext(typeCFromAssemblyB.Assembly);
                Assert.NotSame(AssemblyLoadContext.Default, alcA);
                Assert.Same(alcA, alcB);
                Assert.Equal($"ComponentLoadContext({SharedLoadContextIdentifier})", alcA.Name);
            }
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
                string[] typeNamesToValidate = {
                    "ValidRegistrationTypeCallbacks",
                    "ValidRegistrationStringCallbacks",
                    "InheritedRegistrationTypeCallbacks",
                    "InheritedRegistrationStringCallbacks"
                };

                foreach (string typeName in typeNamesToValidate)
                {
                    Console.WriteLine($"Validating {typeName}...");

                    var cxt = new ComActivationContext()
                    {
                        ClassId = CLSID_NotUsed,
                        InterfaceId = typeof(IClassFactory).GUID,
                        AssemblyPath = assemblyAPath,
                        AssemblyName = "AssemblyA",
                        TypeName = typeName
                    };

                    var factory = GetClassFactoryForType(cxt);

                    IntPtr svrRaw;
                    factory.CreateInstance(null, ref iid, out svrRaw);
                    var svr = Marshal.GetObjectForIUnknown(svrRaw);
                    Marshal.Release(svrRaw);

                    var inst = (IValidateRegistrationCallbacks)svr;
                    Assert.False(inst.DidRegister());
                    Assert.False(inst.DidUnregister());

                    cxt.InterfaceId = Guid.Empty;
                    ClassRegistrationScenarioForType(cxt, true);
                    ClassRegistrationScenarioForType(cxt, false);

                    Assert.True(inst.DidRegister(), $"User-defined register function should have been called.");
                    Assert.True(inst.DidUnregister(), $"User-defined unregister function should have been called.");
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

                    var factory = GetClassFactoryForType(cxt);

                    IntPtr svrRaw;
                    factory.CreateInstance(null, ref iid, out svrRaw);
                    var svr = Marshal.GetObjectForIUnknown(svrRaw);
                    Marshal.Release(svrRaw);

                    var inst = (IValidateRegistrationCallbacks)svr;
                    cxt.InterfaceId = Guid.Empty;
                    bool exceptionThrown = false;
                    try
                    {
                        ClassRegistrationScenarioForType(cxt, true);
                    }
                    catch
                    {
                        exceptionThrown = true;
                    }

                    Assert.True(exceptionThrown || !inst.DidRegister());

                    exceptionThrown = false;
                    try
                    {
                        ClassRegistrationScenarioForType(cxt, false);
                    }
                    catch
                    {
                        exceptionThrown = true;
                    }

                    Assert.True(exceptionThrown || !inst.DidUnregister());
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                bool builtInComDisabled = false;
                var comConfig = AppContext.GetData("System.Runtime.InteropServices.BuiltInComInterop.IsSupported");
                if (comConfig != null && !bool.Parse(comConfig.ToString()))
                {
                    builtInComDisabled = true;
                }
                Console.WriteLine($"Built-in COM Disabled?: {builtInComDisabled}");

                InvalidInterfaceRequest();
                ClassNotRegistered(builtInComDisabled);
                NonrootedAssemblyPath(builtInComDisabled);
                ValidateAssemblyLoadContext(builtInComDisabled, LoadContextKind.Isolated);
                if (!builtInComDisabled)
                {
                    // When built-in COM is disabled, the isolated case above verifies that activation fails
                    // before load context selection, so testing the other load context kinds would be redundant.
                    ValidateAssemblyLoadContext(builtInComDisabled, LoadContextKind.Default);
                    ValidateAssemblyLoadContext(builtInComDisabled, LoadContextKind.Named);
                    ValidateUserDefinedRegistrationCallbacks();
                }
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
