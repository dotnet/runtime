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
    internal class ExcAncestralNamespaceContextManager : AncestralNamespaceContextManager
    {
        private Hashtable _inclusivePrefixSet = null;

        internal ExcAncestralNamespaceContextManager(string inclusiveNamespacesPrefixList)
        {
            _inclusivePrefixSet = Utils.TokenizePrefixListString(inclusiveNamespacesPrefixList);
        }

        private bool HasNonRedundantInclusivePrefix(XmlAttribute attr)
        {
            int tmp;
            string nsPrefix = Utils.GetNamespacePrefix(attr);
            return _inclusivePrefixSet.ContainsKey(nsPrefix) &&
                Utils.IsNonRedundantNamespaceDecl(attr, GetNearestRenderedNamespaceWithMatchingPrefix(nsPrefix, out tmp));
        }

        private void GatherNamespaceToRender(string nsPrefix, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            foreach (object a in nsListToRender.GetKeyList())
            {
                if (Utils.HasNamespacePrefix((XmlAttribute)a, nsPrefix))
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
                    nsListToRender.Add(local, null);
                }
            }
            else
            {
                int uDepth;
                XmlAttribute uAncestral = GetNearestUnrenderedNamespaceWithMatchingPrefix(nsPrefix, out uDepth);
                if (uAncestral != null && uDepth > rDepth && Utils.IsNonRedundantNamespaceDecl(uAncestral, rAncestral))
                {
                    nsListToRender.Add(uAncestral, null);
                }
            }
        }

        internal override void GetNamespacesToRender(XmlElement element, SortedList attrListToRender, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            GatherNamespaceToRender(element.Prefix, nsListToRender, nsLocallyDeclared);
            foreach (object attr in attrListToRender.GetKeyList())
            {
                string prefix = ((XmlAttribute)attr).Prefix;
                if (prefix.Length > 0)
                    GatherNamespaceToRender(prefix, nsListToRender, nsLocallyDeclared);
            }
        }

        internal override void TrackNamespaceNode(XmlAttribute attr, SortedList nsListToRender, Hashtable nsLocallyDeclared)
        {
            if (!Utils.IsXmlPrefixDefinitionNode(attr))
            {
                if (HasNonRedundantInclusivePrefix(attr))
                    nsListToRender.Add(attr, null);
                else
                    nsLocallyDeclared.Add(Utils.GetNamespacePrefix(attr), attr);
            }
        }

        internal override void TrackXmlNamespaceNode(XmlAttribute attr, SortedList nsListToRender, SortedList attrListToRender, Hashtable nsLocallyDeclared)
        {
            // exclusive canonicalization treats Xml namespaces as simple attributes. They are not propagated.
            attrListToRender.Add(attr, null);
        }
    }
}
