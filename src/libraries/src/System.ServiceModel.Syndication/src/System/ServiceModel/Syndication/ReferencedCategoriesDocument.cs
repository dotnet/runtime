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
    public class ReferencedCategoriesDocument : CategoriesDocument
    {
        private Uri _link;

        public ReferencedCategoriesDocument()
        {
        }

        public ReferencedCategoriesDocument(Uri link)
            : base()
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            _link = link;
        }

        public Uri Link
        {
            get { return _link; }
            set { _link = value; }
        }

        internal override bool IsInline
        {
            get { return false; }
        }
    }
}
