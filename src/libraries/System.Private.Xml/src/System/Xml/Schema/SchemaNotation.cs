// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Xml.Schema
{
    internal sealed class SchemaNotation
    {
        internal const int SYSTEM = 0;
        internal const int PUBLIC = 1;

        private readonly XmlQualifiedName _name;
        private string? _systemLiteral;   // System literal
        private string? _pubid;    // pubid literal

        internal SchemaNotation(XmlQualifiedName name)
        {
            _name = name;
        }

        internal XmlQualifiedName Name
        {
            get { return _name; }
        }

        internal string? SystemLiteral
        {
            get { return _systemLiteral; }
            set { _systemLiteral = value; }
        }

        internal string? Pubid
        {
            get { return _pubid; }
            set { _pubid = value; }
        }
    };
}
