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

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public override FileStream GetFile(String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
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
#if FEATURE_CORECLR
            [SecurityCritical]
#endif // FEATURE_CORECLR
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
            }
        }

        public override String CodeBase
        {
#if FEATURE_CORECLR
            [SecurityCritical]
#endif // FEATURE_CORECLR
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
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_AssemblyBuilder))]
    [ComVisible(true)]
    public sealed class AssemblyBuilder : Assembly, _AssemblyBuilder
    {
        #region FCALL
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        private Module nGetInMemoryAssemblyModule()
        {
            return AssemblyBuilder.GetInMemoryAssemblyModule(GetNativeHandle());
        }

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule GetOnDiskAssemblyModule(RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        private ModuleBuilder GetOnDiskAssemblyModuleBuilder()
        {
            if (m_onDiskAssemblyModuleBuilder == null)
            {
                Module module = AssemblyBuilder.GetOnDiskAssemblyModule(InternalAssembly.GetNativeHandle());
                ModuleBuilder modBuilder = new ModuleBuilder(this, (InternalModuleBuilder)module);
                modBuilder.Init("RefEmit_OnDiskManifestModule", null, 0);
                m_onDiskAssemblyModuleBuilder = modBuilder;
            }

            return m_onDiskAssemblyModuleBuilder;
        }
#endif // FEATURE_CORECLR

        #endregion

        #region Internal Data Members
        // This is only valid in the "external" AssemblyBuilder
        internal AssemblyBuilderData m_assemblyData;
        private InternalAssemblyBuilder m_internalAssemblyBuilder;
        private ModuleBuilder m_manifestModuleBuilder;
        // Set to true if the manifest module was returned by code:DefineDynamicModule to the user
        private bool m_fManifestModuleUsedAsDefinedModule;
        internal const string MANIFEST_MODULE_NAME = "RefEmit_InMemoryManifestModule";
#if !FEATURE_CORECLR
        private ModuleBuilder m_onDiskAssemblyModuleBuilder;
#endif // !FEATURE_CORECLR

#if FEATURE_APPX
        private bool m_profileAPICheck;
#endif

        internal ModuleBuilder GetModuleBuilder(InternalModuleBuilder module)
        {
            Contract.Requires(module != null);
            Contract.Assert(this.InternalAssembly == module.Assembly);

            lock(SyncRoot)
            {
#if !FEATURE_CORECLR
                foreach (ModuleBuilder modBuilder in m_assemblyData.m_moduleBuilderList)
                {
                    if (modBuilder.InternalModule == module)
                        return modBuilder;
                }

                // m_onDiskAssemblyModuleBuilder is null before Save
                if (m_onDiskAssemblyModuleBuilder != null && m_onDiskAssemblyModuleBuilder.InternalModule == module)
                    return m_onDiskAssemblyModuleBuilder;
#endif // !FEATURE_CORECLR

                // in CoreCLR there is only one module in each dynamic assembly, the manifest module
                if (m_manifestModuleBuilder.InternalModule == module)
                    return m_manifestModuleBuilder;

                throw new ArgumentException("module");
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

        [SecurityCritical]
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
        [System.Security.SecurityCritical]  // auto-generated
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
                throw new ArgumentNullException("name");

            if (access != AssemblyBuilderAccess.Run
#if !FEATURE_CORECLR
                && access != AssemblyBuilderAccess.Save
                && access != AssemblyBuilderAccess.RunAndSave
#endif // !FEATURE_CORECLR
#if FEATURE_REFLECTION_ONLY_LOAD
                && access != AssemblyBuilderAccess.ReflectionOnly
#endif // FEATURE_REFLECTION_ONLY_LOAD
#if FEATURE_COLLECTIBLE_TYPES
                && access != AssemblyBuilderAccess.RunAndCollect
#endif // FEATURE_COLLECTIBLE_TYPES
                )
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access), "access");
            }

            if (securityContextSource < SecurityContextSource.CurrentAppDomain ||
                securityContextSource > SecurityContextSource.CurrentAssembly)
            {
                throw new ArgumentOutOfRangeException("securityContextSource");
            }

            // Clone the name in case the caller modifies it underneath us.
            name = (AssemblyName)name.Clone();
            
#if !FEATURE_CORECLR
            // Set the public key from the key pair if one has been provided.
            // (Overwite any public key in the Assembly name, since it's no
            // longer valid to have a disparity).
            if (name.KeyPair != null)
                name.SetPublicKey(name.KeyPair.PublicKey);
#endif

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

#if FEATURE_COLLECTIBLE_TYPES && !FEATURE_CORECLR
            // Collectible assemblies require FullTrust. This demand may be removed if we deem the
            // feature robust enough to be used directly by untrusted API consumers.
            if (access == AssemblyBuilderAccess.RunAndCollect)
                new PermissionSet(PermissionState.Unrestricted).Demand();
#endif // FEATURE_COLLECTIBLE_TYPES && !FEATURE_CORECLR

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
#if !FEATURE_CORECLR
                        SecurityCriticalScope scope = SecurityCriticalScope.Everything;
                        if (attribute.m_constructorArgs != null &&
                            attribute.m_constructorArgs.Length == 1 &&
                            attribute.m_constructorArgs[0] is SecurityCriticalScope)
                        {
                            scope = (SecurityCriticalScope)attribute.m_constructorArgs[0];
                        }

                        assemblyFlags |= DynamicAssemblyFlags.Critical;
                        if (scope == SecurityCriticalScope.Everything)
#endif // !FEATURE_CORECLR
                        {
                            assemblyFlags |= DynamicAssemblyFlags.AllCritical;
                        }
                    }
#if !FEATURE_CORECLR
                    else if (attribute.m_con.DeclaringType == typeof(SecurityRulesAttribute))
                    {
                        securityRulesBlob = new byte[attribute.m_blob.Length];
                        Buffer.BlockCopy(attribute.m_blob, 0, securityRulesBlob, 0, securityRulesBlob.Length);
                    }
                    else if (attribute.m_con.DeclaringType == typeof(SecurityTreatAsSafeAttribute))
                    {
                        assemblyFlags |= DynamicAssemblyFlags.TreatAsSafe;
                    }
#endif // !FEATURE_CORECLR
#if FEATURE_APTCA
                    else if (attribute.m_con.DeclaringType == typeof(AllowPartiallyTrustedCallersAttribute))
                    {
                        assemblyFlags |= DynamicAssemblyFlags.Aptca;
                        aptcaBlob = new byte[attribute.m_blob.Length];
                        Buffer.BlockCopy(attribute.m_blob, 0, aptcaBlob, 0, aptcaBlob.Length);
                    }
#endif // FEATURE_APTCA
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

        [System.Security.SecurityCritical]  // auto-generated
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
        [System.Security.SecuritySafeCritical]  // auto-generated
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

        [System.Security.SecuritySafeCritical]  // auto-generated
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


        [System.Security.SecurityCritical]  // auto-generated
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

        [System.Security.SecurityCritical]  // auto-generated
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
#if FEATURE_CAS_POLICY
            if (evidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }
#endif // FEATURE_CAS_POLICY

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
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String      name)
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, false, ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String      name,
            bool        emitSymbolInfo)         // specify if emit symbol info or not
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal( name, emitSymbolInfo, ref stackMark );
        }

        [System.Security.SecurityCritical]  // auto-generated
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

        [System.Security.SecurityCritical]  // auto-generated
        private ModuleBuilder DefineDynamicModuleInternalNoLock(
            String      name,
            bool        emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), "name");
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.DefineDynamicModule( " + name + " )");

            Contract.Assert(m_assemblyData != null, "m_assemblyData is null in DefineDynamicModuleInternal");

            ModuleBuilder dynModule;
            ISymbolWriter writer = null;
            IntPtr pInternalSymWriter = new IntPtr();

            // create the dynamic module

#if FEATURE_MULTIMODULE_ASSEMBLIES

#if FEATURE_CORECLR
#error FEATURE_MULTIMODULE_ASSEMBLIES should always go with !FEATURE_CORECLR
#endif //FEATURE_CORECLR

            m_assemblyData.CheckNameConflict(name);

            if (m_fManifestModuleUsedAsDefinedModule == true)
            {   // We need to define a new module
                int tkFile;
                InternalModuleBuilder internalDynModule = (InternalModuleBuilder)DefineDynamicModule(
                    InternalAssembly, 
                    emitSymbolInfo, 
                    name,
                    name,
                    ref stackMark, 
                    ref pInternalSymWriter,
                    true /*fIsTransient*/,
                    out tkFile);
                dynModule = new ModuleBuilder(this, internalDynModule);

                // initialize the dynamic module's managed side information
                dynModule.Init(name, null, tkFile);
            }
            else
            {   // We will reuse the manifest module
                m_manifestModuleBuilder.ModifyModuleName(name);
                dynModule = m_manifestModuleBuilder;

                if (emitSymbolInfo)
                {
                    pInternalSymWriter = ModuleBuilder.nCreateISymWriterForDynamicModule(dynModule.InternalModule, name);
                }
            }

#else // FEATURE_MULTIMODULE_ASSEMBLIES
            // Without FEATURE_MULTIMODULE_ASSEMBLIES only one ModuleBuilder per AssemblyBuilder can be created
            if (m_fManifestModuleUsedAsDefinedModule == true)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoMultiModuleAssembly"));

            // Init(...) has already been called on m_manifestModuleBuilder in InitManifestModule()
            dynModule = m_manifestModuleBuilder;
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

            // Create the symbol writer
            if (emitSymbolInfo)
            {
#if FEATURE_MULTIMODULE_ASSEMBLIES && !FEATURE_CORECLR
                // this is the code path for the desktop runtime

                // create the default SymWriter
                Assembly assem = LoadISymWrapper();
                Type symWriter = assem.GetType("System.Diagnostics.SymbolStore.SymWriter", true, false);
                if (symWriter != null && !symWriter.IsVisible)
                    symWriter = null;

                if (symWriter == null)
                {
                    // cannot find SymWriter - throw TypeLoadException since we couldnt find the type.
                    throw new TypeLoadException(Environment.GetResourceString(ResId.MissingType, "SymWriter"));
                }

                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

                try
                {
                    (new PermissionSet(PermissionState.Unrestricted)).Assert();
                    writer = (ISymbolWriter)Activator.CreateInstance(symWriter);

                    // Set the underlying writer for the managed writer
                    // that we're using.  Note that this function requires
                    // unmanaged code access.
                    writer.SetUnderlyingWriter(pInternalSymWriter);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
#endif // FEATURE_MULTIMODULE_ASSEMBLIES && !FEATURE_CORECLR

#if !FEATURE_MULTIMODULE_ASSEMBLIES && FEATURE_CORECLR
                // this is the code path for CoreCLR

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
#endif // !FEATURE_MULTIMODULE_ASSEMBLIES && FEATURE_CORECLR
            } // Creating the symbol writer

            dynModule.SetSymWriter(writer);
            m_assemblyData.AddModule(dynModule);

            if (dynModule == m_manifestModuleBuilder)
            {   // We are reusing manifest module as user-defined dynamic module
                m_fManifestModuleUsedAsDefinedModule = true;
            }

            return dynModule;
        } // DefineDynamicModuleInternalNoLock

#if !FEATURE_CORECLR
        // All dynamic modules in SilverLight are transient so we removed this overload of DefineDynamicModule
        // Note that it is assumed that !FEATURE_CORECLR always goes with FEATURE_MULTIMODULE_ASSEMBLIES
        // If we ever will build a non coreclr version of the runtime without FEATURE_MULTIMODULE_ASSEMBLIES
        // we will need to make the same changes here as the ones we made in the transient overload

        /**********************************************
        *
        * Defines a named dynamic module. It is an error to define multiple 
        * modules within an Assembly with the same name. No symbol information
        * will be emitted.
        * 
        **********************************************/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String name,
            String fileName)
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            // delegate to the next DefineDynamicModule 
            return DefineDynamicModuleInternal(name, fileName, false, ref stackMark);
        }

        /**********************************************
        *
        * Emit symbol information if emitSymbolInfo is true using the
        * default symbol writer interface.
        * An exception will be thrown if the assembly is transient.
        *
        **********************************************/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public ModuleBuilder DefineDynamicModule(
            String name,                   // module name
            String fileName,               // module file name
            bool emitSymbolInfo)         // specify if emit symbol info or not
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, fileName, emitSymbolInfo, ref stackMark);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private ModuleBuilder DefineDynamicModuleInternal(
            String name,                   // module name
            String fileName,               // module file name
            bool emitSymbolInfo,         // specify if emit symbol info or not
            ref StackCrawlMark stackMark)       // stack crawl mark used to find caller
        {
            lock(SyncRoot)
            {
                return DefineDynamicModuleInternalNoLock(name, fileName, emitSymbolInfo, ref stackMark);
            }
        }

        // "name" will be used for:
        //     1. The Name field in the Module table.
        //     2. ModuleBuilder.GetModule(string).
        // "fileName" will be used for:
        //     1. The name field in the ModuleRef table when this module is being referenced by
        //        another module in the same assembly.
        //     2. .file record in the in memory assembly manifest when the module is created in memory 
        //     3. .file record in the on disk assembly manifest when the assembly is saved to disk 
        //     4. The file name of the saved module.
        [System.Security.SecurityCritical]  // auto-generated
        private ModuleBuilder DefineDynamicModuleInternalNoLock(
            String name,                        // module name
            String fileName,                    // module file name
            bool   emitSymbolInfo,              // specify if emit symbol info or not
            ref    StackCrawlMark stackMark)    // stack crawl mark used to find caller
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), "name");

            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "fileName");
            if (!String.Equals(fileName, Path.GetFileName(fileName)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSimpleFileName"), "fileName");
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.DefineDynamicModule( " + name + ", " + fileName + ", " + emitSymbolInfo + " )");
            if (m_assemblyData.m_access == AssemblyBuilderAccess.Run)
            {
                // Error! You cannot define a persistable module within a transient data.
                throw new NotSupportedException(Environment.GetResourceString("Argument_BadPersistableModuleInTransientAssembly"));
            }

            if (m_assemblyData.m_isSaved == true)
            {
                // assembly has been saved before!
                throw new InvalidOperationException(Environment.GetResourceString(
                    "InvalidOperation_CannotAlterAssembly"));
            }

            ModuleBuilder dynModule;
            ISymbolWriter writer = null;
            IntPtr pInternalSymWriter = new IntPtr();

            // create the dynamic module

            m_assemblyData.CheckNameConflict(name);
            m_assemblyData.CheckFileNameConflict(fileName);

            int tkFile;
            InternalModuleBuilder internalDynModule = (InternalModuleBuilder)DefineDynamicModule(
                InternalAssembly,
                emitSymbolInfo,
                name,
                fileName,
                ref stackMark,
                ref pInternalSymWriter,
                false /*fIsTransient*/,
                out tkFile);
            dynModule = new ModuleBuilder(this, internalDynModule);

            // initialize the dynamic module's managed side information
            dynModule.Init(name, fileName, tkFile);

            // Create the symbol writer
            if (emitSymbolInfo)
            {
                // create the default SymWriter
                Assembly assem = LoadISymWrapper();
                Type symWriter = assem.GetType("System.Diagnostics.SymbolStore.SymWriter", true, false);
                if (symWriter != null && !symWriter.IsVisible)
                    symWriter = null;

                if (symWriter == null)
                {
                    // cannot find SymWriter - throw TypeLoadException since we couldnt find the type.
                    throw new TypeLoadException(Environment.GetResourceString("MissingType", "SymWriter"));
                }
                try
                {
                    (new PermissionSet(PermissionState.Unrestricted)).Assert();
                    writer = (ISymbolWriter)Activator.CreateInstance(symWriter);

                    // Set the underlying writer for the managed writer
                    // that we're using.  Note that this function requires
                    // unmanaged code access.
                    writer.SetUnderlyingWriter(pInternalSymWriter);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
            }

            dynModule.SetSymWriter(writer);

            m_assemblyData.AddModule(dynModule);

            return dynModule;
        } // DefineDynamicModuleInternalNoLock
#endif // !FEATURE_CORECLR
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

#if !FEATURE_CORECLR
        /**********************************************
        *
        * Define stand alone managed resource for Assembly
        *
        **********************************************/
        public IResourceWriter DefineResource(
            String      name,
            String      description,
            String      fileName)
        {
            return DefineResource(name, description, fileName, ResourceAttributes.Public);
        }

        /**********************************************
        *
        * Define stand alone managed resource for Assembly
        *
        **********************************************/
        public IResourceWriter DefineResource(
            String      name,
            String      description,
            String      fileName,
            ResourceAttributes attribute)
        {
            lock(SyncRoot)
            {
                return DefineResourceNoLock(name, description, fileName, attribute);
            }
        }

        private IResourceWriter DefineResourceNoLock(
            String      name,
            String      description,
            String      fileName,
            ResourceAttributes attribute)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), name);
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "fileName");
            if (!String.Equals(fileName, Path.GetFileName(fileName)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSimpleFileName"), "fileName");
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.DefineResource( " + name + ", " + fileName + ")");

            m_assemblyData.CheckResNameConflict(name);
            m_assemblyData.CheckFileNameConflict(fileName);

            ResourceWriter resWriter;
            String  fullFileName;

            if (m_assemblyData.m_strDir == null)
            {
                // If assembly directory is null, use current directory
                fullFileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                resWriter = new ResourceWriter(fullFileName);
            }
            else
            {
                // Form the full path given the directory provided by user
                fullFileName = Path.Combine(m_assemblyData.m_strDir, fileName);
                resWriter = new ResourceWriter(fullFileName);
            }
            // get the full path
            fullFileName = Path.GetFullPath(fullFileName);
            
            // retrieve just the file name
            fileName = Path.GetFileName(fullFileName);
            
            m_assemblyData.AddResWriter( new ResWriterData( resWriter, null, name, fileName, fullFileName, attribute) );
            return resWriter;
        }

#endif // !FEATURE_CORECLR

        /**********************************************
        *
        * Add an existing resource file to the Assembly
        *
        **********************************************/
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void AddResourceFile(
            String      name,
            String      fileName)
        {
            AddResourceFile(name, fileName, ResourceAttributes.Public);
        }

        /**********************************************
        *
        * Add an existing resource file to the Assembly
        *
        **********************************************/
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void AddResourceFile(
            String      name,
            String      fileName,
            ResourceAttributes attribute)
        {
            lock(SyncRoot)
            {
                AddResourceFileNoLock(name, fileName, attribute);
            }
        }

        [System.Security.SecuritySafeCritical]
        private void AddResourceFileNoLock(
            String      name,
            String      fileName,
            ResourceAttributes attribute)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), name);
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), fileName);
            if (!String.Equals(fileName, Path.GetFileName(fileName)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSimpleFileName"), "fileName");
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.AddResourceFile( " + name + ", " + fileName + ")");

            m_assemblyData.CheckResNameConflict(name);
            m_assemblyData.CheckFileNameConflict(fileName);

            String  fullFileName;

            if (m_assemblyData.m_strDir == null)
            {
                // If assembly directory is null, use current directory
                fullFileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            }
            else
            {
                // Form the full path given the directory provided by user
                fullFileName = Path.Combine(m_assemblyData.m_strDir, fileName);
            }
            
            // get the full path
            fullFileName = Path.UnsafeGetFullPath(fullFileName);
            
            // retrieve just the file name
            fileName = Path.GetFileName(fullFileName);
            
            if (File.UnsafeExists(fullFileName) == false)
                throw new FileNotFoundException(Environment.GetResourceString(
                    "IO.FileNotFound_FileName",
                    fileName), fileName);
            m_assemblyData.AddResWriter( new ResWriterData( null, null, name, fileName, fullFileName, attribute) );
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
        
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public override FileStream GetFile(String name)
        {
            return InternalAssembly.GetFile(name);
        }
        
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
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
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
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
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
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

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
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

#if FEATURE_CAS_POLICY
        public override Evidence Evidence
        {
            get
            {
                return InternalAssembly.Evidence;
            }
        }

        public override PermissionSet PermissionSet
        {
            [SecurityCritical]
            get
            {
                return InternalAssembly.PermissionSet;
            }
        }

        public override SecurityRuleSet SecurityRuleSet
        {
            get
            {
                return InternalAssembly.SecurityRuleSet;
            }
        }
#endif // FEATURE_CAS_POLICY

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
        * Add an unmanaged Version resource to the
        *  assembly
        *
        **********************************************/
        public void DefineVersionInfoResource(
            String      product, 
            String      productVersion, 
            String      company, 
            String      copyright, 
            String      trademark)
        {
            lock(SyncRoot)
            {
                DefineVersionInfoResourceNoLock(
                    product,
                    productVersion,
                    company,
                    copyright,
                    trademark);
            }
        }

        private void DefineVersionInfoResourceNoLock(
            String product,
            String productVersion,
            String company,
            String copyright,
            String trademark)
        {
            if (m_assemblyData.m_strResourceFileName != null ||
                m_assemblyData.m_resourceBytes != null ||
                m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));

            m_assemblyData.m_nativeVersion = new NativeVersionInfo();

            m_assemblyData.m_nativeVersion.m_strCopyright = copyright;
            m_assemblyData.m_nativeVersion.m_strTrademark = trademark;
            m_assemblyData.m_nativeVersion.m_strCompany = company;
            m_assemblyData.m_nativeVersion.m_strProduct = product;
            m_assemblyData.m_nativeVersion.m_strProductVersion = productVersion;
            m_assemblyData.m_hasUnmanagedVersionInfo = true;
            m_assemblyData.m_OverrideUnmanagedVersionInfo = true;

        }
        
        public void DefineVersionInfoResource()
        {
            lock(SyncRoot)
            {
                DefineVersionInfoResourceNoLock();
            }
        }

        private void DefineVersionInfoResourceNoLock()
        {
            if (m_assemblyData.m_strResourceFileName != null ||
                m_assemblyData.m_resourceBytes != null ||
                m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            
            m_assemblyData.m_hasUnmanagedVersionInfo = true;
            m_assemblyData.m_nativeVersion = new NativeVersionInfo();
        }

        public void DefineUnmanagedResource(Byte[] resource)
        {
            if (resource == null)
                throw new ArgumentNullException("resource");
            Contract.EndContractBlock();

            lock(SyncRoot)
            {
                DefineUnmanagedResourceNoLock(resource);
            }
        }

        private void DefineUnmanagedResourceNoLock(Byte[] resource)
        {
            if (m_assemblyData.m_strResourceFileName != null ||
                m_assemblyData.m_resourceBytes != null ||
                m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            
            m_assemblyData.m_resourceBytes = new byte[resource.Length];
            Buffer.BlockCopy(resource, 0, m_assemblyData.m_resourceBytes, 0, resource.Length);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void DefineUnmanagedResource(String resourceFileName)
        {
            if (resourceFileName == null)
                throw new ArgumentNullException("resourceFileName");
            Contract.EndContractBlock();

            lock(SyncRoot)
            {
                DefineUnmanagedResourceNoLock(resourceFileName);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void DefineUnmanagedResourceNoLock(String resourceFileName)
        {
            if (m_assemblyData.m_strResourceFileName != null ||
                m_assemblyData.m_resourceBytes != null ||
                m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            
            // Check caller has the right to read the file.
            string      strFullFileName;
            if (m_assemblyData.m_strDir == null)
            {
                // If assembly directory is null, use current directory
                strFullFileName = Path.Combine(Directory.GetCurrentDirectory(), resourceFileName);
            }
            else
            {
                // Form the full path given the directory provided by user
                strFullFileName = Path.Combine(m_assemblyData.m_strDir, resourceFileName);
            }
            strFullFileName = Path.GetFullPath(resourceFileName);
            new FileIOPermission(FileIOPermissionAccess.Read, strFullFileName).Demand();
            
            if (File.Exists(strFullFileName) == false)
                throw new FileNotFoundException(Environment.GetResourceString(
                    "IO.FileNotFound_FileName",
                    resourceFileName), resourceFileName);
            m_assemblyData.m_strResourceFileName = strFullFileName;
        }
        

        
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
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
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
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void SetEntryPoint(
            MethodInfo  entryMethod) 
        {
            SetEntryPoint(entryMethod, PEFileKinds.ConsoleApplication);
        }
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
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
                throw new ArgumentNullException("entryMethod");
            Contract.EndContractBlock();

            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.SetEntryPoint");

            Module tmpModule = entryMethod.Module;
            if (tmpModule == null || !InternalAssembly.Equals(tmpModule.Assembly))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EntryMethodNotDefinedInAssembly"));

            m_assemblyData.m_entryPointMethod = entryMethod;
            m_assemblyData.m_peFileKind = fileKind;
            
#if !FEATURE_CORECLR
            // Setting the entry point
            ModuleBuilder tmpMB = tmpModule as ModuleBuilder;
            if (tmpMB != null)
                m_assemblyData.m_entryPointModule = tmpMB;
            else
                m_assemblyData.m_entryPointModule = GetModuleBuilder((InternalModuleBuilder)tmpModule);

            MethodToken entryMethodToken = m_assemblyData.m_entryPointModule.GetMethodToken(entryMethod);
            m_assemblyData.m_entryPointModule.SetEntryPoint(entryMethodToken);
#endif //!FEATURE_CORECLR
        }


        /**********************************************
        * Use this function if client decides to form the custom attribute blob themselves
        **********************************************/
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
    
            lock(SyncRoot)
            {
                SetCustomAttributeNoLock(con, binaryAttribute);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
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
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }
            Contract.EndContractBlock();

            lock(SyncRoot)
            {
                SetCustomAttributeNoLock(customBuilder);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
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
            
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Save(String assemblyFileName, 
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            lock(SyncRoot)
            {
                SaveNoLock(assemblyFileName, portableExecutableKind, imageFileMachine);
            }
        }

#if FEATURE_CORECLR
        private void SaveNoLock(String assemblyFileName, 
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            // AssemblyBuilderAccess.Save can never be set with FEATURE_CORECLR
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CantSaveTransientAssembly"));
        }
#else // FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        private void SaveNoLock(String assemblyFileName, 
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            if (assemblyFileName == null)
                throw new ArgumentNullException("assemblyFileName");
            if (assemblyFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "assemblyFileName");
            if (!String.Equals(assemblyFileName, Path.GetFileName(assemblyFileName)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSimpleFileName"), "assemblyFileName");
            Contract.EndContractBlock();

            int i;
            int         size;
            Type        type;
            TypeBuilder typeBuilder;
            ModuleBuilder modBuilder;
            String      strModFileName;
            ModuleBuilder assemblyModule;
            ResWriterData tempRes;
            int[]       tkAttrs = null;
            int[]       tkAttrs2 = null;
            ModuleBuilder onDiskAssemblyModule;

            BCLDebug.Log("DYNIL","## DYNIL LOGGING: AssemblyBuilder.Save( " + assemblyFileName + " )");

            String tmpVersionFile = null;

            try 
            {
                if (m_assemblyData.m_iCABuilder != 0)
                    tkAttrs = new int[m_assemblyData.m_iCABuilder];
                if ( m_assemblyData.m_iCAs != 0)
                    tkAttrs2 = new int[m_assemblyData.m_iCAs];
    
                if (m_assemblyData.m_isSaved == true)
                {
                    // assembly has been saved before!
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_AssemblyHasBeenSaved,
                        InternalAssembly.GetSimpleName()));
                }

                if ((m_assemblyData.m_access & AssemblyBuilderAccess.Save) != AssemblyBuilderAccess.Save)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CantSaveTransientAssembly"));
                }
    
                // Check if assembly info is supposed to be stored with one of the module files.
                assemblyModule = m_assemblyData.FindModuleWithFileName(assemblyFileName);
    
                if (assemblyModule != null)
                {
                    m_onDiskAssemblyModuleBuilder = assemblyModule;

                    // In memory this module is not the manifest module and has a valid file token
                    // On disk it will be the manifest module so lets clean the file token
                    // We should not retrieve FileToken after the assembly has been saved
                    // If that is absolutely necessary, we need two separate fields on ModuleBuilderData:
                    // the in memory file token and the on disk file token.
                    assemblyModule.m_moduleData.FileToken = 0;
                }
                else
                {   // If assembly is to be stored alone, then no file name should conflict with it.
                    // This check will ensure resource file names are different assembly file name.
                    m_assemblyData.CheckFileNameConflict(assemblyFileName);
                }
    
                if (m_assemblyData.m_strDir == null)
                {
                    // set it to current directory
                    m_assemblyData.m_strDir = Directory.GetCurrentDirectory();
                }
                else if (Directory.Exists(m_assemblyData.m_strDir) == false)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectory", 
                        m_assemblyData.m_strDir));
                }
    
                // after this point, assemblyFileName is the full path name.
                assemblyFileName = Path.Combine(m_assemblyData.m_strDir, assemblyFileName);
                assemblyFileName = Path.GetFullPath(assemblyFileName);
    
                // Check caller has the right to create the assembly file itself.
                new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Append, assemblyFileName).Demand();
    
                // 1. setup/create the IMetaDataAssemblyEmit for the on disk version
                if (assemblyModule != null)
                {
                    // prepare saving CAs on assembly def. We need to introduce the MemberRef for
                    // the CA's type first of all. This is for the case the we have embedded manifest.
                    // We need to introduce these MRs before we call PreSave where we will snap
                    // into a ondisk metadata. If we do it after this, the ondisk metadata will
                    // not contain the proper MRs.
                    //
                    for (i=0; i < m_assemblyData.m_iCABuilder; i++)
                    {
                        tkAttrs[i] = m_assemblyData.m_CABuilders[i].PrepareCreateCustomAttributeToDisk(
                            assemblyModule); 
                    }
                    for (i=0; i < m_assemblyData.m_iCAs; i++)
                    {
                        tkAttrs2[i] = assemblyModule.InternalGetConstructorToken(m_assemblyData.m_CACons[i], true).Token;
                    }
                    assemblyModule.PreSave(assemblyFileName, portableExecutableKind, imageFileMachine);
                }
    
                RuntimeModule runtimeAssemblyModule = (assemblyModule != null) ? assemblyModule.ModuleHandle.GetRuntimeModule() : null;
                PrepareForSavingManifestToDisk(GetNativeHandle(), runtimeAssemblyModule);

                // This function will return the embedded manifest module, an already exposed ModuleBuilder
                // created by user, or make the stand alone manifest module exposed through managed code.
                //
                onDiskAssemblyModule = GetOnDiskAssemblyModuleBuilder();
    
                // Set any native resources on the OnDiskAssemblyModule.
                if (m_assemblyData.m_strResourceFileName != null)
                    onDiskAssemblyModule.DefineUnmanagedResourceFileInternalNoLock(m_assemblyData.m_strResourceFileName);
                else if (m_assemblyData.m_resourceBytes != null)
                    onDiskAssemblyModule.DefineUnmanagedResourceInternalNoLock(m_assemblyData.m_resourceBytes);
                else if (m_assemblyData.m_hasUnmanagedVersionInfo == true)
                {
                    // calculate unmanaged version info from assembly's custom attributes
                    m_assemblyData.FillUnmanagedVersionInfo();
    
                    String strFileVersion = m_assemblyData.m_nativeVersion.m_strFileVersion;
                    if (strFileVersion == null)
                        strFileVersion = GetVersion().ToString();
    
                    // Create the file.
                    CreateVersionInfoResource(
                         assemblyFileName,
                         m_assemblyData.m_nativeVersion.m_strTitle,   // title
                         null, // Icon filename
                         m_assemblyData.m_nativeVersion.m_strDescription,   // description
                         m_assemblyData.m_nativeVersion.m_strCopyright,
                         m_assemblyData.m_nativeVersion.m_strTrademark,
                         m_assemblyData.m_nativeVersion.m_strCompany,
                         m_assemblyData.m_nativeVersion.m_strProduct,
                         m_assemblyData.m_nativeVersion.m_strProductVersion,
                         strFileVersion, 
                         m_assemblyData.m_nativeVersion.m_lcid,
                         m_assemblyData.m_peFileKind == PEFileKinds.Dll,
                         JitHelpers.GetStringHandleOnStack(ref tmpVersionFile));
    
                    onDiskAssemblyModule.DefineUnmanagedResourceFileInternalNoLock(tmpVersionFile);
                }
    
                if (assemblyModule == null)
                {
                
                    // This is for introducing the MRs for CA's type. This case is for stand alone
                    // manifest. We need to wait till PrepareForSavingManifestToDisk is called. 
                    // That will trigger the creation of the on-disk stand alone manifest module.
                    //
                    for (i=0; i < m_assemblyData.m_iCABuilder; i++)
                    {
                        tkAttrs[i] = m_assemblyData.m_CABuilders[i].PrepareCreateCustomAttributeToDisk(
                            onDiskAssemblyModule); 
                    }
                    for (i=0; i < m_assemblyData.m_iCAs; i++)
                    {
                        tkAttrs2[i] = onDiskAssemblyModule.InternalGetConstructorToken(m_assemblyData.m_CACons[i], true).Token;
                    }
                }
            
                // 2. save all of the persistable modules contained by this AssemblyBuilder except the module that is going to contain
                // Assembly information
                // 
                // 3. create the file list in the manifest and track the file token. If it is embedded assembly,
                // the assembly file should not be on the file list.
                // 
                size = m_assemblyData.m_moduleBuilderList.Count;
                for (i = 0; i < size; i++) 
                {
                    ModuleBuilder mBuilder = (ModuleBuilder) m_assemblyData.m_moduleBuilderList[i];
                    if (mBuilder.IsTransient() == false && mBuilder != assemblyModule)
                    {
                        strModFileName = mBuilder.m_moduleData.m_strFileName;
                        if (m_assemblyData.m_strDir != null)
                        {
                            strModFileName = Path.Combine(m_assemblyData.m_strDir, strModFileName);
                            strModFileName = Path.GetFullPath(strModFileName);
                        }
                        
                        // Check caller has the right to create the Module file itself.
                        new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Append, strModFileName).Demand();

                        mBuilder.m_moduleData.FileToken = AddFile(GetNativeHandle(), mBuilder.m_moduleData.m_strFileName);
                        mBuilder.PreSave(strModFileName, portableExecutableKind, imageFileMachine);
                        mBuilder.Save(strModFileName, false, portableExecutableKind, imageFileMachine);
    
                        // Cannot set the hash value when creating the file since the file token
                        // is needed to created the entries for the embedded resources in the
                        // module and the resources need to be there before you figure the hash.
                        SetFileHashValue(GetNativeHandle(), mBuilder.m_moduleData.FileToken, strModFileName);
                    }
                }
        
                // 4. Add the public ComType
                for (i=0; i < m_assemblyData.m_iPublicComTypeCount; i++)
                {
                    type = m_assemblyData.m_publicComTypeList[i];
                    // If the type that was added as a Public Com Type was obtained via Reflection,
                    //  it will be a System.RuntimeType, even if it was really, at the same time,
                    //  a TypeBuilder.  Unfortunately, you can't get back to the TypeBuilder, so 
                    //  this code has to deal with either-or.
                    if (type is RuntimeType)
                    {
                        // If type is a runtime type, it must be a baked TypeBuilder,
                        // ttype.Module should be an InternalModuleBuilder

                        InternalModuleBuilder internalMB = (InternalModuleBuilder)type.Module;
                        modBuilder = this.GetModuleBuilder(internalMB);
                        if (modBuilder != assemblyModule)
                            DefineNestedComType(type, modBuilder.m_moduleData.FileToken, type.MetadataToken);
                    }
                    else
                    {
                        // Could assert that "type" is a TypeBuilder, but next statement throws if it isn't.
                        typeBuilder = (TypeBuilder) type;
                        // If type is a TypeBuilder, type.Module must be a ModuleBuilder.
                        modBuilder = typeBuilder.GetModuleBuilder();
                        if (modBuilder != assemblyModule)
                            DefineNestedComType(type, modBuilder.m_moduleData.FileToken, typeBuilder.MetadataTokenInternal);
                    }
                }
    
                // 5. write AssemblyDef's CAs (only if we are not saving directly the manifest module itself)
                if (onDiskAssemblyModule != m_manifestModuleBuilder)
                {
                    for (i = 0; i < m_assemblyData.m_iCABuilder; i++)
                    {
                        m_assemblyData.m_CABuilders[i].CreateCustomAttribute(
                            onDiskAssemblyModule,
                            AssemblyBuilderData.m_tkAssembly,   // This is the AssemblyDef token
                            tkAttrs[i], true);
                    }

                    for (i = 0; i < m_assemblyData.m_iCAs; i++)
                    {
                        TypeBuilder.DefineCustomAttribute(
                            onDiskAssemblyModule,               // pass in the in-memory assembly module
                            AssemblyBuilderData.m_tkAssembly,   // This is the AssemblyDef token
                            tkAttrs2[i],
                            m_assemblyData.m_CABytes[i],
                            true, false);
                    }
                }
    
                // 6. write security permission requests to the manifest.
#pragma warning disable 618
                if (m_assemblyData.m_RequiredPset != null)
                    AddDeclarativeSecurity(m_assemblyData.m_RequiredPset, SecurityAction.RequestMinimum);

                if (m_assemblyData.m_RefusedPset != null)
                    AddDeclarativeSecurity(m_assemblyData.m_RefusedPset, SecurityAction.RequestRefuse);

                if (m_assemblyData.m_OptionalPset != null)
                    AddDeclarativeSecurity(m_assemblyData.m_OptionalPset, SecurityAction.RequestOptional);
#pragma warning restore 618

                // 7. Save the stand alone managed resources
                size = m_assemblyData.m_resWriterList.Count;
                for (i = 0; i < size; i++)
                {
                    tempRes = null;
    
                    try
                    {
                        tempRes = (ResWriterData)m_assemblyData.m_resWriterList[i];
    
                        // If the user added an existing resource to the manifest, the
                        // corresponding ResourceWriter will be null.
                        if (tempRes.m_resWriter != null)
                            // Check caller has the right to create the Resource file itself.
                            new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Append, tempRes.m_strFullFileName).Demand();
                    }
                    finally
                    {
                        if (tempRes != null && tempRes.m_resWriter != null)
                            tempRes.m_resWriter.Close();
                    }
    
                    // Add entry to manifest for this stand alone resource
                    AddStandAloneResource(GetNativeHandle(), tempRes.m_strName, tempRes.m_strFileName, tempRes.m_strFullFileName, (int)tempRes.m_attribute);
                }
    
                // Save now!!
                if (assemblyModule == null)
                {
                    onDiskAssemblyModule.DefineNativeResource(portableExecutableKind, imageFileMachine);
    
                    // Stand alone manifest
                    int entryPoint = (m_assemblyData.m_entryPointModule != null) ? m_assemblyData.m_entryPointModule.m_moduleData.FileToken : 0;

                    SaveManifestToDisk(GetNativeHandle(), assemblyFileName, entryPoint, (int)m_assemblyData.m_peFileKind,
                            (int)portableExecutableKind, (int)imageFileMachine);
                    }
                else
                {
                    // embedded manifest
                    
                    // If the module containing the entry point is not the manifest file, we need to
                    // let the manifest file point to the module which contains the entry point.
                    // 
                    // 
                    // 
                    // 
                    if (m_assemblyData.m_entryPointModule != null && m_assemblyData.m_entryPointModule != assemblyModule)
                        assemblyModule.SetEntryPoint(new MethodToken(m_assemblyData.m_entryPointModule.m_moduleData.FileToken));
                    assemblyModule.Save(assemblyFileName, true, portableExecutableKind, imageFileMachine);
                }
                m_assemblyData.m_isSaved = true;
            }
            finally
            {
                if (tmpVersionFile != null)
                {
                    // Delete file.
                    System.IO.File.Delete(tmpVersionFile);
                }
            }
        }
#endif // FEATURE_CORECLR
    
#if FEATURE_CAS_POLICY
        [System.Security.SecurityCritical]  // auto-generated
        private void AddDeclarativeSecurity(PermissionSet pset, SecurityAction action)
        {
            // Translate sets into internal encoding (uses standard binary serialization).
            byte[] blob = pset.EncodeXml();
            AddDeclarativeSecurity(GetNativeHandle(), action, blob, blob.Length);
        }
#endif // FEATURE_CAS_POLICY
        
        internal bool IsPersistable()
        {
#if !FEATURE_CORECLR // AssemblyBuilderAccess.Save is never set in CoreCLR
            if ((m_assemblyData.m_access & AssemblyBuilderAccess.Save) == AssemblyBuilderAccess.Save)
            {
                return true;
            }
            else
#endif // FEATURE_CORECLR
            {
                return false;
            }
        }
    
        /**********************************************
        *
        * Internal helper to walk the nested type hierachy
        *
        **********************************************/
        [System.Security.SecurityCritical]  // auto-generated
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

        [System.Security.SecurityCritical]  // auto-generated
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

#if !FEATURE_CORECLR
        void _AssemblyBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _AssemblyBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _AssemblyBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _AssemblyBuilder.Invoke in VM\DangerousAPIs.h and 
        // include _AssemblyBuilder in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _AssemblyBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

        // Create a new module in which to emit code. This module will not contain the manifest.
        [System.Security.SecurityCritical]  // auto-generated
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

        [System.Security.SecurityCritical]  // auto-generated
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
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void PrepareForSavingManifestToDisk(RuntimeAssembly assembly, RuntimeModule assemblyModule);  // module to contain assembly information if assembly is embedded

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void SaveManifestToDisk(RuntimeAssembly assembly,
                                                String strFileName, 
                                                int entryPoint,
                                                int fileKind,
                                                int portableExecutableKind, 
                                                int ImageFileMachine);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddFile(RuntimeAssembly assembly, String strFileName);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void SetFileHashValue(RuntimeAssembly assembly,
                                                    int tkFile, 
                                                    String strFullFileName);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddExportedTypeInMemory(RuntimeAssembly assembly,
                                                          String strComTypeName,
                                                          int tkAssemblyRef,
                                                          int tkTypeDef,
                                                          TypeAttributes flags);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern int AddExportedTypeOnDisk(RuntimeAssembly assembly, 
                                                        String strComTypeName, 
                                                        int tkAssemblyRef, 
                                                        int tkTypeDef, 
                                                        TypeAttributes flags);

        // Add an entry to assembly's manifestResource table for a stand alone resource.
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void AddStandAloneResource(RuntimeAssembly assembly,
                                                         String strName,
                                                         String strFileName,
                                                         String strFullFileName,
                                                         int attribute);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
#pragma warning disable 618
        static private extern void AddDeclarativeSecurity(RuntimeAssembly assembly, SecurityAction action, byte[] blob, int length);
#pragma warning restore 618

        // Functions for defining unmanaged resources.
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static private extern void CreateVersionInfoResource(String filename, String title, String iconFilename, String description,
                                                             String copyright, String trademark, String company, String product,
                                                             String productVersion, String fileVersion, int lcid, bool isDll,
                                                             StringHandleOnStack retFileName);
    }
}
