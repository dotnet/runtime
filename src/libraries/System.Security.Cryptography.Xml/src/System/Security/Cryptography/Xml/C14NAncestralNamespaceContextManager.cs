// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.IO;
using System.Text;
using System.Collections;

namespace System.Security.Cryptography.Xml
{
    // the stack of currently active NamespaceFrame contexts. this
    // object also maintains the inclusive prefix list in a tokenized form.
    internal class C14NAncestralNamespaceContextManager : AncestralNamespaceContextManager
    {
        internal C14NAncestralNamespaceContextManager() { }

        private void GetNamespaceToRender(string nsPrefix, SortedList attrListToRender, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            foreach (object a in nsListToRender.GetKeyList())
            {
                if (Utils.HasNamespacePrefix((XmlAttribute)a, nsPrefix))
                    return;
            }
            foreach (object a in attrListToRender.GetKeyList())
            {
                if (((XmlAttribute)a).LocalName.Equals(nsPrefix))
                    return;
            }

            int rDepth;
            XmlAttribute local = (XmlAttribute)nsLocallyDeclared[nsPrefix];
            XmlAttribute rAncestral = GetNearestRenderedNamespaceWithMatchingPrefix(nsPrefix, out rDepth);
            if (local != null)
            {
                if (Utils.IsNonRedundantNamespaceDecl(local, rAncestral))
                {
                    nsLocallyDeclared.Remove(nsPrefix);
                    if (Utils.IsXmlNamespaceNode(local))
                        attrListToRender.Add(local, null);
                    else
                        nsListToRender.Add(local, null);
                }
            }
            else
            {
                int uDepth;
                XmlAttribute uAncestral = GetNearestUnrenderedNamespaceWithMatchingPrefix(nsPrefix, out uDepth);
                if (uAncestral != null && uDepth > rDepth && Utils.IsNonRedundantNamespaceDecl(uAncestral, rAncestral))
                {
                    if (Utils.IsXmlNamespaceNode(uAncestral))
                        attrListToRender.Add(uAncestral, null);
                    else
                        nsListToRender.Add(uAncestral, null);
                }
            }
        }

        internal override void GetNamespacesToRender(XmlElement element, SortedList attrListToRender, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            XmlAttribute attrib = null;
            object[] attrs = new object[nsLocallyDeclared.Count];
            nsLocallyDeclared.Values.CopyTo(attrs, 0);
            foreach (object a in attrs)
            {
                attrib = (XmlAttribute)a;
                int rDepth;
                XmlAttribute rAncestral = GetNearestRenderedNamespaceWithMatchingPrefix(Utils.GetNamespacePrefix(attrib), out rDepth);
                if (Utils.IsNonRedundantNamespaceDecl(attrib, rAncestral))
                {
                    nsLocallyDeclared.Remove(Utils.GetNamespacePrefix(attrib));
                    if (Utils.IsXmlNamespaceNode(attrib))
                        attrListToRender.Add(attrib, null);
                    else
                        nsListToRender.Add(attrib, null);
                }
            }

            for (int i = _ancestorStack.Count - 1; i >= 0; i--)
            {
                foreach (object a in GetScopeAt(i).GetUnrendered().Values)
                {
                    attrib = (XmlAttribute)a;
                    if (attrib != null)
                        GetNamespaceToRender(Utils.GetNamespacePrefix(attrib), attrListToRender, nsListToRender, nsLocallyDeclared);
                }
            }
        }

        internal override void TrackNamespaceNode(XmlAttribute attr, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            nsLocallyDeclared.Add(Utils.GetNamespacePrefix(attr), attr);
        }

        internal override void TrackXmlNamespaceNode(XmlAttribute attr, SortedList nsListToRender, SortedList attrListToRender, Hashtable nsLocallyDeclared)
        {
            nsLocallyDeclared.Add(Utils.GetNamespacePrefix(attr), attr);
        }
    }
}
