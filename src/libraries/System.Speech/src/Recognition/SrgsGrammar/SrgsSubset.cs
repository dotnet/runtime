// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    // Note that currently if multiple words are stored in a Subset they are treated internally
    // and in the result as multiple tokens.
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsSubset : SrgsElement, ISubset
    {
        #region Constructors
        public SrgsSubset(string text)
            : this(text, SubsetMatchingMode.Subsequence)
        {
        }
        public SrgsSubset(string text, SubsetMatchingMode matchingMode)
        {
            Helpers.ThrowIfEmptyOrNull(text, nameof(text));

            if (matchingMode != SubsetMatchingMode.OrderedSubset && matchingMode != SubsetMatchingMode.Subsequence && matchingMode != SubsetMatchingMode.OrderedSubsetContentRequired && matchingMode != SubsetMatchingMode.SubsequenceContentRequired)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidSubsetAttribute), nameof(matchingMode));
            }

            _matchMode = matchingMode;

            _text = text.Trim(Helpers._achTrimChars);
            Helpers.ThrowIfEmptyOrNull(_text, nameof(text));
        }

        #endregion

        #region public Properties
        public SubsetMatchingMode MatchingMode
        {
            get
            {
                return _matchMode;
            }
            set
            {
                if (value != SubsetMatchingMode.OrderedSubset && value != SubsetMatchingMode.Subsequence && value != SubsetMatchingMode.OrderedSubsetContentRequired && value != SubsetMatchingMode.SubsequenceContentRequired)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidSubsetAttribute), nameof(value));
                }

                _matchMode = value;
            }
        }
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));
                value = value.Trim(Helpers._achTrimChars);
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));

                _text = value;
            }
        }

        #endregion

        #region Internal methods
        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write <token sapi:pron="_pronunciation">_text</token>
            writer.WriteStartElement("sapi", "subset", XmlParser.sapiNamespace);

            if (_matchMode != SubsetMatchingMode.Subsequence)
            {
                string sMatchMode = null;
                switch (_matchMode)
                {
                    case SubsetMatchingMode.Subsequence:
                        sMatchMode = "subsequence";
                        break;

                    case SubsetMatchingMode.OrderedSubset:
                        sMatchMode = "ordered-subset";
                        break;

                    case SubsetMatchingMode.SubsequenceContentRequired:
                        sMatchMode = "subsequence-content-required";
                        break;

                    case SubsetMatchingMode.OrderedSubsetContentRequired:
                        sMatchMode = "ordered-subset-content-required";
                        break;
                }

                writer.WriteAttributeString("sapi", "match", XmlParser.sapiNamespace, sMatchMode);
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
            grammar.HasSapiExtension = true;

            base.Validate(grammar);
        }

        internal override string DebuggerDisplayString()
        {
            return _text + " [" + _matchMode.ToString() + "]";
        }

        #endregion

        #region Private Fields

        private SubsetMatchingMode _matchMode;

        private string _text;

        #endregion
    }
}
