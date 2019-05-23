// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using CultureInfo = System.Globalization.CultureInfo;
using System.IO;
using System.Configuration.Assemblies;
using StackCrawlMark = System.Threading.StackCrawlMark;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Reflection
{
    internal class RuntimeAssembly : Assembly
    {
        internal RuntimeAssembly() { throw new NotSupportedException(); }

        #region private data members
        private event ModuleResolveEventHandler _ModuleResolve;
        private string? m_fullname;
        private object? m_syncRoot;   // Used to keep collectible types alive and as the syncroot for reflection.emit
#pragma warning disable 169
        private IntPtr m_assembly;    // slack for ptr datum on unmanaged side
#pragma warning restore 169

        #endregion

        internal IntPtr GetUnderlyingNativeHandle() { return m_assembly; }

        private sealed class ManifestResourceStream : UnmanagedMemoryStream
        {
            private RuntimeAssembly _manifestAssembly;

            internal unsafe ManifestResourceStream(RuntimeAssembly manifestAssembly, byte* pointer, long length, long capacity, FileAccess access) : base(pointer, length, capacity, access)
            {
                _manifestAssembly = manifestAssembly;
            }
        }

        internal object SyncRoot
        {
            get
            {
                if (m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object?>(ref m_syncRoot, new object(), null);
                }
                return m_syncRoot!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
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

        private const string s_localFilePrefix = "file:";

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetCodeBase(QCallAssembly assembly,
                                               bool copiedName,
                                               StringHandleOnStack retString);

        internal string? GetCodeBase(bool copiedName)
        {
            string? codeBase = null;
            RuntimeAssembly runtimeAssembly = this;
            GetCodeBase(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), copiedName, JitHelpers.GetStringHandleOnStack(ref codeBase));
            return codeBase;
        }

        public override string? CodeBase => GetCodeBase(false);

        internal RuntimeAssembly GetNativeHandle() => this;

        // If the assembly is copied before it is loaded, the codebase will be set to the
        // actual file loaded if copiedName is true. If it is false, then the original code base
        // is returned.
        public override AssemblyName GetName(bool copiedName)
        {
            string? codeBase = GetCodeBase(copiedName);

            var an = new AssemblyName(GetSimpleName(),
                    GetPublicKey(),
                    null, // public key token
                    GetVersion(),
                    GetLocale(),
                    GetHashAlgorithm(),
                    AssemblyVersionCompatibility.SameMachine,
                    codeBase,
                    GetFlags() | AssemblyNameFlags.PublicKey,
                    null); // strong name key pair

            Module? manifestModule = ManifestModule;
            if (manifestModule != null)
            {
                if (manifestModule.MDStreamVersion > 0x10000)
                {
                    manifestModule.GetPEKind(out PortableExecutableKinds pek, out ImageFileMachine ifm);
                    an.SetProcArchIndex(pek, ifm);
                }
            }
            return an;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetFullName(QCallAssembly assembly, StringHandleOnStack retString);

        public override string? FullName
        {
            get
            {
                // If called by Object.ToString(), return val may be NULL.
                if (m_fullname == null)
                {
                    string? s = null;
                    RuntimeAssembly runtimeAssembly = this;
                    GetFullName(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetStringHandleOnStack(ref s));
                    Interlocked.CompareExchange(ref m_fullname, s, null);
                }

                return m_fullname;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack retMethod);

        public override MethodInfo? EntryPoint
        {
            get
            {
                IRuntimeMethodInfo? methodHandle = null;
                RuntimeAssembly runtimeAssembly = this;
                GetEntryPoint(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetObjectHandleOnStack(ref methodHandle));

                if (methodHandle == null)
                    return null;

                return (MethodInfo?)RuntimeType.GetMethodBase(methodHandle);
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetType(QCallAssembly assembly,
                                            string name,
                                            bool throwOnError,
                                            bool ignoreCase,
                                            ObjectHandleOnStack type,
                                            ObjectHandleOnStack keepAlive,
                                            ObjectHandleOnStack assemblyLoadContext);

        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            // throw on null strings regardless of the value of "throwOnError"
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            RuntimeType? type = null;
            object? keepAlive = null;
            AssemblyLoadContext? assemblyLoadContextStack = AssemblyLoadContext.CurrentContextualReflectionContext;

            RuntimeAssembly runtimeAssembly = this;
            GetType(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly),
                    name,
                    throwOnError,
                    ignoreCase,
                    JitHelpers.GetObjectHandleOnStack(ref type),
                    JitHelpers.GetObjectHandleOnStack(ref keepAlive),
                    JitHelpers.GetObjectHandleOnStack(ref assemblyLoadContextStack));
            GC.KeepAlive(keepAlive);

            return type;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExportedTypes(QCallAssembly assembly, ObjectHandleOnStack retTypes);

        public override Type[] GetExportedTypes()
        {
            Type[]? types = null;
            RuntimeAssembly runtimeAssembly = this;
            GetExportedTypes(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetObjectHandleOnStack(ref types));
            return types!;
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                RuntimeModule[] modules = GetModulesInternal(true, false);
                if (modules.Length == 1)
                {
                    return modules[0].GetDefinedTypes();
                }

                List<RuntimeType> rtTypes = new List<RuntimeType>();

                for (int i = 0; i < modules.Length; i++)
                {
                    rtTypes.AddRange(modules[i].GetDefinedTypes());
                }

                return rtTypes.ToArray();
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL GetIsCollectible(QCallAssembly assembly);

        public override bool IsCollectible
        {
            get
            {
                RuntimeAssembly runtimeAssembly = this;
                return GetIsCollectible(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly)) != Interop.BOOL.FALSE;
            }
        }

        // GetResource will return a pointer to the resources in memory.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern unsafe byte* GetResource(QCallAssembly assembly,
                                                       string resourceName,
                                                       out uint length);

        // Load a resource based on the NameSpace of the type.
        public override Stream? GetManifestResourceStream(Type type, string name)
        {
            if (type == null && name == null)
                throw new ArgumentNullException(nameof(type));

            string? nameSpace = type?.Namespace;

            char c = Type.Delimiter;
            string resourceName = nameSpace != null && name != null ?
                string.Concat(nameSpace, new ReadOnlySpan<char>(ref c, 1), name) :
                string.Concat(nameSpace, name);

            return GetManifestResourceStream(resourceName);
        }

        public unsafe override Stream? GetManifestResourceStream(string name)
        {
            uint length = 0;
            RuntimeAssembly runtimeAssembly = this;
            byte* pbInMemoryResource = GetResource(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), name, out length);

            if (pbInMemoryResource != null)
            {
                return new ManifestResourceStream(this, pbInMemoryResource, length, length, FileAccess.Read);
            }

            return null;
        }

        // ISerializable implementation
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public override Module? ManifestModule
        {
            get
            {
                // We don't need to return the "external" ModuleBuilder because
                // it is meant to be read-only
                return RuntimeAssembly.GetManifestModule(GetNativeHandle());
            }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        internal static RuntimeAssembly InternalLoad(string assemblyString, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext = null)
        {
            AssemblyName an = new AssemblyName(assemblyString);

            return InternalLoadAssemblyName(an, ref stackMark, assemblyLoadContext);
        }

        internal static RuntimeAssembly InternalLoadAssemblyName(AssemblyName assemblyRef, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext = null)
        {
#if FEATURE_APPX
            if (ApplicationModel.IsUap)
            {
                if (assemblyRef.CodeBase != null)
                {
                    throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.LoadFrom"));
                }
            }
#endif

            assemblyRef = (AssemblyName)assemblyRef.Clone();
            if (assemblyRef.ProcessorArchitecture != ProcessorArchitecture.None)
            {
                // PA does not have a semantics for by-name binds for execution
                assemblyRef.ProcessorArchitecture = ProcessorArchitecture.None;
            }

            string? codeBase = VerifyCodeBase(assemblyRef.CodeBase);

            return nLoad(assemblyRef, codeBase, null, ref stackMark, true, assemblyLoadContext);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeAssembly nLoad(AssemblyName fileName,
                                                    string? codeBase,
                                                    RuntimeAssembly? assemblyContext,
                                                    ref StackCrawlMark stackMark,
                                                    bool throwOnFileNotFound,
                                                    AssemblyLoadContext? assemblyLoadContext = null);

        public override bool ReflectionOnly
        {
            get
            {
                return false;
            }
        }

        // Returns the module in this assembly with name 'name'

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetModule(QCallAssembly assembly, string name, ObjectHandleOnStack retModule);

        public override Module? GetModule(string name)
        {
            Module? retModule = null;
            RuntimeAssembly runtimeAssembly = this;
            GetModule(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), name, JitHelpers.GetObjectHandleOnStack(ref retModule));
            return retModule;
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
        public override FileStream? GetFile(string name)
        {
            RuntimeModule? m = (RuntimeModule?)GetModule(name);
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
        private static extern string[] GetManifestResourceNames(RuntimeAssembly assembly);

        // Returns the names of all the resources
        public override string[] GetManifestResourceNames()
        {
            return GetManifestResourceNames(GetNativeHandle());
        }

        // Returns the names of all the resources
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return GetReferencedAssemblies(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int GetManifestResourceInfo(QCallAssembly assembly,
                                                          string resourceName,
                                                          ObjectHandleOnStack assemblyRef,
                                                          StringHandleOnStack retFileName);

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            RuntimeAssembly? retAssembly = null;
            string? fileName = null;
            RuntimeAssembly runtimeAssembly = this;
            int location = GetManifestResourceInfo(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), resourceName,
                                                   JitHelpers.GetObjectHandleOnStack(ref retAssembly),
                                                   JitHelpers.GetStringHandleOnStack(ref fileName));

            if (location == -1)
                return null;

            return new ManifestResourceInfo(retAssembly!, fileName!,
                                                (ResourceLocation)location);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetLocation(QCallAssembly assembly, StringHandleOnStack retString);

        public override string Location
        {
            get
            {
                string? location = null;

                RuntimeAssembly runtimeAssembly = this;
                GetLocation(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetStringHandleOnStack(ref location));

                return location!;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetImageRuntimeVersion(QCallAssembly assembly, StringHandleOnStack retString);

        public override string ImageRuntimeVersion
        {
            get
            {
                string? s = null;
                RuntimeAssembly runtimeAssembly = this;
                GetImageRuntimeVersion(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetStringHandleOnStack(ref s));
                return s!;
            }
        }

        public override bool GlobalAssemblyCache
        {
            get
            {
                return false;
            }
        }

        public override long HostContext
        {
            get
            {
                return 0;
            }
        }

        private static string? VerifyCodeBase(string? codebase)
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
#if PLATFORM_WINDOWS
            else if ((len > 2) && (codebase[0] == '\\') && (codebase[1] == '\\'))
                return "file://" + codebase;
            else
                return "file:///" + Path.GetFullPath(codebase);
#else
            else
                return "file://" + Path.GetFullPath(codebase);
#endif // PLATFORM_WINDOWS
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetVersion(QCallAssembly assembly,
                                              out int majVer,
                                              out int minVer,
                                              out int buildNum,
                                              out int revNum);

        internal Version GetVersion()
        {
            int majorVer, minorVer, build, revision;
            RuntimeAssembly runtimeAssembly = this;
            GetVersion(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), out majorVer, out minorVer, out build, out revision);
            return new Version(majorVer, minorVer, build, revision);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetLocale(QCallAssembly assembly, StringHandleOnStack retString);

        internal CultureInfo GetLocale()
        {
            string? locale = null;

            RuntimeAssembly runtimeAssembly = this;
            GetLocale(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetStringHandleOnStack(ref locale));

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
        private static extern void GetSimpleName(QCallAssembly assembly, StringHandleOnStack retSimpleName);

        internal string? GetSimpleName()
        {
            RuntimeAssembly runtimeAssembly = this;
            string? name = null;
            GetSimpleName(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AssemblyHashAlgorithm GetHashAlgorithm(QCallAssembly assembly);

        private AssemblyHashAlgorithm GetHashAlgorithm()
        {
            RuntimeAssembly runtimeAssembly = this;
            return GetHashAlgorithm(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly));
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AssemblyNameFlags GetFlags(QCallAssembly assembly);

        private AssemblyNameFlags GetFlags()
        {
            RuntimeAssembly runtimeAssembly = this;
            return GetFlags(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly));
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetPublicKey(QCallAssembly assembly, ObjectHandleOnStack retPublicKey);

        internal byte[]? GetPublicKey()
        {
            byte[]? publicKey = null;
            RuntimeAssembly runtimeAssembly = this;
            GetPublicKey(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), JitHelpers.GetObjectHandleOnStack(ref publicKey));
            return publicKey;
        }

        // This method is called by the VM.
        private RuntimeModule? OnModuleResolveEvent(string moduleName)
        {
            ModuleResolveEventHandler moduleResolve = _ModuleResolve;
            if (moduleResolve == null)
                return null;

            foreach (ModuleResolveEventHandler handler in moduleResolve.GetInvocationList())
            {
                RuntimeModule ret = (RuntimeModule)handler(this, new ResolveEventArgs(moduleName, this));
                if (ret != null)
                    return ret;
            }

            return null;
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            return GetSatelliteAssembly(culture, null);
        }

        // Useful for binding to a very specific version of a satellite assembly
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));

            return InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: true)!;
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal Assembly? InternalGetSatelliteAssembly(CultureInfo culture,
                                                       Version? version,
                                                       bool throwOnFileNotFound)
        {
            AssemblyName an = new AssemblyName();

            an.SetPublicKey(GetPublicKey());
            an.Flags = GetFlags() | AssemblyNameFlags.PublicKey;

            if (version == null)
                an.Version = GetVersion();
            else
                an.Version = version;

            an.CultureInfo = culture;
            an.Name = GetSimpleName() + ".resources";

            // This stack crawl mark is never used because the requesting assembly is explicitly specified,
            // so the value could be anything.
            StackCrawlMark unused = default;
            RuntimeAssembly? retAssembly = nLoad(an, null, this, ref unused, throwOnFileNotFound);

            if (retAssembly == this)
            {
                retAssembly = null;
            }

            if (retAssembly == null && throwOnFileNotFound)
            {
                throw new FileNotFoundException(SR.Format(culture, SR.IO_FileNotFound_FileName, an.Name));
            }

            return retAssembly;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetModules(QCallAssembly assembly,
                                              bool loadIfNotFound,
                                              bool getResourceModules,
                                              ObjectHandleOnStack retModuleHandles);

        private RuntimeModule[] GetModulesInternal(bool loadIfNotFound,
                                     bool getResourceModules)
        {
            RuntimeModule[]? modules = null;
            RuntimeAssembly runtimeAssembly = this;

            GetModules(JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly), loadIfNotFound, getResourceModules, JitHelpers.GetObjectHandleOnStack(ref modules));
            return modules!;
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

        public sealed override Type[] GetForwardedTypes()
        {
            List<Type> types = new List<Type>();
            List<Exception> exceptions = new List<Exception>();

            MetadataImport scope = GetManifestModule(GetNativeHandle()).MetadataImport;
            scope.Enum(MetadataTokenType.ExportedType, 0, out MetadataEnumResult enumResult);
            RuntimeAssembly runtimeAssembly = this;
            QCallAssembly pAssembly = JitHelpers.GetQCallAssemblyOnStack(ref runtimeAssembly);
            for (int i = 0; i < enumResult.Length; i++)
            {
                MetadataToken mdtExternalType = enumResult[i];
                Type? type = null;
                Exception? exception = null;
                ObjectHandleOnStack pType = JitHelpers.GetObjectHandleOnStack(ref type);
                try
                {
                    GetForwardedType(pAssembly, mdtExternalType, pType);
                    if (type == null)
                        continue;  // mdtExternalType was not a forwarder entry.
                }
                catch (Exception e)
                {
                    type = null;
                    exception = e;
                }

                Debug.Assert((type != null) != (exception != null)); // Exactly one of these must be non-null. // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2388

                if (type != null)
                {
                    types.Add(type);
                    AddPublicNestedTypes(type, types, exceptions);
                }
                else
                {
                    exceptions.Add(exception!);
                }
            }

            if (exceptions.Count != 0)
            {
                int numTypes = types.Count;
                int numExceptions = exceptions.Count;
                types.AddRange(new Type[numExceptions]); // add one null Type for each exception.
                exceptions.InsertRange(0, new Exception[numTypes]); // align the Exceptions with the null Types.
                throw new ReflectionTypeLoadException(types.ToArray(), exceptions.ToArray());
            }

            return types.ToArray();
        }

        private static void AddPublicNestedTypes(Type type, List<Type> types, List<Exception> exceptions)
        {
            Type[] nestedTypes;
            try
            {
                nestedTypes = type.GetNestedTypes(BindingFlags.Public);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                return;
            }
            foreach (Type nestedType in nestedTypes)
            {
                types.Add(nestedType);
                AddPublicNestedTypes(nestedType, types, exceptions);
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetForwardedType(QCallAssembly assembly, MetadataToken mdtExternalType, ObjectHandleOnStack type);
    }
}
