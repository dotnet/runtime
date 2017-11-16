// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class TextSyndicationContent : SyndicationContent
    {
        private string _text;
        private TextSyndicationContentKind _textKind;

        public TextSyndicationContent(string text) : this(text, TextSyndicationContentKind.Plaintext)
        {
        }

        public TextSyndicationContent(string text, TextSyndicationContentKind textKind)
        {
            if (!TextSyndicationContentKindHelper.IsDefined(textKind))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("textKind"));
            }
            _text = text;
            _textKind = textKind;
        }

        protected TextSyndicationContent(TextSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            _text = source._text;
            _textKind = source._textKind;
        }

        public string Text
        {
            get { return _text; }
        }

        public override string Type
        {
            get
            {
                switch (_textKind)
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
            string val = _text ?? string.Empty;
            if (_textKind == TextSyndicationContentKind.XHtml)
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
