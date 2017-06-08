//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System;
    using System.Xml;
    using System.Xml.Schema;
    //using System.ServiceModel.Channels;
    //using System.ServiceModel.Diagnostics;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = App10Constants.Categories, Namespace = App10Constants.Namespace)]
    public class AtomPub10CategoriesDocumentFormatter : CategoriesDocumentFormatter, IXmlSerializable
    {
        Type inlineDocumentType;
        int maxExtensionSize;
        bool preserveAttributeExtensions;
        bool preserveElementExtensions;
        Type referencedDocumentType;

        public AtomPub10CategoriesDocumentFormatter()
            : this(typeof(InlineCategoriesDocument), typeof(ReferencedCategoriesDocument))
        {
        }

        public AtomPub10CategoriesDocumentFormatter(Type inlineDocumentType, Type referencedDocumentType)
            : base()
        {
            if (inlineDocumentType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("inlineDocumentType");
            }
            if (!typeof(InlineCategoriesDocument).IsAssignableFrom(inlineDocumentType))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("inlineDocumentType",
                    SR.GetString(SR.InvalidObjectTypePassed, "inlineDocumentType", "InlineCategoriesDocument"));
            }
            if (referencedDocumentType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("referencedDocumentType");
            }
            if (!typeof(ReferencedCategoriesDocument).IsAssignableFrom(referencedDocumentType))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("referencedDocumentType",
                    SR.GetString(SR.InvalidObjectTypePassed, "referencedDocumentType", "ReferencedCategoriesDocument"));
            }
            this.maxExtensionSize = int.MaxValue;
            this.preserveAttributeExtensions = true;
            this.preserveElementExtensions = true;
            this.inlineDocumentType = inlineDocumentType;
            this.referencedDocumentType = referencedDocumentType;
        }

        public AtomPub10CategoriesDocumentFormatter(CategoriesDocument documentToWrite)
            : base(documentToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            this.maxExtensionSize = int.MaxValue;
            preserveAttributeExtensions = true;
            preserveElementExtensions = true;
            if (documentToWrite.IsInline)
            {
                this.inlineDocumentType = documentToWrite.GetType();
                this.referencedDocumentType = typeof(ReferencedCategoriesDocument);
            }
            else
            {
                this.referencedDocumentType = documentToWrite.GetType();
                this.inlineDocumentType = typeof(InlineCategoriesDocument);
            }
        }

        public override string Version
        {
            get { return App10Constants.Namespace; }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            return reader.IsStartElement(App10Constants.Categories, App10Constants.Namespace);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            TraceCategoriesDocumentReadBegin();
            ReadDocument(reader);
            TraceCategoriesDocumentReadEnd();
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.Document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(SR.DocumentFormatterDoesNotHaveDocument)));
            }
            TraceCategoriesDocumentWriteBegin();
            WriteDocument(writer);
            TraceCategoriesDocumentWriteEnd();
        }

        public override void ReadFrom(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            if (!CanRead(reader))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.GetString(SR.UnknownDocumentXml, reader.LocalName, reader.NamespaceURI)));
            }
            TraceCategoriesDocumentReadBegin();
            ReadDocument(reader);
            TraceCategoriesDocumentReadEnd();
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.Document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(SR.DocumentFormatterDoesNotHaveDocument)));
            }
            TraceCategoriesDocumentWriteBegin();
            writer.WriteStartElement(App10Constants.Prefix, App10Constants.Categories, App10Constants.Namespace);
            WriteDocument(writer);
            writer.WriteEndElement();
            TraceCategoriesDocumentWriteEnd();
        }

        internal static void TraceCategoriesDocumentReadBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                //TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadCategoriesDocumentBegin, SR.GetString(SR.TraceCodeSyndicationReadCategoriesDocumentBegin));
            }
        }

        internal static void TraceCategoriesDocumentReadEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                //TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadCategoriesDocumentEnd, SR.GetString(SR.TraceCodeSyndicationReadCategoriesDocumentEnd));
            }
        }

        internal static void TraceCategoriesDocumentWriteBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                //TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteCategoriesDocumentBegin, SR.GetString(SR.TraceCodeSyndicationWriteCategoriesDocumentBegin));
            }
        }

        internal static void TraceCategoriesDocumentWriteEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                //TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteCategoriesDocumentEnd, SR.GetString(SR.TraceCodeSyndicationWriteCategoriesDocumentEnd));
            }
        }

        protected override InlineCategoriesDocument CreateInlineCategoriesDocument()
        {
            if (inlineDocumentType == typeof(InlineCategoriesDocument))
            {
                return new InlineCategoriesDocument();
            }
            else
            {
                return (InlineCategoriesDocument)Activator.CreateInstance(this.inlineDocumentType);
            }
        }

        protected override ReferencedCategoriesDocument CreateReferencedCategoriesDocument()
        {
            if (referencedDocumentType == typeof(ReferencedCategoriesDocument))
            {
                return new ReferencedCategoriesDocument();
            }
            else
            {
                return (ReferencedCategoriesDocument)Activator.CreateInstance(this.referencedDocumentType);
            }
        }

        void ReadDocument(XmlReader reader)
        {
            try
            {
                SyndicationFeedFormatter.MoveToStartElement(reader);
                SetDocument(AtomPub10ServiceDocumentFormatter.ReadCategories(reader, null,
                    delegate()
                    {
                        return this.CreateInlineCategoriesDocument();
                    },

                    delegate()
                    {
                        return this.CreateReferencedCategoriesDocument();
                    },
                    this.Version,
                    this.preserveElementExtensions,
                    this.preserveAttributeExtensions,
                    this.maxExtensionSize));
            }
            catch (FormatException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e));
            }
            catch (ArgumentException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e));
            }
        }

        void WriteDocument(XmlWriter writer)
        {
            // declare the atom10 namespace upfront for compactness
            writer.WriteAttributeString(Atom10Constants.Atom10Prefix, Atom10FeedFormatter.XmlNsNs, Atom10Constants.Atom10Namespace);
            AtomPub10ServiceDocumentFormatter.WriteCategoriesInnerXml(writer, this.Document, null, this.Version);
        }
    }
}
