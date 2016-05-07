// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
// 

namespace System.Reflection.Emit {
    using System;
    using IList = System.Collections.IList;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics;
    using CultureInfo = System.Globalization.CultureInfo;
#if !FEATURE_CORECLR
    using ResourceWriter = System.Resources.ResourceWriter;
#else // FEATURE_CORECLR
    using IResourceWriter = System.Resources.IResourceWriter;
#endif // !FEATURE_CORECLR
    using System.IO;
    using System.Runtime.Versioning;
    using System.Diagnostics.SymbolStore;
    using System.Diagnostics.Contracts;

    // This is a package private class. This class hold all of the managed
    // data member for AssemblyBuilder. Note that what ever data members added to
    // this class cannot be accessed from the EE.
    internal class AssemblyBuilderData
    {
        [SecurityCritical]
        internal AssemblyBuilderData(
            InternalAssemblyBuilder assembly, 
            String                  strAssemblyName, 
            AssemblyBuilderAccess   access,
            String                  dir)
        {
            m_assembly = assembly;
            m_strAssemblyName = strAssemblyName;
            m_access = access;
            m_moduleBuilderList = new List<ModuleBuilder>();
            m_resWriterList = new List<ResWriterData>();

            //Init to null/0 done for you by the CLR.  FXCop has spoken

            if (dir == null && access != AssemblyBuilderAccess.Run)
                m_strDir = Environment.CurrentDirectory;
            else
                m_strDir = dir;

            m_peFileKind = PEFileKinds.Dll;
         }
    
        // Helper to add a dynamic module into the tracking list
        internal void AddModule(ModuleBuilder dynModule)
        {
            m_moduleBuilderList.Add(dynModule);
        }
    
        // Helper to add a resource information into the tracking list
        internal void AddResWriter(ResWriterData resData)
        {
            m_resWriterList.Add(resData);
        }


        // Helper to track CAs to persist onto disk
        internal void AddCustomAttribute(CustomAttributeBuilder customBuilder)
        {

            // make sure we have room for this CA
            if (m_CABuilders == null)
            {
                m_CABuilders = new CustomAttributeBuilder[m_iInitialSize];
            }
            if (m_iCABuilder == m_CABuilders.Length)
            {
                CustomAttributeBuilder[]  tempCABuilders = new CustomAttributeBuilder[m_iCABuilder * 2];
                Array.Copy(m_CABuilders, 0, tempCABuilders, 0, m_iCABuilder);
                m_CABuilders = tempCABuilders;            
            }
            m_CABuilders[m_iCABuilder] = customBuilder;
            
            m_iCABuilder++;
        }

        // Helper to track CAs to persist onto disk
        internal void AddCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {

            // make sure we have room for this CA
            if (m_CABytes == null)
            {
                m_CABytes = new byte[m_iInitialSize][];
                m_CACons = new ConstructorInfo[m_iInitialSize];                
            }
            if (m_iCAs == m_CABytes.Length)
            {
                // enlarge the arrays
                byte[][]  temp = new byte[m_iCAs * 2][];
                ConstructorInfo[] tempCon = new ConstructorInfo[m_iCAs * 2];
                for (int i=0; i < m_iCAs; i++)
                {
                    temp[i] = m_CABytes[i];
                    tempCon[i] = m_CACons[i];
                }
                m_CABytes = temp;
                m_CACons = tempCon;
            }

            byte[] attrs = new byte[binaryAttribute.Length];
            Buffer.BlockCopy(binaryAttribute, 0, attrs, 0, binaryAttribute.Length);
            m_CABytes[m_iCAs] = attrs;
            m_CACons[m_iCAs] = con;
            m_iCAs++;
        }

        // Helper to calculate unmanaged version info from Assembly's custom attributes.
        // If DefineUnmanagedVersionInfo is called, the parameter provided will override
        // the CA's value.
        //                      
        [System.Security.SecurityCritical]  // auto-generated
        internal void FillUnmanagedVersionInfo()
        {
            // Get the lcid set on the assembly name as default if available
            // Note that if LCID is not avaible from neither AssemblyName or AssemblyCultureAttribute,
            // it is default to -1 which is treated as language neutral. 
            //
            CultureInfo locale = m_assembly.GetLocale();
#if FEATURE_USE_LCID            
            if (locale != null)
                m_nativeVersion.m_lcid = locale.LCID;
#endif            
                                                    
            for (int i = 0; i < m_iCABuilder; i++)
            {
                // check for known attributes
                Type conType = m_CABuilders[i].m_con.DeclaringType;
                if (m_CABuilders[i].m_constructorArgs.Length == 0 || m_CABuilders[i].m_constructorArgs[0] == null)
                    continue;

                if (conType.Equals(typeof(System.Reflection.AssemblyCopyrightAttribute)))
                {
                        // assert that we should only have one argument for this CA and the type should
                        // be a string.
                        // 
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strCopyright = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyTrademarkAttribute))) 
                {
                        // assert that we should only have one argument for this CA and the type should
                        // be a string.
                        // 
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strTrademark = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyProductAttribute))) 
                {
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        // assert that we should only have one argument for this CA and the type should
                        // be a string.
                        // 
                        m_nativeVersion.m_strProduct = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyCompanyAttribute))) 
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        // assert that we should only have one argument for this CA and the type should
                        // be a string.
                        // 
                        m_nativeVersion.m_strCompany = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyDescriptionAttribute))) 
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    m_nativeVersion.m_strDescription = m_CABuilders[i].m_constructorArgs[0].ToString();
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyTitleAttribute))) 
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    m_nativeVersion.m_strTitle = m_CABuilders[i].m_constructorArgs[0].ToString();
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyInformationalVersionAttribute))) 
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strProductVersion = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyCultureAttribute))) 
                {
                    // retrieve the LCID
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    // CultureInfo attribute overrides the lcid from AssemblyName.                                      
                    CultureInfo culture = new CultureInfo(m_CABuilders[i].m_constructorArgs[0].ToString());
#if FEATURE_USE_LCID                    
                    m_nativeVersion.m_lcid = culture.LCID;
#endif
                }
                else if (conType.Equals(typeof(System.Reflection.AssemblyFileVersionAttribute))) 
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_BadCAForUnmngRSC",
                            m_CABuilders[i].m_con.ReflectedType.Name));
                    }
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strFileVersion = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
            }
        }

        
        // Helper to ensure the resource name is unique underneath assemblyBuilder
        internal void CheckResNameConflict(String strNewResName)
        {
            int         size = m_resWriterList.Count;
            int         i;
            for (i = 0; i < size; i++) 
            {
                ResWriterData resWriter = m_resWriterList[i];
                if (resWriter.m_strName.Equals(strNewResName))
                {
                    // Cannot have two resources with the same name
                    throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateResourceName"));
                }
            }        
        }

    
        // Helper to ensure the module name is unique underneath assemblyBuilder
        internal void CheckNameConflict(String strNewModuleName)
        {
            int         size = m_moduleBuilderList.Count;
            int         i;
            for (i = 0; i < size; i++) 
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName.Equals(strNewModuleName))
                {
                    // Cannot have two dynamic modules with the same name
                    throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateModuleName"));
                }
            }

            // Right now dynamic modules can only be added to dynamic assemblies in which
            // all modules are dynamic. Otherwise we would also need to check the static modules 
            // with this:
            //  if (m_assembly.GetModule(strNewModuleName) != null)
            //  {
            //      // Cannot have two dynamic modules with the same name
            //      throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateModuleName"));
            //  }
        }
    
        // Helper to ensure the type name is unique underneath assemblyBuilder
        internal void CheckTypeNameConflict(String strTypeName, TypeBuilder enclosingType)
        {
            BCLDebug.Log("DYNIL","## DYNIL LOGGING: AssemblyBuilderData.CheckTypeNameConflict( " + strTypeName + " )"); 
            for (int i = 0; i < m_moduleBuilderList.Count; i++) 
            {
                ModuleBuilder curModule = m_moduleBuilderList[i];
                curModule.CheckTypeNameConflict(strTypeName, enclosingType);
            }

            // Right now dynamic modules can only be added to dynamic assemblies in which
            // all modules are dynamic. Otherwise we would also need to check loaded types.
            // We only need to make this test for non-nested types since any
            // duplicates in nested types will be caught at the top level.
            //      if (enclosingType == null && m_assembly.GetType(strTypeName, false, false) != null)
            //      {
            //          // Cannot have two types with the same name
            //          throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateTypeName"));
            //      }
        }
        

        // Helper to ensure the file name is unique underneath assemblyBuilder. This includes 
        // modules and resources.
        //  
        //  
        //  
        internal void CheckFileNameConflict(String strFileName)
        {
            int         size = m_moduleBuilderList.Count;
            int         i;
            for (i = 0; i < size; i++) 
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strFileName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // Cannot have two dynamic module with the same name
                        throw new ArgumentException(Environment.GetResourceString("Argument_DuplicatedFileName"));
                    }
                }
            }    
            size = m_resWriterList.Count;
            for (i = 0; i < size; i++) 
            {
                ResWriterData resWriter = m_resWriterList[i];
                if (resWriter.m_strFileName != null)
                {
                    if (String.Compare(resWriter.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // Cannot have two dynamic module with the same name
                        throw new ArgumentException(Environment.GetResourceString("Argument_DuplicatedFileName"));
                    }
                }
            }    

        }
    
        // Helper to look up which module that assembly is supposed to be stored at
        //  
        // 
        //  
        internal ModuleBuilder FindModuleWithFileName(String strFileName)
        {
            int         size = m_moduleBuilderList.Count;
            int         i;
            for (i = 0; i < size; i++) 
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strFileName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return moduleBuilder;
                    }
                }
            } 
            return null;       
        }

        // Helper to look up module by name.
        //  
        // 
        //  
        internal ModuleBuilder FindModuleWithName(String strName)
        {
            int         size = m_moduleBuilderList.Count;
            int         i;
            for (i = 0; i < size; i++) 
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strModuleName, strName, StringComparison.OrdinalIgnoreCase) == 0)
                        return moduleBuilder;
                }
            } 
            return null;       
        }

        
        // add type to public COMType tracking list
        internal void AddPublicComType(Type type)
        {
            BCLDebug.Log("DYNIL","## DYNIL LOGGING: AssemblyBuilderData.AddPublicComType( " + type.FullName + " )"); 
            if (m_isSaved == true)
            {
                // assembly has been saved before!
                throw new InvalidOperationException(Environment.GetResourceString(
                    "InvalidOperation_CannotAlterAssembly"));
            }        
            EnsurePublicComTypeCapacity();
            m_publicComTypeList[m_iPublicComTypeCount] = type;
            m_iPublicComTypeCount++;
        }
        
        // add security permission requests
        internal void AddPermissionRequests(
            PermissionSet       required,
            PermissionSet       optional,
            PermissionSet       refused)
        {
            BCLDebug.Log("DYNIL","## DYNIL LOGGING: AssemblyBuilderData.AddPermissionRequests");
            if (m_isSaved == true)
            {
                // assembly has been saved before!
                throw new InvalidOperationException(Environment.GetResourceString(
                    "InvalidOperation_CannotAlterAssembly"));
            }        
            m_RequiredPset = required;
            m_OptionalPset = optional;
            m_RefusedPset = refused;
        }
        
        private void EnsurePublicComTypeCapacity()
        {
            if (m_publicComTypeList == null)
            {
                m_publicComTypeList = new Type[m_iInitialSize];
            }
            if (m_iPublicComTypeCount == m_publicComTypeList.Length)
            {
                Type[]  tempTypeList = new Type[m_iPublicComTypeCount * 2];
                Array.Copy(m_publicComTypeList, 0, tempTypeList, 0, m_iPublicComTypeCount);
                m_publicComTypeList = tempTypeList;            
            }
        }
         
        internal List<ModuleBuilder>    m_moduleBuilderList;
        internal List<ResWriterData>    m_resWriterList;
        internal String                 m_strAssemblyName;
        internal AssemblyBuilderAccess  m_access;
        private InternalAssemblyBuilder m_assembly;
        
        internal Type[]                 m_publicComTypeList;
        internal int                    m_iPublicComTypeCount;
        
        internal bool                   m_isSaved;    
        internal const int              m_iInitialSize = 16;
        internal String                 m_strDir;

        // hard coding the assembly def token
        internal const int              m_tkAssembly = 0x20000001;
    
        // Security permission requests
        internal PermissionSet          m_RequiredPset;
        internal PermissionSet          m_OptionalPset;
        internal PermissionSet          m_RefusedPset;
        
        // tracking AssemblyDef's CAs for persistence to disk
        internal CustomAttributeBuilder[] m_CABuilders;
        internal int                    m_iCABuilder;
        internal byte[][]               m_CABytes;
        internal ConstructorInfo[]      m_CACons;
        internal int                    m_iCAs;
        internal PEFileKinds            m_peFileKind;           // assembly file kind
        internal MethodInfo             m_entryPointMethod;
        internal Assembly               m_ISymWrapperAssembly;

#if !FEATURE_CORECLR
        internal ModuleBuilder          m_entryPointModule;
#endif //!FEATURE_CORECLR
                                  
        // For unmanaged resources
        internal String                 m_strResourceFileName;
        internal byte[]                 m_resourceBytes;
        internal NativeVersionInfo      m_nativeVersion;
        internal bool                   m_hasUnmanagedVersionInfo;
        internal bool                   m_OverrideUnmanagedVersionInfo;

    }

    
    /**********************************************
    *
    * Internal structure to track the list of ResourceWriter for
    * AssemblyBuilder & ModuleBuilder.
    *
    **********************************************/
    internal class ResWriterData 
    {
#if FEATURE_CORECLR
        internal ResWriterData(
            IResourceWriter  resWriter,
            Stream          memoryStream,
            String          strName,
            String          strFileName,
            String          strFullFileName,
            ResourceAttributes attribute)
        {
            m_resWriter = resWriter;
            m_memoryStream = memoryStream;
            m_strName = strName;
            m_strFileName = strFileName;
            m_strFullFileName = strFullFileName;
            m_nextResWriter = null;
            m_attribute = attribute;
        }
#else
        internal ResWriterData(
            ResourceWriter  resWriter,
            Stream          memoryStream,
            String          strName,
            String          strFileName,
            String          strFullFileName,
            ResourceAttributes attribute)
        {
            m_resWriter = resWriter;
            m_memoryStream = memoryStream;
            m_strName = strName;
            m_strFileName = strFileName;
            m_strFullFileName = strFullFileName;
            m_nextResWriter = null;
            m_attribute = attribute;
        }
#endif
#if !FEATURE_CORECLR
        internal ResourceWriter         m_resWriter;
#else // FEATURE_CORECLR
         internal IResourceWriter         m_resWriter;
#endif // !FEATURE_CORECLR
        internal String                 m_strName;
        internal String                 m_strFileName;
        internal String                 m_strFullFileName;
        internal Stream                 m_memoryStream;
        internal ResWriterData          m_nextResWriter;
        internal ResourceAttributes     m_attribute;
    }

    internal class NativeVersionInfo
    {
        internal NativeVersionInfo()
        {
            m_strDescription = null;
            m_strCompany = null;
            m_strTitle = null;
            m_strCopyright = null;
            m_strTrademark = null;
            m_strProduct = null;
            m_strProductVersion = null;
            m_strFileVersion = null;
            m_lcid = -1;
        }
        
        internal String     m_strDescription;
        internal String     m_strCompany;           
        internal String     m_strTitle;
        internal String     m_strCopyright;         
        internal String     m_strTrademark;
        internal String     m_strProduct;           
        internal String     m_strProductVersion;        
        internal String     m_strFileVersion;
        internal int        m_lcid;
    }
}
