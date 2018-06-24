// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
// 

namespace System.Reflection.Emit
{
    using System;
    using IList = System.Collections.IList;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.IO;
    using System.Runtime.Versioning;
    using System.Diagnostics.SymbolStore;

    // This is a package private class. This class hold all of the managed
    // data member for AssemblyBuilder. Note that what ever data members added to
    // this class cannot be accessed from the EE.
    internal class AssemblyBuilderData
    {
        internal AssemblyBuilderData(
            InternalAssemblyBuilder assembly,
            string strAssemblyName,
            AssemblyBuilderAccess access)
        {
            m_assembly = assembly;
            m_strAssemblyName = strAssemblyName;
            m_access = access;
            m_moduleBuilderList = new List<ModuleBuilder>();
            m_resWriterList = new List<ResWriterData>();

            m_peFileKind = PEFileKinds.Dll;
        }

        // Helper to add a dynamic module into the tracking list
        internal void AddModule(ModuleBuilder dynModule)
        {
            m_moduleBuilderList.Add(dynModule);
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
                CustomAttributeBuilder[] tempCABuilders = new CustomAttributeBuilder[m_iCABuilder * 2];
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
                byte[][] temp = new byte[m_iCAs * 2][];
                ConstructorInfo[] tempCon = new ConstructorInfo[m_iCAs * 2];
                for (int i = 0; i < m_iCAs; i++)
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

        // Helper to ensure the type name is unique underneath assemblyBuilder
        internal void CheckTypeNameConflict(string strTypeName, TypeBuilder enclosingType)
        {
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
            //          throw new ArgumentException(SR.Argument_DuplicateTypeName);
            //      }
        }

        internal List<ModuleBuilder> m_moduleBuilderList;
        internal List<ResWriterData> m_resWriterList;
        internal string m_strAssemblyName;
        internal AssemblyBuilderAccess m_access;
        private InternalAssemblyBuilder m_assembly;

        internal Type[] m_publicComTypeList;
        internal int m_iPublicComTypeCount;

        internal bool m_isSaved;
        internal const int m_iInitialSize = 16;

        // hard coding the assembly def token
        internal const int m_tkAssembly = 0x20000001;

        // tracking AssemblyDef's CAs for persistence to disk
        internal CustomAttributeBuilder[] m_CABuilders;
        internal int m_iCABuilder;
        internal byte[][] m_CABytes;
        internal ConstructorInfo[] m_CACons;
        internal int m_iCAs;
        internal PEFileKinds m_peFileKind;           // assembly file kind
        internal MethodInfo m_entryPointMethod;
        internal Assembly m_ISymWrapperAssembly;

        // For unmanaged resources
        internal string m_strResourceFileName;
        internal byte[] m_resourceBytes;
        internal NativeVersionInfo m_nativeVersion;
        internal bool m_hasUnmanagedVersionInfo;
        internal bool m_OverrideUnmanagedVersionInfo;
    }


    /**********************************************
    *
    * Internal structure to track the list of ResourceWriter for
    * AssemblyBuilder & ModuleBuilder.
    *
    **********************************************/
    internal class ResWriterData
    {
        internal string m_strName;
        internal string m_strFileName;
        internal string m_strFullFileName;
        internal Stream m_memoryStream;
        internal ResWriterData m_nextResWriter;
        internal ResourceAttributes m_attribute;
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

        internal string m_strDescription;
        internal string m_strCompany;
        internal string m_strTitle;
        internal string m_strCopyright;
        internal string m_strTrademark;
        internal string m_strProduct;
        internal string m_strProductVersion;
        internal string m_strFileVersion;
        internal int m_lcid;
    }
}
