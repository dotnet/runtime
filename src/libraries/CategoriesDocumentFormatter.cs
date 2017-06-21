// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.Xml;
    using System.Runtime.CompilerServices;
    using System;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [DataContract]
    public abstract class CategoriesDocumentFormatter
    {
        CategoriesDocument document;

        protected CategoriesDocumentFormatter()
        {
        }
        protected CategoriesDocumentFormatter(CategoriesDocument documentToWrite)
        {
            if (documentToWrite == null)
            {
                throw new ArgumentNullException("documentToWrite");
            }
            this.document = documentToWrite;
        }

        public CategoriesDocument Document
        {
            get { return this.document; }
        }

        public abstract string Version
        { get; }

        public abstract bool CanRead(XmlReader reader);
        public abstract void ReadFrom(XmlReader reader);
        public abstract void WriteTo(XmlWriter writer);

        protected virtual InlineCategoriesDocument CreateInlineCategoriesDocument()
        {
            return new InlineCategoriesDocument();
        }

        protected virtual ReferencedCategoriesDocument CreateReferencedCategoriesDocument()
        {
            return new ReferencedCategoriesDocument();
        }

        protected virtual void SetDocument(CategoriesDocument document)
        {
            this.document = document;
        }
    }
}
