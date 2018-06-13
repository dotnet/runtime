// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class TextSyndicationContent : SyndicationContent
    {
        private TextSyndicationContentKind _textKind;

        public TextSyndicationContent(string text) : this(text, TextSyndicationContentKind.Plaintext)
        {
        }

        public TextSyndicationContent(string text, TextSyndicationContentKind textKind)
        {
            if (textKind < TextSyndicationContentKind.Plaintext || textKind > TextSyndicationContentKind.XHtml)
            {
                throw new ArgumentOutOfRangeException(nameof(textKind));
            }

            Text = text;
            _textKind = textKind;
        }

        protected TextSyndicationContent(TextSyndicationContent source) : base(source)
        {
            Debug.Assert(source != null, "The base constructor already checks if source is valid.");
            Text = source.Text;
            _textKind = source._textKind;
        }

        public string Text { get; }

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

        public override SyndicationContent Clone() => new TextSyndicationContent(this);

        protected override void WriteContentsTo(XmlWriter writer)
        {
            string val = Text ?? string.Empty;
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
