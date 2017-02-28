// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    public class SearchResultReference
    {
        private Uri[] _resultReferences = null;
        private DirectoryControl[] _resultControls = null;

        internal SearchResultReference(Uri[] uris)
        {
            _resultReferences = uris;
        }

        public Uri[] Reference
        {
            get
            {
                if (_resultReferences == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempUri = new Uri[_resultReferences.Length];
                    for (int i = 0; i < _resultReferences.Length; i++)
                    {
                        tempUri[i] = new Uri(_resultReferences[i].AbsoluteUri);
                    }
                    return tempUri;
                }
            }
        }

        public DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;

                if (_resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[_resultControls.Length];
                    for (int i = 0; i < _resultControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }
    }

    public class SearchResultReferenceCollection : ReadOnlyCollectionBase
    {
        internal SearchResultReferenceCollection()
        {
        }

        public SearchResultReference this[int index]
        {
            get
            {
                return (SearchResultReference)InnerList[index];
            }
        }

        internal int Add(SearchResultReference reference)
        {
            return InnerList.Add(reference);
        }

        public bool Contains(SearchResultReference value)
        {
            return InnerList.Contains(value);
        }

        public int IndexOf(SearchResultReference value)
        {
            return InnerList.IndexOf(value);
        }

        public void CopyTo(SearchResultReference[] values, int index)
        {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
    }

    public class SearchResultEntry
    {
        private string _distinguishedName = null;
        private SearchResultAttributeCollection _attributes = new SearchResultAttributeCollection();
        private DirectoryControl[] _resultControls = null;

        internal SearchResultEntry(string dn, SearchResultAttributeCollection attrs)
        {
            _distinguishedName = dn;
            _attributes = attrs;
        }

        internal SearchResultEntry(string dn)
        {
            _distinguishedName = dn;
        }

        public string DistinguishedName
        {
            get
            {
                return _distinguishedName;
            }
        }

        public SearchResultAttributeCollection Attributes
        {
            get
            {
                return _attributes;
            }
        }

        public DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;

                if (_resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[_resultControls.Length];
                    for (int i = 0; i < _resultControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }
    }

    public class SearchResultEntryCollection : ReadOnlyCollectionBase
    {
        internal SearchResultEntryCollection()
        {
        }

        public SearchResultEntry this[int index]
        {
            get
            {
                return (SearchResultEntry)InnerList[index];
            }
        }

        internal int Add(SearchResultEntry entry)
        {
            return InnerList.Add(entry);
        }

        public bool Contains(SearchResultEntry value)
        {
            return InnerList.Contains(value);
        }

        public int IndexOf(SearchResultEntry value)
        {
            return InnerList.IndexOf(value);
        }

        public void CopyTo(SearchResultEntry[] values, int index)
        {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
    }
}

