// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Uncomment for more asserts (slows down tests)
// This will add additional asserts to help debugging issues
//#define ASSERT_VERBOSE
// This will trigger comparison between attributes and namespaces
// Currently XDocument is sorting attributes while other XPathNavigators aren't
//#define CHECK_ATTRIBUTE_ORDER

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using Xunit;

namespace System.Xml.XPath.XDocument.Tests.XDocument
{
    public class NavigatorComparer : XPathNavigator
    {
        private XPathNavigator _nav1, _nav2;

        private static void CompareNavigators(XPathNavigator a, XPathNavigator b)
        {
#if ASSERT_VERBOSE
            Assert.NotNull(a);
            Assert.NotNull(b);
            CompareNodeTypes(a.NodeType, b.NodeType);

            if (AreComparableNodes(a.NodeType, b.NodeType))
            {
                CompareValues(a, b);
                Assert.Equal(a.Name, b.Name);
            }
#endif
        }

        private static bool IsWhitespaceOrText(XPathNodeType nodeType)
        {
            return nodeType == XPathNodeType.Whitespace || nodeType == XPathNodeType.Text || nodeType == XPathNodeType.SignificantWhitespace;
        }

        private static bool IsNamespaceOrAttribute(XPathNodeType nodeType)
        {
            return nodeType == XPathNodeType.Namespace || nodeType == XPathNodeType.Attribute;
        }

        private static bool AreComparableNodes(XPathNodeType a, XPathNodeType b)
        {
            bool areBothTextOrWhitespaces = IsWhitespaceOrText(a) && IsWhitespaceOrText(b);
            bool areBothNamespacesOrAttributes = IsNamespaceOrAttribute(a) && IsNamespaceOrAttribute(b);
#if CHECK_ATTRIBUTE_ORDER
            areBothNamespacesOrAttributes = false
#endif

            return !areBothTextOrWhitespaces && !areBothNamespacesOrAttributes;
        }

        private static void CompareNodeTypes(XPathNodeType a, XPathNodeType b)
        {
            // XPath.XDocument interprets whitespace as XPathNodeType.Text
            // while other XPath navigators do it properly
            Assert.Equal(IsWhitespaceOrText(a), IsWhitespaceOrText(b));
            Assert.Equal(IsNamespaceOrAttribute(a), IsNamespaceOrAttribute(b));

            if (!IsWhitespaceOrText(a) && !IsNamespaceOrAttribute(a))
            {
                Assert.Equal(a, b);
            }
        }
        
        private static void CompareValues(XPathNavigator a, XPathNavigator b)
        {
            // In order to account for Desktop vs CoreCLR difference in implementation of XmlDocument we need to normalize line endings to conform XML specification.
            string sa = a.Value.Replace("\r\n", "\n").Replace("\r", "\n");
            string sb = b.Value.Replace("\r\n", "\n").Replace("\r", "\n");

            Assert.Equal(sa, sb);
        }

        public NavigatorComparer(XPathNavigator nav1, XPathNavigator nav2)
        {
            _nav1 = nav1;
            _nav2 = nav2;
        }

        public override string ToString()
        {
            var r1 = _nav1.ToString();
            var r2 = _nav2.ToString();
            Assert.Equal(r1, r2);
            return r1;
        }

        public override void SetValue(string value)
        {
            _nav1.SetValue(value);
            _nav2.SetValue(value);
            CompareNavigators(_nav1, _nav2);
        }

        public override object TypedValue
        {
            get
            {
                // No point of comparing by ref
                return _nav1.TypedValue;
            }
        }

        public override void SetTypedValue(object value)
        {
            _nav1.SetTypedValue(value);
            _nav2.SetTypedValue(value);
            CompareNavigators(_nav1, _nav2);
        }

        public override Type ValueType
        {
            get
            {
                var r1 = _nav1.ValueType;
                var r2 = _nav2.ValueType;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override bool ValueAsBoolean
        {
            get
            {
                var r1 = _nav1.ValueAsBoolean;
                var r2 = _nav2.ValueAsBoolean;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override DateTime ValueAsDateTime
        {
            get
            {
                var r1 = _nav1.ValueAsDateTime;
                var r2 = _nav2.ValueAsDateTime;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override double ValueAsDouble
        {
            get
            {
                var r1 = _nav1.ValueAsDouble;
                var r2 = _nav2.ValueAsDouble;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override int ValueAsInt
        {
            get
            {
                var r1 = _nav1.ValueAsInt;
                var r2 = _nav2.ValueAsInt;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override long ValueAsLong
        {
            get
            {
                var r1 = _nav1.ValueAsLong;
                var r2 = _nav2.ValueAsLong;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override object ValueAs(Type type, IXmlNamespaceResolver resolver)
        {
            var r1 = _nav1.ValueAs(type, resolver);
            var r2 = _nav2.ValueAs(type, resolver);
            Assert.Equal(r1, r2);
            return r1;
        }

        public override XPathNavigator CreateNavigator()
        {
            var r1 = _nav1.CreateNavigator();
            var r2 = _nav2.CreateNavigator();
            return new NavigatorComparer(r1, r2);
        }

        public override XmlNameTable NameTable
        {
            get
            {
                // comparing NameTable might be unreliable
                return _nav1.NameTable;
            }
        }

        public override string LookupNamespace(string value)
        {
            var r1 = _nav1.LookupNamespace(value);
            var r2 = _nav2.LookupNamespace(value);
            Assert.Equal(r1, r2);
            return r1;
        }

        public override string LookupPrefix(string value)
        {
            var r1 = _nav1.LookupPrefix(value);
            var r2 = _nav2.LookupPrefix(value);
            Assert.Equal(r1, r2);
            return r1;
        }
        public override IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope value)
        {
            var r1 = _nav1.GetNamespacesInScope(value);
            var r2 = _nav2.GetNamespacesInScope(value);
            Assert.Equal(r1, r2);
            return r1;
        }

        public override XPathNavigator Clone()
        {
            return new NavigatorComparer(_nav1.Clone(), _nav2.Clone());
        }

        public override XPathNodeType NodeType
        {
            get
            {
                var r1 = _nav1.NodeType;
                var r2 = _nav2.NodeType;
                CompareNodeTypes(r1, r2);
                return r1;
            }
        }

        public override string LocalName
        {
            get
            {
                var r1 = _nav1.LocalName;
                var r2 = _nav2.LocalName;
#if CHECK_ATTRIBUTE_ORDER
                Assert.Equal(r1, r2);
#else
                CompareNodeTypes(_nav1.NodeType, _nav2.NodeType);
                if (!IsNamespaceOrAttribute(_nav1.NodeType))
                {
                    Assert.Equal(r1, r2);
                }
#endif
                return r1;
            }
        }

        public override string Name
        {
            get
            {
                var r1 = _nav1.Name;
                var r2 = _nav2.Name;
#if CHECK_ATTRIBUTE_ORDER
                Assert.Equal(r1, r2);
#else
                CompareNodeTypes(_nav1.NodeType, _nav2.NodeType);
                if (!IsNamespaceOrAttribute(_nav1.NodeType))
                {
                    Assert.Equal(r1, r2);
                }
#endif
                return r1;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                var r1 = _nav1.NamespaceURI;
                var r2 = _nav2.NamespaceURI;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override string Prefix
        {
            get
            {
                var r1 = _nav1.Prefix;
                var r2 = _nav2.Prefix;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override string BaseURI
        {
            get
            {
                var r1 = _nav1.BaseURI;
                var r2 = _nav2.BaseURI;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                var r1 = _nav1.IsEmptyElement;
                var r2 = _nav2.IsEmptyElement;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override string XmlLang
        {
            get
            {
                var r1 = _nav1.XmlLang;
                var r2 = _nav2.XmlLang;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override XmlReader ReadSubtree()
        {
            // no point of comparing
            return _nav1.ReadSubtree();
        }

        public override void WriteSubtree(XmlWriter writer)
        {
            throw new NotSupportedException("WriteSubtree not supported yet.");
        }

        public override object UnderlyingObject
        {
            get
            {
                // no point of comparing
                return _nav1.UnderlyingObject;
            }
        }
        public override bool HasAttributes
        {
            get
            {
                var r1 = _nav1.HasAttributes;
                var r2 = _nav2.HasAttributes;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override string GetAttribute(string a, string b)
        {
            var r1 = _nav1.GetAttribute(a, b);
            var r2 = _nav2.GetAttribute(a, b);
            Assert.Equal(r1, r2);
            return r1;
        }

        public override bool MoveToAttribute(string a, string b)
        {
            var r1 = _nav1.MoveToAttribute(a, b);
            var r2 = _nav2.MoveToAttribute(a, b);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFirstAttribute()
        {
            var r1 = _nav1.MoveToFirstAttribute();
            var r2 = _nav2.MoveToFirstAttribute();
            Assert.Equal(r1, r2);
#if CHECK_ATTRIBUTE_ORDER
            CompareNavigators(nav1, nav2);
#endif
            return r1;
        }

        public override bool MoveToNextAttribute()
        {
            var r1 = _nav1.MoveToNextAttribute();
            var r2 = _nav2.MoveToNextAttribute();
            Assert.Equal(r1, r2);
#if CHECK_ATTRIBUTE_ORDER
            CompareNavigators(nav1, nav2);
#endif
            return r1;
        }

        public override string GetNamespace(string value)
        {
            var r1 = _nav1.GetNamespace(value);
            var r2 = _nav2.GetNamespace(value);
            Assert.Equal(r1, r2);
            return r1;
        }

        public override bool MoveToNamespace(string value)
        {
            var r1 = _nav1.MoveToNamespace(value);
            var r2 = _nav2.MoveToNamespace(value);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope value)
        {
            var r1 = _nav1.MoveToFirstNamespace(value);
            var r2 = _nav2.MoveToFirstNamespace(value);
            Assert.Equal(r1, r2);
#if CHECK_ATTRIBUTE_ORDER
            CompareNavigators(nav1, nav2);
#endif
            return r1;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope value)
        {
            var r1 = _nav1.MoveToNextNamespace(value);
            var r2 = _nav2.MoveToNextNamespace(value);
            Assert.Equal(r1, r2);
#if CHECK_ATTRIBUTE_ORDER
            CompareNavigators(nav1, nav2);
#endif
            return r1;
        }

        public override bool MoveToNext()
        {
            var r1 = _nav1.MoveToNext();
            var r2 = _nav2.MoveToNext();
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToPrevious()
        {
            var r1 = _nav1.MoveToPrevious();
            var r2 = _nav2.MoveToPrevious();
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFirst()
        {
            var r1 = _nav1.MoveToFirst();
            var r2 = _nav2.MoveToFirst();
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFirstChild()
        {
            var r1 = _nav1.MoveToFirstChild();
            var r2 = _nav2.MoveToFirstChild();
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToParent()
        {
            var r1 = _nav1.MoveToParent();
            var r2 = _nav2.MoveToParent();
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override void MoveToRoot()
        {
            _nav1.MoveToRoot();
            _nav2.MoveToRoot();
            CompareNavigators(_nav1, _nav2);
        }

        public override bool MoveTo(XPathNavigator value)
        {
            NavigatorComparer comp = value as NavigatorComparer;
            if (comp == null)
            {
                throw new NotSupportedException("MoveTo(XPathNavigator) not supported.");
            }

            var r1 = _nav1.MoveTo(comp._nav1);
            var r2 = _nav2.MoveTo(comp._nav2);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToId(string value)
        {
            var r1 = _nav1.MoveToId(value);
            var r2 = _nav2.MoveToId(value);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToChild(string a, string b)
        {
            var r1 = _nav1.MoveToChild(a, b);
            var r2 = _nav2.MoveToChild(a, b);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToChild(XPathNodeType value)
        {
            var r1 = _nav1.MoveToChild(value);
            var r2 = _nav2.MoveToChild(value);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFollowing(string a, string b)
        {
            var r1 = _nav1.MoveToFollowing(a, b);
            var r2 = _nav2.MoveToFollowing(a, b);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFollowing(string a, string b, XPathNavigator c)
        {
            throw new NotSupportedException("MoveToFollowing(string, string, XPathNavigator) not supported.");
        }

        public override bool MoveToFollowing(XPathNodeType value)
        {
            var r1 = _nav1.MoveToFollowing(value);
            var r2 = _nav2.MoveToFollowing(value);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToFollowing(XPathNodeType a, XPathNavigator b)
        {
            var r1 = _nav1.MoveToFollowing(a, b);
            var r2 = _nav2.MoveToFollowing(a, b);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToNext(string a, string b)
        {
            var r1 = _nav1.MoveToNext(a, b);
            var r2 = _nav2.MoveToNext(a, b);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool MoveToNext(XPathNodeType value)
        {
            var r1 = _nav1.MoveToNext(value);
            var r2 = _nav2.MoveToNext(value);
            Assert.Equal(r1, r2);
            CompareNavigators(_nav1, _nav2);
            return r1;
        }

        public override bool HasChildren
        {
            get
            {
                var r1 = _nav1.HasChildren;
                var r2 = _nav2.HasChildren;
                Assert.Equal(r1, r2);
                return r1;
            }
        }

        public override bool IsSamePosition(XPathNavigator value)
        {
            NavigatorComparer comp = value as NavigatorComparer;
            if (comp != null)
            {
                var r1 = _nav1.IsSamePosition(comp._nav1);
                var r2 = _nav2.IsSamePosition(comp._nav2);
#if CHECK_ATTRIBUTE_ORDER
                Assert.Equal(r1, r2);
#else
                CompareNodeTypes(_nav1.NodeType, _nav2.NodeType);
                if (!IsNamespaceOrAttribute(_nav1.NodeType))
                {
                    Assert.Equal(r1, r2);
                }
#endif
                return r1;
            }
            else
            {
                throw new NotSupportedException("IsSamePosition is not supported.");
            }
        }

        public override string Value
        {
            get
            {
#if CHECK_ATTRIBUTE_ORDER
                CompareValues(_nav1, _nav2);
#else
                CompareNodeTypes(_nav1.NodeType, _nav2.NodeType);
                if (!IsNamespaceOrAttribute(_nav1.NodeType))
                {
                    CompareValues(_nav1, _nav2);
                }
#endif
                return _nav1.Value;
            }
        }
        public override object ValueAs(Type value)
        {
            var r1 = _nav1.ValueAs(value);
            return r1;
        }
        // consider adding in the future
        //public override bool IsDescendant(XPathNavigator value)
        //public override XmlNodeOrder ComparePosition(XPathNavigator value)
        //public override XPathExpression Compile(string value)
        //public override XPathNavigator SelectSingleNode(string value)
        //public override XPathNavigator SelectSingleNode(string a, IXmlNamespaceResolver b)
        //public override XPathNavigator SelectSingleNode(XPathExpression value)
        //public override XPathNodeIterator Select(string value);
        //public override XPathNodeIterator Select(string a, IXmlNamespaceResolver b)
        //public override XPathNodeIterator Select(XPathExpression value)
        //public override object Evaluate(string a)
        //public override object Evaluate(string, System.Xml.IXmlNamespaceResolver)
        //public override object Evaluate(System.Xml.XPath.XPathExpression)
        //public override object Evaluate(System.Xml.XPath.XPathExpression, System.Xml.XPath.XPathNodeIterator)
        //public override bool Matches(System.Xml.XPath.XPathExpression)
        //public override bool Matches(string)
        //public override System.Xml.XPath.XPathNodeIterator SelectChildren(System.Xml.XPath.XPathNodeType)
        //public override System.Xml.XPath.XPathNodeIterator SelectChildren(string, string)
        //public override System.Xml.XPath.XPathNodeIterator SelectAncestors(System.Xml.XPath.XPathNodeType, bool)
        //public override System.Xml.XPath.XPathNodeIterator SelectAncestors(string, string, bool)
        //public override System.Xml.XPath.XPathNodeIterator SelectDescendants(System.Xml.XPath.XPathNodeType, bool)
        //public override System.Xml.XPath.XPathNodeIterator SelectDescendants(string, string, bool)
        //public override bool get_CanEdit()
        //public override System.Xml.XmlWriter PrependChild()
        //public override System.Xml.XmlWriter AppendChild()
        //public override System.Xml.XmlWriter InsertAfter()
        //public override System.Xml.XmlWriter InsertBefore()
        //public override System.Xml.XmlWriter CreateAttributes()
        //public override System.Xml.XmlWriter ReplaceRange(System.Xml.XPath.XPathNavigator)
        //public override void ReplaceSelf(string)
        //public override void ReplaceSelf(System.Xml.XmlReader)
        //public override void ReplaceSelf(System.Xml.XPath.XPathNavigator)
        //public override string get_OuterXml()
        //public override void set_OuterXml(string)
        //public override string get_InnerXml()
        //public override void set_InnerXml(string)
        //public override void AppendChild(string)
        //public override void AppendChild(System.Xml.XmlReader)
        //public override void AppendChild(System.Xml.XPath.XPathNavigator)
        //public override void PrependChild(string)
        //public override void PrependChild(System.Xml.XmlReader)
        //public override void PrependChild(System.Xml.XPath.XPathNavigator)
        //public override void InsertBefore(string)
        //public override void InsertBefore(System.Xml.XmlReader)
        //public override void InsertBefore(System.Xml.XPath.XPathNavigator)
        //public override void InsertAfter(string)
        //public override void InsertAfter(System.Xml.XmlReader)
        //public override void InsertAfter(System.Xml.XPath.XPathNavigator)
        //public override void DeleteRange(System.Xml.XPath.XPathNavigator)
        //public override void DeleteSelf()
        //public override void PrependChildElement(string, string, string, string)
        //public override void AppendChildElement(string, string, string, string)
        //public override void InsertElementBefore(string, string, string, string)
        //public override void InsertElementAfter(string, string, string, string)
        //public override void CreateAttribute(string, string, string, string)
        //public override bool Equals(object)
        //public override Int32 GetHashCode()
    }
}
