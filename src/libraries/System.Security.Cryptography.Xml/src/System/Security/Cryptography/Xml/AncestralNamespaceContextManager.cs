// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    internal abstract class AncestralNamespaceContextManager
    {
        internal ArrayList _ancestorStack = new ArrayList();

        internal NamespaceFrame GetScopeAt(int i)
        {
            return (NamespaceFrame)_ancestorStack[i]!;
        }

        internal NamespaceFrame GetCurrentScope()
        {
            return GetScopeAt(_ancestorStack.Count - 1);
        }

        protected XmlAttribute? GetNearestRenderedNamespaceWithMatchingPrefix(string nsPrefix, out int depth)
        {
            XmlAttribute? attr;
            depth = -1;
            for (int i = _ancestorStack.Count - 1; i >= 0; i--)
            {
                if ((attr = GetScopeAt(i).GetRendered(nsPrefix)) != null)
                {
                    depth = i;
                    return attr;
                }
            }
            return null;
        }

        protected XmlAttribute? GetNearestUnrenderedNamespaceWithMatchingPrefix(string nsPrefix, out int depth)
        {
            XmlAttribute? attr;
            depth = -1;
            for (int i = _ancestorStack.Count - 1; i >= 0; i--)
            {
                if ((attr = GetScopeAt(i).GetUnrendered(nsPrefix)) != null)
                {
                    depth = i;
                    return attr;
                }
            }
            return null;
        }

        internal void EnterElementContext()
        {
            _ancestorStack.Add(new NamespaceFrame());
        }

        internal void ExitElementContext()
        {
            _ancestorStack.RemoveAt(_ancestorStack.Count - 1);
        }

        internal abstract void TrackNamespaceNode(XmlAttribute attr, SortedList nsListToRender, Hashtable nsLocallyDeclared);
        internal abstract void TrackXmlNamespaceNode(XmlAttribute attr, SortedList nsListToRender, SortedList attrListToRender, Hashtable nsLocallyDeclared);
        internal abstract void GetNamespacesToRender(XmlElement element, SortedList attrListToRender, SortedList nsListToRender, Hashtable nsLocallyDeclared);

        internal void LoadUnrenderedNamespaces(Hashtable nsLocallyDeclared)
        {
            object[] attrs = new object[nsLocallyDeclared.Count];
            nsLocallyDeclared.Values.CopyTo(attrs, 0);
            foreach (object attr in attrs)
            {
                AddUnrendered((XmlAttribute)attr);
            }
        }

        internal void LoadRenderedNamespaces(SortedList nsRenderedList)
        {
            foreach (object attr in nsRenderedList.GetKeyList())
            {
                AddRendered((XmlAttribute)attr);
            }
        }

        internal void AddRendered(XmlAttribute attr)
        {
            GetCurrentScope().AddRendered(attr);
        }

        internal void AddUnrendered(XmlAttribute attr)
        {
            GetCurrentScope().AddUnrendered(attr);
        }
    }
}
