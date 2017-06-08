// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class InlineCategoriesDocument : CategoriesDocument
    {
        Collection<SyndicationCategory> categories;
        bool isFixed;
        string scheme;

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
                this.categories = new NullNotAllowedCollection<SyndicationCategory>();
                foreach (SyndicationCategory category in categories)
                {
                    this.categories.Add(category);
                }
            }
            this.isFixed = isFixed;
            this.scheme = scheme;
        }

        public Collection<SyndicationCategory> Categories
        {
            get
            {
                if (this.categories == null)
                {
                    this.categories = new NullNotAllowedCollection<SyndicationCategory>();
                }
                return this.categories;
            }
        }

        public bool IsFixed
        {
            get { return this.isFixed; }
            set { this.isFixed = value; }
        }

        public string Scheme
        {
            get { return this.scheme; }
            set { this.scheme = value; }
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
