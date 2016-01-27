// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Provides a localizable way of retrieving a file
** that is linked into your assembly and/or satellite assembly
** while also leaving the file on disk for unmanaged tools.
**
** 
===========================================================*/

// Removing LooselyLinkedResourceReference from Whidbey.  We don't
// yet have any strong customer need for it yet.
#if LOOSELY_LINKED_RESOURCE_REFERENCE

namespace System.Resources {
    using System.Reflection;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Globalization;

    [Serializable]

[System.Runtime.InteropServices.ComVisible(true)]
    public struct LooselyLinkedResourceReference {
        private String _manifestResourceName;
        private String _typeName;

        public LooselyLinkedResourceReference(String looselyLinkedResourceName, String typeName)
        {
            if (looselyLinkedResourceName == null)
                throw new ArgumentNullException("looselyLinkedResourceName");
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            if (looselyLinkedResourceName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "looselyLinkedResourceName");
            if (typeName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "typeName");
            Contract.EndContractBlock();
            
            _manifestResourceName = looselyLinkedResourceName;
            _typeName = typeName;
        }

        public String LooselyLinkedResourceName { 
            get { return _manifestResourceName; }
        }

        public String TypeName {
            get { return _typeName; }
        }

        public Object Resolve(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();

            Stream data = assembly.GetManifestResourceStream(_manifestResourceName);
            if (data == null)
                throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_LooselyLinked", _manifestResourceName, assembly.FullName));

            Type type = Type.GetType(_typeName, true);
            
            Object obj = Activator.CreateInstance(type, new Object[] { data });
            return obj;
        }

        // For good debugging with tools like ResView
        public override String ToString()
        {
            // This is for debugging only.  Since we use the property names,
            // this does not need to be localized.
            return "LooselyLinkedResourceName = \""+ _manifestResourceName +"\", TypeName = \"" + _typeName + "\"";
        }
    }
}

#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
