// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    // Note that currently if multiple words are stored in a Token they are treated internally
    // and in the result as multiple tokens.
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsToken : SrgsElement, IToken
    {
        #region Constructors
        public SrgsToken(string text)
        {
            Helpers.ThrowIfEmptyOrNull(text, nameof(text));
            Text = text;
        }

        #endregion

        #region public Properties
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));

                // remove all spaces if any
                string text = value.Trim(Helpers._achTrimChars);
                if (string.IsNullOrEmpty(text) || text.Contains('\"'))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidTokenString), nameof(value));
                }
                _text = text;
            }
        }
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));
                _pronunciation = value;
            }
        }
        public string Display
        {
            get
            {
                return _display;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));
                _display = value;
            }
        }

        #endregion

        #region Internal methods

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write <token sapi:pron="_pronunciation">_text</token>
            writer.WriteStartElement("token");

            if (_display != null)
            {
                writer.WriteAttributeString("sapi", "display", XmlParser.sapiNamespace, _display);
            }

            if (_pronunciation != null)
            {
                writer.WriteAttributeString("sapi", "pron", XmlParser.sapiNamespace, _pronunciation);
            }

            // If an empty string is provided, skip the WriteString
            // to have the XmlWrite to put <token/> rather than <token></token>
            if (_text != null && _text.Length > 0)
            {
                writer.WriteString(_text);
            }
            writer.WriteEndElement();
        }

        internal override void Validate(SrgsGrammar grammar)
        {
            if (_pronunciation != null || _display != null)
            {
                grammar.HasPronunciation = true;
            }

            // Validate the pronunciation if any
            if (_pronunciation != null)
            {
                for (int iCurPron = 0, iDeliminator = 0; iCurPron < _pronunciation.Length; iCurPron = iDeliminator + 1)
                {
                    // Find semi-colon delimiter and replace with null
                    iDeliminator = _pronunciation.IndexOf(';', iCurPron);
                    if (iDeliminator == -1)
                    {
                        iDeliminator = _pronunciation.Length;
                    }

                    string subPronunciation = _pronunciation.Substring(iCurPron, iDeliminator - iCurPron);

                    // Convert the pronunciation, will throw if error
                    switch (grammar.PhoneticAlphabet)
                    {
                        case AlphabetType.Sapi:
                            PhonemeConverter.ConvertPronToId(subPronunciation, grammar.Culture.LCID);
                            break;

                        case AlphabetType.Ups:
                            PhonemeConverter.UpsConverter.ConvertPronToId(subPronunciation);
                            break;

                        case AlphabetType.Ipa:
                            PhonemeConverter.ValidateUpsIds(subPronunciation.ToCharArray());
                            break;
                    }
                }
            }

            base.Validate(grammar);
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new("Token '");
            sb.Append(_text);
            sb.Append('\'');

            if (_pronunciation != null)
            {
                sb.Append(" Pronunciation '");
                sb.Append(_pronunciation);
                sb.Append('\'');
            }
            return sb.ToString();
        }

        #endregion

        #region Private Fields

        private string _text = string.Empty;

        private string _pronunciation;

        private string _display;

        #endregion
    }
}
