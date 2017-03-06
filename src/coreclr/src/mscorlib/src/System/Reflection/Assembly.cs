// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** 
** 
**
**
** Purpose: For Assembly-related stuff.
**
**
=============================================================================*/

namespace System.Reflection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Security;
    using System.Security.Policy;
    using System.IO;
    using StringBuilder = System.Text.StringBuilder;
    using System.Configuration.Assemblies;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using Microsoft.Win32;
    using System.Threading;
    using __HResults = System.__HResults;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Runtime.Loader;

    [Serializable]
    public delegate Module ModuleResolveEventHandler(Object sender, ResolveEventArgs e);


    [Serializable]
    public abstract class Assembly : ICustomAttributeProvider, ISerializable
    {
        protected Assembly() { }

        #region public static methods

        public static String CreateQualifiedName(String assemblyName, String typeName)
        {
            return typeName + ", " + assemblyName;
        }

        public static Assembly GetAssembly(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Contract.EndContractBlock();

            Module m = type.Module;
            if (m == null)
                return null;
            else
                return m.Assembly;
        }

        public static bool operator ==(Assembly left, Assembly right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeAssembly || right is RuntimeAssembly)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Assembly left, Assembly right)
        {
            return !(left == right);
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static Assembly LoadFrom(String assemblyFile)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));
            string fullPath = Path.GetFullPath(assemblyFile);
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
        }

        // Locate an assembly for reflection by the name of the file containing the manifest.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly ReflectionOnlyLoadFrom(String assemblyFile)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));
            if (assemblyFile.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReflectionOnlyLoad"));
        }

        // Evidence is protected in Assembly.Load()
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly LoadFrom(String assemblyFile,
                                        Evidence securityEvidence)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile,
                securityEvidence,
                null, // hashValue
                AssemblyHashAlgorithm.None,
                false,// forIntrospection);
                ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly LoadFrom(String assemblyFile,
                                        byte[] hashValue,
                                        AssemblyHashAlgorithm hashAlgorithm)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadFromHash"));
        }

        public static Assembly UnsafeLoadFrom(string assemblyFile)
        {
            return LoadFrom(assemblyFile);
        }

        // Locate an assembly by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(String assemblyString)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false /*forIntrospection*/);
        }

        // Returns type from the assembly while keeping compatibility with Assembly.Load(assemblyString).GetType(typeName) for managed types.
        // Calls Type.GetType for WinRT types.
        // Note: Type.GetType fails for assembly names that start with weird characters like '['. By calling it for managed types we would 
        // break AppCompat.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Type GetType_Compat(String assemblyString, String typeName)
        {
            // Normally we would get the stackMark only in public APIs. This is internal API, but it is AppCompat replacement of public API 
            // call Assembly.Load(assemblyString).GetType(typeName), therefore we take the stackMark here as well, to be fully compatible with 
            // the call sequence.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            RuntimeAssembly assembly;
            AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(
                assemblyString,
                false /*forIntrospection*/,
                out assembly);

            if (assembly == null)
            {
                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    return Type.GetType(typeName + ", " + assemblyString, true /*throwOnError*/, false /*ignoreCase*/);
                }

                assembly = RuntimeAssembly.InternalLoadAssemblyName(
                    assemblyName, null, null, ref stackMark,
                    true /*thrownOnFileNotFound*/, false /*forIntrospection*/);
            }
            return assembly.GetType(typeName, true /*throwOnError*/, false /*ignoreCase*/);
        }

        // Locate an assembly for reflection by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        //
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly ReflectionOnlyLoad(String assemblyString)
        {
            if (assemblyString == null)
                throw new ArgumentNullException(nameof(assemblyString));
            if (assemblyString.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReflectionOnlyLoad"));
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadCodeBase"));
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly Load(AssemblyName assemblyRef, IntPtr ptrLoadContextBinder)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadCodeBase"));
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/, ptrLoadContextBinder);
        }

        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static Assembly LoadWithPartialName(String partialName)
        {
            if (partialName == null)
                throw new ArgumentNullException(nameof(partialName));
            return Load(partialName);
        }

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(byte[] rawAssembly)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            return Load(rawAssembly, null);
        }

        // Loads the assembly for reflection with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly ReflectionOnlyLoad(byte[] rawAssembly)
        {
            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReflectionOnlyLoad"));
        }

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller. The second parameter is the raw bytes
        // representing the symbol store that matches the assembly.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));
            AssemblyLoadContext alc = new IndividualAssemblyLoadContext();
            MemoryStream assemblyStream = new MemoryStream(rawAssembly);
            MemoryStream symbolStream = (rawSymbolStore != null) ? new MemoryStream(rawSymbolStore) : null;
            return alc.LoadFromStream(assemblyStream, symbolStream);
        }

        private static Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();

        public static Assembly LoadFile(String path)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadFileSupported();

            Assembly result = null;
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (PathInternal.IsPartiallyQualified(path))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), nameof(path));
            }

            string normalizedPath = Path.GetFullPath(path);

            lock (s_loadfile)
            {
                if (s_loadfile.TryGetValue(normalizedPath, out result))
                    return result;
                AssemblyLoadContext alc = new IndividualAssemblyLoadContext();
                result = alc.LoadFromAssemblyPath(normalizedPath);
                s_loadfile.Add(normalizedPath, result);
            }
            return result;
        }

        /*
         * Get the assembly that the current code is running from.
         */
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod 
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly GetCallingAssembly()
        {
            // LookForMyCallersCaller is not guarantee to return the correct stack frame
            // because of inlining, tail calls, etc. As a result GetCallingAssembly is not 
            // ganranteed to return the correct result. We should document it as such.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        public static Assembly GetEntryAssembly()
        {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();
            return domainManager.EntryAssembly;
        }

        #endregion // public static methods

        #region public methods
        public virtual event ModuleResolveEventHandler ModuleResolve
        {
            add
            {
                throw new NotImplementedException();
            }
            remove
            {
                throw new NotImplementedException();
            }
        }

        public virtual String CodeBase
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String EscapedCodeBase
        {
            get
            {
                return AssemblyName.EscapeCodeBase(CodeBase);
            }
        }

        public virtual AssemblyName GetName()
        {
            return GetName(false);
        }

        public virtual AssemblyName GetName(bool copiedName)
        {
            throw new NotImplementedException();
        }

        public virtual String FullName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual MethodInfo EntryPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Type GetType(String name)
        {
            return GetType(name, false, false);
        }

        public virtual Type GetType(String name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public virtual Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<Type> ExportedTypes
        {
            get
            {
                return GetExportedTypes();
            }
        }

        public virtual Type[] GetExportedTypes()
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                Type[] types = GetTypes();

                TypeInfo[] typeinfos = new TypeInfo[types.Length];

                for (int i = 0; i < types.Length; i++)
                {
                    TypeInfo typeinfo = types[i].GetTypeInfo();
                    if (typeinfo == null)
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoTypeInfo", types[i].FullName));

                    typeinfos[i] = typeinfo;
                }

                return typeinfos;
            }
        }

        public virtual Type[] GetTypes()
        {
            Module[] m = GetModules(false);

            int iFinalLength = 0;
            Type[][] ModuleTypes = new Type[m.Length][];

            for (int i = 0; i < ModuleTypes.Length; i++)
            {
                ModuleTypes[i] = m[i].GetTypes();
                iFinalLength += ModuleTypes[i].Length;
            }

            int iCurrent = 0;
            Type[] ret = new Type[iFinalLength];
            for (int i = 0; i < ModuleTypes.Length; i++)
            {
                int iLength = ModuleTypes[i].Length;
                Array.Copy(ModuleTypes[i], 0, ret, iCurrent, iLength);
                iCurrent += iLength;
            }

            return ret;
        }

        // Load a resource based on the NameSpace of the type.
        public virtual Stream GetManifestResourceStream(Type type, String name)
        {
            throw new NotImplementedException();
        }

        public virtual Stream GetManifestResourceStream(String name)
        {
            throw new NotImplementedException();
        }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // Useful for binding to a very specific version of a satellite assembly
        public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            throw new NotImplementedException();
        }

        public bool IsFullyTrusted
        {
            get
            {
                return true;
            }
        }

        public virtual SecurityRuleSet SecurityRuleSet
        {
            get
            {
                return SecurityRuleSet.None;
            }
        }

        // ISerializable implementation
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public virtual Module ManifestModule
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.ManifestModule;

                throw new NotImplementedException();
            }
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }
        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            Contract.Ensures(Contract.Result<Object[]>() != null);
            throw new NotImplementedException();
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            Contract.Ensures(Contract.Result<Object[]>() != null);
            throw new NotImplementedException();
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public virtual bool ReflectionOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Module LoadModule(String moduleName,
                                 byte[] rawModule)
        {
            return LoadModule(moduleName, rawModule, null);
        }

        public virtual Module LoadModule(String moduleName,
                                 byte[] rawModule,
                                 byte[] rawSymbolStore)
        {
            throw new NotImplementedException();
        }

        //
        // Locates a type from this assembly and creates an instance of it using
        // the system activator. 
        //
        public Object CreateInstance(String typeName)
        {
            return CreateInstance(typeName,
                                  false, // ignore case
                                  BindingFlags.Public | BindingFlags.Instance,
                                  null, // binder
                                  null, // args
                                  null, // culture
                                  null); // activation attributes
        }

        public Object CreateInstance(String typeName,
                                     bool ignoreCase)
        {
            return CreateInstance(typeName,
                                  ignoreCase,
                                  BindingFlags.Public | BindingFlags.Instance,
                                  null, // binder
                                  null, // args
                                  null, // culture
                                  null); // activation attributes
        }

        public virtual Object CreateInstance(String typeName,
                                     bool ignoreCase,
                                     BindingFlags bindingAttr,
                                     Binder binder,
                                     Object[] args,
                                     CultureInfo culture,
                                     Object[] activationAttributes)
        {
            Type t = GetType(typeName, false, ignoreCase);
            if (t == null) return null;
            return Activator.CreateInstance(t,
                                            bindingAttr,
                                            binder,
                                            args,
                                            culture,
                                            activationAttributes);
        }

        public virtual IEnumerable<Module> Modules
        {
            get
            {
                return GetLoadedModules(true);
            }
        }

        public Module[] GetLoadedModules()
        {
            return GetLoadedModules(false);
        }

        public virtual Module[] GetLoadedModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public Module[] GetModules()
        {
            return GetModules(false);
        }

        public virtual Module[] GetModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public virtual Module GetModule(String name)
        {
            throw new NotImplementedException();
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
        public virtual FileStream GetFile(String name)
        {
            throw new NotImplementedException();
        }

        public virtual FileStream[] GetFiles()
        {
            return GetFiles(false);
        }

        public virtual FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        // Returns the names of all the resources
        public virtual String[] GetManifestResourceNames()
        {
            throw new NotImplementedException();
        }

        public virtual AssemblyName[] GetReferencedAssemblies()
        {
            throw new NotImplementedException();
        }

        public virtual ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            throw new NotImplementedException();
        }

        public override String ToString()
        {
            String displayName = FullName;
            if (displayName == null)
                return base.ToString();
            else
                return displayName;
        }

        public virtual String Location
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String ImageRuntimeVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /*
          Returns true if the assembly was loaded from the global assembly cache.
        */
        public virtual bool GlobalAssemblyCache
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Int64 HostContext
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.HostContext;

                throw new NotImplementedException();
            }
        }

        public virtual bool IsDynamic
        {
            get
            {
                return false;
            }
        }
        #endregion // public methods

    }

    [Serializable]
    internal class RuntimeAssembly : Assembly
    {
#if FEATURE_APPX
        // The highest byte is the flags and the lowest 3 bytes are 
        // the cached ctor token of [DynamicallyInvocableAttribute].
        private enum ASSEMBLY_FLAGS : uint
        {
            ASSEMBLY_FLAGS_UNKNOWN = 0x00000000,
            ASSEMBLY_FLAGS_INITIALIZED = 0x01000000,
            ASSEMBLY_FLAGS_FRAMEWORK = 0x02000000,
            ASSEMBLY_FLAGS_TOKEN_MASK = 0x00FFFFFF,
        }
#endif // FEATURE_APPX

        private const uint COR_E_LOADING_REFERENCE_ASSEMBLY = 0x80131058U;

        internal RuntimeAssembly() { throw new NotSupportedException(); }

        #region private data members
        private event ModuleResolveEventHandler _ModuleResolve;
        private string m_fullname;
        private object m_syncRoot;   // Used to keep collectible types alive and as the syncroot for reflection.emit
        private IntPtr m_assembly;    // slack for ptr datum on unmanaged side

#if FEATURE_APPX
        private ASSEMBLY_FLAGS m_flags;
#endif
        #endregion

#if FEATURE_APPX
        private ASSEMBLY_FLAGS Flags
        {
            get
            {
                if ((m_flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED) == 0)
                {
                    ASSEMBLY_FLAGS flags = ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN
                        | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK;

                    m_flags = flags | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED;
                }

                return m_flags;
            }
        }
#endif // FEATURE_APPX

        internal object SyncRoot
        {
            get
            {
                if (m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), null);
                }
                return m_syncRoot;
            }
        }

        public override event ModuleResolveEventHandler ModuleResolve
        {
            add
            {
                _ModuleResolve += value;
            }
            remove
            {
                _ModuleResolve -= value;
            }
        }

        private const String s_localFilePrefix = "file:";

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetCodeBase(RuntimeAssembly assembly,
                                               bool copiedName,
                                               StringHandleOnStack retString);

        internal String GetCodeBase(bool copiedName)
        {
            String codeBase = null;
            GetCodeBase(GetNativeHandle(), copiedName, JitHelpers.GetStringHandleOnStack(ref codeBase));
            return codeBase;
        }

        public override String CodeBase
        {
            get
            {
                String codeBase = GetCodeBase(false);
                return codeBase;
            }
        }

        internal RuntimeAssembly GetNativeHandle()
        {
            return this;
        }

        // If the assembly is copied before it is loaded, the codebase will be set to the
        // actual file loaded if copiedName is true. If it is false, then the original code base
        // is returned.
        public override AssemblyName GetName(bool copiedName)
        {
            AssemblyName an = new AssemblyName();

            String codeBase = GetCodeBase(copiedName);

            an.Init(GetSimpleName(),
                    GetPublicKey(),
                    null, // public key token
                    GetVersion(),
                    GetLocale(),
                    GetHashAlgorithm(),
                    AssemblyVersionCompatibility.SameMachine,
                    codeBase,
                    GetFlags() | AssemblyNameFlags.PublicKey,
                    null); // strong name key pair

            PortableExecutableKinds pek;
            ImageFileMachine ifm;

            Module manifestModule = ManifestModule;
            if (manifestModule != null)
            {
                if (manifestModule.MDStreamVersion > 0x10000)
                {
                    ManifestModule.GetPEKind(out pek, out ifm);
                    an.SetProcArchIndex(pek, ifm);
                }
            }
            return an;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetFullName(RuntimeAssembly assembly, StringHandleOnStack retString);

        public override String FullName
        {
            get
            {
                // If called by Object.ToString(), return val may be NULL.
                if (m_fullname == null)
                {
                    string s = null;
                    GetFullName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                    Interlocked.CompareExchange<string>(ref m_fullname, s, null);
                }

                return m_fullname;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetEntryPoint(RuntimeAssembly assembly, ObjectHandleOnStack retMethod);

        public override MethodInfo EntryPoint
        {
            get
            {
                IRuntimeMethodInfo methodHandle = null;
                GetEntryPoint(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref methodHandle));

                if (methodHandle == null)
                    return null;

                return (MethodInfo)RuntimeType.GetMethodBase(methodHandle);
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetType(RuntimeAssembly assembly,
                                                        String name,
                                                        bool throwOnError,
                                                        bool ignoreCase,
                                                        ObjectHandleOnStack type,
                                                        ObjectHandleOnStack keepAlive);

        public override Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            // throw on null strings regardless of the value of "throwOnError"
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            RuntimeType type = null;
            Object keepAlive = null;
            GetType(GetNativeHandle(), name, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack(ref type), JitHelpers.GetObjectHandleOnStack(ref keepAlive));
            GC.KeepAlive(keepAlive);

            return type;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetExportedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);

        public override Type[] GetExportedTypes()
        {
            Type[] types = null;
            GetExportedTypes(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));
            return types;
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                List<RuntimeType> rtTypes = new List<RuntimeType>();

                RuntimeModule[] modules = GetModulesInternal(true, false);

                for (int i = 0; i < modules.Length; i++)
                {
                    rtTypes.AddRange(modules[i].GetDefinedTypes());
                }

                return rtTypes.ToArray();
            }
        }

        // Load a resource based on the NameSpace of the type.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Stream GetManifestResourceStream(Type type, String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(type, name, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Stream GetManifestResourceStream(String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(name, ref stackMark, false);
        }

        // ISerializable implementation
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Contract.EndContractBlock();

            UnitySerializationHolder.GetUnitySerializationInfo(info,
                                                               UnitySerializationHolder.AssemblyUnity,
                                                               this.FullName,
                                                               this);
        }

        public override Module ManifestModule
        {
            get
            {
                // We don't need to return the "external" ModuleBuilder because
                // it is meant to be read-only
                return RuntimeAssembly.GetManifestModule(GetNativeHandle());
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static RuntimeAssembly InternalLoadFrom(String assemblyFile,
                                                         Evidence securityEvidence,
                                                         byte[] hashValue,
                                                         AssemblyHashAlgorithm hashAlgorithm,
                                                         bool forIntrospection,
                                                         ref StackCrawlMark stackMark)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));

            Contract.EndContractBlock();

            AssemblyName an = new AssemblyName();
            an.CodeBase = assemblyFile;
            an.SetHashControl(hashValue, hashAlgorithm);
            // The stack mark is used for MDA filtering
            return InternalLoadAssemblyName(an, securityEvidence, null, ref stackMark, true /*thrownOnFileNotFound*/, forIntrospection);
        }

        // Wrapper function to wrap the typical use of InternalLoad.
        internal static RuntimeAssembly InternalLoad(String assemblyString,
                                                     Evidence assemblySecurity,
                                                     ref StackCrawlMark stackMark,
                                                     bool forIntrospection)
        {
            return InternalLoad(assemblyString, assemblySecurity, ref stackMark, IntPtr.Zero, forIntrospection);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static RuntimeAssembly InternalLoad(String assemblyString,
                                                     Evidence assemblySecurity,
                                                     ref StackCrawlMark stackMark,
                                                     IntPtr pPrivHostBinder,
                                                     bool forIntrospection)
        {
            RuntimeAssembly assembly;
            AssemblyName an = CreateAssemblyName(assemblyString, forIntrospection, out assembly);

            if (assembly != null)
            {
                // The assembly was returned from ResolveAssemblyEvent
                return assembly;
            }

            return InternalLoadAssemblyName(an, assemblySecurity, null, ref stackMark,
                                            pPrivHostBinder,
                                            true  /*thrownOnFileNotFound*/, forIntrospection);
        }

        // Creates AssemblyName. Fills assembly if AssemblyResolve event has been raised.
        internal static AssemblyName CreateAssemblyName(
            String assemblyString,
            bool forIntrospection,
            out RuntimeAssembly assemblyFromResolveEvent)
        {
            if (assemblyString == null)
                throw new ArgumentNullException(nameof(assemblyString));
            Contract.EndContractBlock();

            if ((assemblyString.Length == 0) ||
                (assemblyString[0] == '\0'))
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));

            if (forIntrospection)
                AppDomain.CheckReflectionOnlyLoadSupported();

            AssemblyName an = new AssemblyName();

            an.Name = assemblyString;
            an.nInit(out assemblyFromResolveEvent, forIntrospection, true);

            return an;
        }

        // Wrapper function to wrap the typical use of InternalLoadAssemblyName.
        internal static RuntimeAssembly InternalLoadAssemblyName(
            AssemblyName assemblyRef,
            Evidence assemblySecurity,
            RuntimeAssembly reqAssembly,
            ref StackCrawlMark stackMark,
            bool throwOnFileNotFound,
            bool forIntrospection,
            IntPtr ptrLoadContextBinder = default(IntPtr))
        {
            return InternalLoadAssemblyName(assemblyRef, assemblySecurity, reqAssembly, ref stackMark, IntPtr.Zero, true /*throwOnError*/, forIntrospection, ptrLoadContextBinder);
        }

        internal static RuntimeAssembly InternalLoadAssemblyName(
            AssemblyName assemblyRef,
            Evidence assemblySecurity,
            RuntimeAssembly reqAssembly,
            ref StackCrawlMark stackMark,
            IntPtr pPrivHostBinder,
            bool throwOnFileNotFound,
            bool forIntrospection,
            IntPtr ptrLoadContextBinder = default(IntPtr))
        {
            if (assemblyRef == null)
                throw new ArgumentNullException(nameof(assemblyRef));
            Contract.EndContractBlock();

            if (assemblyRef.CodeBase != null)
            {
                AppDomain.CheckLoadFromSupported();
            }

            assemblyRef = (AssemblyName)assemblyRef.Clone();
            if (!forIntrospection &&
                (assemblyRef.ProcessorArchitecture != ProcessorArchitecture.None))
            {
                // PA does not have a semantics for by-name binds for execution
                assemblyRef.ProcessorArchitecture = ProcessorArchitecture.None;
            }

            String codeBase = VerifyCodeBase(assemblyRef.CodeBase);

            return nLoad(assemblyRef, codeBase, assemblySecurity, reqAssembly, ref stackMark,
                pPrivHostBinder,
                throwOnFileNotFound, forIntrospection, ptrLoadContextBinder);
        }

        // These are the framework assemblies that does reflection invocation
        // on behalf of user code. We allow framework code to invoke non-W8P
        // framework APIs but don't want user code to gain that privilege 
        // through these assemblies. So we blaklist them.
        private static string[] s_unsafeFrameworkAssemblyNames = new string[] {
            "System.Reflection.Context",
            "Microsoft.VisualBasic"
        };

#if FEATURE_APPX
        internal bool IsFrameworkAssembly()
        {
            ASSEMBLY_FLAGS flags = Flags;
            return (flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK) != 0;
        }
#endif

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeAssembly _nLoad(AssemblyName fileName,
                                                     String codeBase,
                                                     Evidence assemblySecurity,
                                                     RuntimeAssembly locationHint,
                                                     ref StackCrawlMark stackMark,
                                                     IntPtr pPrivHostBinder,
                                                     bool throwOnFileNotFound,
                                                     bool forIntrospection,
                                                     bool suppressSecurityChecks,
                                                     IntPtr ptrLoadContextBinder);

        private static RuntimeAssembly nLoad(AssemblyName fileName,
                                             String codeBase,
                                             Evidence assemblySecurity,
                                             RuntimeAssembly locationHint,
                                             ref StackCrawlMark stackMark,
                                             IntPtr pPrivHostBinder,
                                             bool throwOnFileNotFound,
                                             bool forIntrospection,
                                             IntPtr ptrLoadContextBinder = default(IntPtr))
        {
            return _nLoad(fileName, codeBase, assemblySecurity, locationHint, ref stackMark,
                pPrivHostBinder,
                throwOnFileNotFound, forIntrospection, true /* suppressSecurityChecks */, ptrLoadContextBinder);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsReflectionOnly(RuntimeAssembly assembly);

        public override bool ReflectionOnly
        {
            get
            {
                return IsReflectionOnly(GetNativeHandle());
            }
        }

        // Returns the module in this assembly with name 'name'

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetModule(RuntimeAssembly assembly, String name, ObjectHandleOnStack retModule);

        public override Module GetModule(String name)
        {
            Module retModule = null;
            GetModule(GetNativeHandle(), name, JitHelpers.GetObjectHandleOnStack(ref retModule));
            return retModule;
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
        public override FileStream GetFile(String name)
        {
            RuntimeModule m = (RuntimeModule)GetModule(name);
            if (m == null)
                return null;

            return new FileStream(m.GetFullyQualifiedName(),
                                  FileMode.Open,
                                  FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            Module[] m = GetModules(getResourceModules);
            FileStream[] fs = new FileStream[m.Length];

            for (int i = 0; i < fs.Length; i++)
            {
                fs[i] = new FileStream(((RuntimeModule)m[i]).GetFullyQualifiedName(),
                                       FileMode.Open,
                                       FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
            }

            return fs;
        }

        // Returns the names of all the resources
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern String[] GetManifestResourceNames(RuntimeAssembly assembly);

        // Returns the names of all the resources
        public override String[] GetManifestResourceNames()
        {
            return GetManifestResourceNames(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }

        // Returns the names of all the resources
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return GetReferencedAssemblies(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int GetManifestResourceInfo(RuntimeAssembly assembly,
                                                          String resourceName,
                                                          ObjectHandleOnStack assemblyRef,
                                                          StringHandleOnStack retFileName,
                                                          StackCrawlMarkHandle stackMark);

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            RuntimeAssembly retAssembly = null;
            String fileName = null;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            int location = GetManifestResourceInfo(GetNativeHandle(), resourceName,
                                                   JitHelpers.GetObjectHandleOnStack(ref retAssembly),
                                                   JitHelpers.GetStringHandleOnStack(ref fileName),
                                                   JitHelpers.GetStackCrawlMarkHandle(ref stackMark));

            if (location == -1)
                return null;

            return new ManifestResourceInfo(retAssembly, fileName,
                                                (ResourceLocation)location);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetLocation(RuntimeAssembly assembly, StringHandleOnStack retString);

        public override String Location
        {
            get
            {
                String location = null;

                GetLocation(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref location));

                return location;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetImageRuntimeVersion(RuntimeAssembly assembly, StringHandleOnStack retString);

        public override String ImageRuntimeVersion
        {
            get
            {
                String s = null;
                GetImageRuntimeVersion(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                return s;
            }
        }

        public override bool GlobalAssemblyCache
        {
            get
            {
                return false;
            }
        }

        public override Int64 HostContext
        {
            get
            {
                return 0;
            }
        }

        private static String VerifyCodeBase(String codebase)
        {
            if (codebase == null)
                return null;

            int len = codebase.Length;
            if (len == 0)
                return null;


            int j = codebase.IndexOf(':');
            // Check to see if the url has a prefix
            if ((j != -1) &&
                (j + 2 < len) &&
                ((codebase[j + 1] == '/') || (codebase[j + 1] == '\\')) &&
                ((codebase[j + 2] == '/') || (codebase[j + 2] == '\\')))
                return codebase;
#if !PLATFORM_UNIX
            else if ((len > 2) && (codebase[0] == '\\') && (codebase[1] == '\\'))
                return "file://" + codebase;
            else
                return "file:///" + Path.GetFullPath(codebase);
#else
            else
                return "file://" + Path.GetFullPath(codebase);
#endif // !PLATFORM_UNIX
        }

        internal Stream GetManifestResourceStream(
            Type type,
            String name,
            bool skipSecurityCheck,
            ref StackCrawlMark stackMark)
        {
            StringBuilder sb = new StringBuilder();
            if (type == null)
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(type));
            }
            else
            {
                String nameSpace = type.Namespace;
                if (nameSpace != null)
                {
                    sb.Append(nameSpace);
                    if (name != null)
                        sb.Append(Type.Delimiter);
                }
            }

            if (name != null)
                sb.Append(name);

            return GetManifestResourceStream(sb.ToString(), ref stackMark, skipSecurityCheck);
        }

        // GetResource will return a pointer to the resources in memory.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static unsafe extern byte* GetResource(RuntimeAssembly assembly,
                                                       String resourceName,
                                                       out ulong length,
                                                       StackCrawlMarkHandle stackMark,
                                                       bool skipSecurityCheck);

        internal unsafe Stream GetManifestResourceStream(String name, ref StackCrawlMark stackMark, bool skipSecurityCheck)
        {
            ulong length = 0;
            byte* pbInMemoryResource = GetResource(GetNativeHandle(), name, out length, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), skipSecurityCheck);

            if (pbInMemoryResource != null)
            {
                //Console.WriteLine("Creating an unmanaged memory stream of length "+length);
                if (length > Int64.MaxValue)
                    throw new NotImplementedException(Environment.GetResourceString("NotImplemented_ResourcesLongerThan2^63"));

                return new UnmanagedMemoryStream(pbInMemoryResource, (long)length, (long)length, FileAccess.Read);
            }

            //Console.WriteLine("GetManifestResourceStream: Blob "+name+" not found...");
            return null;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetVersion(RuntimeAssembly assembly,
                                              out int majVer,
                                              out int minVer,
                                              out int buildNum,
                                              out int revNum);

        internal Version GetVersion()
        {
            int majorVer, minorVer, build, revision;
            GetVersion(GetNativeHandle(), out majorVer, out minorVer, out build, out revision);
            return new Version(majorVer, minorVer, build, revision);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetLocale(RuntimeAssembly assembly, StringHandleOnStack retString);

        internal CultureInfo GetLocale()
        {
            String locale = null;

            GetLocale(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref locale));

            if (locale == null)
                return CultureInfo.InvariantCulture;

            return new CultureInfo(locale);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

        public override bool IsDynamic
        {
            get
            {
                return FCallIsDynamic(GetNativeHandle());
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetSimpleName(RuntimeAssembly assembly, StringHandleOnStack retSimpleName);

        internal String GetSimpleName()
        {
            string name = null;
            GetSimpleName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static AssemblyHashAlgorithm GetHashAlgorithm(RuntimeAssembly assembly);

        private AssemblyHashAlgorithm GetHashAlgorithm()
        {
            return GetHashAlgorithm(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static AssemblyNameFlags GetFlags(RuntimeAssembly assembly);

        private AssemblyNameFlags GetFlags()
        {
            return GetFlags(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetPublicKey(RuntimeAssembly assembly, ObjectHandleOnStack retPublicKey);

        internal byte[] GetPublicKey()
        {
            byte[] publicKey = null;
            GetPublicKey(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref publicKey));
            return publicKey;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsAllSecurityTransparent(RuntimeAssembly assembly);

        // Is everything introduced by this assembly transparent
        internal bool IsAllSecurityTransparent()
        {
            return IsAllSecurityTransparent(GetNativeHandle());
        }

        // This method is called by the VM.
        private RuntimeModule OnModuleResolveEvent(String moduleName)
        {
            ModuleResolveEventHandler moduleResolve = _ModuleResolve;
            if (moduleResolve == null)
                return null;

            Delegate[] ds = moduleResolve.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++)
            {
                RuntimeModule ret = (RuntimeModule)((ModuleResolveEventHandler)ds[i])(this, new ResolveEventArgs(moduleName, this));
                if (ret != null)
                    return ret;
            }

            return null;
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod  
        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, null, ref stackMark);
        }

        // Useful for binding to a very specific version of a satellite assembly
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod  
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, version, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod  
        internal Assembly InternalGetSatelliteAssembly(CultureInfo culture,
                                                       Version version,
                                                       ref StackCrawlMark stackMark)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));
            Contract.EndContractBlock();


            String name = GetSimpleName() + ".resources";
            return InternalGetSatelliteAssembly(name, culture, version, true, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod  
        internal RuntimeAssembly InternalGetSatelliteAssembly(String name,
                                                              CultureInfo culture,
                                                              Version version,
                                                              bool throwOnFileNotFound,
                                                              ref StackCrawlMark stackMark)
        {
            AssemblyName an = new AssemblyName();

            an.SetPublicKey(GetPublicKey());
            an.Flags = GetFlags() | AssemblyNameFlags.PublicKey;

            if (version == null)
                an.Version = GetVersion();
            else
                an.Version = version;

            an.CultureInfo = culture;
            an.Name = name;

            RuntimeAssembly retAssembly = nLoad(an, null, null, this, ref stackMark,
                                IntPtr.Zero,
                                throwOnFileNotFound, false);

            if (retAssembly == this || (retAssembly == null && throwOnFileNotFound))
            {
                throw new FileNotFoundException(String.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), an.Name));
            }

            return retAssembly;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetModules(RuntimeAssembly assembly,
                                              bool loadIfNotFound,
                                              bool getResourceModules,
                                              ObjectHandleOnStack retModuleHandles);

        private RuntimeModule[] GetModulesInternal(bool loadIfNotFound,
                                     bool getResourceModules)
        {
            RuntimeModule[] modules = null;
            GetModules(GetNativeHandle(), loadIfNotFound, getResourceModules, JitHelpers.GetObjectHandleOnStack(ref modules));
            return modules;
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return GetModulesInternal(true, getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModulesInternal(false, getResourceModules);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeAssembly assembly);
    }
}
