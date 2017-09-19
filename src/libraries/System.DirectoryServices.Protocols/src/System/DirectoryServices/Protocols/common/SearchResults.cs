// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.Protocols
{
    public class SearchResultReference
    {
        private Uri[] _resultReferences;
        private DirectoryControl[] _resultControls;

        internal SearchResultReference(Uri[] uris) => _resultReferences = uris;

        public Uri[] Reference
        {
            get
            {
                if (_resultReferences == null)
                {
                    return Array.Empty<Uri>();
                }

                Uri[] tempUri = new Uri[_resultReferences.Length];
                for (int i = 0; i < _resultReferences.Length; i++)
                {
                    tempUri[i] = new Uri(_resultReferences[i].AbsoluteUri);
                }
                return tempUri;
            }
        }

        public DirectoryControl[] Controls
        {
            get
            {
                if (_resultControls == null)
                {
                    return Array.Empty<DirectoryControl>();
                }

                DirectoryControl[] controls = new DirectoryControl[_resultControls.Length];
                for (int i = 0; i < _resultControls.Length; i++)
                {
                    controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                }
                DirectoryControl.TransformControls(controls);
                return controls;
            }
        }
    }

    public class SearchResultReferenceCollection : ReadOnlyCollectionBase
    {
        internal SearchResultReferenceCollection() { }

        public SearchResultReference this[int index] => (SearchResultReference)InnerList[index];

        internal int Add(SearchResultReference reference) => InnerList.Add(reference);

        public bool Contains(SearchResultReference value) => InnerList.Contains(value);

        public int IndexOf(SearchResultReference value) => InnerList.IndexOf(value);

        public void CopyTo(SearchResultReference[] values, int index) => InnerList.CopyTo(values, index);

        internal void Clear() => InnerList.Clear();
    }

    public class SearchResultEntry
    {
        private DirectoryControl[] _resultControls = null;

        internal SearchResultEntry(string dn) : this(dn, new SearchResultAttributeCollection()) {}

        internal SearchResultEntry(string dn, SearchResultAttributeCollection attrs)
        {
            DistinguishedName = dn;
            Attributes = attrs;
        }

        public string DistinguishedName { get; internal set; }

        public SearchResultAttributeCollection Attributes { get; }

        public DirectoryControl[] Controls
        {
            get
            {
                if (_resultControls == null)
                {
                    return Array.Empty<DirectoryControl>();
                }

                DirectoryControl[] controls = new DirectoryControl[_resultControls.Length];
                for (int i = 0; i < _resultControls.Length; i++)
                {
                    controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                }
                DirectoryControl.TransformControls(controls);
                return controls;
            }
        }
    }

    public class SearchResultEntryCollection : ReadOnlyCollectionBase
    {
        internal SearchResultEntryCollection() { }

        public SearchResultEntry this[int index] => (SearchResultEntry)InnerList[index];

        internal int Add(SearchResultEntry entry) => InnerList.Add(entry);

        public bool Contains(SearchResultEntry value) => InnerList.Contains(value);

        public int IndexOf(SearchResultEntry value) => InnerList.IndexOf(value);

        public void CopyTo(SearchResultEntry[] values, int index) => InnerList.CopyTo(values, index);

        internal void Clear() => InnerList.Clear();
    }
}
