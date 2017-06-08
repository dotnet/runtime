//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class TextSyndicationContent : SyndicationContent
    {
        string text;
        TextSyndicationContentKind textKind;

        public TextSyndicationContent(string text) : this(text, TextSyndicationContentKind.Plaintext)
        {
        }

        public TextSyndicationContent(string text, TextSyndicationContentKind textKind)
        {
            if (!TextSyndicationContentKindHelper.IsDefined(textKind))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("textKind"));
            }
            this.text = text;
            this.textKind = textKind;
        }

        protected TextSyndicationContent(TextSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.text = source.text;
            this.textKind = source.textKind;
        }

        public string Text
        {
            get { return this.text; }
        }

        public override string Type
        {
            get
            {
                switch (this.textKind)
                {
                    case TextSyndicationContentKind.Html:
                        return Atom10Constants.HtmlType;
                    case TextSyndicationContentKind.XHtml:
                        return Atom10Constants.XHtmlType;
                    default:
                        return Atom10Constants.PlaintextType;
                }
            }
        }

        public override SyndicationContent Clone()
        {
            return new TextSyndicationContent(this);
        }

        protected override void WriteContentsTo(XmlWriter writer)
        {
            string val = this.text ?? string.Empty;
            if (this.textKind == TextSyndicationContentKind.XHtml)
            {
                writer.WriteRaw(val);
            }
            else
            {
                writer.WriteString(val);
            }
        }
    }
}
