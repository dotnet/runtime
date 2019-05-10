// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Runtime.Serialization;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        private readonly static Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();
        private readonly static List<string> s_loadFromAssemblyList = new List<string>();
        private static bool s_loadFromHandlerSet;
        private static int s_cachedSerializationSwitch;

        protected Assembly() { }

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
                        throw new NotSupportedException(SR.Format(SR.NotSupported_NoTypeInfo, types[i].FullName));

                    typeinfos[i] = typeinfo;
                }
                return typeinfos;
            }
        }

        public virtual Type[] GetTypes()
        {
            Module[] m = GetModules(false);
            if (m.Length == 1)
            {
                return m[0].GetTypes();
            }

            int finalLength = 0;
            Type[][] moduleTypes = new Type[m.Length][];

            for (int i = 0; i < moduleTypes.Length; i++)
            {
                moduleTypes[i] = m[i].GetTypes();
                finalLength += moduleTypes[i].Length;
            }

            int current = 0;
            Type[] ret = new Type[finalLength];
            for (int i = 0; i < moduleTypes.Length; i++)
            {
                int length = moduleTypes[i].Length;
                Array.Copy(moduleTypes[i], 0, ret, current, length);
                current += length;
            }

            return ret;
        }

        public virtual IEnumerable<Type> ExportedTypes => GetExportedTypes();
        public virtual Type[] GetExportedTypes() { throw NotImplemented.ByDesign; }
        public virtual Type[] GetForwardedTypes() { throw NotImplemented.ByDesign; }

        public virtual string? CodeBase { get { throw NotImplemented.ByDesign; } }
        public virtual MethodInfo? EntryPoint { get { throw NotImplemented.ByDesign; } }
        public virtual string? FullName { get { throw NotImplemented.ByDesign; } }
        public virtual string ImageRuntimeVersion { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsDynamic => false;
        public virtual string Location { get { throw NotImplemented.ByDesign; } }
        public virtual bool ReflectionOnly { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsCollectible => true;

        public virtual ManifestResourceInfo? GetManifestResourceInfo(string resourceName) { throw NotImplemented.ByDesign; }
        public virtual string[] GetManifestResourceNames() { throw NotImplemented.ByDesign; }
        public virtual Stream? GetManifestResourceStream(string name) { throw NotImplemented.ByDesign; }
        public virtual Stream? GetManifestResourceStream(Type type, string name) { throw NotImplemented.ByDesign; }

        public bool IsFullyTrusted => true;

        public virtual AssemblyName GetName() => GetName(copiedName: false);
        public virtual AssemblyName GetName(bool copiedName) { throw NotImplemented.ByDesign; }

        public virtual Type GetType(string name) => GetType(name, throwOnError: false, ignoreCase: false);
        public virtual Type GetType(string name, bool throwOnError) => GetType(name, throwOnError: throwOnError, ignoreCase: false);
        public virtual Type GetType(string name, bool throwOnError, bool ignoreCase) { throw NotImplemented.ByDesign; }

        public virtual bool IsDefined(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }

        public virtual object[] GetCustomAttributes(bool inherit) { throw NotImplemented.ByDesign; }
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }

        public virtual string EscapedCodeBase => AssemblyName.EscapeCodeBase(CodeBase);

        public object? CreateInstance(string typeName) => CreateInstance(typeName, false, BindingFlags.Public | BindingFlags.Instance, binder: null, args: null, culture: null, activationAttributes: null);
        public object? CreateInstance(string typeName, bool ignoreCase) => CreateInstance(typeName, ignoreCase, BindingFlags.Public | BindingFlags.Instance, binder: null, args: null, culture: null, activationAttributes: null);
        public virtual object? CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object[]? args, CultureInfo? culture, object[]? activationAttributes)
        {
            Type t = GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
            if (t == null)
                return null;

            return Activator.CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);
        }

        public virtual event ModuleResolveEventHandler ModuleResolve { add { throw NotImplemented.ByDesign; } remove { throw NotImplemented.ByDesign; } }

        public virtual Module? ManifestModule { get { throw NotImplemented.ByDesign; } }
        public virtual Module GetModule(string name) { throw NotImplemented.ByDesign; }

        public Module[] GetModules() => GetModules(getResourceModules: false);
        public virtual Module[] GetModules(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual IEnumerable<Module> Modules => GetLoadedModules(getResourceModules: true);
        public Module[] GetLoadedModules() => GetLoadedModules(getResourceModules: false);
        public virtual Module[] GetLoadedModules(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual AssemblyName[] GetReferencedAssemblies() { throw NotImplemented.ByDesign; }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture) { throw NotImplemented.ByDesign; }
        public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version? version) { throw NotImplemented.ByDesign; }

        public virtual FileStream? GetFile(string name) { throw NotImplemented.ByDesign; }
        public virtual FileStream[] GetFiles() => GetFiles(getResourceModules: false);
        public virtual FileStream[] GetFiles(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { throw NotImplemented.ByDesign; }

        public override string ToString()
        {
            return FullName ?? base.ToString()!;
        }

        /*
          Returns true if the assembly was loaded from the global assembly cache.
        */
        public virtual bool GlobalAssemblyCache { get { throw NotImplemented.ByDesign; } }
        public virtual long HostContext { get { throw NotImplemented.ByDesign; } }

        public override bool Equals(object? o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Assembly? left, Assembly? right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if ((object?)left == (object)right)
            {
                return true;
            }

            return (left is null) ? false : left.Equals(right);
        }

        public static bool operator !=(Assembly? left, Assembly? right)
        {
            return !(left == right);
        }

        public static string CreateQualifiedName(string? assemblyName, string? typeName) => typeName + ", " + assemblyName;

        public static Assembly? GetAssembly(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Module m = type.Module;
            if (m == null)
                return null;
            else
                return m.Assembly;
        }

        public static Assembly Load(byte[] rawAssembly) => Load(rawAssembly, rawSymbolStore: null);

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into a fully isolated ALC with resolution fully deferred to the AssemblyLoadContext.Default.
        // The second parameter is the raw bytes representing the symbol store that matches the assembly.
        public static Assembly Load(byte[] rawAssembly, byte[]? rawSymbolStore)
        {
            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));

            if (rawAssembly.Length == 0)
                throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.Load(byte[], ...)"));
#endif

            SerializationInfo.ThrowIfDeserializationInProgress("AllowAssembliesFromByteArrays",
                ref s_cachedSerializationSwitch);

            AssemblyLoadContext alc = new IndividualAssemblyLoadContext("Assembly.Load(byte[], ...)");
            return alc.InternalLoad(rawAssembly, rawSymbolStore);
        }

        public static Assembly LoadFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.LoadFile"));
#endif

            if (PathInternal.IsPartiallyQualified(path))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(path));
            }

            string normalizedPath = Path.GetFullPath(path);

            Assembly result;
            lock (s_loadfile)
            {
                if (s_loadfile.TryGetValue(normalizedPath, out result))
                    return result;

                AssemblyLoadContext alc = new IndividualAssemblyLoadContext(string.Format("Assembly.LoadFile({0})", normalizedPath));
                result = alc.LoadFromAssemblyPath(normalizedPath);
                s_loadfile.Add(normalizedPath, result);
            }
            return result;
        }

        private static Assembly? LoadFromResolveHandler(object? sender, ResolveEventArgs args)
        {
            Assembly? requestingAssembly = args.RequestingAssembly;
            if (requestingAssembly == null)
            {
                return null;
            }

            // Requesting assembly for LoadFrom is always loaded in defaultContext - proceed only if that
            // is the case.
            if (AssemblyLoadContext.Default != AssemblyLoadContext.GetLoadContext(requestingAssembly))
                return null;

            // Get the path where requesting assembly lives and check if it is in the list
            // of assemblies for which LoadFrom was invoked.
            string requestorPath = Path.GetFullPath(requestingAssembly.Location);
            if (string.IsNullOrEmpty(requestorPath))
                return null;

            lock (s_loadFromAssemblyList)
            {
                // If the requestor assembly was not loaded using LoadFrom, exit.
                if (!s_loadFromAssemblyList.Contains(requestorPath))
                    return null;
            }

            // Requestor assembly was loaded using loadFrom, so look for its dependencies
            // in the same folder as it.
            // Form the name of the assembly using the path of the assembly that requested its load.
            AssemblyName requestedAssemblyName = new AssemblyName(args.Name!);
            string requestedAssemblyPath = Path.Combine(Path.GetDirectoryName(requestorPath)!, requestedAssemblyName.Name + ".dll");

            try
            {
                // Load the dependency via LoadFrom so that it goes through the same path of being in the LoadFrom list.
                return Assembly.LoadFrom(requestedAssemblyPath);
            }
            catch (FileNotFoundException)
            {
                // Catch FileNotFoundException when attempting to resolve assemblies via this handler to account for missing assemblies.
                return null;
            }
        }

        public static Assembly LoadFrom(string assemblyFile)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));
            
            string fullPath = Path.GetFullPath(assemblyFile);

            if (!s_loadFromHandlerSet)
            {
                lock (s_loadFromAssemblyList)
                {
                    if (!s_loadFromHandlerSet)
                    {
                        AssemblyLoadContext.AssemblyResolve += LoadFromResolveHandler!;
                        s_loadFromHandlerSet = true;
                    }
                }
            }

            // Add the path to the LoadFrom path list which we will consult
            // before handling the resolves in our handler.
            lock(s_loadFromAssemblyList)
            {
                if (!s_loadFromAssemblyList.Contains(fullPath))
                {
                    s_loadFromAssemblyList.Add(fullPath);
                }
            }

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
        }

        public static Assembly LoadFrom(string? assemblyFile, byte[]? hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_AssemblyLoadFromHash);
        }

        public static Assembly UnsafeLoadFrom(string assemblyFile) => LoadFrom(assemblyFile);

        public Module LoadModule(string? moduleName, byte[]? rawModule) => LoadModule(moduleName, rawModule, null);
        public virtual Module LoadModule(string? moduleName, byte[]? rawModule, byte[]? rawSymbolStore) { throw NotImplemented.ByDesign; }

        public static Assembly ReflectionOnlyLoad(byte[]? rawAssembly) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }
        public static Assembly ReflectionOnlyLoad(string? assemblyString) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }
        public static Assembly ReflectionOnlyLoadFrom(string? assemblyFile) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }

        public virtual SecurityRuleSet SecurityRuleSet => SecurityRuleSet.None;
    }
}
