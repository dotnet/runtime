// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
#pragma warning disable 67 // events are declared but not used
        private event ModuleResolveEventHandler? _ModuleResolve;
#pragma warning restore 67
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
                return m_syncRoot;
            }
        }

        public override event ModuleResolveEventHandler? ModuleResolve
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool GetCodeBase(QCallAssembly assembly,
                                               StringHandleOnStack retString);

        internal string? GetCodeBase()
        {
            string? codeBase = null;
            RuntimeAssembly runtimeAssembly = this;
            if (GetCodeBase(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref codeBase)))
            {
                return codeBase;
            }
            return null;
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override string? CodeBase
        {
            get
            {
                var codeBase = GetCodeBase();
                if (codeBase is null)
                {
                    // Not supported if the assembly was loaded from memory
                    throw new NotSupportedException(SR.NotSupported_CodeBase);
                }
                return codeBase;
            }
        }

        internal RuntimeAssembly GetNativeHandle() => this;

        // If the assembly is copied before it is loaded, the codebase will be set to the
        // actual file loaded if copiedName is true. If it is false, then the original code base
        // is returned.
        public override AssemblyName GetName(bool copiedName)
        {
            string? codeBase = GetCodeBase();

            var an = new AssemblyName(GetSimpleName(),
                    GetPublicKey(),
                    null, // public key token
                    GetVersion(),
                    GetLocale(),
                    GetHashAlgorithm(),
                    AssemblyVersionCompatibility.SameMachine,
                    codeBase,
                    GetFlags() | AssemblyNameFlags.PublicKey);

            Module manifestModule = ManifestModule;
            if (manifestModule.MDStreamVersion > 0x10000)
            {
                manifestModule.GetPEKind(out PortableExecutableKinds pek, out ImageFileMachine ifm);
                an.SetProcArchIndex(pek, ifm);
            }
            return an;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
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
                    GetFullName(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref s));
                    Interlocked.CompareExchange(ref m_fullname, s, null);
                }

                return m_fullname;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack retMethod);

        public override MethodInfo? EntryPoint
        {
            get
            {
                IRuntimeMethodInfo? methodHandle = null;
                RuntimeAssembly runtimeAssembly = this;
                GetEntryPoint(new QCallAssembly(ref runtimeAssembly), ObjectHandleOnStack.Create(ref methodHandle));

                if (methodHandle == null)
                    return null;

                return (MethodInfo?)RuntimeType.GetMethodBase(methodHandle);
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetType(QCallAssembly assembly,
                                            string name,
                                            bool throwOnError,
                                            bool ignoreCase,
                                            ObjectHandleOnStack type,
                                            ObjectHandleOnStack keepAlive,
                                            ObjectHandleOnStack assemblyLoadContext);

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            // throw on null strings regardless of the value of "throwOnError"
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            RuntimeType? type = null;
            object? keepAlive = null;
            AssemblyLoadContext? assemblyLoadContextStack = AssemblyLoadContext.CurrentContextualReflectionContext;

            RuntimeAssembly runtimeAssembly = this;
            GetType(new QCallAssembly(ref runtimeAssembly),
                    name,
                    throwOnError,
                    ignoreCase,
                    ObjectHandleOnStack.Create(ref type),
                    ObjectHandleOnStack.Create(ref keepAlive),
                    ObjectHandleOnStack.Create(ref assemblyLoadContextStack));
            GC.KeepAlive(keepAlive);

            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExportedTypes(QCallAssembly assembly, ObjectHandleOnStack retTypes);

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetExportedTypes()
        {
            Type[]? types = null;
            RuntimeAssembly runtimeAssembly = this;
            GetExportedTypes(new QCallAssembly(ref runtimeAssembly), ObjectHandleOnStack.Create(ref types));
            return types!;
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            [RequiresUnreferencedCode("Types might be removed")]
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL GetIsCollectible(QCallAssembly assembly);

        public override bool IsCollectible
        {
            get
            {
                RuntimeAssembly runtimeAssembly = this;
                return GetIsCollectible(new QCallAssembly(ref runtimeAssembly)) != Interop.BOOL.FALSE;
            }
        }

        // GetResource will return a pointer to the resources in memory.
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
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

        public override unsafe Stream? GetManifestResourceStream(string name)
        {
            RuntimeAssembly runtimeAssembly = this;
            byte* pbInMemoryResource = GetResource(new QCallAssembly(ref runtimeAssembly), name, out uint length);

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

        public override Module ManifestModule =>
            // We don't need to return the "external" ModuleBuilder because
            // it is meant to be read-only
            RuntimeAssembly.GetManifestModule(GetNativeHandle());

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
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        internal static RuntimeAssembly InternalLoad(string assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext = null)
            => InternalLoad(new AssemblyName(assemblyName), ref stackMark, assemblyLoadContext);

        internal static RuntimeAssembly InternalLoad(AssemblyName assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext = null)
            => InternalLoad(assemblyName, requestingAssembly: null, ref stackMark, throwOnFileNotFound: true, assemblyLoadContext);

        internal static RuntimeAssembly InternalLoad(AssemblyName assemblyName,
                                                     RuntimeAssembly? requestingAssembly,
                                                     ref StackCrawlMark stackMark,
                                                     bool throwOnFileNotFound,
                                                     AssemblyLoadContext? assemblyLoadContext = null)
        {
            RuntimeAssembly? retAssembly = null;
            InternalLoad(ObjectHandleOnStack.Create(ref assemblyName),
                         ObjectHandleOnStack.Create(ref requestingAssembly),
                         new StackCrawlMarkHandle(ref stackMark),
                         throwOnFileNotFound,
                         ObjectHandleOnStack.Create(ref assemblyLoadContext),
                         ObjectHandleOnStack.Create(ref retAssembly));
            return retAssembly!;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InternalLoad(ObjectHandleOnStack assemblyName,
                                                ObjectHandleOnStack requestingAssembly,
                                                StackCrawlMarkHandle stackMark,
                                                bool throwOnFileNotFound,
                                                ObjectHandleOnStack assemblyLoadContext,
                                                ObjectHandleOnStack retAssembly);

        public override bool ReflectionOnly => false;

        // Returns the module in this assembly with name 'name'

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetModule(QCallAssembly assembly, string name, ObjectHandleOnStack retModule);

        public override Module? GetModule(string name)
        {
            Module? retModule = null;
            RuntimeAssembly runtimeAssembly = this;
            GetModule(new QCallAssembly(ref runtimeAssembly), name, ObjectHandleOnStack.Create(ref retModule));
            return retModule;
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override FileStream? GetFile(string name)
        {
            if (Location.Length == 0)
            {
                // Throw if the assembly was loaded from memory, indicated by Location returning an empty string
                throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
            }

            RuntimeModule? m = (RuntimeModule?)GetModule(name);
            if (m == null)
                return null;

            return new FileStream(m.GetFullyQualifiedName(),
                                  FileMode.Open,
                                  FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override FileStream[] GetFiles(bool getResourceModules)
        {
            if (Location.Length == 0)
            {
                // Throw if the assembly was loaded from memory, indicated by Location returning an empty string
                throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
            }

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string[] GetManifestResourceNames(RuntimeAssembly assembly);

        // Returns the names of all the resources
        public override string[] GetManifestResourceNames()
        {
            return GetManifestResourceNames(GetNativeHandle());
        }

        // Returns the names of all the resources
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies()
        {
            return GetReferencedAssemblies(GetNativeHandle());
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int GetManifestResourceInfo(QCallAssembly assembly,
                                                          string resourceName,
                                                          ObjectHandleOnStack assemblyRef,
                                                          StringHandleOnStack retFileName);

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            RuntimeAssembly? retAssembly = null;
            string? fileName = null;
            RuntimeAssembly runtimeAssembly = this;
            int location = GetManifestResourceInfo(new QCallAssembly(ref runtimeAssembly), resourceName,
                                                   ObjectHandleOnStack.Create(ref retAssembly),
                                                   new StringHandleOnStack(ref fileName));

            if (location == -1)
                return null;

            return new ManifestResourceInfo(retAssembly!, fileName!,
                                                (ResourceLocation)location);
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetLocation(QCallAssembly assembly, StringHandleOnStack retString);

        public override string Location
        {
            get
            {
                string? location = null;

                RuntimeAssembly runtimeAssembly = this;
                GetLocation(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref location));

                return location!;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetImageRuntimeVersion(QCallAssembly assembly, StringHandleOnStack retString);

        public override string ImageRuntimeVersion
        {
            get
            {
                string? s = null;
                RuntimeAssembly runtimeAssembly = this;
                GetImageRuntimeVersion(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref s));
                return s!;
            }
        }

        [Obsolete(Obsoletions.GlobalAssemblyCacheMessage, DiagnosticId = Obsoletions.GlobalAssemblyCacheDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override bool GlobalAssemblyCache => false;

        public override long HostContext => 0;

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetVersion(QCallAssembly assembly,
                                              out int majVer,
                                              out int minVer,
                                              out int buildNum,
                                              out int revNum);

        internal Version GetVersion()
        {
            RuntimeAssembly runtimeAssembly = this;
            GetVersion(new QCallAssembly(ref runtimeAssembly), out int majorVer, out int minorVer, out int build, out int revision);
            return new Version(majorVer, minorVer, build, revision);
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetLocale(QCallAssembly assembly, StringHandleOnStack retString);

        internal CultureInfo GetLocale()
        {
            string? locale = null;

            RuntimeAssembly runtimeAssembly = this;
            GetLocale(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref locale));

            if (locale == null)
                return CultureInfo.InvariantCulture;

            return CultureInfo.GetCultureInfo(locale);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

        public override bool IsDynamic => FCallIsDynamic(GetNativeHandle());

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetSimpleName(QCallAssembly assembly, StringHandleOnStack retSimpleName);

        internal string? GetSimpleName()
        {
            RuntimeAssembly runtimeAssembly = this;
            string? name = null;
            GetSimpleName(new QCallAssembly(ref runtimeAssembly), new StringHandleOnStack(ref name));
            return name;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AssemblyHashAlgorithm GetHashAlgorithm(QCallAssembly assembly);

        private AssemblyHashAlgorithm GetHashAlgorithm()
        {
            RuntimeAssembly runtimeAssembly = this;
            return GetHashAlgorithm(new QCallAssembly(ref runtimeAssembly));
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AssemblyNameFlags GetFlags(QCallAssembly assembly);

        private AssemblyNameFlags GetFlags()
        {
            RuntimeAssembly runtimeAssembly = this;
            return GetFlags(new QCallAssembly(ref runtimeAssembly));
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetPublicKey(QCallAssembly assembly, ObjectHandleOnStack retPublicKey);

        internal byte[]? GetPublicKey()
        {
            byte[]? publicKey = null;
            RuntimeAssembly runtimeAssembly = this;
            GetPublicKey(new QCallAssembly(ref runtimeAssembly), ObjectHandleOnStack.Create(ref publicKey));
            return publicKey;
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
            var an = new AssemblyName();
            an.SetPublicKey(GetPublicKey());
            an.Flags = GetFlags() | AssemblyNameFlags.PublicKey;
            an.Version = version ?? GetVersion();
            an.CultureInfo = culture;
            an.Name = GetSimpleName() + ".resources";

            // This stack crawl mark is never used because the requesting assembly is explicitly specified,
            // so the value could be anything.
            StackCrawlMark unused = default;
            RuntimeAssembly? retAssembly = InternalLoad(an, this, ref unused, throwOnFileNotFound);

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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetModules(QCallAssembly assembly,
                                              bool loadIfNotFound,
                                              bool getResourceModules,
                                              ObjectHandleOnStack retModuleHandles);

        private RuntimeModule[] GetModulesInternal(bool loadIfNotFound,
                                     bool getResourceModules)
        {
            RuntimeModule[]? modules = null;
            RuntimeAssembly runtimeAssembly = this;

            GetModules(new QCallAssembly(ref runtimeAssembly), loadIfNotFound, getResourceModules, ObjectHandleOnStack.Create(ref modules));
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeAssembly assembly);

        [RequiresUnreferencedCode("Types might be removed")]
        public sealed override Type[] GetForwardedTypes()
        {
            List<Type> types = new List<Type>();
            List<Exception> exceptions = new List<Exception>();

            MetadataImport scope = GetManifestModule(GetNativeHandle()).MetadataImport;
            scope.Enum(MetadataTokenType.ExportedType, 0, out MetadataEnumResult enumResult);
            RuntimeAssembly runtimeAssembly = this;
            QCallAssembly pAssembly = new QCallAssembly(ref runtimeAssembly);
            for (int i = 0; i < enumResult.Length; i++)
            {
                MetadataToken mdtExternalType = enumResult[i];
                Type? type = null;
                Exception? exception = null;
                ObjectHandleOnStack pType = ObjectHandleOnStack.Create(ref type);
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

                Debug.Assert((type != null) != (exception != null)); // Exactly one of these must be non-null.

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

        [RequiresUnreferencedCode("Types might be removed because recursive nested types can't currently be annotated for dynamic access.")]
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetForwardedType(QCallAssembly assembly, MetadataToken mdtExternalType, ObjectHandleOnStack type);
    }
}
