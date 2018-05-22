// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Reflection.Emit
{
    // This is a package private class. This class hold all of the managed
    // data member for ModuleBuilder. Note that what ever data members added to
    // this class cannot be accessed from the EE.
    internal class ModuleBuilderData
    {
        internal ModuleBuilderData(ModuleBuilder module, string strModuleName, string strFileName, int tkFile)
        {
            m_globalTypeBuilder = new TypeBuilder(module);
            m_module = module;
            m_tkFile = tkFile;

            InitNames(strModuleName, strFileName);
        }

        // Initialize module and file names.
        private void InitNames(string strModuleName, string strFileName)
        {
            m_strModuleName = strModuleName;
            if (strFileName == null)
            {
                // fake a transient module file name
                m_strFileName = strModuleName;
            }
            else
            {
                string strExtension = Path.GetExtension(strFileName);
                if (strExtension == null || strExtension == string.Empty)
                {
                    // This is required by our loader. It cannot load module file that does not have file extension.
                    throw new ArgumentException(SR.Format(SR.Argument_NoModuleFileExtension, strFileName));
                }
                m_strFileName = strFileName;
            }
        }

        internal string m_strModuleName;     // scope name (can be different from file name)
        internal string m_strFileName;
        internal bool m_fGlobalBeenCreated;
        internal bool m_fHasGlobal;
        internal TypeBuilder m_globalTypeBuilder;
        internal ModuleBuilder m_module;

        private int m_tkFile;
        internal bool m_isSaved;
        internal const string MULTI_BYTE_VALUE_CLASS = "$ArrayType$";
        internal string m_strResourceFileName;
        internal byte[] m_resourceBytes;
    }
}
