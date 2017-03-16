// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using CultureInfo = System.Globalization.CultureInfo;
using System.Security;
using System.Security.Policy;
using System.IO;
using System.Configuration.Assemblies;
using StackCrawlMark = System.Threading.StackCrawlMark;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;
using System.Runtime.Loader;

namespace System.Reflection
{
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
}
