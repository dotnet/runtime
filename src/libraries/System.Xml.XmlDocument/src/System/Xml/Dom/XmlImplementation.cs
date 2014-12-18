// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    // Provides methods for performing operations that are independent of any
    // particular instance of the document object model.
    public class XmlImplementation
    {
        private XmlNameTable _nameTable;

        // Initializes a new instance of the XmlImplementation class.
        public XmlImplementation() : this(new NameTable())
        {
        }

        public XmlImplementation(XmlNameTable nt)
        {
            _nameTable = nt;
        }

        // Test if the DOM implementation implements a specific feature.
        public bool HasFeature(string strFeature, string strVersion)
        {
            if (String.Compare("XML", strFeature, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (strVersion == null || strVersion == "1.0" || strVersion == "2.0")
                    return true;
            }
            return false;
        }

        // Creates a new XmlDocument. All documents created from the same 
        // XmlImplementation object share the same name table.
        public virtual XmlDocument CreateDocument()
        {
            return new XmlDocument(this);
        }

        internal XmlNameTable NameTable
        {
            get { return _nameTable; }
        }
    }
}
