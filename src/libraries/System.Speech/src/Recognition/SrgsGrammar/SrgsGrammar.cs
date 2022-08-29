// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Xml;

#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    internal sealed class SrgsGrammar : IGrammar
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Grammar class.
        /// </summary>
        internal SrgsGrammar()
        {
            _rules = new SrgsRulesCollection();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Write the XML fragment describing the object.
        /// </summary>
        /// <param name="writer">XmlWriter to which to write the XML fragment.</param>
        internal void WriteSrgs(XmlWriter writer)
        {
            // Write <grammar
            //          version="1.0"
            //          xml:lang="en"
            //          mode="voice"
            //          xmlns="http://www.w3.org/2001/06/grammar"
            //          xmlns:sapi="http://schemas.microsoft.com/Speech/2002/06/SRGSExtensions"
            //          root="myRule"
            //          xml:base="http://www.example.com/base-file-path">
            writer.WriteStartElement("grammar", XmlParser.srgsNamespace);
            writer.WriteAttributeString("xml", "lang", null, _culture.ToString());

            if (_root != null)
            {
                writer.WriteAttributeString("root", _root.Id);
            }

            // Write the attributes for strongly typed grammars
            WriteSTGAttributes(writer);
            if (_isModeSet)
            {
                switch (_mode)
                {
                    case SrgsGrammarMode.Voice:
                        writer.WriteAttributeString("mode", "voice");
                        break;

                    case SrgsGrammarMode.Dtmf:
                        writer.WriteAttributeString("mode", "dtmf");
                        break;
                }
            }

            // Write the tag format if any
            string tagFormat = null;
            switch (_tagFormat)
            {
                case SrgsTagFormat.Default:
                    // Nothing to do
                    break;

                case SrgsTagFormat.MssV1:
                    tagFormat = "semantics-ms/1.0";
                    break;

                case SrgsTagFormat.W3cV1:
                    tagFormat = "semantics/1.0";
                    break;

                case SrgsTagFormat.KeyValuePairs:
                    tagFormat = "properties-ms/1.0";
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown Tag Format!!!");
                    break;
            }

            if (tagFormat != null)
            {
                writer.WriteAttributeString("tag-format", tagFormat);
            }

            // Write the Alphabet type if not SAPI
            if (_hasPhoneticAlphabetBeenSet || (_phoneticAlphabet != SrgsPhoneticAlphabet.Sapi && HasPronunciation))
            {
                string alphabet = _phoneticAlphabet == SrgsPhoneticAlphabet.Ipa ? "ipa" : _phoneticAlphabet == SrgsPhoneticAlphabet.Ups ? "x-microsoft-ups" : "x-microsoft-sapi";

                writer.WriteAttributeString("sapi", "alphabet", XmlParser.sapiNamespace, alphabet);
            }

            if (_xmlBase != null)
            {
                writer.WriteAttributeString("xml:base", _xmlBase.ToString());
            }

            writer.WriteAttributeString("version", "1.0");

            writer.WriteAttributeString("xmlns", XmlParser.srgsNamespace);

            if (_isSapiExtensionUsed)
            {
                writer.WriteAttributeString("xmlns", "sapi", null, XmlParser.sapiNamespace);
            }

            foreach (SrgsRule rule in _rules)
            {
                // Validate child _rules
                rule.Validate(this);
            }

            // Write the tag elements if any
            foreach (string tag in _globalTags)
            {
                writer.WriteElementString("tag", tag);
            }

            //Write the references to the referenced assemblies and the various scripts
            WriteGrammarElements(writer);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Validate the SRGS element.
        /// </summary>
        internal void Validate()
        {
            // Validation set the pronunciation so reset it to zero
            HasPronunciation = HasSapiExtension = false;

            // validate all the rules
            foreach (SrgsRule rule in _rules)
            {
                // Validate child _rules
                rule.Validate(this);
            }

            // Initial values for ContainsCOde and SapiExtensionUsed.
            _isSapiExtensionUsed |= HasPronunciation;
            _fContainsCode |= _language != null || _script.Length > 0 || _usings.Count > 0 || _assemblyReferences.Count > 0 || _codebehind.Count > 0 || _namespace != null || _fDebug;
            _isSapiExtensionUsed |= _fContainsCode;
            // If the grammar contains no pronunciations, set the phonetic alphabet to SAPI.
            // This way, the CFG data can be loaded by SAPI 5.1.
            if (!HasPronunciation)
            {
                PhoneticAlphabet = AlphabetType.Sapi;
            }

            // Validate root rule reference
            if (_root != null)
            {
                if (!_rules.Contains(_root))
                {
                    XmlParser.ThrowSrgsException(SRID.RootNotDefined, _root.Id);
                }
            }

            if (_globalTags.Count > 0)
            {
                _tagFormat = SrgsTagFormat.W3cV1;
            }

            // Force the tag format to Sapi properties if .NET semantics are used.
            if (_fContainsCode)
            {
                if (_tagFormat == SrgsTagFormat.Default)
                {
                    _tagFormat = SrgsTagFormat.KeyValuePairs;
                }

                // SAPI semantics only for .NET Semantics
                if (_tagFormat != SrgsTagFormat.KeyValuePairs)
                {
                    XmlParser.ThrowSrgsException(SRID.InvalidSemanticProcessingType);
                }
            }
        }

        IRule IGrammar.CreateRule(string id, RulePublic publicRule, RuleDynamic dynamic, bool hasScript)
        {
            SrgsRule rule = new(id);
            if (publicRule != RulePublic.NotSet)
            {
                rule.Scope = publicRule == RulePublic.True ? SrgsRuleScope.Public : SrgsRuleScope.Private;
            }
            rule.Dynamic = dynamic;
            return rule;
        }

        void IElement.PostParse(IElement parent)
        {
            // Check that the root rule is defined
            if (_sRoot != null)
            {
                bool found = false;
                foreach (SrgsRule rule in Rules)
                {
                    if (rule.Id == _sRoot)
                    {
                        Root = rule;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // "Root rule ""%s"" is undefined."
                    XmlParser.ThrowSrgsException(SRID.RootNotDefined, _sRoot);
                }
            }

            // Resolve the references to the scripts
            foreach (XmlParser.ForwardReference script in _scriptsForwardReference)
            {
                SrgsRule rule = Rules[script._name];
                if (rule != null)
                {
                    rule.Script += script._value;
                }
                else
                {
                    XmlParser.ThrowSrgsException(SRID.InvalidScriptDefinition);
                }
            }
            // Validate the whole grammar
            Validate();
        }

#pragma warning disable 56507 // check for null or empty strings

        // Add a script to this grammar or to a rule
        internal void AddScript(string rule, string code)
        {
            if (rule == null)
            {
                _script += code;
            }
            else
            {
                _scriptsForwardReference.Add(new XmlParser.ForwardReference(rule, code));
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Sets the Root element
        /// </summary>
        string IGrammar.Root
        {
            get
            {
                return _sRoot;
            }
            set
            {
                _sRoot = value;
            }
        }

        /// <summary>
        /// Base URI of grammar (xml:base)
        /// </summary>
        public Uri XmlBase
        {
            get
            {
                return _xmlBase;
            }
            set
            {
                _xmlBase = value;
            }
        }

        /// <summary>
        /// Grammar language (xml:lang)
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                Helpers.ThrowIfNull(value, nameof(value));

                _culture = value;
            }
        }

        /// <summary>
        /// Grammar mode.  voice or dtmf
        /// </summary>
        public GrammarType Mode
        {
            get
            {
                return _mode == SrgsGrammarMode.Voice ? GrammarType.VoiceGrammar : GrammarType.DtmfGrammar;
            }
            set
            {
                _mode = value == GrammarType.VoiceGrammar ? SrgsGrammarMode.Voice : SrgsGrammarMode.Dtmf;
                _isModeSet = true;
            }
        }

        /// <summary>
        /// Pronunciation Alphabet, IPA or SAPI or UPS
        /// </summary>
        public AlphabetType PhoneticAlphabet
        {
            get
            {
                return (AlphabetType)_phoneticAlphabet;
            }
            set
            {
                _phoneticAlphabet = (SrgsPhoneticAlphabet)value;
            }
        }

        /// <summary>root
        /// Root rule (srgs:root)
        /// </summary>
        public SrgsRule Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
            }
        }

        /// <summary>
        /// Tag format (srgs:tag-format)
        /// </summary>
        public SrgsTagFormat TagFormat
        {
            get
            {
                return _tagFormat;
            }
            set
            {
                _tagFormat = value;
            }
        }

        /// <summary>
        /// Tag format (srgs:tag-format)
        /// </summary>
        public Collection<string> GlobalTags
        {
            get
            {
                return _globalTags;
            }
            set
            {
                _globalTags = value;
            }
        }

        /// <summary>
        /// language
        /// </summary>
        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                _language = value;
            }
        }

        /// <summary>
        /// namespace
        /// </summary>
        public string Namespace
        {
            get
            {
                return _namespace;
            }
            set
            {
                _namespace = value;
            }
        }

        /// <summary>
        /// CodeBehind
        /// </summary>
        public Collection<string> CodeBehind
        {
            get
            {
                return _codebehind;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Add #line statements to the inline scripts if set
        /// </summary>
        public bool Debug
        {
            get
            {
                return _fDebug;
            }
            set
            {
                _fDebug = value;
            }
        }

        /// <summary>
        /// Scripts
        /// </summary>
        public string Script
        {
            get
            {
                return _script;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));
                _script = value;
            }
        }

        /// <summary>
        /// ImportNameSpaces
        /// </summary>
        public Collection<string> ImportNamespaces
        {
            get
            {
                return _usings;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// ImportNameSpaces
        /// </summary>
        public Collection<string> AssemblyReferences
        {
            get
            {
                return _assemblyReferences;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }
        #endregion

        #region Internal Properties

        /// <summary>
        /// A collection of _rules that this grammar houses.
        /// </summary>
        internal SrgsRulesCollection Rules
        {
            get
            {
                return _rules;
            }
        }

        /// <summary>
        /// A collection of _rules that this grammar houses.
        /// </summary>
        internal bool HasPronunciation
        {
            get
            {
                return _hasPronunciation;
            }
            set
            {
                _hasPronunciation = value;
            }
        }

        /// <summary>
        /// A collection of _rules that this grammar houses.
        /// </summary>
        internal bool HasPhoneticAlphabetBeenSet
        {
            set
            {
                _hasPhoneticAlphabetBeenSet = value;
            }
        }

        /// <summary>
        /// A collection of _rules that this grammar houses.
        /// </summary>
        internal bool HasSapiExtension
        {
            get
            {
                return _isSapiExtensionUsed;
            }
            set
            {
                _isSapiExtensionUsed = value;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Write the attributes of the grammar element for strongly typed grammars
        /// </summary>
        private void WriteSTGAttributes(XmlWriter writer)
        {
            // Write the 'language' attribute
            if (_language != null)
            {
                writer.WriteAttributeString("sapi", "language", XmlParser.sapiNamespace, _language);
            }

            // Write the 'namespace' attribute
            if (_namespace != null)
            {
                writer.WriteAttributeString("sapi", "namespace", XmlParser.sapiNamespace, _namespace);
            }

            // Write the 'codebehind' attribute
            foreach (string sFile in _codebehind)
            {
                if (!string.IsNullOrEmpty(sFile))
                {
                    writer.WriteAttributeString("sapi", "codebehind", XmlParser.sapiNamespace, sFile);
                }
            }

            // Write the 'debug' attribute
            if (_fDebug)
            {
                writer.WriteAttributeString("sapi", "debug", XmlParser.sapiNamespace, "True");
            }
        }

        /// <summary>
        /// Write the references to the referenced assemblies and the various scripts
        /// </summary>
        private void WriteGrammarElements(XmlWriter writer)
        {
            // Write all the <assmblyReference> entries
            foreach (string sAssembly in _assemblyReferences)
            {
                writer.WriteStartElement("sapi", "assemblyReference", XmlParser.sapiNamespace);
                writer.WriteAttributeString("sapi", "assembly", XmlParser.sapiNamespace, sAssembly);
                writer.WriteEndElement();
            }

            // Write all the <assmblyReference> entries
            foreach (string sNamespace in _usings)
            {
                if (!string.IsNullOrEmpty(sNamespace))
                {
                    writer.WriteStartElement("sapi", "importNamespace", XmlParser.sapiNamespace);
                    writer.WriteAttributeString("sapi", "namespace", XmlParser.sapiNamespace, sNamespace);
                    writer.WriteEndElement();
                }
            }
            // Then write the rules
            WriteRules(writer);

            // At the very bottom write the scripts shared by all the rules
            WriteGlobalScripts(writer);
        }

        /// <summary>
        /// Write all Rules.
        /// </summary>
        private void WriteRules(XmlWriter writer)
        {
            // Write <grammar> body and footer.
            foreach (SrgsRule rule in _rules)
            {
                rule.WriteSrgs(writer);
            }
        }

        /// <summary>
        /// Write the script that are global to this grammar
        /// </summary>
        private void WriteGlobalScripts(XmlWriter writer)
        {
            if (_script.Length > 0)
            {
                writer.WriteStartElement("sapi", "script", XmlParser.sapiNamespace);
                writer.WriteCData(_script);
                writer.WriteEndElement();
            }
        }
        #endregion

        #region Private Fields

        private bool _isSapiExtensionUsed;  // Set in *.Validate()

        private Uri _xmlBase;

        private CultureInfo _culture = CultureInfo.CurrentUICulture;

        private SrgsGrammarMode _mode = SrgsGrammarMode.Voice;

        private SrgsPhoneticAlphabet _phoneticAlphabet = SrgsPhoneticAlphabet.Ipa;

        private bool _hasPhoneticAlphabetBeenSet;

        private bool _hasPronunciation;

        private SrgsRule _root;

        private SrgsTagFormat _tagFormat = SrgsTagFormat.Default;

        private Collection<string> _globalTags = new();

        private bool _isModeSet;

        private SrgsRulesCollection _rules;

        private string _sRoot;

        internal bool _fContainsCode;  // Set in *.Validate()

        // .NET Language for this grammar
        private string _language;

        // .NET Language for this grammar
        private Collection<string> _codebehind = new();

        // namespace for the code behind
        private string _namespace;

        // Insert #line statements in the sources code if set
        internal bool _fDebug;

        // .NET language script
        private string _script = string.Empty;

        // .NET language script
        private List<XmlParser.ForwardReference> _scriptsForwardReference = new();

        // .NET Namespaces to import
        private Collection<string> _usings = new();

        // .NET Namespaces to import
        private Collection<string> _assemblyReferences = new();
        #endregion

    }
}
