// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    [ImmutableObject(true)]
    [DebuggerDisplay("{DebuggerDisplayString()}")]
    public class SrgsRuleRef : SrgsElement, IRuleRef
    {
        #region Constructors
        public SrgsRuleRef(Uri uri)
        {
            UriInit(uri, null, null, null);
        }
        public SrgsRuleRef(Uri uri, string rule)
        {
            Helpers.ThrowIfEmptyOrNull(rule, nameof(rule));

            UriInit(uri, rule, null, null);
        }
        public SrgsRuleRef(Uri uri, string rule, string semanticKey)
        {
            Helpers.ThrowIfEmptyOrNull(semanticKey, nameof(semanticKey));

            UriInit(uri, rule, semanticKey, null);
        }
        public SrgsRuleRef(Uri uri, string rule, string semanticKey, string parameters)
        {
            Helpers.ThrowIfEmptyOrNull(parameters, nameof(parameters));

            UriInit(uri, rule, semanticKey, parameters);
        }
        public SrgsRuleRef(SrgsRule rule)
        {
            Helpers.ThrowIfNull(rule, nameof(rule));

            _uri = new Uri("#" + rule.Id, UriKind.Relative);
        }
        public SrgsRuleRef(SrgsRule rule, string semanticKey)
            : this(rule)
        {
            Helpers.ThrowIfEmptyOrNull(semanticKey, nameof(semanticKey));

            _semanticKey = semanticKey;
        }
        public SrgsRuleRef(SrgsRule rule, string semanticKey, string parameters)
            : this(rule)
        {
            Helpers.ThrowIfEmptyOrNull(parameters, nameof(parameters));

            _semanticKey = semanticKey;
            _params = parameters;
        }

        /// <summary>
        /// Special private constructor for Special Rulerefs
        /// </summary>
        private SrgsRuleRef(SpecialRuleRefType type)
        {
            _type = type;
        }

        internal SrgsRuleRef(string semanticKey, string parameters, Uri uri)
        {
            _uri = uri;
            _semanticKey = semanticKey;
            _params = parameters;
        }

        #endregion

        #region public Properties
        // Uri of the rule this rule reference references.
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Set the semanticKey for a Ruleref
        /// </summary>
        public string SemanticKey
        {
            get
            {
                return _semanticKey;
            }
        }

        /// <summary>
        /// Set the init parameters for a Ruleref
        /// </summary>
        public string Params
        {
            get
            {
                return _params;
            }
        }
        // The Null SpecialRuleRef defines a rule that is automatically matched:
        // that is, matched without the user speaking any word.
        public static readonly SrgsRuleRef Null = new(SpecialRuleRefType.Null);
        // The Void SpecialRuleRef defines a rule that can never be spoken. Inserting
        // VOID into a sequence automatically makes that sequence unspeakable.
        public static readonly SrgsRuleRef Void = new(SpecialRuleRefType.Void);
        // The Garbage SpecialRuleRef defines a rule that may match any speech up until
        // the next rule match, the next token or until the end of spoken input.
        public static readonly SrgsRuleRef Garbage = new(SpecialRuleRefType.Garbage);
        public static readonly SrgsRuleRef Dictation = new(new Uri("grammar:dictation"));
        public static readonly SrgsRuleRef MnemonicSpelling = new(new Uri("grammar:dictation#spelling"));

        #endregion

        #region Internal methods

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write <ruleref _uri="_uri" />
            writer.WriteStartElement("ruleref");
            if (_uri != null)
            {
                writer.WriteAttributeString("uri", _uri.ToString());
            }
            else
            {
                string special;
                switch (_type)
                {
                    case SpecialRuleRefType.Null:
                        special = "NULL";
                        break;

                    case SpecialRuleRefType.Void:
                        special = "VOID";
                        break;

                    case SpecialRuleRefType.Garbage:
                        special = "GARBAGE";
                        break;

                    default:
                        XmlParser.ThrowSrgsException(SRID.InvalidSpecialRuleRef);
                        special = null;
                        break;
                }
                writer.WriteAttributeString("special", special);
            }

            // Write the 'name' attribute
            if (_semanticKey != null)
            {
                writer.WriteAttributeString("sapi", "semantic-key", XmlParser.sapiNamespace, _semanticKey);
            }

            // Write the 'params' attribute
            if (_params != null)
            {
                writer.WriteAttributeString("sapi", "params", XmlParser.sapiNamespace, _params);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Validate the SRGS element.
        /// </summary>
        internal override void Validate(SrgsGrammar grammar)
        {
            bool fScript = _params != null || _semanticKey != null;
            grammar._fContainsCode |= fScript;
            grammar.HasSapiExtension |= fScript;

            // Validate _uri
            if (_uri != null)
            {
                string sUri = _uri.ToString();
                if (sUri[0] == '#')
                {
                    bool uriFound = false;
                    if (sUri.IndexOf("#grammar:dictation", StringComparison.Ordinal) == 0 || sUri.IndexOf("#grammar:dictation#spelling", StringComparison.Ordinal) == 0)
                    {
                        uriFound = true;
                    }
                    else
                    {
                        sUri = sUri.Substring(1);
                        foreach (SrgsRule rule in grammar.Rules)
                        {
                            if (rule.Id == sUri)
                            {
                                uriFound = true;
                                break;
                            }
                        }
                    }

                    if (!uriFound)
                    {
                        XmlParser.ThrowSrgsException(SRID.UndefRuleRef, sUri);
                    }
                }
            }

            base.Validate(grammar);
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new("SrgsRuleRef");
            if (_uri != null)
            {
                sb.Append(" uri='");
                sb.Append(_uri.ToString());
                sb.Append('\'');
            }
            else
            {
                sb.Append(" special='");
                sb.Append(_type.ToString());
                sb.Append('\'');
            }
            return sb.ToString();
        }

        #endregion

        #region Private Method

        /// <summary>
        /// Call by constructors. No check is made on the parameters except for the Uri
        /// </summary>
        private void UriInit(Uri uri, string rule, string semanticKey, string initParameters)
        {
            Helpers.ThrowIfNull(uri, nameof(uri));

            if (string.IsNullOrEmpty(rule))
            {
                _uri = uri;
            }
            else
            {
                _uri = new Uri(uri.ToString() + "#" + rule, UriKind.RelativeOrAbsolute);
            }
            _semanticKey = semanticKey;
            _params = initParameters;
        }

        #endregion

        #region Private Fields

        #region Private Enums
        // Special rule references allow grammars based on CFGs to have powerful
        // additional features, such as transitions into dictation (both recognized
        // or not recognized) and word sequences from SAPI 5.0.
        private enum SpecialRuleRefType
        {
            // Defines a rule that is automatically matched that is, matched without
            // the user speaking any word.
            Null,
            // Defines a rule that can never be spoken. Inserting VOID into a sequence
            // automatically makes that sequence unspeakable.
            Void,
            // Defines a rule that may match any speech up until the next rule match,
            // the next token or until the end of spoken input.
            // Designed for applications that would like to recognize some phrases
            // without failing due to irrelevant, or ignorable words.
            Garbage,
        }

        #endregion

        // if the uri is null then it is a special rule ref
        private Uri _uri;

        private SpecialRuleRefType _type;

        // Alias string for the semantic dictionary
        private string _semanticKey;

        // Alias string for the semantic dictionary
        private string _params;

        #endregion
    }
}
