// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
// 

namespace System.Reflection.Emit
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Versioning;

    // This is a package private class. This class hold all of the managed
    // data member for ModuleBuilder. Note that what ever data members added to
    // this class cannot be accessed from the EE.
    [Serializable]
    internal class ModuleBuilderData
    {
        [System.Security.SecurityCritical]  // auto-generated
        internal ModuleBuilderData(ModuleBuilder module, String strModuleName, String strFileName, int tkFile)
        {
            m_globalTypeBuilder = new TypeBuilder(module);
            m_module = module;
            m_tkFile = tkFile;

            InitNames(strModuleName, strFileName);
        }

        // Initialize module and file names.
        [System.Security.SecurityCritical]  // auto-generated
        private void InitNames(String strModuleName, String strFileName)
        {
            m_strModuleName = strModuleName;
            if (strFileName == null)
            {
                // fake a transient module file name
                m_strFileName = strModuleName;
            }
            else
            {
                String strExtension = Path.GetExtension(strFileName);
                if (strExtension == null || strExtension == String.Empty)
                {
                    // This is required by our loader. It cannot load module file that does not have file extension.
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoModuleFileExtension", strFileName));
                }
                m_strFileName = strFileName;
            }
        }

        // This is a method for changing module and file name of the manifest module (created by default for 
        // each assembly).
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual void ModifyModuleName(String strModuleName)
        {
            Contract.Assert(m_strModuleName == AssemblyBuilder.MANIFEST_MODULE_NAME, "Changing names for non-manifest module");
            InitNames(strModuleName, null /*strFileName*/);
        }

        internal int FileToken
        {
            get
            {
                // Before save, the scope of m_tkFile is the in-memory assembly manifest
                // During save, the scope of m_tkFile is the on-disk assembly manifest
                // For transient modules m_tkFile never change. 

                // Theoretically no one should emit anything after a dynamic assembly has
                // been saved. So m_tkFile shouldn't used when m_isSaved is true.
                // But that was never completely enforced: you can still emit everything after 
                // the assembly has been saved (except for public types in persistent modules).

                return m_tkFile;
            }

            set
            {
                m_tkFile = value;
            }
        }

        internal String        m_strModuleName;     // scope name (can be different from file name)
        internal String        m_strFileName;
        internal bool          m_fGlobalBeenCreated;
        internal bool          m_fHasGlobal;   
        [NonSerialized]
        internal TypeBuilder   m_globalTypeBuilder;
        [NonSerialized]
        internal ModuleBuilder m_module;

        private int            m_tkFile;
        internal bool          m_isSaved;
        [NonSerialized]
        internal ResWriterData m_embeddedRes;
        internal const String MULTI_BYTE_VALUE_CLASS = "$ArrayType$";
        internal String        m_strResourceFileName;
        internal byte[]        m_resourceBytes;
    } // class ModuleBuilderData
}
