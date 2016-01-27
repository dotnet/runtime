// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Component that implements the ITypeLibConverter interface and
**          does the actual work of converting a typelib to metadata and
**          vice versa.
**
**
=============================================================================*/
#if !FEATURE_CORECLR // current implementation requires reflection only load 
namespace System.Runtime.InteropServices {
   
    using System;
    using System.Diagnostics.Contracts;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.InteropServices.TCEAdapterGen;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Configuration.Assemblies;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices.ComTypes;
    using System.Runtime.Versioning;
    using WORD = System.UInt16;
    using DWORD = System.UInt32;
    using _TYPELIBATTR = System.Runtime.InteropServices.ComTypes.TYPELIBATTR;

    [Guid("F1C3BF79-C3E4-11d3-88E7-00902754C43A")]
    [ClassInterface(ClassInterfaceType.None)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class TypeLibConverter : ITypeLibConverter
    {
        private const String s_strTypeLibAssemblyTitlePrefix = "TypeLib ";
        private const String s_strTypeLibAssemblyDescPrefix = "Assembly generated from typelib ";
        private const int MAX_NAMESPACE_LENGTH = 1024;


        //
        // ITypeLibConverter interface.
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] Object typeLib, 
                                                        String asmFileName,
                                                        int flags,
                                                        ITypeLibImporterNotifySink notifySink,
                                                        byte[] publicKey,
                                                        StrongNameKeyPair keyPair,
                                                        bool unsafeInterfaces)
        {
            return ConvertTypeLibToAssembly(typeLib,
                                            asmFileName,
                                            (unsafeInterfaces
                                                ? TypeLibImporterFlags.UnsafeInterfaces
                                                : 0),
                                            notifySink,
                                            publicKey,
                                            keyPair,
                                            null,
                                            null);
        }




        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] Object typeLib, 
                                                        String asmFileName,
                                                        TypeLibImporterFlags flags, 
                                                        ITypeLibImporterNotifySink notifySink,
                                                        byte[] publicKey,
                                                        StrongNameKeyPair keyPair,
                                                        String asmNamespace,
                                                        Version asmVersion)
        {
            // Validate the arguments.
            if (typeLib == null)
                throw new ArgumentNullException("typeLib");
            if (asmFileName == null)
                throw new ArgumentNullException("asmFileName");         
            if (notifySink == null)
                throw new ArgumentNullException("notifySink");
            if (String.Empty.Equals(asmFileName))
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileName"), "asmFileName");
            if (asmFileName.Length > Path.MaxPath)
                throw new ArgumentException(Environment.GetResourceString("IO.PathTooLong"), asmFileName);
            if ((flags & TypeLibImporterFlags.PrimaryInteropAssembly) != 0 && publicKey == null && keyPair == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_PIAMustBeStrongNamed"));
            Contract.EndContractBlock();

            ArrayList eventItfInfoList = null;

            // Determine the AssemblyNameFlags
            AssemblyNameFlags asmNameFlags = AssemblyNameFlags.None;
            
            // Retrieve the assembly name from the typelib.
            AssemblyName asmName = GetAssemblyNameFromTypelib(typeLib, asmFileName, publicKey, keyPair, asmVersion, asmNameFlags);

            // Create the dynamic assembly that will contain the converted typelib types.
            AssemblyBuilder asmBldr = CreateAssemblyForTypeLib(typeLib, asmFileName, asmName, 
                                        (flags & TypeLibImporterFlags.PrimaryInteropAssembly) != 0,
                                        (flags & TypeLibImporterFlags.ReflectionOnlyLoading) != 0,
                                        (flags & TypeLibImporterFlags.NoDefineVersionResource) != 0);

            // Define a dynamic module that will contain the contain the imported types.
            String strNonQualifiedAsmFileName = Path.GetFileName(asmFileName);
            ModuleBuilder modBldr = asmBldr.DefineDynamicModule(strNonQualifiedAsmFileName, strNonQualifiedAsmFileName);

            // If the namespace hasn't been specified, then use the assembly name.
            if (asmNamespace == null)
                asmNamespace = asmName.Name;

            // Create a type resolve handler that will also intercept resolve ref messages
            // on the sink interface to build up a list of referenced assemblies.
            TypeResolveHandler typeResolveHandler = new TypeResolveHandler(modBldr, notifySink);

            // Add a listener for the type resolve events.
            AppDomain currentDomain = Thread.GetDomain();
            ResolveEventHandler resolveHandler = new ResolveEventHandler(typeResolveHandler.ResolveEvent);
            ResolveEventHandler asmResolveHandler = new ResolveEventHandler(typeResolveHandler.ResolveAsmEvent);
            ResolveEventHandler ROAsmResolveHandler = new ResolveEventHandler(typeResolveHandler.ResolveROAsmEvent);
            currentDomain.TypeResolve += resolveHandler;
            currentDomain.AssemblyResolve += asmResolveHandler;
            currentDomain.ReflectionOnlyAssemblyResolve += ROAsmResolveHandler;

            // Convert the types contained in the typelib into metadata and add them to the assembly.
            nConvertTypeLibToMetadata(typeLib, asmBldr.InternalAssembly, modBldr.InternalModule, asmNamespace, flags, typeResolveHandler, out eventItfInfoList);

            // Update the COM types in the assembly.
            UpdateComTypesInAssembly(asmBldr, modBldr);

            // If there are any event sources then generate the TCE adapters.
            if (eventItfInfoList.Count > 0)
                new TCEAdapterGenerator().Process(modBldr, eventItfInfoList);

            // Remove the listener for the type resolve events.
            currentDomain.TypeResolve -= resolveHandler;
            currentDomain.AssemblyResolve -= asmResolveHandler;
            currentDomain.ReflectionOnlyAssemblyResolve -= ROAsmResolveHandler;

            // We have finished converting the typelib and now have a fully formed assembly.
            return asmBldr;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [return : MarshalAs(UnmanagedType.Interface)]
        public Object ConvertAssemblyToTypeLib(Assembly assembly, String strTypeLibName, TypeLibExporterFlags flags, ITypeLibExporterNotifySink notifySink)
        {
            RuntimeAssembly rtAssembly;
            AssemblyBuilder ab = assembly as AssemblyBuilder;
            if (ab != null)
                rtAssembly = ab.InternalAssembly;
            else
                rtAssembly = assembly as RuntimeAssembly;

            return nConvertAssemblyToTypeLib(rtAssembly, strTypeLibName, flags, notifySink);
        }

        public bool GetPrimaryInteropAssembly(Guid g, Int32 major, Int32 minor, Int32 lcid, out String asmName, out String asmCodeBase)
        {
            String strTlbId = "{" + g.ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strVersion = major.ToString("x", CultureInfo.InvariantCulture) + "." + minor.ToString("x", CultureInfo.InvariantCulture);

            // Set the two out values to null before we start.
            asmName = null;
            asmCodeBase = null;

            // Try to open the HKEY_CLASS_ROOT\TypeLib key.
            using (RegistryKey TypeLibKey = Registry.ClassesRoot.OpenSubKey("TypeLib", false))
            {
                if (TypeLibKey != null)
                {
                    // Try to open the HKEY_CLASS_ROOT\TypeLib\<TLBID> key.            
                    using (RegistryKey TypeLibSubKey = TypeLibKey.OpenSubKey(strTlbId))
                    {
                        if (TypeLibSubKey != null)
                        {
                            // Try to open the HKEY_CLASS_ROOT\TypeLib\<TLBID>\<Major.Minor> key.
                            using (RegistryKey VersionKey = TypeLibSubKey.OpenSubKey(strVersion, false))
                            {
                                if (VersionKey != null)
                                {
                                    // Attempt to retrieve the assembly name and codebase under the version key.
                                    asmName = (String)VersionKey.GetValue("PrimaryInteropAssemblyName");
                                    asmCodeBase = (String)VersionKey.GetValue("PrimaryInteropAssemblyCodeBase");
                                }
                            }
                        }
                    }
                }
            }
            
            // If the assembly name isn't null, then we found an PIA.
            return asmName != null;
        }


        //
        // Non native helper methods.
        //

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private static AssemblyBuilder CreateAssemblyForTypeLib(Object typeLib, String asmFileName, AssemblyName asmName, bool bPrimaryInteropAssembly, bool bReflectionOnly, bool bNoDefineVersionResource)
        {
            // Retrieve the current app domain.
            AppDomain currentDomain = Thread.GetDomain();

            // Retrieve the directory from the assembly file name.
            String dir = null;
            if (asmFileName != null)
            {
                dir = Path.GetDirectoryName(asmFileName);
                if (String.IsNullOrEmpty(dir))
                    dir = null;
            }

            AssemblyBuilderAccess aba;
            if (bReflectionOnly)
            {
                aba = AssemblyBuilderAccess.ReflectionOnly;
            }
            else
            {
                aba = AssemblyBuilderAccess.RunAndSave;
            }

            // Create the dynamic assembly itself.
            AssemblyBuilder asmBldr;

            List<CustomAttributeBuilder> assemblyAttributes = new List<CustomAttributeBuilder>();
#if !FEATURE_CORECLR
            // mscorlib.dll must specify the security rules that assemblies it emits are to use, since by
            // default all assemblies will follow security rule set level 2, and we want to make that an
            // explicit decision.
            ConstructorInfo securityRulesCtor = typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) });
            CustomAttributeBuilder securityRulesAttribute =
                new CustomAttributeBuilder(securityRulesCtor, new object[] { SecurityRuleSet.Level2 });
            assemblyAttributes.Add(securityRulesAttribute);
#endif // !FEATURE_CORECLR

            asmBldr = currentDomain.DefineDynamicAssembly(asmName, aba, dir, false, assemblyAttributes);

            // Set the Guid custom attribute on the assembly.
            SetGuidAttributeOnAssembly(asmBldr, typeLib);

            // Set the imported from COM attribute on the assembly and return it.
            SetImportedFromTypeLibAttrOnAssembly(asmBldr, typeLib);

            // Set the version information on the typelib.
            if (bNoDefineVersionResource)
            {
                SetTypeLibVersionAttribute(asmBldr, typeLib);
            }
            else
            {
            SetVersionInformation(asmBldr, typeLib, asmName);
            }

            // If we are generating a PIA, then set the PIA custom attribute.
            if (bPrimaryInteropAssembly)
                SetPIAAttributeOnAssembly(asmBldr, typeLib);

            return asmBldr;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static AssemblyName GetAssemblyNameFromTypelib(Object typeLib, String asmFileName, byte[] publicKey, StrongNameKeyPair keyPair, Version asmVersion, AssemblyNameFlags asmNameFlags)
        {
            // Extract the name of the typelib.
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0;
            String strHelpFile = null;
            ITypeLib pTLB = (ITypeLib)typeLib;
            pTLB.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);

            // Retrieve the name to use for the assembly.
            if (asmFileName == null)
            {
                asmFileName = strTypeLibName;
            }
            else
            {
                Contract.Assert((asmFileName != null) && (asmFileName.Length > 0), "The assembly file name cannot be an empty string!");

                String strFileNameNoPath = Path.GetFileName(asmFileName);
                String strExtension = Path.GetExtension(asmFileName);

                // Validate that the extension is valid.
                bool bExtensionValid = ".dll".Equals(strExtension, StringComparison.OrdinalIgnoreCase);

                // If the extension is not valid then tell the user and quit.
                if (!bExtensionValid)
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileExtension"));

                // The assembly cannot contain the path nor the extension.
                asmFileName = strFileNameNoPath.Substring(0, strFileNameNoPath.Length - ".dll".Length);
            }

            // If the version information was not specified, then retrieve it from the typelib.
            if (asmVersion == null)
            {
                int major;
                int minor;
                Marshal.GetTypeLibVersion(pTLB, out major, out minor);
                asmVersion = new Version(major, minor, 0, 0);
            }
            
            // Create the assembly name for the imported typelib's assembly.
            AssemblyName AsmName = new AssemblyName();
            AsmName.Init(
                asmFileName,
                publicKey,
                null,
                asmVersion,
                null,
                AssemblyHashAlgorithm.None,
                AssemblyVersionCompatibility.SameMachine,
                null,
                asmNameFlags,
                keyPair);

            return AsmName;
        }

        private static void UpdateComTypesInAssembly(AssemblyBuilder asmBldr, ModuleBuilder modBldr)
        {
            // Retrieve the AssemblyBuilderData associated with the assembly builder.
            AssemblyBuilderData AsmBldrData = asmBldr.m_assemblyData;

            // Go through the types in the module and add them as public COM types.
            Type[] aTypes = modBldr.GetTypes();
            int NumTypes = aTypes.Length;
            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
                AsmBldrData.AddPublicComType(aTypes[cTypes]);
        }


        [System.Security.SecurityCritical]  // auto-generated
        private static void SetGuidAttributeOnAssembly(AssemblyBuilder asmBldr, Object typeLib)
        {
            // Retrieve the GuidAttribute constructor.
            Type []aConsParams = new Type[1] {typeof(String)};
            ConstructorInfo GuidAttrCons = typeof(GuidAttribute).GetConstructor(aConsParams);

            // Create an instance of the custom attribute builder.
            Object[] aArgs = new Object[1] {Marshal.GetTypeLibGuid((ITypeLib)typeLib).ToString()};
            CustomAttributeBuilder GuidCABuilder = new CustomAttributeBuilder(GuidAttrCons, aArgs);

            // Set the GuidAttribute on the assembly builder.
            asmBldr.SetCustomAttribute(GuidCABuilder);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void SetImportedFromTypeLibAttrOnAssembly(AssemblyBuilder asmBldr, Object typeLib)
        {
            // Retrieve the ImportedFromTypeLibAttribute constructor.
            Type []aConsParams = new Type[1] {typeof(String)};
            ConstructorInfo ImpFromComAttrCons = typeof(ImportedFromTypeLibAttribute).GetConstructor(aConsParams);

            // Retrieve the name of the typelib.
            String strTypeLibName = Marshal.GetTypeLibName((ITypeLib)typeLib);

            // Create an instance of the custom attribute builder.
            Object[] aArgs = new Object[1] {strTypeLibName};
            CustomAttributeBuilder ImpFromComCABuilder = new CustomAttributeBuilder(ImpFromComAttrCons, aArgs);

            // Set the ImportedFromTypeLibAttribute on the assembly builder.
            asmBldr.SetCustomAttribute(ImpFromComCABuilder);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void SetTypeLibVersionAttribute(AssemblyBuilder asmBldr, Object typeLib)
        {
            Type []aConsParams = new Type[2] {typeof(int), typeof(int)};
            ConstructorInfo TypeLibVerCons = typeof(TypeLibVersionAttribute).GetConstructor(aConsParams);

            // Get the typelib version
            int major;
            int minor;
            Marshal.GetTypeLibVersion((ITypeLib)typeLib, out major, out minor);
            
            // Create an instance of the custom attribute builder.
            Object[] aArgs = new Object[2] {major, minor};
            CustomAttributeBuilder TypeLibVerBuilder = new CustomAttributeBuilder(TypeLibVerCons, aArgs);

            // Set the attribute on the assembly builder.
            asmBldr.SetCustomAttribute(TypeLibVerBuilder);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void SetVersionInformation(AssemblyBuilder asmBldr, Object typeLib, AssemblyName asmName)
        {
            // Extract the name of the typelib.
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0;
            String strHelpFile = null;
            ITypeLib pTLB = (ITypeLib)typeLib;
            pTLB.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);

            // Generate the product name string from the named of the typelib.
            String strProductName = String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("TypeLibConverter_ImportedTypeLibProductName"), strTypeLibName);

            // Set the OS version information.
            asmBldr.DefineVersionInfoResource(strProductName, asmName.Version.ToString(), null, null, null);

            // Set the TypeLibVersion attribute
            SetTypeLibVersionAttribute(asmBldr, typeLib);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void SetPIAAttributeOnAssembly(AssemblyBuilder asmBldr, Object typeLib)
        {
            IntPtr pAttr = IntPtr.Zero;
            _TYPELIBATTR Attr;
            ITypeLib pTLB = (ITypeLib)typeLib;
            int Major = 0;
            int Minor = 0;

            // Retrieve the PrimaryInteropAssemblyAttribute constructor.
            Type []aConsParams = new Type[2] {typeof(int), typeof(int)};
            ConstructorInfo PIAAttrCons = typeof(PrimaryInteropAssemblyAttribute).GetConstructor(aConsParams);

            // Retrieve the major and minor version from the typelib.
            try
            {
                pTLB.GetLibAttr(out pAttr);
                Attr = (_TYPELIBATTR)Marshal.PtrToStructure(pAttr, typeof(_TYPELIBATTR));
                Major = Attr.wMajorVerNum;
                Minor = Attr.wMinorVerNum;
            }
            finally
            {
                // Release the typelib attributes.
                if (pAttr != IntPtr.Zero)
                    pTLB.ReleaseTLibAttr(pAttr);
            }

            // Create an instance of the custom attribute builder.
            Object[] aArgs = new Object[2] {Major, Minor};
            CustomAttributeBuilder PIACABuilder = new CustomAttributeBuilder(PIAAttrCons, aArgs);

            // Set the PrimaryInteropAssemblyAttribute on the assembly builder.
            asmBldr.SetCustomAttribute(PIACABuilder);
        }


        //
        // Native helper methods.
        //

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void nConvertTypeLibToMetadata(Object typeLib, RuntimeAssembly asmBldr, RuntimeModule modBldr, String nameSpace, TypeLibImporterFlags flags, ITypeLibImporterNotifySink notifySink, out ArrayList eventItfInfoList);

        // Must use assembly versioning or GuidAttribute to avoid collisions in typelib export or registration.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern Object nConvertAssemblyToTypeLib(RuntimeAssembly assembly, String strTypeLibName, TypeLibExporterFlags flags, ITypeLibExporterNotifySink notifySink);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal extern static void LoadInMemoryTypeByName(RuntimeModule module, String className);

        //
        // Helper class called when a resolve type event is fired.
        //

        private class TypeResolveHandler : ITypeLibImporterNotifySink
        {
            public TypeResolveHandler(ModuleBuilder mod, ITypeLibImporterNotifySink userSink)
            {
                m_Module = mod;
                m_UserSink = userSink;
            }

            public void ReportEvent(ImporterEventKind eventKind, int eventCode, String eventMsg)
            {
                m_UserSink.ReportEvent(eventKind, eventCode, eventMsg);
            }

            public Assembly ResolveRef(Object typeLib)
            {
                Contract.Ensures(Contract.Result<Assembly>() != null && Contract.Result<Assembly>() is RuntimeAssembly);
                Contract.EndContractBlock();

                // Call the user sink to resolve the reference.
                Assembly asm = m_UserSink.ResolveRef(typeLib);

                if (asm == null)
                    throw new ArgumentNullException();

                // Return the resolved assembly. We extract the internal assembly because we are called
                // by the VM which accesses fields of the object directly and does not go via those
                // delegating properties (the fields are empty if asm is an (external) AssemblyBuilder).

                RuntimeAssembly rtAssembly = asm as RuntimeAssembly;
                if (rtAssembly == null)
                {
                    AssemblyBuilder ab = asm as AssemblyBuilder;
                    if (ab != null)
                        rtAssembly = ab.InternalAssembly;
                }

                if (rtAssembly == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));

                // Add the assembly to the list of assemblies.
                m_AsmList.Add(rtAssembly);

                return rtAssembly;
            }

            [System.Security.SecurityCritical]  // auto-generated
            public Assembly ResolveEvent(Object sender, ResolveEventArgs args)
            {
                // We need to load the type in the resolve event so that we will deal with
                // cases where we are trying to load the CoClass before the interface has 
                // been loaded.               
                try
                {
                    LoadInMemoryTypeByName(m_Module.GetNativeHandle(), args.Name); 
                    return m_Module.Assembly;
                }
                catch (TypeLoadException e)
                {
                    if (e.ResourceId != System.__HResults.COR_E_TYPELOAD)  // type not found
                        throw;
                }

                foreach (RuntimeAssembly asm in m_AsmList)
                {
                    try
                    {
                        asm.GetType(args.Name, true, false);
                        return asm;
                    }
                    catch (TypeLoadException e)
                    {
                        if (e._HResult != System.__HResults.COR_E_TYPELOAD)  // type not found
                            throw;
                    }
                }
                
                return null;
            }

            public Assembly ResolveAsmEvent(Object sender, ResolveEventArgs args)
            {
                foreach (RuntimeAssembly asm in m_AsmList)
                {
                    if (String.Compare(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase) == 0)
                        return asm;
                }

                return null;
            }

            public Assembly ResolveROAsmEvent(Object sender, ResolveEventArgs args)
            {
                foreach (RuntimeAssembly asm in m_AsmList)
                {
                    if (String.Compare(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase) == 0)
                        return asm;
                }

                // We failed to find the referenced assembly in our pre-loaded assemblies, so try to load it based on policy.
                string asmName = AppDomain.CurrentDomain.ApplyPolicy(args.Name);
                return Assembly.ReflectionOnlyLoad(asmName);
            }

            private ModuleBuilder m_Module;
            private ITypeLibImporterNotifySink m_UserSink;
            private List<RuntimeAssembly> m_AsmList = new List<RuntimeAssembly>();
        }
    }
}
#endif // !FEATURE_CORECLR // current implementation requires reflection only load

