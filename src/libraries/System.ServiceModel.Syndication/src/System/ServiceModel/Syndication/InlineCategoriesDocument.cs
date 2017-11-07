// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
    public class InlineCategoriesDocument : CategoriesDocument
    {
        private Collection<SyndicationCategory> _categories;
        private bool _isFixed;
        private string _scheme;

        public InlineCategoriesDocument()
        {
        }

        public InlineCategoriesDocument(IEnumerable<SyndicationCategory> categories)
            : this(categories, false, null)
        {
        }

        public InlineCategoriesDocument(IEnumerable<SyndicationCategory> categories, bool isFixed, string scheme)
        {
            if (categories != null)
            {
                _categories = new NullNotAllowedCollection<SyndicationCategory>();
                foreach (SyndicationCategory category in categories)
                {
                    _categories.Add(category);
                }
            }
            _isFixed = isFixed;
            _scheme = scheme;
        }

        public Collection<SyndicationCategory> Categories
        {
            get
            {
                if (_categories == null)
                {
                    _categories = new NullNotAllowedCollection<SyndicationCategory>();
                }
                return _categories;
            }
        }

        public bool IsFixed
        {
            get { return _isFixed; }
            set { _isFixed = value; }
        }

        public string Scheme
        {
            get { return _scheme; }
            set { _scheme = value; }
        }

        internal override bool IsInline
        {
            get { return true; }
        }

        internal protected virtual SyndicationCategory CreateCategory()
        {
            return new SyndicationCategory();
        }
    }
}
