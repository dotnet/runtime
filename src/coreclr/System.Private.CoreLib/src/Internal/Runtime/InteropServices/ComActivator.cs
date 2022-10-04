// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
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
    [Guid("B196B28F-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory2 : IClassFactory
    {
        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
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

    internal partial struct ComActivationContext
    {
        public static unsafe ComActivationContext Create(ref ComActivationContextInternal cxtInt)
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
                TypeName = Marshal.PtrToStringUni(new IntPtr(cxtInt.TypeNameBuffer))!
            };
        }
    }

    [SupportedOSPlatform("windows")]
    internal static class ComActivator
    {
        // Collection of all ALCs used for COM activation. In the event we want to support
        // unloadable COM server ALCs, this will need to be changed.
        private static readonly Dictionary<string, AssemblyLoadContext> s_assemblyLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.InvariantCultureIgnoreCase);

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

            if (cxt.InterfaceId != typeof(IClassFactory).GUID
                && cxt.InterfaceId != typeof(IClassFactory2).GUID)
            {
                throw new NotSupportedException();
            }

            if (!Path.IsPathRooted(cxt.AssemblyPath))
            {
                throw new ArgumentException(null, nameof(cxt));
            }

            Type classType = FindClassType(cxt.ClassId, cxt.AssemblyPath, cxt.AssemblyName, cxt.TypeName);

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

            Type classType = FindClassType(cxt.ClassId, cxt.AssemblyPath, cxt.AssemblyName, cxt.TypeName);

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
                    ParameterInfo[] methParams = method.GetParameters();
                    if (method.ReturnType != typeof(void)
                        || methParams == null
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
        /// Internal entry point for unmanaged COM activation API from native code
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int GetClassFactoryForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ref ComActivationContextInternal cxtInt = ref *pCxtInt;

            if (IsLoggingEnabled())
            {
                Log(
$@"{nameof(GetClassFactoryForTypeInternal)} arguments:
    {cxtInt.ClassId}
    {cxtInt.InterfaceId}
    0x{(ulong)cxtInt.AssemblyPathBuffer:x}
    0x{(ulong)cxtInt.AssemblyNameBuffer:x}
    0x{(ulong)cxtInt.TypeNameBuffer:x}
    0x{cxtInt.ClassFactoryDest.ToInt64():x}");
            }

            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt);
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
                object cf = GetClassFactoryForType(cxt);
#pragma warning restore IL2026
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
        /// Internal entry point for registering a managed COM server API from native code
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int RegisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ref ComActivationContextInternal cxtInt = ref *pCxtInt;

            if (IsLoggingEnabled())
            {
                Log(
$@"{nameof(RegisterClassForTypeInternal)} arguments:
    {cxtInt.ClassId}
    {cxtInt.InterfaceId}
    0x{(ulong)cxtInt.AssemblyPathBuffer:x}
    0x{(ulong)cxtInt.AssemblyNameBuffer:x}
    0x{(ulong)cxtInt.TypeNameBuffer:x}
    0x{cxtInt.ClassFactoryDest.ToInt64():x}");
            }

            if (cxtInt.InterfaceId != Guid.Empty
                || cxtInt.ClassFactoryDest != IntPtr.Zero)
            {
                throw new ArgumentException(null, nameof(pCxtInt));
            }

            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt);
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
        /// Internal entry point for unregistering a managed COM server API from native code
        /// </summary>
        [UnmanagedCallersOnly]
        private static unsafe int UnregisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ref ComActivationContextInternal cxtInt = ref *pCxtInt;

            if (IsLoggingEnabled())
            {
                Log(
$@"{nameof(UnregisterClassForTypeInternal)} arguments:
    {cxtInt.ClassId}
    {cxtInt.InterfaceId}
    0x{(ulong)cxtInt.AssemblyPathBuffer:x}
    0x{(ulong)cxtInt.AssemblyNameBuffer:x}
    0x{(ulong)cxtInt.TypeNameBuffer:x}
    0x{cxtInt.ClassFactoryDest.ToInt64():x}");
            }

            if (cxtInt.InterfaceId != Guid.Empty
                || cxtInt.ClassFactoryDest != IntPtr.Zero)
            {
                throw new ArgumentException(null, nameof(pCxtInt));
            }

            try
            {
                var cxt = ComActivationContext.Create(ref cxtInt);
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

        private static bool IsLoggingEnabled()
        {
#if COM_ACTIVATOR_DEBUG
            return true;
#else
            return false;
#endif
        }

        private static void Log(string fmt, params object[] args)
        {
            // [TODO] Use FrameworkEventSource in release builds
            Debug.WriteLine(fmt, args);
        }

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        private static Type FindClassType(Guid clsid, string assemblyPath, string assemblyName, string typeName)
        {
            try
            {
                AssemblyLoadContext alc = GetALC(assemblyPath);
                var assemblyNameLocal = new AssemblyName(assemblyName);
                Assembly assem = alc.LoadFromAssemblyName(assemblyNameLocal);
                Type? t = assem.GetType(typeName);
                if (t != null)
                {
                    return t;
                }
            }
            catch (Exception e)
            {
                if (IsLoggingEnabled())
                {
                    Log($"COM Activation of {clsid} failed. {e}");
                }
            }

            const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
            throw new COMException(string.Empty, CLASS_E_CLASSNOTAVAILABLE);
        }

        [RequiresUnreferencedCode("The trimmer might remove types which are needed by the assemblies loaded in this method.")]
        private static AssemblyLoadContext GetALC(string assemblyPath)
        {
            AssemblyLoadContext? alc;

            lock (s_assemblyLoadContexts)
            {
                if (!s_assemblyLoadContexts.TryGetValue(assemblyPath, out alc))
                {
                    alc = new IsolatedComponentLoadContext(assemblyPath);
                    s_assemblyLoadContexts.Add(assemblyPath, alc);
                }
            }

            return alc;
        }

        [ComVisible(true)]
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

            public static Type GetValidatedInterfaceType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type classType, ref Guid riid, object? outer)
            {
                Debug.Assert(classType != null);
                if (riid == Marshal.IID_IUnknown)
                {
                    return typeof(object);
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
                        return i;
                    }
                }

                // E_NOINTERFACE
                throw new InvalidCastException();
            }

            public static IntPtr GetObjectAsInterface(object obj, Type interfaceType)
            {
                // If the requested "interface type" is type object then return as IUnknown
                if (interfaceType == typeof(object))
                {
                    return Marshal.GetIUnknownForObject(obj);
                }

                Debug.Assert(interfaceType.IsInterface);

                // The intent of this call is to get AND validate the interface can be
                // marshalled to native code. An exception will be thrown if the
                // type is unable to be marshalled to native code.
                // Scenarios where this is relevant:
                //  - Interfaces that use Generics
                //  - Interfaces that define implementation
                IntPtr interfaceMaybe = Marshal.GetComInterfaceForObject(obj, interfaceType, CustomQueryInterfaceMode.Ignore);

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

            [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
            public void CreateInstance(
                [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
                ref Guid riid,
                out IntPtr ppvObject)
            {
                Type interfaceType = BasicClassFactory.GetValidatedInterfaceType(_classType, ref riid, pUnkOuter);

                object obj = Activator.CreateInstance(_classType)!;
                if (pUnkOuter != null)
                {
                    obj = BasicClassFactory.CreateAggregatedObject(pUnkOuter, obj);
                }

                ppvObject = BasicClassFactory.GetObjectAsInterface(obj, interfaceType);
            }

            public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
            {
                // nop
            }
        }

        [ComVisible(true)]
        private sealed class LicenseClassFactory : IClassFactory2
        {
            private readonly LicenseInteropProxy _licenseProxy = new LicenseInteropProxy();
            private readonly Guid _classId;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicConstructors)]
            private readonly Type _classType;

            public LicenseClassFactory(Guid clsid, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicConstructors)] Type classType)
            {
                _classId = clsid;
                _classType = classType;
            }

            [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
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
                _licenseProxy.GetLicInfo(_classType, out bool runtimeKeyAvail, out bool licVerified);

                // The LICINFO is a struct with a DWORD size field and two BOOL fields. Each BOOL
                // is typedef'd from a DWORD, therefore the size is manually computed as below.
                licInfo.cbLicInfo = sizeof(int) + sizeof(int) + sizeof(int);
                licInfo.fRuntimeKeyAvail = runtimeKeyAvail;
                licInfo.fLicVerified = licVerified;
            }

            public void RequestLicKey(int dwReserved, [MarshalAs(UnmanagedType.BStr)] out string pBstrKey)
            {
                pBstrKey = _licenseProxy.RequestLicKey(_classType);
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
                Type interfaceType = BasicClassFactory.GetValidatedInterfaceType(_classType, ref riid, pUnkOuter);

                object obj = _licenseProxy.AllocateAndValidateLicense(_classType, key, isDesignTime);
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

        // LicenseManager
        private readonly MethodInfo _createWithContext;

        // LicenseInteropHelper
        private readonly MethodInfo _validateTypeAndReturnDetails;
        private readonly MethodInfo _getCurrentContextInfo;

        // CLRLicenseContext
        private readonly MethodInfo _createDesignContext;
        private readonly MethodInfo _createRuntimeContext;

        // LicenseContext
        private readonly MethodInfo _setSavedLicenseKey;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        private readonly Type _licInfoHelper;

        private readonly MethodInfo _licInfoHelperContains;

        // RCW Activation
        private object? _licContext;
        private Type? _targetRcwType;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers",
            Justification = "The type parameter to LicenseManager.CreateWithContext method has PublicConstructors annotation. We only invoke this method" +
                "from AllocateAndValidateLicense which annotates the value passed in with the same annotation.")]
        public LicenseInteropProxy()
        {
            Type licManager = Type.GetType("System.ComponentModel.LicenseManager, System.ComponentModel.TypeConverter", throwOnError: true)!;

            Type licContext = Type.GetType("System.ComponentModel.LicenseContext, System.ComponentModel.TypeConverter", throwOnError: true)!;
            _setSavedLicenseKey = licContext.GetMethod("SetSavedLicenseKey", BindingFlags.Instance | BindingFlags.Public)!;
            _createWithContext = licManager.GetMethod("CreateWithContext", new[] { typeof(Type), licContext })!;

            Type interopHelper = licManager.GetNestedType("LicenseInteropHelper", BindingFlags.NonPublic)!;
            _validateTypeAndReturnDetails = interopHelper.GetMethod("ValidateAndRetrieveLicenseDetails", BindingFlags.Static | BindingFlags.Public)!;
            _getCurrentContextInfo = interopHelper.GetMethod("GetCurrentContextInfo", BindingFlags.Static | BindingFlags.Public)!;

            Type clrLicContext = licManager.GetNestedType("CLRLicenseContext", BindingFlags.NonPublic)!;
            _createDesignContext = clrLicContext.GetMethod("CreateDesignContext", BindingFlags.Static | BindingFlags.Public)!;
            _createRuntimeContext = clrLicContext.GetMethod("CreateRuntimeContext", BindingFlags.Static | BindingFlags.Public)!;

            _licInfoHelper = licManager.GetNestedType("LicInfoHelperLicenseContext", BindingFlags.NonPublic)!;
            _licInfoHelperContains = _licInfoHelper.GetMethod("Contains", BindingFlags.Instance | BindingFlags.Public)!;
        }

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
        public void GetLicInfo(Type type, out bool runtimeKeyAvail, out bool licVerified)
        {
            runtimeKeyAvail = false;
            licVerified = false;

            // Types are as follows:
            // LicenseContext, Type, out License, out string
            object licContext = Activator.CreateInstance(_licInfoHelper)!;
            var parameters = new object?[] { licContext, type, /* out */ null, /* out */ null };
            bool isValid = (bool)_validateTypeAndReturnDetails.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null)!;
            if (!isValid)
            {
                return;
            }

            var license = (IDisposable?)parameters[2];
            if (license != null)
            {
                license.Dispose();
                licVerified = true;
            }

            parameters = new object?[] { type.AssemblyQualifiedName };
            runtimeKeyAvail = (bool)_licInfoHelperContains.Invoke(licContext, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null)!;
        }

        // The CLR invokes this whenever a COM client invokes
        // IClassFactory2::RequestLicKey on a managed class.
        public string RequestLicKey(Type type)
        {
            // License will be null, since we passed no instance,
            // however we can still retrieve the "first" license
            // key from the file. This really will only
            // work for simple COM-compatible license providers
            // like LicFileLicenseProvider that don't require the
            // instance to grant a key.

            // Types are as follows:
            // LicenseContext, Type, out License, out string
            var parameters = new object?[] { /* use global LicenseContext */ null, type, /* out */ null, /* out */ null };
            bool isValid = (bool)_validateTypeAndReturnDetails.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null)!;
            if (!isValid)
            {
                throw new COMException(); // E_FAIL
            }

            ((IDisposable?)parameters[2])?.Dispose();

            var licenseKey = (string?)parameters[3];
            if (licenseKey == null)
            {
                throw new COMException(); // E_FAIL
            }

            return licenseKey;
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
        public object AllocateAndValidateLicense([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string? key, bool isDesignTime)
        {
            object?[] parameters;
            object? licContext;
            if (isDesignTime)
            {
                parameters = new object[] { type };
                licContext = _createDesignContext.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null);
            }
            else
            {
                parameters = new object?[] { type, key };
                licContext = _createRuntimeContext.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null);
            }

            try
            {
                parameters = new object?[] { type, licContext };
                return _createWithContext.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null)!;
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

            // Types are as follows:
            // Type, out bool, out string -> LicenseContext
            var parameters = new object?[] { targetRcwTypeMaybe, /* out */ null, /* out */ null };
            _licContext = _getCurrentContextInfo.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null);

            _targetRcwType = targetRcwTypeMaybe;
            isDesignTime = (bool)parameters[1]!;
            bstrKey = Marshal.StringToBSTR((string)parameters[2]!);
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
            var parameters = new object?[] { _targetRcwType, key };
            _setSavedLicenseKey.Invoke(_licContext, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null);
        }
    }
}
