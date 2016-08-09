// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected XsltContext xsltContext;
        private ResetableIterator _queryIterator;

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
            _queryIterator = (ResetableIterator)Clone(other._queryIterator);
        }

        public override void Reset()
        {
            if (_queryIterator != null)
            {
                _queryIterator.Reset();
            }
        }

        public override XPathNavigator Current
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

        public override XPathNavigator Advance()
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

        protected object ProcessResult(object value)
        {
            if (value is string) return value;
            if (value is double) return value;
            if (value is bool) return value;
            if (value is XPathNavigator) return value;
            if (value is Int32) return (double)(Int32)value;

            if (value == null)
            {
                _queryIterator = XPathEmptyIterator.Instance;
                return this; // We map null to NodeSet to let $null/foo work well.
            }

            ResetableIterator resetable = value as ResetableIterator;
            if (resetable != null)
            {
                // We need Clone() value because variable may be used several times 
                // and they shouldn't 
                _queryIterator = (ResetableIterator)resetable.Clone();
                return this;
            }
            XPathNodeIterator nodeIterator = value as XPathNodeIterator;
            if (nodeIterator != null)
            {
                _queryIterator = new XPathArrayIterator(nodeIterator);
                return this;
            }
            IXPathNavigable navigable = value as IXPathNavigable;
            if (navigable != null)
            {
                return navigable.CreateNavigator();
            }

            if (value is Int16) return (double)(Int16)value;
            if (value is Int64) return (double)(Int64)value;
            if (value is UInt32) return (double)(UInt32)value;
            if (value is UInt16) return (double)(UInt16)value;
            if (value is UInt64) return (double)(UInt64)value;
            if (value is Single) return (double)(Single)value;
            if (value is Decimal) return (double)(Decimal)value;
            return value.ToString();
        }

        protected string QName { get { return prefix.Length != 0 ? prefix + ":" + name : name; } }

        public override int Count { get { return _queryIterator == null ? 1 : _queryIterator.Count; } }
        public override XPathResultType StaticType { get { return XPathResultType.Any; } }
    }
}
