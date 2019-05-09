// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// For each dynamic assembly there will be two AssemblyBuilder objects: the "internal" 
// AssemblyBuilder object and the "external" AssemblyBuilder object.
//  1.  The "internal" object is the real assembly object that the VM creates and knows about. However, 
//      you can perform RefEmit operations on it only if you have its granted permission. From the AppDomain 
//      and other "internal" objects like the "internal" ModuleBuilders and runtime types, you can only
//      get the "internal" objects. This is to prevent low-trust code from getting a hold of the dynamic
//      AssemblyBuilder/ModuleBuilder/TypeBuilder/MethodBuilder/etc other people have created by simply 
//      enumerating the AppDomain and inject code in it.
//  2.  The "external" object is merely an wrapper of the "internal" object and all operations on it
//      are directed to the internal object. This is the one you get by calling DefineDynamicAssembly
//      on AppDomain and the one you can always perform RefEmit operations on. You can get other "external"
//      objects from the "external" AssemblyBuilder, ModuleBuilder, TypeBuilder, MethodBuilder, etc. Note
//      that VM doesn't know about this object. So every time we call into the VM we need to pass in the
//      "internal" object.
//
// "internal" and "external" ModuleBuilders are similar

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace System.Reflection.Emit
{
    // When the user calls AppDomain.DefineDynamicAssembly the loader creates a new InternalAssemblyBuilder. 
    // This InternalAssemblyBuilder can be retrieved via a call to Assembly.GetAssemblies() by untrusted code.
    // In the past, when InternalAssemblyBuilder was AssemblyBuilder, the untrusted user could down cast the
    // Assembly to an AssemblyBuilder and emit code with the elevated permissions of the trusted code which 
    // originally created the AssemblyBuilder via DefineDynamicAssembly. Today, this can no longer happen
    // because the Assembly returned via AssemblyGetAssemblies() will be an InternalAssemblyBuilder.

    // Only the caller of DefineDynamicAssembly will get an AssemblyBuilder. 
    // There is a 1-1 relationship between InternalAssemblyBuilder and AssemblyBuilder. 
    // AssemblyBuilder is composed of its InternalAssemblyBuilder.
    // The AssemblyBuilder data members (e.g. m_foo) were changed to properties which then delegate 
    // the access to the composed InternalAssemblyBuilder. This way, AssemblyBuilder simply wraps 
    // InternalAssemblyBuilder and still operates on InternalAssemblyBuilder members. 
    // This also makes the change transparent to the loader. This is good because most of the complexity
    // of Assembly building is in the loader code so not touching that code reduces the chance of 
    // introducing new bugs.
    internal sealed class InternalAssemblyBuilder : RuntimeAssembly
    {
        private InternalAssemblyBuilder() { }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is InternalAssemblyBuilder)
            {
                return (object)this == obj;
            }

            return obj.Equals(this);
        }

        public override int GetHashCode() => base.GetHashCode();

        // Assembly methods that are overridden by AssemblyBuilder should be overridden by InternalAssemblyBuilder too
        #region Methods inherited from Assembly

        public override string[] GetManifestResourceNames()
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override FileStream GetFile(string name)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override Stream? GetManifestResourceStream(Type type, string name)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override Stream? GetManifestResourceStream(string name)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override string Location
        {
            get => throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override string? CodeBase
        {
            get => throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override Type[] GetExportedTypes()
        {
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        }

        public override string ImageRuntimeVersion => Assembly.GetExecutingAssembly().ImageRuntimeVersion;

        #endregion
    }
    
    public sealed class AssemblyBuilder : Assembly
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);

        #region Internal Data Members

        // This is only valid in the "external" AssemblyBuilder
        internal AssemblyBuilderData _assemblyData;
        private readonly InternalAssemblyBuilder _internalAssemblyBuilder;
        private ModuleBuilder _manifestModuleBuilder = null!;
        // Set to true if the manifest module was returned by code:DefineDynamicModule to the user
        private bool _isManifestModuleUsedAsDefinedModule;

        private const string ManifestModuleName = "RefEmit_InMemoryManifestModule";

        internal ModuleBuilder GetModuleBuilder(InternalModuleBuilder module)
        {
            Debug.Assert(module != null);
            Debug.Assert(InternalAssembly == module.Assembly);

            lock (SyncRoot)
            {
                // in CoreCLR there is only one module in each dynamic assembly, the manifest module
                if (_manifestModuleBuilder.InternalModule == module)
                {
                    return _manifestModuleBuilder;
                }

                throw new ArgumentException(null, nameof(module));
            }
        }

        internal object SyncRoot => InternalAssembly.SyncRoot;

        internal InternalAssemblyBuilder InternalAssembly => _internalAssemblyBuilder;

        internal RuntimeAssembly GetNativeHandle() => InternalAssembly.GetNativeHandle();

        #endregion

        #region Constructor

        internal AssemblyBuilder(AssemblyName name,
                                 AssemblyBuilderAccess access,
                                 ref StackCrawlMark stackMark,
                                 IEnumerable<CustomAttributeBuilder>? unsafeAssemblyAttributes)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (access != AssemblyBuilderAccess.Run && access != AssemblyBuilderAccess.RunAndCollect)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)access), nameof(access));
            }

            // Clone the name in case the caller modifies it underneath us.
            name = (AssemblyName)name.Clone();

            // Scan the assembly level attributes for any attributes which modify how we create the
            // assembly. Currently, we look for any attribute which modifies the security transparency
            // of the assembly.
            List<CustomAttributeBuilder>? assemblyAttributes = null;
            if (unsafeAssemblyAttributes != null)
            {
                // Create a copy to ensure that it cannot be modified from another thread
                // as it is used further below.
                assemblyAttributes = new List<CustomAttributeBuilder>(unsafeAssemblyAttributes);
            }

            _internalAssemblyBuilder = (InternalAssemblyBuilder)nCreateDynamicAssembly(name,
                                                                                       ref stackMark,
                                                                                       access);

            _assemblyData = new AssemblyBuilderData(_internalAssemblyBuilder, access);

            // Make sure that ManifestModule is properly initialized
            // We need to do this before setting any CustomAttribute
            InitManifestModule();

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        private void InitManifestModule()
        {
            InternalModuleBuilder modBuilder = (InternalModuleBuilder)GetInMemoryAssemblyModule(GetNativeHandle());

            // Note that this ModuleBuilder cannot be used for RefEmit yet
            // because it hasn't been initialized.
            // However, it can be used to set the custom attribute on the Assembly
            _manifestModuleBuilder = new ModuleBuilder(this, modBuilder);

            // We are only setting the name in the managed ModuleBuilderData here.
            // The name in the underlying metadata will be set when the
            // manifest module is created during nCreateDynamicAssembly.

            // This name needs to stay in sync with that used in
            // Assembly::Init to call ReflectionModule::Create (in VM)
            _manifestModuleBuilder.Init(ManifestModuleName);

            _isManifestModuleUsedAsDefinedModule = false;
        }

        #endregion

        #region DefineDynamicAssembly

        /// <summary>
        /// If an AssemblyName has a public key specified, the assembly is assumed
        /// to have a strong name and a hash will be computed when the assembly
        /// is saved.
        /// </summary>
        [DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod.
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, ref stackMark, null);
        }
        
        [DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod.
        public static AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, ref stackMark, assemblyAttributes);
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Assembly nCreateDynamicAssembly(AssemblyName name,
                                                              ref StackCrawlMark stackMark,
                                                              AssemblyBuilderAccess access);

        private class AssemblyBuilderLock { }

        internal static AssemblyBuilder InternalDefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            ref StackCrawlMark stackMark,
            IEnumerable<CustomAttributeBuilder>? unsafeAssemblyAttributes)
        {
            lock (typeof(AssemblyBuilderLock))
            {
                // We can only create dynamic assemblies in the current domain
                return new AssemblyBuilder(name,
                                           access,
                                           ref stackMark,
                                           unsafeAssemblyAttributes);
            }
        }
        #endregion

        #region DefineDynamicModule

        /// <summary>
        /// Defines a named dynamic module. It is an error to define multiple 
        /// modules within an Assembly with the same name. This dynamic module is
        /// a transient module.
        /// </summary>
        [DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod.
        public ModuleBuilder DefineDynamicModule(string name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, false, ref stackMark);
        }

        [DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod.
        public ModuleBuilder DefineDynamicModule(string name, bool emitSymbolInfo)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, emitSymbolInfo, ref stackMark);
        }

        private ModuleBuilder DefineDynamicModuleInternal(
            string name,
            bool emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)
        {
            lock (SyncRoot)
            {
                return DefineDynamicModuleInternalNoLock(name, emitSymbolInfo, ref stackMark);
            }
        }

        private ModuleBuilder DefineDynamicModuleInternalNoLock(
            string name,
            bool emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }
            if (name[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_InvalidName, nameof(name));
            }

            // Create the dynamic module- only one ModuleBuilder per AssemblyBuilder can be created.
            if (_isManifestModuleUsedAsDefinedModule)
            {
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            }

            Debug.Assert(_assemblyData != null, "_assemblyData is null in DefineDynamicModuleInternal");

            // Init(...) has already been called on _manifestModuleBuilder in InitManifestModule()
            ModuleBuilder dynModule = _manifestModuleBuilder;

            // Create the symbol writer
            ISymbolWriter? writer = null;
            if (emitSymbolInfo)
            {
                writer = SymWrapperCore.SymWriter.CreateSymWriter();

                // Pass the "real" module to the VM.
                // This symfile is never written to disk so filename does not matter.
                IntPtr pInternalSymWriter = ModuleBuilder.nCreateISymWriterForDynamicModule(dynModule.InternalModule, "Unused");
                ((SymWrapperCore.SymWriter)writer).InternalSetUnderlyingWriter(pInternalSymWriter);
            }

            dynModule.SetSymWriter(writer);
            _assemblyData._moduleBuilderList.Add(dynModule);

            if (dynModule == _manifestModuleBuilder)
            {
                // We are reusing manifest module as user-defined dynamic module
                _isManifestModuleUsedAsDefinedModule = true;
            }

            return dynModule;
        }

        #endregion

        internal void CheckContext(params Type[]?[]? typess)
        {
            if (typess == null)
            {
                return;
            }

            foreach (Type[]? types in typess)
            {
                if (types != null)
                {
                    CheckContext(types);
                }
            }
        }

        internal void CheckContext(params Type?[]? types)
        {
            if (types == null)
            {
                return;
            }

            foreach (Type? type in types)
            {
                if (type == null)
                {
                    continue;
                }

                if (type.Module == null || type.Module.Assembly == null)
                {
                    throw new ArgumentException(SR.Argument_TypeNotValid);
                }

                if (type.Module.Assembly == typeof(object).Module.Assembly)
                {
                    continue;
                }
            }
        }

        public override bool Equals(object? obj) => InternalAssembly.Equals(obj);

        // Need a dummy GetHashCode to pair with Equals
        public override int GetHashCode() => InternalAssembly.GetHashCode();

        #region ICustomAttributeProvider Members
        public override object[] GetCustomAttributes(bool inherit)
        {
            return InternalAssembly.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return InternalAssembly.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return InternalAssembly.IsDefined(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return InternalAssembly.GetCustomAttributesData();
        }

        #endregion

        #region Assembly overrides

        /// <returns>The names of all the resources.</returns>
        public override string[] GetManifestResourceNames()
        {
            return InternalAssembly.GetManifestResourceNames();
        }

        public override FileStream GetFile(string name)
        {
            return InternalAssembly.GetFile(name);
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            return InternalAssembly.GetFiles(getResourceModules);
        }

        public override Stream? GetManifestResourceStream(Type type, string name)
        {
            return InternalAssembly.GetManifestResourceStream(type, name);
        }

        public override Stream? GetManifestResourceStream(string name)
        {
            return InternalAssembly.GetManifestResourceStream(name);
        }

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            return InternalAssembly.GetManifestResourceInfo(resourceName);
        }

        public override string Location => InternalAssembly.Location;

        public override string ImageRuntimeVersion => InternalAssembly.ImageRuntimeVersion;

        public override string? CodeBase => InternalAssembly.CodeBase;

        /// <sumary>
        /// Override the EntryPoint method on Assembly.
        /// This doesn't need to be synchronized because it is simple enough.
        /// </sumary>
        public override MethodInfo? EntryPoint => _assemblyData._entryPointMethod;

        /// <sumary>
        /// Get an array of all the public types defined in this assembly.
        /// </sumary>
        public override Type[] GetExportedTypes() => InternalAssembly.GetExportedTypes();

        public override AssemblyName GetName(bool copiedName) => InternalAssembly.GetName(copiedName);

        public override string? FullName => InternalAssembly.FullName;

        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            return InternalAssembly.GetType(name, throwOnError, ignoreCase);
        }

        public override Module? ManifestModule => _manifestModuleBuilder.InternalModule;

        public override bool ReflectionOnly => InternalAssembly.ReflectionOnly;

        public override Module? GetModule(string name) => InternalAssembly.GetModule(name);

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return InternalAssembly.GetReferencedAssemblies();
        }

        public override bool GlobalAssemblyCache => InternalAssembly.GlobalAssemblyCache;

        public override long HostContext => InternalAssembly.HostContext;

        public override Module[] GetModules(bool getResourceModules)
        {
            return InternalAssembly.GetModules(getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return InternalAssembly.GetLoadedModules(getResourceModules);
        }
        
        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            return InternalAssembly.GetSatelliteAssembly(culture, null);
        }

        /// <sumary> 
        /// Useful for binding to a very specific version of a satellite assembly
        /// </sumary>
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
        {
            return InternalAssembly.GetSatelliteAssembly(culture, version);
        }

        public override bool IsDynamic => true;

        public override bool IsCollectible => InternalAssembly.IsCollectible;

        #endregion
        
        /// <param name="name">The name of module for the look up.</param>
        /// <returns>Dynamic module with the specified name.</returns>
        public ModuleBuilder? GetDynamicModule(string name)
        {
            lock (SyncRoot)
            {
                return GetDynamicModuleNoLock(name);
            }
        }

        /// <param name="name">The name of module for the look up.</param>
        private ModuleBuilder? GetDynamicModuleNoLock(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

            for (int i = 0; i < _assemblyData._moduleBuilderList.Count; i++)
            {
                ModuleBuilder moduleBuilder = _assemblyData._moduleBuilderList[i];
                if (moduleBuilder._moduleData._moduleName.Equals(name))
                {
                    return moduleBuilder;
                }
            }
            return null;
        }

        /// <summary>
        /// Use this function if client decides to form the custom attribute blob themselves.
        /// </summary>
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
            {
                throw new ArgumentNullException(nameof(con));
            }
            if (binaryAttribute == null)
            {
                throw new ArgumentNullException(nameof(binaryAttribute));
            }

            lock (SyncRoot)
            {
                SetCustomAttributeNoLock(con, binaryAttribute);
            }
        }

        private void SetCustomAttributeNoLock(ConstructorInfo con, byte[] binaryAttribute)
        {
            TypeBuilder.DefineCustomAttribute(
                _manifestModuleBuilder,     // pass in the in-memory assembly module
                AssemblyBuilderData.AssemblyDefToken,
                _manifestModuleBuilder.GetConstructorToken(con).Token,
                binaryAttribute,
                false,
                typeof(DebuggableAttribute) == con.DeclaringType);
        }
        
        /// <summary>
        /// Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder.
        /// </summary>
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }

            lock (SyncRoot)
            {
                SetCustomAttributeNoLock(customBuilder);
            }
        }

        private void SetCustomAttributeNoLock(CustomAttributeBuilder customBuilder)
        {
            customBuilder.CreateCustomAttribute(_manifestModuleBuilder, AssemblyBuilderData.AssemblyDefToken);
        }
    }
}
