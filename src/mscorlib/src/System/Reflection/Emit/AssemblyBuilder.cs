// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//*************************************************************************************************************
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
//*************************************************************************************************************

namespace System.Reflection.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.SymbolStore;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Threading;

    // These must match the definitions in Assembly.hpp
    [Flags]
    internal enum DynamicAssemblyFlags
    {
        None = 0x00000000,

        // Security attributes which affect the module security descriptor
        AllCritical             = 0x00000001,
        Aptca                   = 0x00000002,
        Critical                = 0x00000004,
        Transparent             = 0x00000008,
        TreatAsSafe             = 0x00000010,
    }

    // When the user calls AppDomain.DefineDynamicAssembly the loader creates a new InternalAssemblyBuilder. 
    // This InternalAssemblyBuilder can be retrieved via a call to Assembly.GetAssemblies() by untrusted code.
    // In the past, when InternalAssemblyBuilder was AssemblyBuilder, the untrusted user could down cast the
    // Assembly to an AssemblyBuilder and emit code with the elevated permissions of the trusted code which 
    // origionally created the AssemblyBuilder via DefineDynamicAssembly. Today, this can no longer happen
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

        #region object overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is InternalAssemblyBuilder)
                return ((object)this == obj);

            return obj.Equals(this);
        }
        // Need a dummy GetHashCode to pair with Equals
        public override int GetHashCode() { return base.GetHashCode(); }
        #endregion

        // Assembly methods that are overridden by AssemblyBuilder should be overridden by InternalAssemblyBuilder too
        #region Methods inherited from Assembly
        public override String[] GetManifestResourceNames()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override FileStream GetFile(String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override Stream GetManifestResourceStream(Type type, String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override Stream GetManifestResourceStream(String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override String Location
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
            }
        }

        public override String CodeBase
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
            }
        }

        public override Type[] GetExportedTypes()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override String ImageRuntimeVersion
        {
            get
            {
                return RuntimeEnvironment.GetSystemVersion();
            }
        }
        #endregion
    }

    // AssemblyBuilder class.
    // deliberately not [serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_AssemblyBuilder))]
    [ComVisible(true)]
    public sealed class AssemblyBuilder : Assembly, _AssemblyBuilder
    {
        #region FCALL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);

        private Module nGetInMemoryAssemblyModule()
        {
            return AssemblyBuilder.GetInMemoryAssemblyModule(GetNativeHandle());
        }

        #endregion

        #region Internal Data Members
        // This is only valid in the "external" AssemblyBuilder
        internal AssemblyBuilderData m_assemblyData;
        private InternalAssemblyBuilder m_internalAssemblyBuilder;
        private ModuleBuilder m_manifestModuleBuilder;
        // Set to true if the manifest module was returned by code:DefineDynamicModule to the user
        private bool m_fManifestModuleUsedAsDefinedModule;
        internal const string MANIFEST_MODULE_NAME = "RefEmit_InMemoryManifestModule";

#if FEATURE_APPX
        private bool m_profileAPICheck;
#endif

        internal ModuleBuilder GetModuleBuilder(InternalModuleBuilder module)
        {
            Contract.Requires(module != null);
            Debug.Assert(this.InternalAssembly == module.Assembly);

            lock(SyncRoot)
            {
                // in CoreCLR there is only one module in each dynamic assembly, the manifest module
                if (m_manifestModuleBuilder.InternalModule == module)
                    return m_manifestModuleBuilder;

                throw new ArgumentException(null, nameof(module));
            }
        }

        internal object SyncRoot
        {
            get
            {
                return InternalAssembly.SyncRoot;
            }
        }

        internal InternalAssemblyBuilder InternalAssembly
        {
            get
            {
                return m_internalAssemblyBuilder;
            }
        }

        internal RuntimeAssembly GetNativeHandle()
        {
            return InternalAssembly.GetNativeHandle();
        }

        internal Version GetVersion()
        {
            return InternalAssembly.GetVersion();
        }

#if FEATURE_APPX
        internal bool ProfileAPICheck
        {
            get
            {
                return m_profileAPICheck;
            }
        }
#endif
        #endregion

        #region Constructor
        internal AssemblyBuilder(AppDomain domain,
                                 AssemblyName name,
                                 AssemblyBuilderAccess access,
                                 String dir,
                                 Evidence evidence,
                                 PermissionSet requiredPermissions,
                                 PermissionSet optionalPermissions,
                                 PermissionSet refusedPermissions,
                                 ref StackCrawlMark stackMark,
                                 IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes,
                                 SecurityContextSource securityContextSource)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (access != AssemblyBuilderAccess.Run
#if FEATURE_REFLECTION_ONLY_LOAD
                && access != AssemblyBuilderAccess.ReflectionOnly
#endif // FEATURE_REFLECTION_ONLY_LOAD
                && access != AssemblyBuilderAccess.RunAndCollect
                )
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access), nameof(access));
            }

            if (securityContextSource < SecurityContextSource.CurrentAppDomain ||
                securityContextSource > SecurityContextSource.CurrentAssembly)
            {
                throw new ArgumentOutOfRangeException(nameof(securityContextSource));
            }

            // Clone the name in case the caller modifies it underneath us.
            name = (AssemblyName)name.Clone();

            // If the caller is trusted they can supply identity
            // evidence for the new assembly. Otherwise we copy the
            // current grant and deny sets from the caller's assembly,
            // inject them into the new assembly and mark policy as
            // resolved. If/when the assembly is persisted and
            // reloaded, the normal rules for gathering evidence will
            // be used.
            if (evidence != null)
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
#pragma warning restore 618

            // Scan the assembly level attributes for any attributes which modify how we create the
            // assembly. Currently, we look for any attribute which modifies the security transparency
            // of the assembly.
            List<CustomAttributeBuilder> assemblyAttributes = null;
            DynamicAssemblyFlags assemblyFlags = DynamicAssemblyFlags.None;
            byte[] securityRulesBlob = null;
            byte[] aptcaBlob = null;
            if (unsafeAssemblyAttributes != null)
            {
                // Create a copy to ensure that it cannot be modified from another thread
                // as it is used further below.
                assemblyAttributes = new List<CustomAttributeBuilder>(unsafeAssemblyAttributes);

#pragma warning disable 618 // We deal with legacy attributes here as well for compat
                foreach (CustomAttributeBuilder attribute in assemblyAttributes)
                {
                    if (attribute.m_con.DeclaringType == typeof(SecurityTransparentAttribute))
                    {
                        assemblyFlags |= DynamicAssemblyFlags.Transparent;
                    }
                    else if (attribute.m_con.DeclaringType == typeof(SecurityCriticalAttribute))
                    {
                        {
                            assemblyFlags |= DynamicAssemblyFlags.AllCritical;
                        }
                    }
                }
#pragma warning restore 618
            }

            m_internalAssemblyBuilder = (InternalAssemblyBuilder)nCreateDynamicAssembly(domain,
                                                                                        name,
                                                                                        evidence,
                                                                                        ref stackMark,
                                                                                        requiredPermissions,
                                                                                        optionalPermissions,
                                                                                        refusedPermissions,
                                                                                        securityRulesBlob,
                                                                                        aptcaBlob,
                                                                                        access,
                                                                                        assemblyFlags,
                                                                                        securityContextSource);

            m_assemblyData = new AssemblyBuilderData(m_internalAssemblyBuilder,
                                                     name.Name,
                                                     access,
                                                     dir);
            m_assemblyData.AddPermissionRequests(requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions);

#if FEATURE_APPX
            if (AppDomain.ProfileAPICheck)
            {
                RuntimeAssembly creator = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (creator != null && !creator.IsFrameworkAssembly())
                    m_profileAPICheck = true;
            }
#endif
            // Make sure that ManifestModule is properly initialized
            // We need to do this before setting any CustomAttribute
            InitManifestModule();

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                    SetCustomAttribute(assemblyAttribute);
            }
        }

        private void InitManifestModule()
        {
            InternalModuleBuilder modBuilder = (InternalModuleBuilder)nGetInMemoryAssemblyModule();

            // Note that this ModuleBuilder cannot be used for RefEmit yet
            // because it hasn't been initialized.
            // However, it can be used to set the custom attribute on the Assembly
            m_manifestModuleBuilder = new ModuleBuilder(this, modBuilder);
            
            // We are only setting the name in the managed ModuleBuilderData here.
            // The name in the underlying metadata will be set when the
            // manifest module is created during nCreateDynamicAssembly.

            // This name needs to stay in sync with that used in
            // Assembly::Init to call ReflectionModule::Create (in VM)
            m_manifestModuleBuilder.Init(AssemblyBuilder.MANIFEST_MODULE_NAME, null, 0);

            m_fManifestModuleUsedAsDefinedModule = false;
        }
        #endregion

        #region DefineDynamicAssembly

        /**********************************************
        * If an AssemblyName has a public key specified, the assembly is assumed
        * to have a strong name and a hash will be computed when the assembly
        * is saved.
        **********************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null,
                                                 null, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 null, null, null, null, null,
                                                 ref stackMark,
                                                 assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Assembly nCreateDynamicAssembly(AppDomain domain,
                                                              AssemblyName name,
                                                              Evidence identity,
                                                              ref StackCrawlMark stackMark,
                                                              PermissionSet requiredPermissions,
                                                              PermissionSet optionalPermissions,
                                                              PermissionSet refusedPermissions,
                                                              byte[] securityRulesBlob,
                                                              byte[] aptcaBlob,
                                                              AssemblyBuilderAccess access,
                                                              DynamicAssemblyFlags flags,
                                                              SecurityContextSource securityContextSource);

        private class AssemblyBuilderLock { }

        internal static AssemblyBuilder InternalDefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            String dir,
            Evidence evidence,
            PermissionSet requiredPermissions,
            PermissionSet optionalPermissions,
            PermissionSet refusedPermissions,
            ref StackCrawlMark stackMark,
            IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes,
            SecurityContextSource securityContextSource)
        {
            lock (typeof(AssemblyBuilderLock))
            {
                // we can only create dynamic assemblies in the current domain
                return new AssemblyBuilder(AppDomain.CurrentDomain,
                                           name,
                                           access,
                                           dir,
                                           evidence,
                                           requiredPermissions,
                                           optionalPermissions,
                                           refusedPermissions,
                                           ref stackMark,
                                           unsafeAssemblyAttributes,
                                           securityContextSource);
            } //lock(typeof(AssemblyBuilderLock))
        }
        #endregion

        #region DefineDynamicModule
        /**********************************************
        *
        * Defines a named dynamic module. It is an error to define multiple 
        * modules within an Assembly with the same name. This dynamic module is
        * a transient module.
        * 
        **********************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String      name)
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String      name,
            bool        emitSymbolInfo)         // specify if emit symbol info or not
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal( name, emitSymbolInfo, ref stackMark );
        }

        private ModuleBuilder DefineDynamicModuleInternal(
            String      name,
            bool        emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)
        {
            lock(SyncRoot)
            {
                return DefineDynamicModuleInternalNoLock(name, emitSymbolInfo, ref stackMark);
            }
        }

        private ModuleBuilder DefineDynamicModuleInternalNoLock(
            String      name,
            bool        emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), nameof(name));
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), nameof(name));
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.DefineDynamicModule( " + name + " )");

            Debug.Assert(m_assemblyData != null, "m_assemblyData is null in DefineDynamicModuleInternal");

            ModuleBuilder dynModule;
            ISymbolWriter writer = null;
            IntPtr pInternalSymWriter = new IntPtr();

            // create the dynamic module- only one ModuleBuilder per AssemblyBuilder can be created
            if (m_fManifestModuleUsedAsDefinedModule == true)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoMultiModuleAssembly"));

            // Init(...) has already been called on m_manifestModuleBuilder in InitManifestModule()
            dynModule = m_manifestModuleBuilder;

            // Create the symbol writer
            if (emitSymbolInfo)
            {
                writer = SymWrapperCore.SymWriter.CreateSymWriter();
                // Set the underlying writer for the managed writer
                // that we're using.  Note that this function requires
                // unmanaged code access.
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
#pragma warning restore 618

                String fileName = "Unused"; // this symfile is never written to disk so filename does not matter.
                
                // Pass the "real" module to the VM
                pInternalSymWriter = ModuleBuilder.nCreateISymWriterForDynamicModule(dynModule.InternalModule, fileName);

                // In Telesto, we took the SetUnderlyingWriter method private as it's a very rickety method.
                // This might someday be a good move for the desktop CLR too.
                ((SymWrapperCore.SymWriter)writer).InternalSetUnderlyingWriter(pInternalSymWriter);
            } // Creating the symbol writer

            dynModule.SetSymWriter(writer);
            m_assemblyData.AddModule(dynModule);

            if (dynModule == m_manifestModuleBuilder)
            {   // We are reusing manifest module as user-defined dynamic module
                m_fManifestModuleUsedAsDefinedModule = true;
            }

            return dynModule;
        } // DefineDynamicModuleInternalNoLock
        #endregion

        private Assembly LoadISymWrapper()
        {
            if (m_assemblyData.m_ISymWrapperAssembly != null)
                return m_assemblyData.m_ISymWrapperAssembly;

            Assembly assem = Assembly.Load("ISymWrapper, Version=" + ThisAssembly.Version +
                ", Culture=neutral, PublicKeyToken=" + AssemblyRef.MicrosoftPublicKeyToken);

            m_assemblyData.m_ISymWrapperAssembly = assem;
            return assem;
        }

        internal void CheckContext(params Type[][] typess)
        {
            if (typess == null)
                return;
            
            foreach(Type[] types in typess)
                if (types != null)
                    CheckContext(types);
        }

        internal void CheckContext(params Type[] types)
        {
            if (types == null)
                return;
        
            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                if (type.Module == null || type.Module.Assembly == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotValid"));

                if (type.Module.Assembly == typeof(object).Module.Assembly)
                    continue;

                if (type.Module.Assembly.ReflectionOnly && !ReflectionOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("Arugment_EmitMixedContext1", type.AssemblyQualifiedName));

                if (!type.Module.Assembly.ReflectionOnly && ReflectionOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("Arugment_EmitMixedContext2", type.AssemblyQualifiedName));
            }
        }

        #region object overrides
        public override bool Equals(object obj)
        {
            return InternalAssembly.Equals(obj);
        }
        // Need a dummy GetHashCode to pair with Equals
        public override int GetHashCode() { return InternalAssembly.GetHashCode(); }
        #endregion

        #region ICustomAttributeProvider Members
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return InternalAssembly.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
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
        // Returns the names of all the resources
        public override String[] GetManifestResourceNames()
        {
            return InternalAssembly.GetManifestResourceNames();
        }
        
        public override FileStream GetFile(String name)
        {
            return InternalAssembly.GetFile(name);
        }
        
        public override FileStream[] GetFiles(bool getResourceModules)
        {
            return InternalAssembly.GetFiles(getResourceModules);
        }
        
        public override Stream GetManifestResourceStream(Type type, String name)
        {
            return InternalAssembly.GetManifestResourceStream(type, name);
        }
        
        public override Stream GetManifestResourceStream(String name)
        {
            return InternalAssembly.GetManifestResourceStream(name);
        }
                      
        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            return InternalAssembly.GetManifestResourceInfo(resourceName);
        }

        public override String Location
        {
            get
            {
                return InternalAssembly.Location;
            }
        }

        public override String ImageRuntimeVersion
        {
            get
            {
                return InternalAssembly.ImageRuntimeVersion;
            }
        }
        
        public override String CodeBase
        {
            get
            {
                return InternalAssembly.CodeBase;
            }
        }

        // Override the EntryPoint method on Assembly.
        // This doesn't need to be synchronized because it is simple enough
        public override MethodInfo EntryPoint 
        {
            get 
            {
                return m_assemblyData.m_entryPointMethod;
            }
        }

        // Get an array of all the public types defined in this assembly
        public override Type[] GetExportedTypes()
        {
            return InternalAssembly.GetExportedTypes();
        }

        public override AssemblyName GetName(bool copiedName)
        {
            return InternalAssembly.GetName(copiedName);
        }

        public override String FullName
        {
            get
            {
                return InternalAssembly.FullName;
            }
        }

        public override Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            return InternalAssembly.GetType(name, throwOnError, ignoreCase);
        }

        public override Module ManifestModule
        {
            get
            {
                return m_manifestModuleBuilder.InternalModule;
            }
        }

        public override bool ReflectionOnly
        {
            get
            {
                return InternalAssembly.ReflectionOnly;
            }
        }

        public override Module GetModule(String name)
        {
            return InternalAssembly.GetModule(name);
        }

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return InternalAssembly.GetReferencedAssemblies();
        }

        public override bool GlobalAssemblyCache
        {
            get
            {
                return InternalAssembly.GlobalAssemblyCache;
            }
        }

        public override Int64 HostContext
        {
            get
            {
                return InternalAssembly.HostContext;
            }
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return InternalAssembly.GetModules(getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return InternalAssembly.GetLoadedModules(getResourceModules);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalAssembly.InternalGetSatelliteAssembly(culture, null, ref stackMark);
        }

        // Useful for binding to a very specific version of a satellite assembly
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalAssembly.InternalGetSatelliteAssembly(culture, version, ref stackMark);
        }

        public override bool IsDynamic
        {
            get {
                return true;
            }
        }
        #endregion


        /**********************************************
        *
        * return a dynamic module with the specified name.
        *
        **********************************************/
        public ModuleBuilder GetDynamicModule(
            String      name)                   // the name of module for the look up
        {
            lock(SyncRoot)
            {
                return GetDynamicModuleNoLock(name);
            }
        }

        private ModuleBuilder GetDynamicModuleNoLock(
            String      name)                   // the name of module for the look up
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), nameof(name));
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.GetDynamicModule( " + name + " )");
            int size = m_assemblyData.m_moduleBuilderList.Count;
            for (int i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = (ModuleBuilder) m_assemblyData.m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName.Equals(name))
                {
                    return moduleBuilder;
                }
            }
            return null;
        }
    
        /**********************************************
        *
        * Setting the entry point if the assembly builder is building
        * an exe.
        *
        **********************************************/
        public void SetEntryPoint(
            MethodInfo  entryMethod) 
        {
            SetEntryPoint(entryMethod, PEFileKinds.ConsoleApplication);
        }
        public void SetEntryPoint(
            MethodInfo  entryMethod,        // entry method for the assembly. We use this to determine the entry module
            PEFileKinds fileKind)           // file kind for the assembly.
        {
            lock(SyncRoot)
            {
                SetEntryPointNoLock(entryMethod, fileKind);
            }
        }

        private void SetEntryPointNoLock(
            MethodInfo  entryMethod,        // entry method for the assembly. We use this to determine the entry module
            PEFileKinds fileKind)           // file kind for the assembly.
        {

            if (entryMethod == null)
                throw new ArgumentNullException(nameof(entryMethod));
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.SetEntryPoint");

            Module tmpModule = entryMethod.Module;
            if (tmpModule == null || !InternalAssembly.Equals(tmpModule.Assembly))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EntryMethodNotDefinedInAssembly"));

            m_assemblyData.m_entryPointMethod = entryMethod;
            m_assemblyData.m_peFileKind = fileKind;
        }


        /**********************************************
        * Use this function if client decides to form the custom attribute blob themselves
        **********************************************/
        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException(nameof(con));
            if (binaryAttribute == null)
                throw new ArgumentNullException(nameof(binaryAttribute));
            Contract.EndContractBlock();
    
            lock(SyncRoot)
            {
                SetCustomAttributeNoLock(con, binaryAttribute);
            }
        }

        private void SetCustomAttributeNoLock(ConstructorInfo con, byte[] binaryAttribute)
        {
            TypeBuilder.DefineCustomAttribute(
                m_manifestModuleBuilder,     // pass in the in-memory assembly module
                AssemblyBuilderData.m_tkAssembly,           // This is the AssemblyDef token
                m_manifestModuleBuilder.GetConstructorToken(con).Token,
                binaryAttribute,
                false,
                typeof(System.Diagnostics.DebuggableAttribute) == con.DeclaringType);

            // Track the CA for persistence
            if (m_assemblyData.m_access != AssemblyBuilderAccess.Run)
            {
                // tracking the CAs for persistence
                m_assemblyData.AddCustomAttribute(con, binaryAttribute);
            }
        }

        /**********************************************
        * Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        **********************************************/
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }
            Contract.EndContractBlock();

            lock(SyncRoot)
            {
                SetCustomAttributeNoLock(customBuilder);
            }
        }

        private void SetCustomAttributeNoLock(CustomAttributeBuilder customBuilder)
        {
            customBuilder.CreateCustomAttribute(
                m_manifestModuleBuilder, 
                AssemblyBuilderData.m_tkAssembly);          // This is the AssemblyDef token 

            // Track the CA for persistence
            if (m_assemblyData.m_access != AssemblyBuilderAccess.Run)
            {
                m_assemblyData.AddCustomAttribute(customBuilder);
            }
        }


        /**********************************************
        *
        * Saves the assembly to disk. Also saves all dynamic modules defined
        * in this dynamic assembly. Assembly file name can be the same as one of 
        * the module's name. If so, assembly info is stored within that module.
        * Assembly file name can be different from all of the modules underneath. In
        * this case, assembly is stored stand alone. 
        *
        **********************************************/

        public void Save(String assemblyFileName)       // assembly file name
        {
            Save(assemblyFileName, System.Reflection.PortableExecutableKinds.ILOnly, System.Reflection.ImageFileMachine.I386);
        }
            
        public void Save(String assemblyFileName, 
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            lock(SyncRoot)
            {
                SaveNoLock(assemblyFileName, portableExecutableKind, imageFileMachine);
            }
        }

        private void SaveNoLock(String assemblyFileName, 
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            // AssemblyBuilderAccess.Save can never be set in CoreCLR
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CantSaveTransientAssembly"));
        }

        internal bool IsPersistable()
        {
            {
                return false;
            }
        }
    
        /**********************************************
        *
        * Internal helper to walk the nested type hierachy
        *
        **********************************************/
        private int DefineNestedComType(Type type, int tkResolutionScope, int tkTypeDef)
        {
            Type        enclosingType = type.DeclaringType;
            if (enclosingType == null)
            {
                // Use full type name for non-nested types.
                return AddExportedTypeOnDisk(GetNativeHandle(), type.FullName, tkResolutionScope, tkTypeDef, type.Attributes);
            }

            tkResolutionScope = DefineNestedComType(enclosingType, tkResolutionScope, tkTypeDef);
            // Use simple name for nested types.
            return AddExportedTypeOnDisk(GetNativeHandle(), type.Name, tkResolutionScope, tkTypeDef, type.Attributes);
        }

        internal int DefineExportedTypeInMemory(Type type, int tkResolutionScope, int tkTypeDef)
        {
            Type enclosingType = type.DeclaringType;
            if (enclosingType == null)
            {
                // Use full type name for non-nested types.
                return AddExportedTypeInMemory(GetNativeHandle(), type.FullName, tkResolutionScope, tkTypeDef, type.Attributes);
            }

            tkResolutionScope = DefineExportedTypeInMemory(enclosingType, tkResolutionScope, tkTypeDef);
            // Use simple name for nested types.
            return AddExportedTypeInMemory(GetNativeHandle(), type.Name, tkResolutionScope, tkTypeDef, type.Attributes);
        }

        /**********************************************
         * 
         * Private methods
         * 
         **********************************************/
    
        /**********************************************
         * Make a private constructor so these cannot be constructed externally.
         * @internonly
         **********************************************/
        private AssemblyBuilder() {}

        // Create a new module in which to emit code. This module will not contain the manifest.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void DefineDynamicModule(RuntimeAssembly containingAssembly,
                                                       bool emitSymbolInfo,
                                                       String name,
                                                       String filename,
                                                       StackCrawlMarkHandle stackMark,
                                                       ref IntPtr pInternalSymWriter,
                                                       ObjectHandleOnStack retModule,
                                                       bool fIsTransient,
                                                       out int tkFile);

        private static Module DefineDynamicModule(RuntimeAssembly containingAssembly, 
                                           bool emitSymbolInfo,
                                           String name,
                                           String filename,
                                           ref StackCrawlMark stackMark,
                                           ref IntPtr pInternalSymWriter,
                                           bool fIsTransient,
                                           out int tkFile)
        {
            RuntimeModule retModule = null;

            DefineDynamicModule(containingAssembly.GetNativeHandle(),
                                emitSymbolInfo,
                                name,
                                filename,
                                JitHelpers.GetStackCrawlMarkHandle(ref stackMark),
                                ref pInternalSymWriter,
                                JitHelpers.GetObjectHandleOnStack(ref retModule),
                                fIsTransient,
                                out tkFile);

            return retModule;
        }

        // The following functions are native helpers for creating on-disk manifest
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void PrepareForSavingManifestToDisk(RuntimeAssembly assembly, RuntimeModule assemblyModule);  // module to contain assembly information if assembly is embedded

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void SaveManifestToDisk(RuntimeAssembly assembly,
                                                String strFileName, 
                                                int entryPoint,
                                                int fileKind,
                                                int portableExecutableKind, 
                                                int ImageFileMachine);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddFile(RuntimeAssembly assembly, String strFileName);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void SetFileHashValue(RuntimeAssembly assembly,
                                                    int tkFile, 
                                                    String strFullFileName);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddExportedTypeInMemory(RuntimeAssembly assembly,
                                                          String strComTypeName,
                                                          int tkAssemblyRef,
                                                          int tkTypeDef,
                                                          TypeAttributes flags);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddExportedTypeOnDisk(RuntimeAssembly assembly, 
                                                        String strComTypeName, 
                                                        int tkAssemblyRef, 
                                                        int tkTypeDef, 
                                                        TypeAttributes flags);

        // Add an entry to assembly's manifestResource table for a stand alone resource.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void AddStandAloneResource(RuntimeAssembly assembly,
                                                         String strName,
                                                         String strFileName,
                                                         String strFullFileName,
                                                         int attribute);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
#pragma warning disable 618
        static private extern void AddDeclarativeSecurity(RuntimeAssembly assembly, SecurityAction action, byte[] blob, int length);
#pragma warning restore 618

        // Functions for defining unmanaged resources.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void CreateVersionInfoResource(String filename, String title, String iconFilename, String description,
                                                             String copyright, String trademark, String company, String product,
                                                             String productVersion, String fileVersion, int lcid, bool isDll,
                                                             StringHandleOnStack retFileName);
    }
}
