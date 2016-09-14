// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    internal class XmlSchemaSubstitutionGroup : XmlSchemaObject
    {
        private ArrayList _membersList = new ArrayList();
        private XmlQualifiedName _examplar = XmlQualifiedName.Empty;

        [XmlIgnore]
        internal ArrayList Members
        {
            get { return _membersList; }
        }

        [XmlIgnore]
        internal XmlQualifiedName Examplar
        {
            get { return _examplar; }
            set { _examplar = value; }
        }
    }

    internal class XmlSchemaSubstitutionGroupV1Compat : XmlSchemaSubstitutionGroup
    {
        private XmlSchemaChoice _choice = new XmlSchemaChoice();

        [XmlIgnore]
        internal XmlSchemaChoice Choice
        {
            get { return _choice; }
        }
    }
}
