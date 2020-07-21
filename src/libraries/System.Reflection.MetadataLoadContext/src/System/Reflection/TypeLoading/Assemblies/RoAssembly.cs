// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoAssembly : LeveledAssembly
    {
        private readonly RoModule?[] _loadedModules; // Any loaded modules indexed by [rid - 1]. Does NOT include the manifest module.

        protected RoAssembly(MetadataLoadContext loader, int assemblyFileCount)
            : base()
        {
            Loader = loader;
            IsSingleModule = (assemblyFileCount == 0);
            _loadedModules = (assemblyFileCount == 0) ? Array.Empty<RoModule>() : new RoModule[assemblyFileCount];
        }

        public sealed override Module ManifestModule => GetRoManifestModule();
        internal abstract RoModule GetRoManifestModule();
        protected bool IsSingleModule { get; }

        public sealed override string ToString() => Loader.GetDisposedString() ?? base.ToString();

        // Naming
        public sealed override AssemblyName GetName(bool copiedName) => GetAssemblyNameDataNoCopy().CreateAssemblyName();
        internal AssemblyNameData GetAssemblyNameDataNoCopy() => _lazyAssemblyNameData ?? (_lazyAssemblyNameData = ComputeNameData());
        protected abstract AssemblyNameData ComputeNameData();
        private volatile AssemblyNameData? _lazyAssemblyNameData;

        public sealed override string FullName => _lazyFullName ?? (_lazyFullName = GetName().FullName);
        private volatile string? _lazyFullName;

        // Location and codebase
        public abstract override string Location { get; }
        public sealed override string CodeBase => throw new NotSupportedException(SR.NotSupported_AssemblyCodeBase);
        public sealed override string EscapedCodeBase => throw new NotSupportedException(SR.NotSupported_AssemblyCodeBase);

        // Custom Attributes
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }

        // Apis to retrieved types physically defined in this module.
        public sealed override Type[] GetTypes() => IsSingleModule ? ManifestModule.GetTypes() : base.GetTypes();
        public sealed override IEnumerable<TypeInfo> DefinedTypes => GetDefinedRoTypes()!;

        private IEnumerable<RoType>? GetDefinedRoTypes() => IsSingleModule ? GetRoManifestModule().GetDefinedRoTypes() : MultiModuleGetDefinedRoTypes();
        private IEnumerable<RoType> MultiModuleGetDefinedRoTypes()
        {
            foreach (RoModule module in ComputeRoModules(getResourceModules: false))
            {
                foreach (RoType t in module.GetDefinedRoTypes()!)
                {
                    yield return t;
                }
            }
        }

        // Apis to retrieve public types physically defined in this module.
        public sealed override Type[] GetExportedTypes()
        {
            // todo: use IEnumerable<T> extension instead: ExportedTypes.ToArray();
            List<Type> list = new List<Type>(ExportedTypes);
            return list.ToArray();
        }

        public sealed override IEnumerable<Type> ExportedTypes
        {
            get
            {
                foreach (RoType type in GetDefinedRoTypes()!)
                {
                    if (type.IsVisibleOutsideAssembly())
                        yield return type;
                }
            }
        }

        // Api to retrieve types by name. Retrieves both types physically defined in this module and types this assembly forwards from another assembly.
        public sealed override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // Known compat disagreement: This api is supposed to throw an ArgumentException if the name has an assembly qualification
            // (though the intended meaning seems clear.) This is difficult for us to implement as we don't have our own type name parser.
            // (We can't just throw in the assemblyResolve delegate because assembly qualifications are permitted inside generic arguments,
            // just not in the top level type name.) In the bigger scheme of things, this does not seem worth worrying about.

            return Helpers.LoadTypeFromAssemblyQualifiedName(name, defaultAssembly: this, ignoreCase: ignoreCase, throwOnError: throwOnError);
        }

        /// <summary>
        /// Helper routine for the more general Assembly.GetType() family of apis. Also used in typeRef resolution.
        ///
        /// Resolves top-level named types only. No nested types. No constructed types. The input name must not be escaped.
        ///
        /// If a type is not contained or forwarded from the assembly, this method returns null (does not throw.)
        /// This supports the "throwOnError: false" behavior of Assembly.GetType(string, bool).
        /// </summary>
        internal RoDefinitionType? GetTypeCore(string ns, string name, bool ignoreCase, out Exception? e) => GetTypeCore(ns.ToUtf8(), name.ToUtf8(), ignoreCase, out e);
        internal RoDefinitionType? GetTypeCore(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, bool ignoreCase, out Exception? e)
        {
            RoDefinitionType? result = GetRoManifestModule().GetTypeCore(ns, name, ignoreCase, out e);
            if (IsSingleModule || result != null)
                return result;

            foreach (RoModule module in ComputeRoModules(getResourceModules: false))
            {
                if (module == ManifestModule)
                    continue;

                result = module.GetTypeCore(ns, name, ignoreCase, out e);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Assembly dependencies
        public sealed override AssemblyName[] GetReferencedAssemblies()
        {
            // For compat, this api only searches the manifest module. Tools normally ensure the manifest module's assemblyRef
            // table represents the union of all module's assemblyRef table.
            AssemblyNameData[] data = GetReferencedAssembliesNoCopy();
            AssemblyName[] result = new AssemblyName[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = data[i].CreateAssemblyName();
            }
            return result;
        }

        private AssemblyNameData[] GetReferencedAssembliesNoCopy() => _lazyAssemblyReferences ?? (_lazyAssemblyReferences = ComputeAssemblyReferences());
        protected abstract AssemblyNameData[] ComputeAssemblyReferences();
        private volatile AssemblyNameData[]? _lazyAssemblyReferences;

        // Miscellaneous properties
        public sealed override bool ReflectionOnly => true;
#if NET50_OBSOLETIONS
        [Obsolete("The Global Assembly Cache is not supported.", DiagnosticId = "SYSLIB0005", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        public sealed override bool GlobalAssemblyCache => false;
        public sealed override long HostContext => 0;
        public abstract override string ImageRuntimeVersion { get; }
        public abstract override bool IsDynamic { get; }
        public abstract override MethodInfo? EntryPoint { get; }

        // Manifest resource support.
        public abstract override ManifestResourceInfo GetManifestResourceInfo(string resourceName);
        public abstract override string[] GetManifestResourceNames();
        public abstract override Stream? GetManifestResourceStream(string name);
        public sealed override Stream? GetManifestResourceStream(Type type, string name)
        {
            StringBuilder sb = new StringBuilder();
            if (type == null)
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(type));
            }
            else
            {
                string? ns = type.Namespace;
                if (ns != null)
                {
                    sb.Append(ns);
                    if (name != null)
                        sb.Append(Type.Delimiter);
                }
            }

            if (name != null)
                sb.Append(name);

            return GetManifestResourceStream(sb.ToString());
        }

        // Serialization
        public sealed override void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotSupportedException();

        // Satellite assemblies
        public sealed override Assembly GetSatelliteAssembly(CultureInfo culture) => throw new NotSupportedException(SR.NotSupported_SatelliteAssembly);
        public sealed override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version) => throw new NotSupportedException(SR.NotSupported_SatelliteAssembly);

        // Operations that are invalid for ReflectionOnly objects.
        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyCA);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyCA);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyCA);
        // Compat quirk: Why ArgumentException instead of InvalidOperationException?
        public sealed override object CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes) => throw new ArgumentException(SR.Arg_ReflectionOnlyInvoke);

        internal MetadataLoadContext Loader { get; }
    }
}
