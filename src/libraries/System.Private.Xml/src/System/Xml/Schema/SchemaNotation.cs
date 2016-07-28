// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Diagnostics;

    internal sealed class SchemaNotation
    {
        internal const int SYSTEM = 0;
        internal const int PUBLIC = 1;

        private XmlQualifiedName _name;
        private String _systemLiteral;   // System literal
        private String _pubid;    // pubid literal

        internal SchemaNotation(XmlQualifiedName name)
        {
            _name = name;
        }

        internal XmlQualifiedName Name
        {
            get { return _name; }
        }

        internal String SystemLiteral
        {
            get { return _systemLiteral; }
            set { _systemLiteral = value; }
        }

        internal String Pubid
        {
            get { return _pubid; }
            set { _pubid = value; }
        }
    };
}
