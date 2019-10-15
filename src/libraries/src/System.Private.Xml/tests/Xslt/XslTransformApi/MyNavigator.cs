// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.XPath;

/// <summary>
/// Summary description for Class3.
/// </summary>
public class MyNavigator : XPathNavigator
{
    private XPathNavigator _xn;
    private string _strFileName;

    public MyNavigator(string filename)
    {
        _strFileName = filename;
        _xn = new XPathDocument(filename, XmlSpace.None).CreateNavigator();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.Clone"]/*' />
    public override XPathNavigator Clone()
    {
        return new MyNavigator(_strFileName);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.NodeType"]/*' />
    public override XPathNodeType NodeType { get { return _xn.NodeType; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.LocalName"]/*' />
    public override string LocalName { get { return _xn.LocalName; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.NamespaceURI"]/*' />
    public override string NamespaceURI { get { return _xn.NamespaceURI; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.Name"]/*' />
    public override string Name { get { return _xn.Name; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.Prefix"]/*' />
    public override string Prefix { get { return _xn.Prefix; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.Value"]/*' />
    public override string Value { get { return _xn.Value; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.BaseURI"]/*' />
    public override string BaseURI { get { return _xn.BaseURI; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.XmlLang"]/*' />
    public override string XmlLang { get { return _xn.XmlLang; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.IsEmptyElement"]/*' />
    public override bool IsEmptyElement { get { return _xn.IsEmptyElement; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.NameTable"]/*' />
    public override XmlNameTable NameTable { get { return _xn.NameTable; } }

    // Attributes
    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.HasAttributes"]/*' />
    public override bool HasAttributes { get { return _xn.HasAttributes; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.GetAttribute"]/*' />
    public override string GetAttribute(string localName, string namespaceURI)
    {
        return _xn.GetAttribute(localName, namespaceURI);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToAttribute"]/*' />
    public override bool MoveToAttribute(string localName, string namespaceURI)
    {
        return _xn.MoveToAttribute(localName, namespaceURI);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToFirstAttribute"]/*' />
    public override bool MoveToFirstAttribute()
    {
        return _xn.MoveToFirstAttribute();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToNextAttribute"]/*' />
    public override bool MoveToNextAttribute()
    {
        return _xn.MoveToNextAttribute();
    }

    // Namespaces
    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.GetNamespace"]/*' />
    public override string GetNamespace(string name)
    {
        return _xn.GetNamespace(name);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToNamespace"]/*' />
    public override bool MoveToNamespace(string name)
    {
        return _xn.MoveToNamespace(name);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToFirstNamespace1"]/*' />
    public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
    {
        return _xn.MoveToFirstNamespace(namespaceScope);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToNextNamespace1"]/*' />
    public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
    {
        return _xn.MoveToNextNamespace(namespaceScope);
    }

    // Tree
    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToNext"]/*' />
    public override bool MoveToNext()
    {
        return _xn.MoveToNext();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToPrevious"]/*' />
    public override bool MoveToPrevious()
    {
        return _xn.MoveToPrevious();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToFirst"]/*' />
    public override bool MoveToFirst()
    {
        return _xn.MoveToFirst();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.HasChildren"]/*' />
    public override bool HasChildren { get { return _xn.HasChildren; } }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToFirstChild"]/*' />
    public override bool MoveToFirstChild()
    {
        return _xn.MoveToFirstChild();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToParent"]/*' />
    public override bool MoveToParent()
    {
        return _xn.MoveToParent();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToRoot"]/*' />
    public override void MoveToRoot()
    {
        _xn.MoveToRoot();
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveTo"]/*' />
    public override bool MoveTo(XPathNavigator other)
    {
        return _xn.MoveTo(other);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.MoveToId"]/*' />
    public override bool MoveToId(string id)
    {
        return _xn.MoveToId(id);
    }

    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.IsSamePosition"]/*' />
    public override bool IsSamePosition(XPathNavigator other)
    {
        return _xn.IsSamePosition(other);
    }

    // Selection
    /// <include file='doc\XPathNavigator.uex' path='docs/doc[@for="XPathNavigator.Compile"]/*' />
    public override XPathExpression Compile(string xpath)
    {
        if (xpath.IndexOf("custom", 0, xpath.Length) >= 0)
            xpath = "custom:dangerous()";

        return _xn.Compile(xpath);
    }
}
