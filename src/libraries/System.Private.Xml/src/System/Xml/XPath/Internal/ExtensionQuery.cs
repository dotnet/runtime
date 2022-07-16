// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal abstract class ExtensionQuery : Query
    {
        protected string prefix;
        protected string name;
        protected XsltContext? xsltContext;
        private ResettableIterator? _queryIterator;

        public ExtensionQuery(string prefix, string name) : base()
        {
            this.prefix = prefix;
            this.name = name;
        }
        protected ExtensionQuery(ExtensionQuery other) : base(other)
        {
            this.prefix = other.prefix;
            this.name = other.name;
            this.xsltContext = other.xsltContext;
            _queryIterator = (ResettableIterator?)Clone(other._queryIterator);
        }

        public override void Reset()
        {
            _queryIterator?.Reset();
        }

        public override XPathNavigator? Current
        {
            get
            {
                if (_queryIterator == null)
                {
                    throw XPathException.Create(SR.Xp_NodeSetExpected);
                }
                if (_queryIterator.CurrentPosition == 0)
                {
                    Advance();
                }
                return _queryIterator.Current;
            }
        }

        public override XPathNavigator? Advance()
        {
            if (_queryIterator == null)
            {
                throw XPathException.Create(SR.Xp_NodeSetExpected);
            }
            if (_queryIterator.MoveNext())
            {
                return _queryIterator.Current;
            }
            return null;
        }

        public override int CurrentPosition
        {
            get
            {
                if (_queryIterator != null)
                {
                    return _queryIterator.CurrentPosition;
                }
                return 0;
            }
        }

        protected object? ProcessResult(object? value)
        {
            if (value is string) return value;
            if (value is double) return value;
            if (value is bool) return value;
            if (value is XPathNavigator) return value;
            if (value is int) return (double)(int)value;

            if (value == null)
            {
                _queryIterator = XPathEmptyIterator.Instance;
                return this; // We map null to NodeSet to let $null/foo work well.
            }

            ResettableIterator? resettable = value as ResettableIterator;
            if (resettable != null)
            {
                // We need Clone() value because variable may be used several times
                // and they shouldn't
                _queryIterator = (ResettableIterator)resettable.Clone();
                return this;
            }
            XPathNodeIterator? nodeIterator = value as XPathNodeIterator;
            if (nodeIterator != null)
            {
                _queryIterator = new XPathArrayIterator(nodeIterator);
                return this;
            }
            IXPathNavigable? navigable = value as IXPathNavigable;
            if (navigable != null)
            {
                // This may be null if navigable is typeof(XmlNode).
                return navigable.CreateNavigator();
            }

            if (value is short) return (double)(short)value;
            if (value is long) return (double)(long)value;
            if (value is uint) return (double)(uint)value;
            if (value is ushort) return (double)(ushort)value;
            if (value is ulong) return (double)(ulong)value;
            if (value is float) return (double)(float)value;
            if (value is decimal) return (double)(decimal)value;
            return value.ToString()!;
        }

        protected string QName { get { return prefix.Length != 0 ? $"{prefix}:{name}" : name; } }

        public override int Count { get { return _queryIterator == null ? 1 : _queryIterator.Count; } }
        public override XPathResultType StaticType { get { return XPathResultType.Any; } }
    }
}
