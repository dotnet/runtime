// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal sealed class ExclusiveCanonicalNamespaceManager
    {
        private const int MaxPoolSize = 32;
        private List<NamespaceEntry> _localNamespacesToRender = new List<NamespaceEntry>();
        private List<NamespaceEntry> _namespaceContext = new List<NamespaceEntry>();
        private Pool<NamespaceEntry> _pool = new Pool<NamespaceEntry>(MaxPoolSize);

        public ExclusiveCanonicalNamespaceManager()
        {
            Reset();
        }

        public void AddLocalNamespaceIfNotRedundant(string prefix, string namespaceUri)
        {
            if (IsNonRedundantNamespaceDeclaration(prefix, namespaceUri))
            {
                _namespaceContext.Add(CreateNamespaceEntry(prefix, namespaceUri));
            }
        }

        private NamespaceEntry CloneNamespaceEntryToRender(NamespaceEntry ne)
        {
            NamespaceEntry entry = _pool.Take();
            if (entry == null)
            {
                entry = new NamespaceEntry();
            }
            entry.CopyAndSetToRender(ne);
            return entry;
        }

        private NamespaceEntry CreateNamespaceEntry(string prefix, string namespaceUri)
        {
            return CreateNamespaceEntry(prefix, namespaceUri, false);
        }

        private NamespaceEntry CreateNamespaceEntry(string prefix, string namespaceUri, bool render)
        {
            NamespaceEntry entry = _pool.Take();
            if (entry == null)
            {
                entry = new NamespaceEntry();
            }
            entry.Init(prefix, namespaceUri, render);
            return entry;
        }

        public void EnterElementContext()
        {
            _namespaceContext.Add(null);
        }

        public void ExitElementContext()
        {
            int count = _namespaceContext.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                NamespaceEntry ne = _namespaceContext[i];
                _namespaceContext.RemoveAt(i);
                if (ne != null)
                {
                    ne.Clear();
                    _pool.Return(ne);
                }
                else
                {
                    break;
                }
            }
        }

        private bool IsNonRedundantNamespaceDeclaration(string prefix, string namespaceUri)
        {
            for (int i = _namespaceContext.Count - 1; i >= 0; i--)
            {
                NamespaceEntry ne = _namespaceContext[i];
                if (ne != null && ne.Prefix == prefix)
                {
                    return ne.NamespaceUri != namespaceUri;
                }
            }
            return !C14nUtil.IsEmptyDefaultNamespaceDeclaration(prefix, namespaceUri) &&
                !C14nUtil.IsXmlPrefixDeclaration(prefix, namespaceUri);
        }

        public string LookupNamespace(string prefix)
        {
            for (int i = _namespaceContext.Count - 1; i >= 0; i--)
            {
                NamespaceEntry n = _namespaceContext[i];
                if (n != null && n.Prefix == prefix)
                {
                    return n.NamespaceUri;
                }
            }
            return null;
        }

        public string LookupPrefix(string ne, bool isForAttribute)
        {
            for (int i = _namespaceContext.Count - 1; i >= 0; i--)
            {
                NamespaceEntry n = _namespaceContext[i];
                if (n != null && n.NamespaceUri == ne && (!isForAttribute || n.Prefix.Length > 0))
                {
                    string prefix = n.Prefix;
                    for (int j = i + 1; j < _namespaceContext.Count; j++)
                    {
                        NamespaceEntry m = _namespaceContext[j];
                        if (m != null && m.Prefix == prefix)
                        {
                            // redefined later
                            return null;
                        }
                    }
                    return prefix;
                }
            }
            return null;
        }

        public void MarkToRenderForInclusivePrefix(string prefix, bool searchOuterContext, IAncestralNamespaceContextProvider context)
        {
            MarkToRender(prefix, searchOuterContext, context, true);
        }

        public void MarkToRenderForVisiblyUsedPrefix(string prefix, bool searchOuterContext, IAncestralNamespaceContextProvider context)
        {
            if (!MarkToRender(prefix, searchOuterContext, context, false))
            {
                string nodeName = context != null ? context.CurrentNodeName : null;
                throw new XmlException(string.Format("Unable to find prefix: {0}, {1}", prefix, nodeName));
            }
        }

        private bool MarkToRender(string prefix, bool searchOuterContext, IAncestralNamespaceContextProvider context, bool isInclusivePrefix)
        {
            if (prefix == "xml")
            {
                return true;
            }
            bool currentFrame = true;
            for (int i = _namespaceContext.Count - 1; i >= 0; i--)
            {
                NamespaceEntry ne = _namespaceContext[i];
                if (ne == null)
                {
                    if (isInclusivePrefix)
                    {
                        break;
                    }
                    currentFrame = false;
                }
                else if (ne.Prefix == prefix)
                {
                    if (ne.Rendered)
                    {
                        return true;
                    }
                    bool shouldRender;
                    if (prefix.Length > 0 || ne.NamespaceUri.Length > 0)
                    {
                        shouldRender = true;
                    }
                    else
                    {
                        NamespaceEntry match = null;
                        for (int j = i - 1; j >= 0; j--)
                        {
                            NamespaceEntry p = _namespaceContext[j];
                            if (p != null && p.Rendered && p.Prefix.Length == 0)
                            {
                                match = p;
                                break;
                            }
                        }
                        shouldRender = match != null && match.NamespaceUri.Length > 0;
                    }
                    if (shouldRender)
                    {
                        if (currentFrame)
                        {
                            ne.Rendered = true;
                        }
                        else
                        {
                            _namespaceContext.Add(CloneNamespaceEntryToRender(ne));
                        }
                    }
                    return true;
                }
            }

            if (searchOuterContext)
            {
                string namespaceUri;
                if (context != null)
                {
                    namespaceUri = context.LookupNamespace(prefix);
                }
                else
                {
                    namespaceUri = null;
                }
                if (namespaceUri != null && namespaceUri.Length > 0)
                {
                    _namespaceContext.Add(CreateNamespaceEntry(prefix, namespaceUri, true));
                    return true;
                }
                else
                {
                    return prefix.Length == 0 || isInclusivePrefix;
                }
            }
            return true;
        }

        public void Render(CanonicalEncoder encoder)
        {
            for (int i = _namespaceContext.Count - 1; i >= 0; i--)
            {
                NamespaceEntry ne = _namespaceContext[i];
                if (ne == null)
                {
                    break;
                }
                else if (ne.Rendered)
                {
                    _localNamespacesToRender.Add(ne);
                }
            }
            if (_localNamespacesToRender.Count == 0)
            {
                return;
            }
            _localNamespacesToRender.Sort(NamespaceComparer.Instance);
            for (int i = 0; i < _localNamespacesToRender.Count; i++)
            {
                NamespaceEntry ne = _localNamespacesToRender[i];
                encoder.Encode(" xmlns");
                if (ne.Prefix != null && ne.Prefix.Length > 0)
                {
                    encoder.Encode(':');
                    encoder.Encode(ne.Prefix);
                }
                encoder.Encode("=\"");
                encoder.EncodeWithTranslation(ne.NamespaceUri, CanonicalEncoder.XmlStringType.AttributeValue);
                encoder.Encode('\"');
            }
            _localNamespacesToRender.Clear();
        }

        public void Reset()
        {
            _localNamespacesToRender.Clear();
            int count = _namespaceContext.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                NamespaceEntry ne = _namespaceContext[i];
                _namespaceContext.RemoveAt(i);
                if (ne != null)
                {
                    ne.Clear();
                    _pool.Return(ne);
                }
            }
        }

        // this is a class instead of a struct due to the sorting requirement: objects of this type are pooled
        private class NamespaceEntry
        {
            private string _prefix;
            private string _namespaceUri;
            private bool _rendered;

            public NamespaceEntry()
            {
            }

            public string Prefix
            {
                get { return _prefix; }
            }

            public string NamespaceUri
            {
                get { return _namespaceUri; }
            }

            public bool Rendered
            {
                get { return _rendered; }
                set { _rendered = value; }
            }

            public void Clear()
            {
                _prefix = null;
                _namespaceUri = null;
                _rendered = false;
            }

            public void CopyAndSetToRender(NamespaceEntry src)
            {
                Init(src._prefix, src._namespaceUri, true);
            }

            public void Init(string prefix, string namespaceUri, bool rendered)
            {
                _prefix = prefix;
                _namespaceUri = namespaceUri;
                _rendered = rendered;
            }
        }

        private class NamespaceComparer : IComparer<NamespaceEntry>
        {
            private static NamespaceComparer s_instance = new NamespaceComparer();

            private NamespaceComparer() { }

            public static NamespaceComparer Instance
            {
                get { return s_instance; }
            }

            public int Compare(NamespaceEntry x, NamespaceEntry y)
            {
                return string.Compare(x.Prefix, y.Prefix, StringComparison.Ordinal);
            }
        }
    }
}
