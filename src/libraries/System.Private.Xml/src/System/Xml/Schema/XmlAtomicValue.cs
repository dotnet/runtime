// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.XPath;
using System.Diagnostics;

namespace System.Xml.Schema
{
    /// <summary>
    /// This class contains a (CLR Object, XmlType) pair that represents an instance of an Xml atomic value.
    /// It is optimized to avoid boxing.
    /// </summary>
    public sealed class XmlAtomicValue : XPathItem, ICloneable
    {
        private readonly XmlSchemaType _xmlType;
        private readonly object? _objVal;
        private readonly TypeCode _clrType;
        private Union _unionVal;
        private readonly NamespacePrefixForQName? _nsPrefix;

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct Union
        {
            [FieldOffset(0)]
            public bool boolVal;
            [FieldOffset(0)]
            public double dblVal;
            [FieldOffset(0)]
            public long i64Val;
            [FieldOffset(0)]
            public int i32Val;
            [FieldOffset(0)]
            public DateTime dtVal;
        }

        private sealed class NamespacePrefixForQName : IXmlNamespaceResolver
        {
            public string prefix;
            public string ns;

            public NamespacePrefixForQName(string prefix, string ns)
            {
                this.ns = ns;
                this.prefix = prefix;
            }

            public string? LookupNamespace(string? prefix)
            {
                if (prefix == this.prefix)
                {
                    return ns;
                }
                return null;
            }

            public string? LookupPrefix(string? namespaceName)
            {
                if (ns == namespaceName)
                {
                    return prefix;
                }
                return null;
            }

            public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>(1);
                dict[prefix] = ns;
                return dict;
            }
        }

        //-----------------------------------------------
        // XmlAtomicValue constructors and methods
        //-----------------------------------------------

        internal XmlAtomicValue(XmlSchemaType xmlType, bool value)
        {
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _clrType = TypeCode.Boolean;
            _unionVal.boolVal = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, DateTime value)
        {
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _clrType = TypeCode.DateTime;
            _unionVal.dtVal = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, double value)
        {
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _clrType = TypeCode.Double;
            _unionVal.dblVal = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, int value)
        {
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _clrType = TypeCode.Int32;
            _unionVal.i32Val = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, long value)
        {
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _clrType = TypeCode.Int64;
            _unionVal.i64Val = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _objVal = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, string value, IXmlNamespaceResolver? nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _objVal = value;
            if (nsResolver != null && (_xmlType.TypeCode == XmlTypeCode.QName || _xmlType.TypeCode == XmlTypeCode.Notation))
            {
                string prefix = GetPrefixFromQName(value)!;
                _nsPrefix = new NamespacePrefixForQName(prefix, nsResolver.LookupNamespace(prefix)!);
            }
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _objVal = value;
        }

        internal XmlAtomicValue(XmlSchemaType xmlType, object value, IXmlNamespaceResolver? nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (xmlType == null) throw new ArgumentNullException(nameof(xmlType));
            _xmlType = xmlType;
            _objVal = value;

            if (nsResolver != null && (_xmlType.TypeCode == XmlTypeCode.QName || _xmlType.TypeCode == XmlTypeCode.Notation))
            { //Its a qualifiedName
                XmlQualifiedName qname = (_objVal as XmlQualifiedName)!;
                Debug.Assert(qname != null); //string representation is handled in a different overload
                string ns = qname.Namespace;
                _nsPrefix = new NamespacePrefixForQName(nsResolver.LookupPrefix(ns)!, ns);
            }
        }

        /// <summary>
        /// Since XmlAtomicValue is immutable, clone simply returns this.
        /// </summary>
        public XmlAtomicValue Clone()
        {
            return this;
        }


        //-----------------------------------------------
        // ICloneable methods
        //-----------------------------------------------

        /// <summary>
        /// Since XmlAtomicValue is immutable, clone simply returns this.
        /// </summary>
        object ICloneable.Clone()
        {
            return this;
        }


        //-----------------------------------------------
        // XPathItem methods
        //-----------------------------------------------

        public override bool IsNode
        {
            get { return false; }
        }

        public override XmlSchemaType XmlType
        {
            get { return _xmlType; }
        }

        public override Type ValueType
        {
            get { return _xmlType.Datatype!.ValueType; }
        }

        public override object TypedValue
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ChangeType(_unionVal.boolVal, ValueType);
                        case TypeCode.Int32: return valueConverter.ChangeType(_unionVal.i32Val, ValueType);
                        case TypeCode.Int64: return valueConverter.ChangeType(_unionVal.i64Val, ValueType);
                        case TypeCode.Double: return valueConverter.ChangeType(_unionVal.dblVal, ValueType);
                        case TypeCode.DateTime: return valueConverter.ChangeType(_unionVal.dtVal, ValueType);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }
                return valueConverter.ChangeType(_objVal, ValueType, _nsPrefix);
            }
        }

        public override bool ValueAsBoolean
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return _unionVal.boolVal;
                        case TypeCode.Int32: return valueConverter.ToBoolean(_unionVal.i32Val);
                        case TypeCode.Int64: return valueConverter.ToBoolean(_unionVal.i64Val);
                        case TypeCode.Double: return valueConverter.ToBoolean(_unionVal.dblVal);
                        case TypeCode.DateTime: return valueConverter.ToBoolean(_unionVal.dtVal);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }

                return valueConverter.ToBoolean(_objVal);
            }
        }

        public override DateTime ValueAsDateTime
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ToDateTime(_unionVal.boolVal);
                        case TypeCode.Int32: return valueConverter.ToDateTime(_unionVal.i32Val);
                        case TypeCode.Int64: return valueConverter.ToDateTime(_unionVal.i64Val);
                        case TypeCode.Double: return valueConverter.ToDateTime(_unionVal.dblVal);
                        case TypeCode.DateTime: return _unionVal.dtVal;
                        default: Debug.Fail("Should never get here"); break;
                    }
                }

                return valueConverter.ToDateTime(_objVal);
            }
        }


        public override double ValueAsDouble
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ToDouble(_unionVal.boolVal);
                        case TypeCode.Int32: return valueConverter.ToDouble(_unionVal.i32Val);
                        case TypeCode.Int64: return valueConverter.ToDouble(_unionVal.i64Val);
                        case TypeCode.Double: return _unionVal.dblVal;
                        case TypeCode.DateTime: return valueConverter.ToDouble(_unionVal.dtVal);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }

                return valueConverter.ToDouble(_objVal);
            }
        }

        public override int ValueAsInt
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ToInt32(_unionVal.boolVal);
                        case TypeCode.Int32: return _unionVal.i32Val;
                        case TypeCode.Int64: return valueConverter.ToInt32(_unionVal.i64Val);
                        case TypeCode.Double: return valueConverter.ToInt32(_unionVal.dblVal);
                        case TypeCode.DateTime: return valueConverter.ToInt32(_unionVal.dtVal);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }

                return valueConverter.ToInt32(_objVal);
            }
        }

        public override long ValueAsLong
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ToInt64(_unionVal.boolVal);
                        case TypeCode.Int32: return valueConverter.ToInt64(_unionVal.i32Val);
                        case TypeCode.Int64: return _unionVal.i64Val;
                        case TypeCode.Double: return valueConverter.ToInt64(_unionVal.dblVal);
                        case TypeCode.DateTime: return valueConverter.ToInt64(_unionVal.dtVal);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }

                return valueConverter.ToInt64(_objVal);
            }
        }

        public override object ValueAs(Type type, IXmlNamespaceResolver? nsResolver)
        {
            XmlValueConverter valueConverter = _xmlType.ValueConverter;

            if (type == typeof(XPathItem) || type == typeof(XmlAtomicValue))
                return this;

            if (_objVal == null)
            {
                switch (_clrType)
                {
                    case TypeCode.Boolean: return valueConverter.ChangeType(_unionVal.boolVal, type);
                    case TypeCode.Int32: return valueConverter.ChangeType(_unionVal.i32Val, type);
                    case TypeCode.Int64: return valueConverter.ChangeType(_unionVal.i64Val, type);
                    case TypeCode.Double: return valueConverter.ChangeType(_unionVal.dblVal, type);
                    case TypeCode.DateTime: return valueConverter.ChangeType(_unionVal.dtVal, type);
                    default: Debug.Fail("Should never get here"); break;
                }
            }

            return valueConverter.ChangeType(_objVal, type, nsResolver);
        }

        public override string Value
        {
            get
            {
                XmlValueConverter valueConverter = _xmlType.ValueConverter;

                if (_objVal == null)
                {
                    switch (_clrType)
                    {
                        case TypeCode.Boolean: return valueConverter.ToString(_unionVal.boolVal);
                        case TypeCode.Int32: return valueConverter.ToString(_unionVal.i32Val);
                        case TypeCode.Int64: return valueConverter.ToString(_unionVal.i64Val);
                        case TypeCode.Double: return valueConverter.ToString(_unionVal.dblVal);
                        case TypeCode.DateTime: return valueConverter.ToString(_unionVal.dtVal);
                        default: Debug.Fail("Should never get here"); break;
                    }
                }
                return valueConverter.ToString(_objVal, _nsPrefix);
            }
        }

        public override string ToString()
        {
            return Value;
        }

        private string? GetPrefixFromQName(string value)
        {
            int colonOffset;
            int len = ValidateNames.ParseQName(value, 0, out colonOffset);

            if (len == 0 || len != value.Length)
            {
                return null;
            }
            if (colonOffset != 0)
            {
                return value.Substring(0, colonOffset);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
