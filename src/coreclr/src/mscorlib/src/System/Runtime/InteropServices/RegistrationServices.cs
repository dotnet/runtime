// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This class provides services for registering and unregistering
**          a managed server for use by COM.
**
**
**
**
** Change the way how to register and unregister a managed server
**
=============================================================================*/
namespace System.Runtime.InteropServices {
    
    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Flags]
    public enum RegistrationClassContext
    {
    
 
        InProcessServer                 = 0x1, 
        InProcessHandler                = 0x2, 
        LocalServer                     = 0x4, 
        InProcessServer16               = 0x8,
        RemoteServer                    = 0x10,
        InProcessHandler16              = 0x20,
        Reserved1                       = 0x40,
        Reserved2                       = 0x80,
        Reserved3                       = 0x100,
        Reserved4                       = 0x200,
        NoCodeDownload                  = 0x400,
        Reserved5                       = 0x800,
        NoCustomMarshal                 = 0x1000,
        EnableCodeDownload              = 0x2000,
        NoFailureLog                    = 0x4000,
        DisableActivateAsActivator      = 0x8000,
        EnableActivateAsActivator       = 0x10000,
        FromDefaultContext              = 0x20000
    }


    [Flags]
    public enum RegistrationConnectionType
    {
        SingleUse                = 0, 
        MultipleUse              = 1, 
        MultiSeparate            = 2, 
        Suspended                = 4, 
        Surrogate                = 8, 
    }

    [Guid("475E398F-8AFA-43a7-A3BE-F4EF8D6787C9")]
    [ClassInterface(ClassInterfaceType.None)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class RegistrationServices : IRegistrationServices
    {
        #region Constants

        private const String strManagedCategoryGuid = "{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}";
        private const String strDocStringPrefix = "";
        private const String strManagedTypeThreadingModel = "Both";
        private const String strComponentCategorySubKey = "Component Categories";
        private const String strManagedCategoryDescription = ".NET Category";
        private const String strImplementedCategoriesSubKey = "Implemented Categories";       
        private const String strMsCorEEFileName = "mscoree.dll";
        private const String strRecordRootName = "Record";      
        private const String strClsIdRootName = "CLSID";  
        private const String strTlbRootName = "TypeLib";
        private static Guid s_ManagedCategoryGuid = new Guid(strManagedCategoryGuid);

        #endregion

        
        #region IRegistrationServices

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags)
        {
            // Validate the arguments.
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
            Contract.EndContractBlock();

            RuntimeAssembly rtAssembly = assembly as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));

            // Retrieve the assembly names.
            String strAsmName = assembly.FullName;
            if (strAsmName == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmName"));

            // Retrieve the assembly codebase.
            String strAsmCodeBase = null;
            if ((flags & AssemblyRegistrationFlags.SetCodeBase) != 0)
            {
                strAsmCodeBase = rtAssembly.GetCodeBase(false);
                if (strAsmCodeBase == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmCodeBase"));
            }

            // Go through all the registerable types in the assembly and register them.
            Type[] aTypes = GetRegistrableTypesInAssembly(assembly);
            int NumTypes = aTypes.Length;

            String strAsmVersion = rtAssembly.GetVersion().ToString();
            
            // Retrieve the runtime version used to build the assembly.
            String strRuntimeVersion = assembly.ImageRuntimeVersion;

            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
            {
                if (IsRegisteredAsValueType(aTypes[cTypes]))
                    RegisterValueType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);
                else if (TypeRepresentsComType(aTypes[cTypes]))
                    RegisterComImportedType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);
                else
                    RegisterManagedType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);

                CallUserDefinedRegistrationMethod(aTypes[cTypes], true);
            }

            // If this assembly has the PIA attribute, then register it as a PIA.
            Object[] aPIAAttrs = assembly.GetCustomAttributes(typeof(PrimaryInteropAssemblyAttribute), false);
            int NumPIAAttrs = aPIAAttrs.Length;
            for (int cPIAAttrs = 0; cPIAAttrs < NumPIAAttrs; cPIAAttrs++)
                RegisterPrimaryInteropAssembly(rtAssembly, strAsmCodeBase, (PrimaryInteropAssemblyAttribute)aPIAAttrs[cPIAAttrs]);

            // Return value indicating if we actually registered any types.
            if (aTypes.Length > 0 || NumPIAAttrs > 0)
                return true;
            else 
                return false;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual bool UnregisterAssembly(Assembly assembly)
        {
            // Validate the arguments.
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
            Contract.EndContractBlock();

            RuntimeAssembly rtAssembly = assembly as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));

            bool bAllVersionsGone = true;

            // Go through all the registrable types in the assembly and register them.
            Type[] aTypes = GetRegistrableTypesInAssembly(assembly);
            int NumTypes = aTypes.Length;

            // Retrieve the assembly version
            String strAsmVersion = rtAssembly.GetVersion().ToString();
            for (int cTypes = 0;cTypes < NumTypes;cTypes++)
            {
                CallUserDefinedRegistrationMethod(aTypes[cTypes], false);

                if (IsRegisteredAsValueType(aTypes[cTypes]))
                {
                    if (!UnregisterValueType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
                else if (TypeRepresentsComType(aTypes[cTypes]))
                {
                    if (!UnregisterComImportedType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
                else
                {
                    if (!UnregisterManagedType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
            }

            // If this assembly has the PIA attribute, then unregister it as a PIA.
            Object[] aPIAAttrs = assembly.GetCustomAttributes(typeof(PrimaryInteropAssemblyAttribute),false);
            int NumPIAAttrs = aPIAAttrs.Length;
            if (bAllVersionsGone)
            {
                for (int cPIAAttrs = 0;cPIAAttrs < NumPIAAttrs;cPIAAttrs++)
                    UnregisterPrimaryInteropAssembly(assembly, (PrimaryInteropAssemblyAttribute)aPIAAttrs[cPIAAttrs]);
            }

            // Return value indicating if we actually un-registered any types.
            if (aTypes.Length > 0 || NumPIAAttrs > 0)
                return true;
            else 
                return false;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual Type[] GetRegistrableTypesInAssembly(Assembly assembly)
        {
            // Validate the arguments.
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();

            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");

            // Retrieve the list of types in the assembly.
            Type[] aTypes = assembly.GetExportedTypes();
            int NumTypes = aTypes.Length;

            // Create an array list that will be filled in.
            ArrayList TypeList = new ArrayList();

            // Register all the types that require registration.
            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
            {
                Type CurrentType = aTypes[cTypes];
                if (TypeRequiresRegistration(CurrentType))
                    TypeList.Add(CurrentType);
            }

            // Copy the array list to an array and return it.
            Type[] RetArray = new Type[TypeList.Count];
            TypeList.CopyTo(RetArray);
            return RetArray;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual String GetProgIdForType(Type type)
        {
            return Marshal.GenerateProgIdForType(type);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void RegisterTypeForComClients(Type type, ref Guid g)
        {
#if FEATURE_COMINTEROP_MANAGED_ACTIVATION
            if(type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            if((type as RuntimeType) == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"),"type");
            if(!TypeRequiresRegistration(type))
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"),"type");
            
            // Call the native method to do CoRegisterClassObject
            RegisterTypeForComClientsNative(type, ref g);
#else // FEATURE_COMINTEROP_MANAGED_ACTIVATION
            throw new NotImplementedException("CoreCLR_REMOVED -- managed activation removed");
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION
        }

        public virtual Guid GetManagedCategoryGuid()
        {
            return s_ManagedCategoryGuid;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual bool TypeRequiresRegistration(Type type)
        {
            return TypeRequiresRegistrationHelper(type);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual bool TypeRepresentsComType(Type type)
        {
            // If the type is not a COM import, then it does not represent a COM type.
            if (!type.IsCOMObject)
                return false;

            // If it is marked as tdImport, then it represents a COM type directly.
            if (type.IsImport)
                return true;

            // If the type is derived from a tdImport class and has the same GUID as the
            // imported class, then it represents a COM type.
            Type baseComImportType = GetBaseComImportType(type);
            Contract.Assert(baseComImportType != null, "baseComImportType != null");
            if (Marshal.GenerateGuidForType(type) == Marshal.GenerateGuidForType(baseComImportType))
                return true;

            return false;
        }

        #endregion

        
        #region Public methods not on IRegistrationServices
        [System.Security.SecurityCritical]  // auto-generated_required
        [ComVisible(false)]
        public virtual int RegisterTypeForComClients(Type type, RegistrationClassContext classContext, RegistrationConnectionType flags)
        {
#if FEATURE_COMINTEROP_MANAGED_ACTIVATION
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            if ((type as RuntimeType) == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"),"type");
            if (!TypeRequiresRegistration(type))
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"),"type");
            
            // Call the native method to do CoRegisterClassObject
            return RegisterTypeForComClientsExNative(type, classContext, flags);
#else // FEATURE_COMINTEROP_MANAGED_ACTIVATION
            throw new NotImplementedException("CoreCLR_REMOVED -- managed activation removed");
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [ComVisible(false)]
        public virtual void UnregisterTypeForComClients(int cookie)
        {
            // Call the native method to do CoRevokeClassObject.
            CoRevokeClassObject(cookie);
        }

        #endregion


        #region Internal helpers

        [System.Security.SecurityCritical]  // auto-generated_required
        internal static bool TypeRequiresRegistrationHelper(Type type)
        {
            // If the type is not a class or a value class, then it does not get registered.
            if (!type.IsClass && !type.IsValueType)
                return false;

            // If the type is abstract then it does not get registered.
            if (type.IsAbstract)
                return false;

            // If the does not have a public default constructor then is not creatable from COM so 
            // it does not require registration unless it is a value class.
            if (!type.IsValueType && type.GetConstructor(BindingFlags.Instance | BindingFlags.Public,null,Array.Empty<Type>(),null) == null)
                return false;

            // All other conditions are met so check to see if the type is visible from COM.
            return Marshal.IsTypeVisibleFromCom(type);
        }

        #endregion


        #region Private helpers

        [System.Security.SecurityCritical]  // auto-generated
        private void RegisterValueType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            // Retrieve some information that will be used during the registration process.
            String strRecordId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";           

            // Create the HKEY_CLASS_ROOT\Record key.
            using (RegistryKey RecordRootKey = Registry.ClassesRoot.CreateSubKey(strRecordRootName))
            {
                // Create the HKEY_CLASS_ROOT\Record\<RecordID> key.
                using (RegistryKey RecordKey = RecordRootKey.CreateSubKey(strRecordId))
                {
                    // Create the HKEY_CLASS_ROOT\Record\<RecordId>\<version> key.
                    using (RegistryKey RecordVersionKey = RecordKey.CreateSubKey(strAsmVersion))
                    {                   
                        // Set the class value.
                        RecordVersionKey.SetValue("Class", type.FullName);

                        // Set the assembly value.
                        RecordVersionKey.SetValue("Assembly", strAsmName);

                        // Set the runtime version value.
                        RecordVersionKey.SetValue("RuntimeVersion", strRuntimeVersion);

                        // Set the assembly code base value if a code base was specified.
                        if (strAsmCodeBase != null)
                            RecordVersionKey.SetValue("CodeBase", strAsmCodeBase);
                    }
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void RegisterManagedType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            //
            // Retrieve some information that will be used during the registration process.
            //

            String strDocString = strDocStringPrefix + type.FullName;
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strProgId = GetProgIdForType(type);


            //
            // Write the actual type information in the registry.
            //

            if (strProgId != String.Empty)
            {
                // Create the HKEY_CLASS_ROOT\<wzProgId> key.
                using (RegistryKey TypeNameKey = Registry.ClassesRoot.CreateSubKey(strProgId))
                {
                    TypeNameKey.SetValue("", strDocString);

                    // Create the HKEY_CLASS_ROOT\<wzProgId>\CLSID key.
                    using (RegistryKey ProgIdClsIdKey = TypeNameKey.CreateSubKey("CLSID"))
                    {
                        ProgIdClsIdKey.SetValue("", strClsId);
                    }
                }
            }

            // Create the HKEY_CLASS_ROOT\CLSID key.
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.CreateSubKey(strClsIdRootName))
            {           
                // Create the HKEY_CLASS_ROOT\CLSID\<CLSID> key.
                using (RegistryKey ClsIdKey = ClsIdRootKey.CreateSubKey(strClsId))
                {
                    ClsIdKey.SetValue("", strDocString);

                    // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\InprocServer32 key.
                    using (RegistryKey InProcServerKey = ClsIdKey.CreateSubKey("InprocServer32"))
                    {
                        InProcServerKey.SetValue("", strMsCorEEFileName);
                        InProcServerKey.SetValue("ThreadingModel", strManagedTypeThreadingModel);
                        InProcServerKey.SetValue("Class", type.FullName);
                        InProcServerKey.SetValue("Assembly", strAsmName);
                        InProcServerKey.SetValue("RuntimeVersion", strRuntimeVersion);
                        if (strAsmCodeBase != null)
                            InProcServerKey.SetValue("CodeBase", strAsmCodeBase);

                        // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\InprocServer32\<Version> subkey
                        using (RegistryKey VersionSubKey = InProcServerKey.CreateSubKey(strAsmVersion))
                        {
                            VersionSubKey.SetValue("Class", type.FullName);
                            VersionSubKey.SetValue("Assembly", strAsmName);
                            VersionSubKey.SetValue("RuntimeVersion", strRuntimeVersion);
                            if (strAsmCodeBase != null)
                                VersionSubKey.SetValue("CodeBase", strAsmCodeBase);
                        }

                        if (strProgId != String.Empty)
                        {
                            // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\ProdId key.
                            using (RegistryKey ProgIdKey = ClsIdKey.CreateSubKey("ProgId"))
                            {
                                ProgIdKey.SetValue("", strProgId);
                            }
                        }
                    }

                    // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\Implemented Categories\<Managed Category Guid> key.
                    using (RegistryKey CategoryKey = ClsIdKey.CreateSubKey(strImplementedCategoriesSubKey))
                    {
                        using (RegistryKey ManagedCategoryKey = CategoryKey.CreateSubKey(strManagedCategoryGuid)) {}
                    }
                }
            }


            //
            // Ensure that the managed category exists.
            //

            EnsureManagedCategoryExists();
        } 
        
        [System.Security.SecurityCritical]  // auto-generated
        private void RegisterComImportedType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            // Retrieve some information that will be used during the registration process.
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";

            // Create the HKEY_CLASS_ROOT\CLSID key.
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.CreateSubKey(strClsIdRootName))
            {
                // Create the HKEY_CLASS_ROOT\CLSID\<CLSID> key.
                using (RegistryKey ClsIdKey = ClsIdRootKey.CreateSubKey(strClsId))
                {
                    // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\InProcServer32 key.
                    using (RegistryKey InProcServerKey = ClsIdKey.CreateSubKey("InprocServer32"))
                    {              
                        // Set the class value.
                        InProcServerKey.SetValue("Class", type.FullName);

                        // Set the assembly value.
                        InProcServerKey.SetValue("Assembly", strAsmName);

                        // Set the runtime version value.
                        InProcServerKey.SetValue("RuntimeVersion", strRuntimeVersion);

                        // Set the assembly code base value if a code base was specified.
                        if (strAsmCodeBase != null)
                            InProcServerKey.SetValue("CodeBase", strAsmCodeBase);

                        // Create the HKEY_CLASS_ROOT\CLSID\<CLSID>\InprocServer32\<Version> subkey
                        using (RegistryKey VersionSubKey = InProcServerKey.CreateSubKey(strAsmVersion))
                        {
                            VersionSubKey.SetValue("Class", type.FullName);
                            VersionSubKey.SetValue("Assembly", strAsmName);
                            VersionSubKey.SetValue("RuntimeVersion", strRuntimeVersion);
                            if (strAsmCodeBase != null)
                                VersionSubKey.SetValue("CodeBase", strAsmCodeBase);
                        }
                    }
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private bool UnregisterValueType(Type type, String strAsmVersion)
        {
            bool bAllVersionsGone = true;

            // Try to open the HKEY_CLASS_ROOT\Record key.
            String strRecordId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            
            using (RegistryKey RecordRootKey = Registry.ClassesRoot.OpenSubKey(strRecordRootName, true))
            {
                if (RecordRootKey != null)
                {
                    // Open the HKEY_CLASS_ROOT\Record\{RecordId} key.
                    using (RegistryKey RecordKey = RecordRootKey.OpenSubKey(strRecordId,true))
                    {
                        if (RecordKey != null)
                        {
                            using (RegistryKey VersionSubKey = RecordKey.OpenSubKey(strAsmVersion,true))
                            {
                                if (VersionSubKey != null)
                                {
                                    // Delete the values we created.
                                    VersionSubKey.DeleteValue("Assembly",false);
                                    VersionSubKey.DeleteValue("Class",false);
                                    VersionSubKey.DeleteValue("CodeBase",false);
                                    VersionSubKey.DeleteValue("RuntimeVersion",false);

                                    // delete the version sub key if no value or subkeys under it
                                    if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                        RecordKey.DeleteSubKey(strAsmVersion);
                                }
                            }

                            // If there are sub keys left then there are versions left.
                            if (RecordKey.SubKeyCount != 0)
                                bAllVersionsGone = false;

                            // If there are no other values or subkeys then we can delete the HKEY_CLASS_ROOT\Record\{RecordId}.
                            if ((RecordKey.SubKeyCount == 0) && (RecordKey.ValueCount == 0))
                                RecordRootKey.DeleteSubKey(strRecordId);
                        }
                    }

                    // If there are no other values or subkeys then we can delete the HKEY_CLASS_ROOT\Record.
                    if ((RecordRootKey.SubKeyCount == 0) && (RecordRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strRecordRootName);
                }
            }

            return bAllVersionsGone;
        }

        // UnregisterManagedType
        //
        // Return :
        //      true:   All versions are gone.
        //      false:  Some versions are still left in registry
        [System.Security.SecurityCritical]  // auto-generated
        private bool UnregisterManagedType(Type type,String strAsmVersion)
        {
            bool bAllVersionsGone = true;
            
            //
            // Create the CLSID string.
            //

            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strProgId = GetProgIdForType(type);


            //
            // Remove the entries under HKEY_CLASS_ROOT\CLSID key.
            //

            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.OpenSubKey(strClsIdRootName, true))
            {
                if (ClsIdRootKey != null)
                {
                    //
                    // Remove the entries under HKEY_CLASS_ROOT\CLSID\<CLSID> key.
                    //

                    using (RegistryKey ClsIdKey = ClsIdRootKey.OpenSubKey(strClsId, true))
                    {
                        if (ClsIdKey != null)
                        {
                            //
                            // Remove the entries in the HKEY_CLASS_ROOT\CLSID\<CLSID>\InprocServer32 key.
                            //

                            using (RegistryKey InProcServerKey = ClsIdKey.OpenSubKey("InprocServer32", true))
                            {
                                if (InProcServerKey != null)
                                {
                                    //
                                    // Remove the entries in HKEY_CLASS_ROOT\CLSID\<CLSID>\InprocServer32\<Version>
                                    //

                                    using (RegistryKey VersionSubKey = InProcServerKey.OpenSubKey(strAsmVersion, true))
                                    {
                                        if (VersionSubKey != null)
                                        {
                                            // Delete the values we created
                                            VersionSubKey.DeleteValue("Assembly",false);
                                            VersionSubKey.DeleteValue("Class",false);
                                            VersionSubKey.DeleteValue("RuntimeVersion",false);
                                            VersionSubKey.DeleteValue("CodeBase",false);

                                            // If there are no other values or subkeys then we can delete the VersionSubKey.
                                            if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                                InProcServerKey.DeleteSubKey(strAsmVersion);
                                        }
                                    }

                                    // If there are sub keys left then there are versions left.
                                    if (InProcServerKey.SubKeyCount != 0)
                                        bAllVersionsGone = false;

                                    // If there are no versions left, then delete the threading model and default value.
                                    if (bAllVersionsGone)
                                    {
                                        InProcServerKey.DeleteValue("",false);
                                        InProcServerKey.DeleteValue("ThreadingModel",false);
                                    }

                                    InProcServerKey.DeleteValue("Assembly",false);
                                    InProcServerKey.DeleteValue("Class",false);
                                    InProcServerKey.DeleteValue("RuntimeVersion",false);
                                    InProcServerKey.DeleteValue("CodeBase",false);

                                    // If there are no other values or subkeys then we can delete the InProcServerKey.
                                    if ((InProcServerKey.SubKeyCount == 0) && (InProcServerKey.ValueCount == 0))
                                        ClsIdKey.DeleteSubKey("InprocServer32");
                                }
                            }

                            // remove HKEY_CLASS_ROOT\CLSID\<CLSID>\ProgId
                            // and HKEY_CLASS_ROOT\CLSID\<CLSID>\Implemented Category
                            // only when all versions are removed
                            if (bAllVersionsGone)
                            {
                                // Delete the value we created.
                                ClsIdKey.DeleteValue("",false);

                                if (strProgId != String.Empty)
                                {
                                    //
                                    // Remove the entries in the HKEY_CLASS_ROOT\CLSID\<CLSID>\ProgId key.
                                    //

                                    using (RegistryKey ProgIdKey = ClsIdKey.OpenSubKey("ProgId", true))
                                    {
                                        if (ProgIdKey != null)
                                        {
                                            // Delete the value we created.
                                            ProgIdKey.DeleteValue("",false);

                                            // If there are no other values or subkeys then we can delete the ProgIdSubKey.
                                            if ((ProgIdKey.SubKeyCount == 0) && (ProgIdKey.ValueCount == 0))
                                                ClsIdKey.DeleteSubKey("ProgId");
                                        }
                                    }
                                }
                
            
                                //
                                // Remove entries in the  HKEY_CLASS_ROOT\CLSID\<CLSID>\Implemented Categories\<Managed Category Guid> key.
                                //
        
                                using (RegistryKey CategoryKey = ClsIdKey.OpenSubKey(strImplementedCategoriesSubKey, true))
                                {
                                    if (CategoryKey != null)
                                    {
                                        using (RegistryKey ManagedCategoryKey = CategoryKey.OpenSubKey(strManagedCategoryGuid, true))
                                        {
                                            if (ManagedCategoryKey != null)
                                            {
                                                // If there are no other values or subkeys then we can delete the ManagedCategoryKey.
                                                if ((ManagedCategoryKey.SubKeyCount == 0) && (ManagedCategoryKey.ValueCount == 0))
                                                    CategoryKey.DeleteSubKey(strManagedCategoryGuid);
                                            }
                                        }

                                        // If there are no other values or subkeys then we can delete the CategoryKey.
                                        if ((CategoryKey.SubKeyCount == 0) && (CategoryKey.ValueCount == 0))
                                            ClsIdKey.DeleteSubKey(strImplementedCategoriesSubKey);
                                    }
                                }
                            }

                            // If there are no other values or subkeys then we can delete the ClsIdKey.
                            if ((ClsIdKey.SubKeyCount == 0) && (ClsIdKey.ValueCount == 0))
                                ClsIdRootKey.DeleteSubKey(strClsId);
                        }
                    }

                    // If there are no other values or subkeys then we can delete the CLSID key.
                    if ((ClsIdRootKey.SubKeyCount == 0) && (ClsIdRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strClsIdRootName);
                }
            

                //
                // Remove the entries under HKEY_CLASS_ROOT\<wzProgId> key.
                //

                if (bAllVersionsGone)
                {
                    if (strProgId != String.Empty)
                    {
                        using (RegistryKey TypeNameKey = Registry.ClassesRoot.OpenSubKey(strProgId, true))
                        {                            
                            if (TypeNameKey != null)
                            {
                                // Delete the values we created.
                                TypeNameKey.DeleteValue("",false);


                                //
                                // Remove the entries in the HKEY_CLASS_ROOT\<wzProgId>\CLSID key.
                                //

                                using (RegistryKey ProgIdClsIdKey = TypeNameKey.OpenSubKey("CLSID", true))
                                {
                                    if (ProgIdClsIdKey != null)
                                    {
                                        // Delete the values we created.
                                        ProgIdClsIdKey.DeleteValue("",false);

                                        // If there are no other values or subkeys then we can delete the ProgIdClsIdKey.
                                        if ((ProgIdClsIdKey.SubKeyCount == 0) && (ProgIdClsIdKey.ValueCount == 0))
                                            TypeNameKey.DeleteSubKey("CLSID");
                                    }
                                }

                                // If there are no other values or subkeys then we can delete the TypeNameKey.
                                if ((TypeNameKey.SubKeyCount == 0) && (TypeNameKey.ValueCount == 0))
                                    Registry.ClassesRoot.DeleteSubKey(strProgId);
                            }
                        }
                    }
                }
            }

            return bAllVersionsGone;
        }

        // UnregisterComImportedType
        // Return:
        //      true:      All version information are gone.
        //      false:     There are still some version left in registry
        [System.Security.SecurityCritical]  // auto-generated
        private bool UnregisterComImportedType(Type type, String strAsmVersion)
        {
            bool bAllVersionsGone = true;
            
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
        
            // Try to open the HKEY_CLASS_ROOT\CLSID key.
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.OpenSubKey(strClsIdRootName, true))
            {
                if (ClsIdRootKey != null)
                {
                    // Try to open the HKEY_CLASS_ROOT\CLSID\<CLSID> key.
                    using (RegistryKey ClsIdKey = ClsIdRootKey.OpenSubKey(strClsId, true))
                    {
                        if (ClsIdKey != null)
                        {
                            // Try to open the HKEY_CLASS_ROOT\CLSID\<CLSID>\InProcServer32 key.
                            using (RegistryKey InProcServerKey = ClsIdKey.OpenSubKey("InprocServer32", true))
                            {
                                if (InProcServerKey != null)
                                {
                                    // Delete the values we created.
                                    InProcServerKey.DeleteValue("Assembly",false);
                                    InProcServerKey.DeleteValue("Class",false);
                                    InProcServerKey.DeleteValue("RuntimeVersion",false);
                                    InProcServerKey.DeleteValue("CodeBase",false);
                                
                                    // Try to open the entries in HKEY_CLASS_ROOT\CLSID\<CLSID>\InProcServer32\<Version>
                                    using (RegistryKey VersionSubKey = InProcServerKey.OpenSubKey(strAsmVersion,true))
                                    {
                                        if (VersionSubKey != null)
                                        {
                                            // Delete the value we created
                                            VersionSubKey.DeleteValue("Assembly",false);
                                            VersionSubKey.DeleteValue("Class",false);
                                            VersionSubKey.DeleteValue("RuntimeVersion",false);
                                            VersionSubKey.DeleteValue("CodeBase",false);

                                            // If there are no other values or subkeys then we can delete the VersionSubKey
                                            if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                                InProcServerKey.DeleteSubKey(strAsmVersion);
                                        }
                                    }

                                    // If there are sub keys left then there are versions left.
                                    if (InProcServerKey.SubKeyCount != 0)
                                        bAllVersionsGone = false;

                                    // If there are no other values or subkeys then we can delete the InProcServerKey.
                                    if ((InProcServerKey.SubKeyCount == 0) && (InProcServerKey.ValueCount == 0))
                                        ClsIdKey.DeleteSubKey("InprocServer32");
                                }
                            }

                            // If there are no other values or subkeys then we can delete the ClsIdKey.
                            if ((ClsIdKey.SubKeyCount == 0) && (ClsIdKey.ValueCount == 0))
                                ClsIdRootKey.DeleteSubKey(strClsId);                            
                        }                       
                    }

                    // If there are no other values or subkeys then we can delete the CLSID key.
                    if ((ClsIdRootKey.SubKeyCount == 0) && (ClsIdRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strClsIdRootName);                    
                }
            }

            return bAllVersionsGone;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void RegisterPrimaryInteropAssembly(RuntimeAssembly assembly, String strAsmCodeBase, PrimaryInteropAssemblyAttribute attr)
        {
            // Validate that the PIA has a strong name.
            if (assembly.GetPublicKey().Length == 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_PIAMustBeStrongNamed"));

            String strTlbId = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strVersion = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);

            // Create the HKEY_CLASS_ROOT\TypeLib key.
            using (RegistryKey TypeLibRootKey = Registry.ClassesRoot.CreateSubKey(strTlbRootName))
            {
                // Create the HKEY_CLASS_ROOT\TypeLib\<TLBID> key.
                using (RegistryKey TypeLibKey = TypeLibRootKey.CreateSubKey(strTlbId))
                {
                    // Create the HKEY_CLASS_ROOT\TypeLib\<TLBID>\<Major.Minor> key.
                    using (RegistryKey VersionSubKey = TypeLibKey.CreateSubKey(strVersion))
                    {
                        // Create the HKEY_CLASS_ROOT\TypeLib\<TLBID>\PrimaryInteropAssembly key.
                        VersionSubKey.SetValue("PrimaryInteropAssemblyName", assembly.FullName);
                        if (strAsmCodeBase != null)
                            VersionSubKey.SetValue("PrimaryInteropAssemblyCodeBase", strAsmCodeBase);
                    }
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void UnregisterPrimaryInteropAssembly(Assembly assembly, PrimaryInteropAssemblyAttribute attr)
        {
            String strTlbId = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strVersion = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);

            // Try to open the HKEY_CLASS_ROOT\TypeLib key.
            using (RegistryKey TypeLibRootKey = Registry.ClassesRoot.OpenSubKey(strTlbRootName, true))
            {
                if (TypeLibRootKey != null)
                {
                    // Try to open the HKEY_CLASS_ROOT\TypeLib\<TLBID> key.
                    using (RegistryKey TypeLibKey = TypeLibRootKey.OpenSubKey(strTlbId, true))
                    {
                        if (TypeLibKey != null)
                        {
                            // Try to open the HKEY_CLASS_ROOT\TypeLib<TLBID>\<Major.Minor> key.
                            using (RegistryKey VersionSubKey = TypeLibKey.OpenSubKey(strVersion, true))
                            {
                                if (VersionSubKey != null)
                                {
                                    // Delete the values we created.
                                    VersionSubKey.DeleteValue("PrimaryInteropAssemblyName",false);
                                    VersionSubKey.DeleteValue("PrimaryInteropAssemblyCodeBase",false);

                                    // If there are no other values or subkeys then we can delete the VersionKey.
                                    if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                        TypeLibKey.DeleteSubKey(strVersion);
                                }
                            }

                            // If there are no other values or subkeys then we can delete the TypeLibKey.
                            if ((TypeLibKey.SubKeyCount == 0) && (TypeLibKey.ValueCount == 0))
                                TypeLibRootKey.DeleteSubKey(strTlbId);                            
                        }
                    }

                    // If there are no other values or subkeys then we can delete the TypeLib key.
                    if ((TypeLibRootKey.SubKeyCount == 0) && (TypeLibRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strTlbRootName);                    
                }
            }
        }

        private void EnsureManagedCategoryExists()
        {
            if (!ManagedCategoryExists())
            {
                // Create the HKEY_CLASS_ROOT\Component Category key.
                using (RegistryKey ComponentCategoryKey = Registry.ClassesRoot.CreateSubKey(strComponentCategorySubKey))
                {
                    // Create the HKEY_CLASS_ROOT\Component Category\<Managed Category Guid> key.
                    using (RegistryKey ManagedCategoryKey = ComponentCategoryKey.CreateSubKey(strManagedCategoryGuid))
                    {
                        ManagedCategoryKey.SetValue("0", strManagedCategoryDescription);
                    }
                }
            }
        }

        private static bool ManagedCategoryExists()
        {
            using (RegistryKey componentCategoryKey = Registry.ClassesRoot.OpenSubKey(strComponentCategorySubKey, 
#if FEATURE_MACL
                                                                                      RegistryKeyPermissionCheck.ReadSubTree))
#else
                                                                                      false))
#endif
            {
                if (componentCategoryKey == null)
                    return false;
                using (RegistryKey managedCategoryKey = componentCategoryKey.OpenSubKey(strManagedCategoryGuid,
#if FEATURE_MACL
                                                                                        RegistryKeyPermissionCheck.ReadSubTree))
#else
                                                                                        false))
#endif
                {
                    if (managedCategoryKey == null)
                        return false;
                    object value = managedCategoryKey.GetValue("0");
                    if (value == null || value.GetType() != typeof(string))
                        return false;
                    string stringValue = (string)value;
                    if (stringValue != strManagedCategoryDescription)
                        return false;
                }
            }
            
            return true;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        private void CallUserDefinedRegistrationMethod(Type type, bool bRegister)
        {
            bool bFunctionCalled = false;

            // Retrieve the attribute type to use to determine if a function is the requested user defined
            // registration function.
            Type RegFuncAttrType = null;
            if(bRegister)
                RegFuncAttrType = typeof(ComRegisterFunctionAttribute);
            else 
                RegFuncAttrType = typeof(ComUnregisterFunctionAttribute);

            for(Type currType = type; !bFunctionCalled && currType != null; currType = currType.BaseType)
            {
                // Retrieve all the methods.
                MethodInfo[] aMethods = currType.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                int NumMethods = aMethods.Length;

                // Go through all the methods and check for the ComRegisterMethod custom attribute.
                for(int cMethods = 0;cMethods < NumMethods;cMethods++)
                {
                    MethodInfo CurrentMethod = aMethods[cMethods];

                    // Check to see if the method has the custom attribute.
                    if(CurrentMethod.GetCustomAttributes(RegFuncAttrType, true).Length != 0)
                    {
                        // Check to see if the method is static before we call it.
                        if(!CurrentMethod.IsStatic)
                        {
                            if(bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComRegFunction",CurrentMethod.Name,currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComUnRegFunction",CurrentMethod.Name,currType.Name));
                        }

                        // Finally check that the signature is string ret void.
                        ParameterInfo[] aParams = CurrentMethod.GetParameters();
                        if (CurrentMethod.ReturnType != typeof(void) || 
                            aParams == null ||
                            aParams.Length != 1 || 
                            (aParams[0].ParameterType != typeof(String) && aParams[0].ParameterType != typeof(Type)))
                        {
                            if(bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComRegFunctionSig",CurrentMethod.Name,currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComUnRegFunctionSig",CurrentMethod.Name,currType.Name));
                        }

                        // There can only be one register and one unregister function per type.
                        if(bFunctionCalled)
                        {
                            if(bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComRegFunctions",currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComUnRegFunctions",currType.Name));
                        }

                        // The function is valid so set up the arguments to call it.
                        Object[] objs = new Object[1];
                        if(aParams[0].ParameterType == typeof(String))
                        {
                            // We are dealing with the string overload of the function.
                            objs[0] = "HKEY_CLASSES_ROOT\\CLSID\\{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
                        }
                        else
                        {
                            // We are dealing with the type overload of the function.
                            objs[0] = type;
                        }

                        // Invoke the COM register function.
                        CurrentMethod.Invoke(null, objs);

                        // Mark the function as having been called.
                        bFunctionCalled = true;
                    }
                }
            }
        }

        private Type GetBaseComImportType(Type type)
        {
            for (; type != null && !type.IsImport; type = type.BaseType);
            return type;
        }

        private bool IsRegisteredAsValueType(Type type)
        {
            if (!type.IsValueType)
                return false;

            return true;
        }

        #endregion

    
        #region FCalls and DllImports

#if FEATURE_COMINTEROP_MANAGED_ACTIVATION
        // GUID versioning can be controlled by using the GuidAttribute or 
        // letting the runtime generate it based on type and assembly strong name.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RegisterTypeForComClientsNative(Type type,ref Guid g);
        
        // GUID versioning can be controlled by using the GuidAttribute or 
        // letting the runtime generate it based on type and assembly strong name.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int RegisterTypeForComClientsExNative(Type t, RegistrationClassContext clsContext, RegistrationConnectionType flags);
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION

        [DllImport(Win32Native.OLE32,CharSet=CharSet.Auto,PreserveSig=false)]
        private static extern void CoRevokeClassObject(int cookie);
        #endregion
    }
}
