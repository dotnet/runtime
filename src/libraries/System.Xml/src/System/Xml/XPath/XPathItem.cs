// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Schema;

namespace System.Xml.XPath
{
    /// <summary>
    /// Base class for XPathNavigator and XmlAtomicValue.
    /// </summary>
    public abstract class XPathItem
    {
        /// <summary>
        /// True if this item is a node, and not an atomic value.
        /// </summary>
        public abstract bool IsNode { get; }

        /// <summary>
        /// Returns Xsd type of atomic value, or of node's content.
        /// </summary>
        public abstract XmlSchemaType XmlType { get; }

        /// <summary>
        /// Typed and untyped value accessors.
        /// </summary>
        public abstract string Value { get; }
        public abstract object TypedValue { get; }
        public abstract Type ValueType { get; }
        public abstract bool ValueAsBoolean { get; }
        public abstract DateTime ValueAsDateTime { get; }
        public abstract double ValueAsDouble { get; }
        public abstract int ValueAsInt { get; }
        public abstract long ValueAsLong { get; }
        public virtual object ValueAs(Type returnType) { return ValueAs(returnType, null); }
        public abstract object ValueAs(Type returnType, IXmlNamespaceResolver nsResolver);
    }
}

