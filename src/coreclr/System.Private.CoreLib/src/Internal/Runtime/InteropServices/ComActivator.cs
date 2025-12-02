// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

//
// Types in this file marked as 'public' are done so only to aid in
// testing of functionality and should not be considered publicly consumable.
//
namespace Internal.Runtime.InteropServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LICINFO
    {
        public int cbLicInfo;

        [MarshalAs(UnmanagedType.Bool)]
        public bool fRuntimeKeyAvail;

        [MarshalAs(UnmanagedType.Bool)]
        public bool fLicVerified;
    }

    [ComImport]
    [ComVisible(false)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory
    {
        void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject);

        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }

    [ComImport]
    [ComVisible(false)]
    [Guid("B196B28F-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory2 : IClassFactory
    {
        new void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject);

        new void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);

        void GetLicInfo(ref LICINFO pLicInfo);

        void RequestLicKey(
            int dwReserved,
            [MarshalAs(UnmanagedType.BStr)] out string pBstrKey);

        void CreateInstanceLic(
            [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
            [MarshalAs(UnmanagedType.Interface)] object? pUnkReserved,
            ref Guid riid,
            [MarshalAs(UnmanagedType.BStr)] string bstrKey,
            out IntPtr ppvObject);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComActivationContext
    {
        public Guid ClassId;
        public Guid InterfaceId;
        public string AssemblyPath;
        public string AssemblyName;
        public string TypeName;
        public bool IsolatedContext;

        public static unsafe ComActivationContext Create(ref ComActivationContextInternal cxtInt, bool isolatedContext)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return new ComActivationContext()
            {
                ClassId = cxtInt.ClassId,
                InterfaceId = cxtInt.InterfaceId,
                AssemblyPath = Marshal.PtrToStringUni(new IntPtr(cxtInt.AssemblyPathBuffer))!,
                AssemblyName = Marshal.PtrToStringUni(new IntPtr(cxtInt.AssemblyNameBuffer))!,
                TypeName = Marshal.PtrToStringUni(new IntPtr(cxtInt.TypeNameBuffer))!,
                IsolatedContext = isolatedContext
            };
        }
    }

    [SupportedOSPlatform("windows")]
    internal static class ComActivator
    {
        // Collection of all ALCs used for COM activation. In the event we want to support
        // unloadable COM server ALCs, this will need to be changed.
        private static readonly Dictionary<string, AssemblyLoadContext> s_assemblyLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.InvariantCultureIgnoreCase);

        // COM component assembly paths loaded in the default ALC
        private static readonly HashSet<string> s_loadedInDefaultContext = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Entry point for unmanaged COM activation API from managed code
        /// </summary>
        /// <param name="cxt">Reference to a <see cref="ComActivationContext"/> instance</param>
        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private static object GetClassFactoryForType(ComActivationContext cxt)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            if (cxt.InterfaceId != Marshal.IID_IUnknown
                && cxt.InterfaceId != typeof(IClassFactory).GUID
                && cxt.InterfaceId != typeof(IClassFactory2).GUID)
            {
                throw new NotSupportedException();
            }

            if (!Path.IsPathRooted(cxt.AssemblyPath))
            {
                throw new ArgumentException(null, nameof(cxt));
            }

            Type classType = FindClassType(cxt);

            if (LicenseInteropProxy.HasLicense(classType))
            {
                return new LicenseClassFactory(cxt.ClassId, classType);
            }

            return new BasicClassFactory(cxt.ClassId, classType);
        }

        /// <summary>
        /// Entry point for unmanaged COM register/unregister API from managed code
        /// </summary>
        /// <param name="cxt">Reference to a <see cref="ComActivationContext"/> instance</param>
        /// <param name="register">true if called for register or false to indicate unregister</param>
        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private static void ClassRegistrationScenarioForType(ComActivationContext cxt, bool register)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            // Retrieve the attribute type to use to determine if a function is the requested user defined
            // registration function.
            string attributeName = register ? "ComRegisterFunctionAttribute" : "ComUnregisterFunctionAttribute";
            Type? regFuncAttrType = Type.GetType($"System.Runtime.InteropServices.{attributeName}, System.Runtime.InteropServices", throwOnError: false);
            if (regFuncAttrType == null)
            {
                // If the COM registration attributes can't be found then it is not on the type.
                return;
            }

            if (!Path.IsPathRooted(cxt.AssemblyPath))
            {
                throw new ArgumentException(null, nameof(cxt));
            }

            Type classType = FindClassType(cxt);

            Type? currentType = classType;
            bool calledFunction = false;

            // Walk up the inheritance hierarchy. The first register/unregister function found is called; any additional functions on base types are ignored.
            while (currentType != null && !calledFunction)
            {
                // Retrieve all the methods.
                MethodInfo[] methods = currentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                // Go through all the methods and check for the custom attribute.
                foreach (MethodInfo method in methods)
                {
                    // Check to see if the method has the custom attribute.
                    if (method.GetCustomAttributes(regFuncAttrType!, inherit: true).Length == 0)
                    {
                        continue;
                    }

                    // Check to see if the method is static before we call it.
                    if (!method.IsStatic)
                    {
                        string msg = register ? SR.InvalidOperation_NonStaticComRegFunction : SR.InvalidOperation_NonStaticComUnRegFunction;
                        throw new InvalidOperationException(SR.Format(msg));
                    }

                    // Finally validate signature
                    ReadOnlySpan<ParameterInfo> methParams = method.GetParametersAsSpan();
                    if (method.ReturnType != typeof(void)
                        || methParams.Length != 1
                        || (methParams[0].ParameterType != typeof(string) && methParams[0].ParameterType != typeof(Type)))
                    {
                        string msg = register ? SR.InvalidOperation_InvalidComRegFunctionSig : SR.InvalidOperation_InvalidComUnRegFunctionSig;
                        throw new InvalidOperationException(SR.Format(msg));
                    }

                    if (calledFunction)
                    {
                        string msg = register ? SR.InvalidOperation_MultipleComRegFunctions : SR.InvalidOperation_MultipleComUnRegFunctions;
                        throw new InvalidOperationException(SR.Format(msg));
                    }

                    // The function is valid so set up the arguments to call it.
                    var objs = new object[1];
                    if (methParams[0].ParameterType == typeof(string))
                    {
                        // We are dealing with the string overload of the function - provide the registry key - see comhost.dll implementation
                        objs[0] = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{cxt.ClassId:B}";
                    }
                    else
                    {
                        // We are dealing with the type overload of the function.
                        objs[0] = classType;
                    }

                    // Invoke the COM register function.
                    method.Invoke(null, objs);
                    calledFunction = true;
                }

                // Go through all the base types
                currentType = currentType.BaseType;
            }
        }

        /// <summary>
        /// Gets a class factory for COM activation in an isolated load context
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int GetClassFactoryForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            return GetClassFactoryForTypeImpl(pCxtInt, isolatedContext: true);
#pragma warning restore IL2026
        }

        /// <summary>
        /// Gets a class factory for COM activation in the specified load context
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        /// <param name="loadContext">Load context - currently must be IntPtr.Zero (default context) or -1 (isolated context)</param>
        [UnmanagedCallersOnly]
        private static unsafe int GetClassFactoryForTypeInContext(ComActivationContextInternal* pCxtInt, IntPtr loadContext)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

            if (loadContext != IntPtr.Zero && loadContext != (IntPtr)(-1))
                throw new ArgumentOutOfRangeException(nameof(loadContext));

            return GetClassFactoryForTypeLocal(pCxtInt, isolatedContext: loadContext != IntPtr.Zero);

            // Use a local function for a targeted suppression of the requires unreferenced code warning
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The same feature switch applies to GetClassFactoryForTypeInternal and this function. We rely on the warning from GetClassFactoryForTypeInternal.")]
            static int GetClassFactoryForTypeLocal(ComActivationContextInternal* pCxtInt, bool isolatedContext) => GetClassFactoryForTypeImpl(pCxtInt, isolatedContext);
        }

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private static unsafe int GetClassFactoryForTypeImpl(ComActivationContextInternal* pCxtInt, bool isolatedContext)
        {
            ref ComActivationContextInternal cxtInt = ref *pCxtInt;
            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt, isolatedContext);
                object cf = GetClassFactoryForType(cxt);
                IntPtr nativeIUnknown = Marshal.GetIUnknownForObject(cf);
                Marshal.WriteIntPtr(cxtInt.ClassFactoryDest, nativeIUnknown);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;
        }

        /// <summary>
        /// Registers a managed COM server in an isolated load context
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int RegisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

            return RegisterClassForTypeImpl(pCxtInt, isolatedContext: true);
        }

        /// <summary>
        /// Registers a managed COM server in the specified load context
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        /// <param name="loadContext">Load context - currently must be IntPtr.Zero (default context) or -1 (isolated context)</param>
        [UnmanagedCallersOnly]
        private static unsafe int RegisterClassForTypeInContext(ComActivationContextInternal* pCxtInt, IntPtr loadContext)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

            if (loadContext != IntPtr.Zero && loadContext != (IntPtr)(-1))
                throw new ArgumentOutOfRangeException(nameof(loadContext));

            return RegisterClassForTypeImpl(pCxtInt, isolatedContext: loadContext != IntPtr.Zero);
        }

        private static unsafe int RegisterClassForTypeImpl(ComActivationContextInternal* pCxtInt, bool isolatedContext)
        {
            ref ComActivationContextInternal cxtInt = ref *pCxtInt;
            if (cxtInt.InterfaceId != Guid.Empty
                || cxtInt.ClassFactoryDest != IntPtr.Zero)
            {
                throw new ArgumentException(null, nameof(pCxtInt));
            }

            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt, isolatedContext);
                ClassRegistrationScenarioForTypeLocal(cxt, register: true);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;

            // Use a local function for a targeted suppression of the requires unreferenced code warning
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The same feature switch applies to GetClassFactoryForTypeInternal and this function. We rely on the warning from GetClassFactoryForTypeInternal.")]
            static void ClassRegistrationScenarioForTypeLocal(ComActivationContext cxt, bool register) => ClassRegistrationScenarioForType(cxt, register);
        }

        /// <summary>
        /// Unregisters a managed COM server in an isolated load context
        /// </summary>
        [UnmanagedCallersOnly]
        private static unsafe int UnregisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

            return UnregisterClassForTypeImpl(pCxtInt, isolatedContext: true);
        }

        /// <summary>
        /// Unregisters a managed COM server in the specified load context
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        /// <param name="loadContext">Load context - currently must be IntPtr.Zero (default context) or -1 (isolated context)</param>
        [UnmanagedCallersOnly]
        private static unsafe int UnregisterClassForTypeInContext(ComActivationContextInternal* pCxtInt, IntPtr loadContext)
        {
            if (!Marshal.IsBuiltInComSupported)
                throw new NotSupportedException(SR.NotSupported_COM);

            if (loadContext != IntPtr.Zero && loadContext != (IntPtr)(-1))
                throw new ArgumentOutOfRangeException(nameof(loadContext));

            return UnregisterClassForTypeImpl(pCxtInt, isolatedContext: loadContext != IntPtr.Zero);
        }

        private static unsafe int UnregisterClassForTypeImpl(ComActivationContextInternal* pCxtInt, bool isolatedContext)
        {
            ref ComActivationContextInternal cxtInt = ref *pCxtInt;
            if (cxtInt.InterfaceId != Guid.Empty
                || cxtInt.ClassFactoryDest != IntPtr.Zero)
            {
                throw new ArgumentException(null, nameof(pCxtInt));
            }

            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt, isolatedContext);
                ClassRegistrationScenarioForTypeLocal(cxt, register: false);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;

            // Use a local function for a targeted suppression of the requires unreferenced code warning
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The same feature switch applies to GetClassFactoryForTypeInternal and this function. We rely on the warning from GetClassFactoryForTypeInternal.")]
            static void ClassRegistrationScenarioForTypeLocal(ComActivationContext cxt, bool register) => ClassRegistrationScenarioForType(cxt, register);
        }

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private static Type FindClassType(ComActivationContext cxt)
        {
            try
            {
                AssemblyLoadContext alc = GetALC(cxt.AssemblyPath, cxt.IsolatedContext);
                var assemblyNameLocal = new AssemblyName(cxt.AssemblyName);
                Assembly assem = alc.LoadFromAssemblyName(assemblyNameLocal);
                Type? t = assem.GetType(cxt.TypeName);
                if (t != null)
                {
                    return t;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"COM Activation of {cxt.ClassId} failed. {e}");
            }

            const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
            throw new COMException(string.Empty, CLASS_E_CLASSNOTAVAILABLE);
        }

        [RequiresUnreferencedCode("The trimmer might remove types which are needed by the assemblies loaded in this method.")]
        private static AssemblyLoadContext GetALC(string assemblyPath, bool isolatedContext)
        {
            AssemblyLoadContext? alc;
            if (isolatedContext)
            {
                lock (s_assemblyLoadContexts)
                {
                    if (!s_assemblyLoadContexts.TryGetValue(assemblyPath, out alc))
                    {
                        alc = new IsolatedComponentLoadContext(assemblyPath);
                        s_assemblyLoadContexts.Add(assemblyPath, alc);
                    }
                }
            }
            else
            {
                alc = AssemblyLoadContext.Default;
                lock (s_loadedInDefaultContext)
                {
                    if (!s_loadedInDefaultContext.Contains(assemblyPath))
                    {
                        var resolver = new AssemblyDependencyResolver(assemblyPath);
                        AssemblyLoadContext.Default.Resolving +=
                            (context, assemblyName) =>
                            {
                                string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                                return assemblyPath != null
                                    ? context.LoadFromAssemblyPath(assemblyPath)
                                    : null;
                            };

                        s_loadedInDefaultContext.Add(assemblyPath);
                    }
                }
            }

            return alc;
        }

        [ComVisible(true)]
        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private sealed class BasicClassFactory : IClassFactory
        {
            private readonly Guid _classId;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            private readonly Type _classType;

            public BasicClassFactory(Guid clsid, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type classType)
            {
                _classId = clsid;
                _classType = classType;
            }

            public enum ValidatedInterfaceKind
            {
                IUnknown,
                IDispatch,
                ManagedType,
            }

            public struct ValidatedInterfaceType
            {
                public ValidatedInterfaceKind Kind { get; init; }
                public Type? ManagedType { get; init; }
            }

            public static ValidatedInterfaceType CreateValidatedInterfaceType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type classType, ref Guid riid, object? outer)
            {
                Debug.Assert(classType != null);
                if (riid == Marshal.IID_IUnknown)
                {
                    return new ValidatedInterfaceType() { Kind = ValidatedInterfaceKind.IUnknown, ManagedType = null };
                }
                else if (riid == Marshal.IID_IDispatch)
                {
                    ClassInterfaceAttribute? attr =
                        classType.GetCustomAttribute<ClassInterfaceAttribute>()
                        ?? classType.Assembly.GetCustomAttribute<ClassInterfaceAttribute>(); // If there is no attribute on the Type, check the Assembly.

                    // If the attribute is unspecified, the default is ClassInterfaceType.AutoDispatch.
                    // See DEFAULT_CLASS_INTERFACE_TYPE in native.
                    if (attr is null
                        || attr.Value is ClassInterfaceType.AutoDispatch or ClassInterfaceType.AutoDual)
                    {
                        return new ValidatedInterfaceType() { Kind = ValidatedInterfaceKind.IDispatch, ManagedType = null };
                    }
                }

                // Aggregation can only be done when requesting IUnknown.
                if (outer != null)
                {
                    const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
                    throw new COMException(string.Empty, CLASS_E_NOAGGREGATION);
                }

                // Verify the class implements the desired interface
                foreach (Type i in classType.GetInterfaces())
                {
                    if (i.GUID == riid)
                    {
                        return new ValidatedInterfaceType() { Kind = ValidatedInterfaceKind.ManagedType, ManagedType = i };
                    }
                }

                // E_NOINTERFACE
                throw new InvalidCastException();
            }

            public static IntPtr GetObjectAsInterface(object obj, ValidatedInterfaceType interfaceType)
            {
                if (interfaceType.Kind is ValidatedInterfaceKind.IUnknown)
                {
                    Debug.Assert(interfaceType.ManagedType is null);
                    return Marshal.GetIUnknownForObject(obj);
                }
                else if (interfaceType.Kind is ValidatedInterfaceKind.IDispatch)
                {
                    Debug.Assert(interfaceType.ManagedType is null);
                    return Marshal.GetIDispatchForObject(obj);
                }

                Debug.Assert(interfaceType.Kind is ValidatedInterfaceKind.ManagedType
                    && interfaceType.ManagedType != null
                    && interfaceType.ManagedType.IsInterface);

                // The intent of this call is to get AND validate the interface can be
                // marshalled to native code. An exception will be thrown if the
                // type is unable to be marshalled to native code.
                // Scenarios where this is relevant:
                //  - Interfaces that use Generics
                //  - Interfaces that define implementation
                IntPtr interfaceMaybe = Marshal.GetComInterfaceForObject(obj, interfaceType.ManagedType, CustomQueryInterfaceMode.Ignore);

                if (interfaceMaybe == IntPtr.Zero)
                {
                    // E_NOINTERFACE
                    throw new InvalidCastException();
                }

                return interfaceMaybe;
            }

            public static object CreateAggregatedObject(object pUnkOuter, object comObject)
            {
                Debug.Assert(pUnkOuter != null && comObject != null);
                IntPtr outerPtr = Marshal.GetIUnknownForObject(pUnkOuter);

                try
                {
                    IntPtr innerPtr = Marshal.CreateAggregatedObject(outerPtr, comObject);
                    return Marshal.GetObjectForIUnknown(innerPtr);
                }
                finally
                {
                    // Decrement the above 'Marshal.GetIUnknownForObject()'
                    Marshal.Release(outerPtr);
                }
            }

            public void CreateInstance(
                [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
                ref Guid riid,
                out IntPtr ppvObject)
            {
                var interfaceType = CreateValidatedInterfaceType(_classType, ref riid, pUnkOuter);

                object obj = Activator.CreateInstance(_classType)!;
                if (pUnkOuter != null)
                {
                    obj = CreateAggregatedObject(pUnkOuter, obj);
                }

                ppvObject = GetObjectAsInterface(obj, interfaceType);
            }

            public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
            {
                // nop
            }
        }

        [ComVisible(true)]
        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private sealed class LicenseClassFactory : IClassFactory2
        {
            private readonly Guid _classId;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicConstructors)]
            private readonly Type _classType;

            public LicenseClassFactory(Guid clsid, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicConstructors)] Type classType)
            {
                _classId = clsid;
                _classType = classType;
            }

            public void CreateInstance(
                [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
                ref Guid riid,
                out IntPtr ppvObject)
            {
                CreateInstanceInner(pUnkOuter, ref riid, key: null, isDesignTime: true, out ppvObject);
            }

            public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
            {
                // nop
            }

            public void GetLicInfo(ref LICINFO licInfo)
            {
                LicenseInteropProxy.GetLicInfo(_classType, out bool runtimeKeyAvail, out bool licVerified);

                // The LICINFO is a struct with a DWORD size field and two BOOL fields. Each BOOL
                // is typedef'd from a DWORD, therefore the size is manually computed as below.
                licInfo.cbLicInfo = sizeof(int) + sizeof(int) + sizeof(int);
                licInfo.fRuntimeKeyAvail = runtimeKeyAvail;
                licInfo.fLicVerified = licVerified;
            }

            public void RequestLicKey(int dwReserved, [MarshalAs(UnmanagedType.BStr)] out string pBstrKey)
            {
                pBstrKey = LicenseInteropProxy.RequestLicKey(_classType);
            }

            public void CreateInstanceLic(
                [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
                [MarshalAs(UnmanagedType.Interface)] object? pUnkReserved,
                ref Guid riid,
                [MarshalAs(UnmanagedType.BStr)] string bstrKey,
                out IntPtr ppvObject)
            {
                Debug.Assert(pUnkReserved == null);
                CreateInstanceInner(pUnkOuter, ref riid, bstrKey, isDesignTime: false, out ppvObject);
            }

            private void CreateInstanceInner(
                object? pUnkOuter,
                ref Guid riid,
                string? key,
                bool isDesignTime,
                out IntPtr ppvObject)
            {
                var interfaceType = BasicClassFactory.CreateValidatedInterfaceType(_classType, ref riid, pUnkOuter);

                object obj = LicenseInteropProxy.AllocateAndValidateLicense(_classType, key, isDesignTime);
                if (pUnkOuter != null)
                {
                    obj = BasicClassFactory.CreateAggregatedObject(pUnkOuter, obj);
                }

                ppvObject = BasicClassFactory.GetObjectAsInterface(obj, interfaceType);
            }
        }
    }

    // This is a helper class that supports the CLR's IClassFactory2 marshaling
    // support.
    //
    // When a managed object is exposed to COM, the CLR invokes
    // AllocateAndValidateLicense() to set up the appropriate
    // license context and instantiate the object.
    internal sealed class LicenseInteropProxy
    {
        private static readonly Type? s_licenseAttrType = Type.GetType("System.ComponentModel.LicenseProviderAttribute, System.ComponentModel.TypeConverter", throwOnError: false);
        private static readonly Type? s_licenseExceptionType = Type.GetType("System.ComponentModel.LicenseException, System.ComponentModel.TypeConverter", throwOnError: false);

        private const string LicenseManagerTypeName = "System.ComponentModel.LicenseManager, System.ComponentModel.TypeConverter";
        private const string LicenseContextTypeName = "System.ComponentModel.LicenseContext, System.ComponentModel.TypeConverter";
        private const string LicenseInteropHelperTypeName = "System.ComponentModel.LicenseManager+LicenseInteropHelper, System.ComponentModel.TypeConverter";
        private const string CLRLicenseContextTypeName = "System.ComponentModel.LicenseManager+CLRLicenseContext, System.ComponentModel.TypeConverter";
        private const string LicenseRefTypeName = "System.ComponentModel.License&, System.ComponentModel.TypeConverter";
        private const string LicInfoHelperLicenseContextTypeName = "System.ComponentModel.LicenseManager+LicInfoHelperLicenseContext, System.ComponentModel.TypeConverter";

        // RCW Activation
        private object? _licContext;
        private Type? _targetRcwType;

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        private static extern void SetSavedLicenseKey(
            [UnsafeAccessorType(LicenseContextTypeName)] object licContext,
            Type type,
            string key);

        [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Manually validated that the annotations are kept in sync.")]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        private static extern object? CreateWithContext(
            [UnsafeAccessorType(LicenseManagerTypeName)] object? licManager,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            [UnsafeAccessorType(LicenseContextTypeName)] object licContext
        );

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        private static extern bool ValidateAndRetrieveLicenseDetails(
            [UnsafeAccessorType(LicenseInteropHelperTypeName)] object? licInteropHelper,
            [UnsafeAccessorType(LicenseContextTypeName)] object? licContext,
            Type type,
            [UnsafeAccessorType(LicenseRefTypeName)] out object? license,
            out string? licenseKey);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        [return: UnsafeAccessorType(LicenseContextTypeName)]
        private static extern object? GetCurrentContextInfo(
            [UnsafeAccessorType(LicenseInteropHelperTypeName)] object? licInteropHelper,
            Type type,
            out bool isDesignTime,
            out string? key);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        [return: UnsafeAccessorType(CLRLicenseContextTypeName)]
        private static extern object CreateDesignContext(
            [UnsafeAccessorType(CLRLicenseContextTypeName)] object? context,
            Type type);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        [return: UnsafeAccessorType(CLRLicenseContextTypeName)]
        private static extern object CreateRuntimeContext(
            [UnsafeAccessorType(CLRLicenseContextTypeName)] object? context,
            Type type,
            string? key);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return:UnsafeAccessorType(LicInfoHelperLicenseContextTypeName)]
        private static extern object CreateLicInfoHelperLicenseContext();

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        private static extern bool Contains(
            [UnsafeAccessorType(LicInfoHelperLicenseContextTypeName)] object? licInfoHelperContext,
            string assemblyName);

        // Helper function to create an object from the native side
        public static object Create()
        {
            return new LicenseInteropProxy();
        }

        // Determine if the type supports licensing
        public static bool HasLicense(Type type)
        {
            // If the attribute type can't be found, then the type
            // definitely doesn't support licensing.
            if (s_licenseAttrType == null)
            {
                return false;
            }

            return type.IsDefined(s_licenseAttrType, inherit: true);
        }

        // The CLR invokes this whenever a COM client invokes
        // IClassFactory2::GetLicInfo on a managed class.
        //
        // COM normally doesn't expect this function to fail so this method
        // should only throw in the case of a catastrophic error (stack, memory, etc.)
        public static void GetLicInfo(Type type, out bool runtimeKeyAvail, out bool licVerified)
        {
            runtimeKeyAvail = false;
            licVerified = false;

            object licContext = CreateLicInfoHelperLicenseContext();
            bool isValid = ValidateAndRetrieveLicenseDetails(null, licContext, type, out object? license, out _);
            if (!isValid)
            {
                return;
            }

            if (license is IDisposable disp)
            {
                // Dispose of the license if it implements IDisposable
                // and we are not in design mode.  This is a bit of a hack
                // but we need to do this to avoid leaking the license.
                // The license will be disposed of when the context is
                // disposed of.
                disp.Dispose();
                licVerified = true;
            }

            runtimeKeyAvail = Contains(licContext, type.AssemblyQualifiedName!);
        }

        // The CLR invokes this whenever a COM client invokes
        // IClassFactory2::RequestLicKey on a managed class.
        public static string RequestLicKey(Type type)
        {
            // License will be null, since we passed no instance,
            // however we can still retrieve the "first" license
            // key from the file. This really will only
            // work for simple COM-compatible license providers
            // like LicFileLicenseProvider that don't require the
            // instance to grant a key.

            if (!ValidateAndRetrieveLicenseDetails(null, null, type, out object? license, out string? licenseKey))
            {
                throw new COMException(); // E_FAIL
            }

            ((IDisposable?)license)?.Dispose();

            return licenseKey ?? throw new COMException(); // E_FAIL
        }

        // The CLR invokes this whenever a COM client invokes
        // IClassFactory::CreateInstance() or IClassFactory2::CreateInstanceLic()
        // on a managed that has a LicenseProvider custom attribute.
        //
        // If we are being entered because of a call to ICF::CreateInstance(),
        // "isDesignTime" will be "true".
        //
        // If we are being entered because of a call to ICF::CreateInstanceLic(),
        // "isDesignTime" will be "false" and "key" will point to a non-null
        // license key.
        public static object AllocateAndValidateLicense([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string? key, bool isDesignTime)
        {
            object licContext;
            if (isDesignTime)
            {
                licContext = CreateDesignContext(null, type);
            }
            else
            {
                licContext = CreateRuntimeContext(null, type, key);
            }

            try
            {
                return CreateWithContext(null, type, licContext)!;
            }
            catch (Exception exception) when (exception.GetType() == s_licenseExceptionType)
            {
                const int CLASS_E_NOTLICENSED = unchecked((int)0x80040112);
                throw new COMException(exception.Message, CLASS_E_NOTLICENSED);
            }
        }

        // See usage in native RCW code
        public void GetCurrentContextInfo(RuntimeTypeHandle rth, out bool isDesignTime, out IntPtr bstrKey)
        {
            Type targetRcwTypeMaybe = Type.GetTypeFromHandle(rth)!;

            _licContext = GetCurrentContextInfo(null, targetRcwTypeMaybe, out isDesignTime, out string? key);

            _targetRcwType = targetRcwTypeMaybe;
            bstrKey = Marshal.StringToBSTR((string)key!);
        }

        // The CLR invokes this when instantiating a licensed COM
        // object inside a designtime license context.
        // It's purpose is to save away the license key that the CLR
        // retrieved using RequestLicKey().
        public void SaveKeyInCurrentContext(IntPtr bstrKey)
        {
            if (bstrKey == IntPtr.Zero)
            {
                return;
            }

            string key = Marshal.PtrToStringBSTR(bstrKey);

            SetSavedLicenseKey(_licContext!, _targetRcwType!, key);
        }
    }
}
