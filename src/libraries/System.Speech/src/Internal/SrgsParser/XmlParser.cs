// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;
using System.Xml;

#pragma warning disable 56524 // The _reader and _xmlReader are not created in this module and should not be disposed

// Remove all the check for null or empty warnings

namespace System.Speech.Internal.SrgsParser
{
    internal class XmlParser : ISrgsParser
    {
        #region Constructors

        internal XmlParser(XmlReader reader, Uri uri)
        {
            _reader = reader;
            _xmlTextReader = reader as XmlTextReader;

            // Try to guess the Uri
            if (uri == null)
            {
                // Keep a reference to the filename and XmlTextReader if it is one.
                if (_xmlTextReader != null && _xmlTextReader.BaseURI.Length > 0)
                {
                    try
                    {
                        uri = new Uri(_xmlTextReader.BaseURI);
                    }
#pragma warning disable 56502 // Remove the empty catch statements warnings
                    catch (UriFormatException)
                    {
                    }
#pragma warning restore 56502
                }
            }

            // Saves the path to the file and the file name
            if (uri != null)
            {
                // Saves the full path to the file
                _filename = !uri.IsAbsoluteUri || !uri.IsFile ? uri.OriginalString : uri.LocalPath;

                // Saves the filename without the path
                int iPosSlash = _filename.LastIndexOfAny(s_slashBackSlash);
                _shortFilename = iPosSlash >= 0 ? _filename.Substring(iPosSlash + 1) : _filename;
            }
        }

        #endregion

        #region Internal Methods

        // Initializes the object from a stream containing SRGS in XML
        public void Parse()
        {
            try
            {
                bool isGrammarElementFound = false;

                while (_reader.Read())
                {
                    // Ignore XmlDeclaration, ProcessingInstruction, Comment, DocumentType, Entity, Notation.
                    if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "grammar")
                    {
                        if (_reader.NamespaceURI != srgsNamespace)
                        {
                            ThrowSrgsException(SRID.InvalidSrgsNamespace);
                        }

                        if (isGrammarElementFound)
                        {
                            ThrowSrgsException(SRID.GrammarDefTwice);
                        }
                        else
                        {
                            ParseGrammar(_reader, _parser.Grammar);
                            isGrammarElementFound = true;
                        }
                    }
                }

                if (!isGrammarElementFound)
                {
                    ThrowSrgsException(SRID.InvalidSrgs);
                }
            }
            catch (XmlException eXml)
            {
                _parser.RemoveAllRules();
                ThrowSrgsExceptionWithPosition(_filename, _reader, SR.Get(SRID.InvalidXmlFormat), eXml);
            }
            catch (FormatException e)
            {
                // Adds a placeholder for the rule.
                // Once all the rules and scripts are read, the placeholder will be replaced with the proper rule.
                _parser.RemoveAllRules();
                ThrowSrgsExceptionWithPosition(_filename, _reader, e.Message, e.InnerException);
            }
            catch
            {
                // clear all the rules
                _parser.RemoveAllRules();
                throw;
            }
        }

        /// <summary>
        /// Break the string into individual tokens and ParseToken() each individual token.
        ///
        /// Token string is a sequence of 0 or more white space delimited tokens.
        /// Tokens may also be delimited by double quotes.  In these cases, the double
        /// quotes token must be surrounded by white space or string boundary.
        /// </summary>
        internal static void ParseText(IElement parent, string sChars, string pronunciation, string display, float reqConfidence, CreateTokenCallback createTokens)
        {
            sChars = sChars.Trim(Helpers._achTrimChars);

            char[] achToken = sChars.ToCharArray();
            int iTokenEnd = 0;
            int cChars = sChars.Length;

            for (int i = 0; i < achToken.Length; i = iTokenEnd + 1)
            {
                if (achToken[i] == ' ')                            // Skip white spaces
                {
                    iTokenEnd = i;
                    continue;
                }

                // Find the next token
                if (achToken[i] == '"')
                {
                    // Quoted string.  Find end of quoted string.
                    iTokenEnd = ++i;
                    while ((iTokenEnd < cChars) && (achToken[iTokenEnd] != '"'))
                    {
                        iTokenEnd++;
                    }

                    if (iTokenEnd >= cChars || achToken[iTokenEnd] != '"')
                    {
                        // Cannot find matching double quote.
                        // "Invalid double-quoted string."
                        XmlParser.ThrowSrgsException(SRID.InvalidQuotedString);
                    }

                    if (iTokenEnd + 1 != cChars && achToken[iTokenEnd + 1] != ' ')
                    {
                        // Quoted token not surrounded by whitespace.");
                        // "Invalid double-quoted string."
                        XmlParser.ThrowSrgsException(SRID.InvalidQuotedString);
                    }
                }
                else
                {
                    // Regular token.  Find next white space character or end of string
                    iTokenEnd = i + 1;
                    while ((iTokenEnd < cChars) && achToken[iTokenEnd] != ' ')
                    {
                        iTokenEnd++;
                    }
                }

                string sToken = sChars.Substring(i, iTokenEnd - i);
                if (sToken.IndexOf('"') != -1)
                {
                    // "The token string is not allowed to contain double quote character."
                    XmlParser.ThrowSrgsException(SRID.InvalidTokenString);
                }

                // Parse the token.
                if (createTokens != null)
                {
                    createTokens(parent, sToken, pronunciation, display, reqConfidence);
                }
            }
        }

        /// <summary>
        /// Throws an Exception with the error specified by the resource ID.
        /// Add the line and column number if the XmlReader is a TextReader
        /// </summary>
        internal static void ThrowSrgsException(SRID id, params object[] args)
        {
            throw new FormatException(SR.Get(id, args));
        }

        /// <summary>
        /// Throws an Exception with the error specified by the resource ID.
        /// Add the line and column number if the XmlReader is a TextReader
        /// </summary>
        internal static void ThrowSrgsExceptionWithPosition(string filename, XmlReader xmlReader, string sError, Exception innerException)
        {
            // Add the line and column number if the XmlReader is a XmlTextReader
            XmlTextReader xmlTextReader = xmlReader as XmlTextReader;
            if (xmlTextReader != null)
            {
                string sLine = SR.Get(SRID.Line);
                string sPosition = SR.Get(SRID.Position);
                int line = xmlTextReader.LineNumber;
                int position = xmlTextReader.LinePosition;
                if (filename == null)
                {
                    sError += string.Format(CultureInfo.InvariantCulture, " [{0}={1}, {2}={3}]", sLine, line, sPosition, position);
                }
                else
                {
                    sError = string.Format(CultureInfo.InvariantCulture, "{0}({1},{2}): error : {3}", filename, line, position, sError);
                }
            }
            throw new FormatException(sError, innerException);
        }

        #endregion

        #region Internal Methods

        // Implementation of the internal interface ISrgsParser
        public IElementFactory ElementFactory
        {
            set
            {
                _parser = value;
            }
        }

        #endregion

        #region Internal fields

        internal const string emptyNamespace = "";

        internal const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";

        internal const string srgsNamespace = "http://www.w3.org/2001/06/grammar";

        internal const string sapiNamespace = "http://schemas.microsoft.com/Speech/2002/06/SRGSExtensions";

        #endregion

        #region Private Type

        // Must be a class to be used with generics
        [Serializable]
        internal class ForwardReference
        {
            internal ForwardReference(string name, string value)
            {
                _name = name;
                _value = value;
            }

            internal string _name;
            internal string _value;
        }
        #endregion

        #region Private Methods

        // The perf gain using .Lengh == 0 other  readability is not worth it fixing this FxCop issue
        private void ParseGrammar(XmlReader reader, IGrammar grammar)
        {
            string sAlphabet = null;
            string sLanguage = null;
            string sNamespace = null;
            string sVersion = null;
            GrammarType grammarType = GrammarType.VoiceGrammar;

            // Process attributes.
            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        switch (reader.LocalName)
                        {
                            case "root":
                                if (grammar.Root == null)
                                {
                                    grammar.Root = reader.Value;
                                }
                                else
                                {
                                    ThrowSrgsException(SRID.RootRuleAlreadyDefined);
                                }
                                break;

                            case "version":
                                CheckForDuplicates(ref sVersion, reader);
                                if (sVersion != "1.0")
                                {
                                    ThrowSrgsException(SRID.InvalidVersion);
                                }

                                break;

                            case "tag-format":
                                switch (reader.Value)
                                {
                                    case "semantics/1.0":
                                        grammar.TagFormat = SrgsTagFormat.W3cV1;
                                        _hasTagFormat = true;
                                        break;

                                    case "semantics-ms/1.0":
                                        grammar.TagFormat = SrgsTagFormat.MssV1;
                                        _hasTagFormat = true;
                                        break;

                                    case "properties-ms/1.0":
                                        grammar.TagFormat = SrgsTagFormat.KeyValuePairs;
                                        _hasTagFormat = true;
                                        break;

                                    case "":
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidTagFormat);
                                        break;
                                }
                                break;

                            case "mode":
                                switch (reader.Value)
                                {
                                    case "voice":
                                        grammar.Mode = GrammarType.VoiceGrammar;
                                        break;

                                    case "dtmf":
                                        grammarType = grammar.Mode = GrammarType.DtmfGrammar;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidGrammarMode);
                                        break;
                                }
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;

                    case xmlNamespace:
                        switch (reader.LocalName)
                        {
                            case "lang":
                                string language = reader.Value;
                                try
                                {
                                    grammar.Culture = _langId = new CultureInfo(language);
                                }
                                catch (ArgumentException)
                                {
                                    // Unknown Culture info, fall back to the base culture.
                                    int pos = reader.Value.IndexOf("-", StringComparison.Ordinal);
                                    if (pos > 0)
                                    {
                                        grammar.Culture = _langId = new CultureInfo(reader.Value.Substring(0, pos));
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                                break;

                            case "base":
                                grammar.XmlBase = new Uri(reader.Value);
                                break;
                        }
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "alphabet":
                                CheckForDuplicates(ref sAlphabet, reader);
                                switch (sAlphabet)
                                {
                                    case "ipa":
                                        grammar.PhoneticAlphabet = AlphabetType.Ipa;
                                        break;

                                    case "sapi":
                                    case "x-sapi":
                                    case "x-microsoft-sapi":
                                        grammar.PhoneticAlphabet = AlphabetType.Sapi;
                                        break;

                                    case "ups":
                                    case "x-ups":
                                    case "x-microsoft-ups":
                                        grammar.PhoneticAlphabet = AlphabetType.Ups;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.UnsupportedPhoneticAlphabet, reader.Value);
                                        break;
                                }
                                break;

                            case "language":
                                CheckForDuplicates(ref sLanguage, reader);
                                if (sLanguage == "C#" || sLanguage == "VB.Net")
                                {
                                    grammar.Language = sLanguage;
                                }
                                else
                                {
                                    ThrowSrgsException(SRID.UnsupportedLanguage, reader.Value);
                                }
                                break;

                            case "namespace":
                                CheckForDuplicates(ref sNamespace, reader);
                                if (string.IsNullOrEmpty(sNamespace))
                                {
                                    ThrowSrgsException(SRID.NoName1, "namespace");
                                }
                                grammar.Namespace = sNamespace;
                                break;

                            case "codebehind":
                                if (reader.Value.Length == 0)
                                {
                                    ThrowSrgsException(SRID.NoName1, "codebehind");
                                }
                                grammar.CodeBehind.Add(reader.Value);
                                break;

                            case "debug":
                                bool f;
                                if (bool.TryParse(reader.Value, out f))
                                {
                                    grammar.Debug = f;
                                }
                                break;
                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidGrammarAttribute, reader.Name);
                }
            }

            // The version attribute is required  for the grammar element
            if (sVersion == null)
            {
                ThrowSrgsException(SRID.MissingRequiredAttribute, "version", "grammar");
            }

            // The langId is require for voice grammars
            if (_langId == null)
            {
                if (grammarType == GrammarType.VoiceGrammar)
                {
                    ThrowSrgsException(SRID.MissingRequiredAttribute, "xml:lang", "grammar");
                }
                else
                {
                    _langId = CultureInfo.CurrentUICulture;
                }
            }

            // Process child elements.
            ProcessRulesAndScriptsNodes(reader, grammar);

            // Validate all the scripts elements
            ValidateScripts();

            // Add all the scripts to the rules
            foreach (ForwardReference script in _scripts)
            {
                _parser.AddScript(grammar, script._name, script._value);
            }
            // Finish all initialization - should check for the Root and the all
            // rules are defined
            grammar.PostParse(null);
        }

        // The perf gain using .Lengh == 0 other  readability is not worth it fixing this FxCop issue
        private IRule ParseRule(IGrammar grammar, XmlReader reader)
        {
            string id = null;
            string scope = null;
            string dynamic = null;
            RulePublic publicRule = RulePublic.NotSet;
            RuleDynamic ruleDynamic = RuleDynamic.NotSet;

            string sBaseClass = null;
            string sInit = null;
            string sParse = null;
            string sError = null;
            string sRecognition = null;

            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        switch (reader.LocalName)
                        {
                            case "id":
                                CheckForDuplicates(ref id, reader);
                                break;

                            case "scope":
                                CheckForDuplicates(ref scope, reader);
                                switch (scope)
                                {
                                    case "private":
                                        publicRule = RulePublic.False;
                                        break;

                                    case "public":
                                        publicRule = RulePublic.True;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidRuleScope);
                                        break;
                                }
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "dynamic":
                                CheckForDuplicates(ref dynamic, reader);
                                switch (dynamic)
                                {
                                    case "true":
                                        ruleDynamic = RuleDynamic.True;
                                        break;

                                    case "false":
                                        ruleDynamic = RuleDynamic.False;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidDynamicSetting);
                                        break;
                                }
                                break;

                            case "baseclass":
                                CheckForDuplicates(ref sBaseClass, reader);
                                if (string.IsNullOrEmpty(sBaseClass))
                                {
                                    ThrowSrgsException(SRID.NoName1, "baseclass");
                                }
                                break;

                            case "onInit":
                                CheckForDuplicates(ref sInit, reader);
                                sInit = reader.Value;
                                break;

                            case "onParse":
                                CheckForDuplicates(ref sParse, reader);
                                sParse = reader.Value;
                                break;

                            case "onError":
                                CheckForDuplicates(ref sError, reader);
                                sError = reader.Value;
                                break;

                            case "onRecognition":
                                CheckForDuplicates(ref sRecognition, reader);
                                break;
                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidRuleAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(id))
            {
                ThrowSrgsException(SRID.NoRuleId);
            }

            if (sInit != null && publicRule != RulePublic.True)
            {
                XmlParser.ThrowSrgsException(SRID.OnInitOnPublicRule, "OnInit", id);
            }

            if (sRecognition != null && publicRule != RulePublic.True)
            {
                XmlParser.ThrowSrgsException(SRID.OnInitOnPublicRule, "OnRecognition", id);
            }

            ValidateRuleId(id);

            bool hasScript = sInit != null || sParse != null || sError != null || sRecognition != null;
            IRule rule = grammar.CreateRule(id, publicRule, ruleDynamic, hasScript);

            if (!string.IsNullOrEmpty(sInit))
            {
                rule.CreateScript(grammar, id, sInit, RuleMethodScript.onInit);
            }

            if (!string.IsNullOrEmpty(sParse))
            {
                rule.CreateScript(grammar, id, sParse, RuleMethodScript.onParse);
            }

            if (!string.IsNullOrEmpty(sError))
            {
                rule.CreateScript(grammar, id, sError, RuleMethodScript.onError);
            }

            if (!string.IsNullOrEmpty(sRecognition))
            {
                rule.CreateScript(grammar, id, sRecognition, RuleMethodScript.onRecognition);
            }

            rule.BaseClass = sBaseClass;
            _rules.Add(id);

            if (!ProcessChildNodes(reader, rule, rule, "rule"))
            {
                if (ruleDynamic != RuleDynamic.True)
                {
                    ThrowSrgsException(SRID.InvalidEmptyRule, "rule", id);
                }
            }
            return rule;
        }

        // The perf gain using .Lengh == 0 other  readability is not worth it fixing this FxCop issue
        private IRuleRef ParseRuleRef(IElement parent, XmlReader reader)
        {
            IRuleRef ruleRef = null;

            string sAlias = null;
            string sParams = null;
            string uri = null;

            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        switch (reader.LocalName)
                        {
                            case "uri":
                                // Check that the uri pointed to in the ruleref does not point this file
                                // in srgs.xml: ... <ruleref uri="srgs.xml#rule>
                                CheckForDuplicates(ref uri, reader);
                                ValidateRulerefNotPointingToSelf(uri);
                                break;

                            case "special":
                                if (ruleRef != null)
                                {
                                    ThrowSrgsException(SRID.InvalidAttributeDefinedTwice, reader.Value, "special");
                                }
                                switch (reader.Value)
                                {
                                    case "NULL":
                                        ruleRef = _parser.Null;
                                        break;

                                    case "VOID":
                                        ruleRef = _parser.Void;
                                        break;

                                    case "GARBAGE":
                                        ruleRef = _parser.Garbage;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidSpecialRuleRef);
                                        break;
                                }
                                _parser.InitSpecialRuleRef(parent, ruleRef);
                                break;

                            case "type":
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "semantic-key":
                                CheckForDuplicates(ref sAlias, reader);
                                break;

                            case "params":
                                CheckForDuplicates(ref sParams, reader);
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;

                    default:
                        isInvalidAttribute = true;
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidRulerefAttribute, reader.Name);
                }
            }

            // No children allowed
            ProcessChildNodes(reader, null, null, "ruleref");

            if (ruleRef == null)
            {
                if (uri == null)
                {
                    // 'ruleref' without a URI
                    ThrowSrgsException(SRID.InvalidRuleRef, "uri");
                }

                ruleRef = _parser.CreateRuleRef(parent, new Uri(uri, UriKind.RelativeOrAbsolute), sAlias, sParams);
            }
            else
            {
                if (uri != null)
                {
                    ThrowSrgsException(SRID.NoUriForSpecialRuleRef);
                }
                if (!string.IsNullOrEmpty(sAlias) || !string.IsNullOrEmpty(sParams))
                {
                    ThrowSrgsException(SRID.NoAliasForSpecialRuleRef);
                }
            }

            ruleRef.PostParse(parent);
            return ruleRef;
        }

        private IOneOf ParseOneOf(IElement parent, IRule rule, XmlReader reader)
        {
            IOneOf oneOf = _parser.CreateOneOf(parent, rule);

            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                    case sapiNamespace:
                        isInvalidAttribute = true;
                        break;
                }

                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidOneOfAttribute, reader.Name);
                }
            }

            // Process child elements.
            ProcessChildNodes(reader, oneOf, rule, "one-of");
            oneOf.PostParse(parent);
            return oneOf;
        }

        private IItem ParseItem(IElement parent, IRule rule, XmlReader reader)
        {
            float repeatProbability = 0.5f;
            int minRepeat = 1;
            int maxRepeat = 1;
            float weight = 1f;

            while (reader.MoveToNextAttribute())
            {
                // All attributes must belong to the empty namespace
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        switch (reader.LocalName)
                        {
                            case "repeat":
                                SetRepeatValues(reader.Value, out minRepeat, out maxRepeat);
                                break;

                            case "repeat-prob":
                                repeatProbability = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
                                break;

                            case "weight":
                                weight = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;

                    case sapiNamespace:
                        isInvalidAttribute = true;
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            IItem item = _parser.CreateItem(parent, rule, minRepeat, maxRepeat, repeatProbability, weight);

            // Process child elements.
            ProcessChildNodes(reader, item, rule, "item");
            item.PostParse(parent);
            return item;
        }

        private ISubset ParseSubset(IElement parent, XmlReader reader)
        {
            string sMatch = null;
            MatchMode matchMode = MatchMode.Subsequence;

            while (reader.MoveToNextAttribute())
            {
                // All attributes must not belong to the empty namespace
                bool isInvalidAttribute = reader.NamespaceURI.Length == 0;

                switch (reader.NamespaceURI)
                {
                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "match":
                                CheckForDuplicates(ref sMatch, reader);
                                switch (reader.Value)
                                {
                                    case "subsequence":
                                        matchMode = MatchMode.Subsequence;
                                        break;

                                    case "ordered-subset":
                                        matchMode = MatchMode.OrderedSubset;
                                        break;

                                    case "subsequence-content-required":
                                        matchMode = MatchMode.SubsequenceContentRequired;
                                        break;

                                    case "ordered-subset-content-required":
                                        matchMode = MatchMode.OrderedSubsetContentRequired;
                                        break;

                                    default:
                                        isInvalidAttribute = true;
                                        break;
                                }
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidSubsetAttribute, reader.Name);
                }
            }

            // Process child elements, set parent to null as not children element are allowed.
            string text = GetStringContent(reader).Trim();
            if (text.Length == 0)
            {
                ThrowSrgsException(SRID.InvalidEmptyElement, "subset");
            }

            // Create the text buffer element
            return _parser.CreateSubset(parent, text, matchMode);
        }

        private IToken ParseToken(IElement parent, XmlReader reader)
        {
            string sPronunciation = null;
            string sDisplay = null;
            float reqConfidence = -1;

            while (reader.MoveToNextAttribute())
            {
                // Empty namespace is invalid
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        isInvalidAttribute = true;
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "pron":
                                if (string.IsNullOrEmpty(sPronunciation))
                                {
                                    sPronunciation = reader.Value.Trim(Helpers._achTrimChars);

                                    // Check for empty pronunciation - Trim fails if only blanks are contained in the string
                                    if (string.IsNullOrEmpty(sPronunciation))
                                    {
                                        // This error doesn't make much sense since "  ;  ; " would not trigger this.
                                        // "Pronunciation string cannot be empty."
                                        XmlParser.ThrowSrgsException(SRID.EmptyPronunciationString);
                                    }
                                }
                                else
                                {
                                    XmlParser.ThrowSrgsException(SRID.MuliplePronunciationString);
                                }
                                break;

                            case "display":
                                if (string.IsNullOrEmpty(sDisplay))
                                {
                                    sDisplay = reader.Value.Trim(Helpers._achTrimChars);

                                    // Check for empty pronunciation - Trim fails if only blanks are contained in the string
                                    if (string.IsNullOrEmpty(sDisplay))
                                    {
                                        // This error doesn't make much sense since "  ;  ; " would not trigger this.
                                        // "Display string cannot be empty."
                                        XmlParser.ThrowSrgsException(SRID.EmptyDisplayString);
                                    }
                                }
                                else
                                {
                                    XmlParser.ThrowSrgsException(SRID.MultipleDisplayString);
                                }
                                break;

                            case "reqconf":
                                switch (reader.Value)
                                {
                                    case "high":
                                        reqConfidence = 0.8f;
                                        break;

                                    case "normal":
                                        reqConfidence = 0.5f;
                                        break;

                                    case "low":
                                        reqConfidence = 0.2f;
                                        break;

                                    default:
                                        ThrowSrgsException(SRID.InvalidReqConfAttribute, reader.Name);
                                        break;
                                }
                                break;

                            default:
                                isInvalidAttribute = true;
                                break;
                        }
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidTokenAttribute, reader.Name);
                }
            }

            string content = GetStringContent(reader).Trim(Helpers._achTrimChars);

            // Empty token are invalid
            if (string.IsNullOrEmpty(content))
            {
                ThrowSrgsException(SRID.InvalidEmptyElement, "token");
            }

            if (content.IndexOf('\"') >= 0)
            {
                ThrowSrgsException(SRID.InvalidTokenString);
            }

            return _parser.CreateToken(parent, content, sPronunciation, sDisplay, reqConfidence);
        }

        /// <summary>
        /// Break the string into individual tokens and ParseToken() each individual token.
        ///
        /// Token string is a sequence of 0 or more white space delimited tokens.
        /// Tokens may also be delimited by double quotes.  In these cases, the double
        /// quotes token must be surrounded by white space or string boundary.
        /// </summary>
        private void ParseText(IElement parent, string sChars, string pronunciation, string display, float reqConfidence)
        {
            System.Diagnostics.Debug.Assert((parent != null) && (!string.IsNullOrEmpty(sChars)));

            ParseText(parent, sChars, pronunciation, display, reqConfidence, new CreateTokenCallback(_parser.CreateToken));
        }

        private IElement ParseTag(IElement parent, XmlReader reader)
        {
            string content = GetTagContent(parent, reader);

            //Return an empty tag if the content is empty
            if (string.IsNullOrEmpty(content))
            {
                return _parser.CreateSemanticTag(parent);
            }

            if (_parser.Grammar.TagFormat != SrgsTagFormat.KeyValuePairs)
            {
                ISemanticTag semanticTag = _parser.CreateSemanticTag(parent);

                semanticTag.Content(parent, content, 0);
                return semanticTag;
            }

            System.Diagnostics.Debug.Assert(_parser.Grammar.TagFormat == SrgsTagFormat.KeyValuePairs);

            IPropertyTag propertyTag = _parser.CreatePropertyTag(parent);
            string name;
            object value;
            ParsePropertyTag(content, out name, out value);
            propertyTag.NameValue(parent, name, value);
            return propertyTag;
        }

        private string GetTagContent(IElement parent, XmlReader reader)
        {
            // A tag format must be specified in the grammar header
            if (!_hasTagFormat)
            {
                ThrowSrgsException(SRID.MissingTagFormat);
            }

            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                    case sapiNamespace:
                        isInvalidAttribute = true;
                        break;
                }
                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidTagAttribute, reader.Name);
                }
            }

            return GetStringContent(reader).Trim(Helpers._achTrimChars);
        }

        /// <summary>
        /// Parse the lexicon Element
        ///
        /// Attributes:
        ///     uri: required
        ///     type: optional
        /// </summary>
        private static void ParseLexicon(XmlReader reader)
        {
            bool isInvalidAttribute = false;
            bool fFoundUri = false;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "uri":
                        fFoundUri = true;
                        break;

                    case "type":
                        break;

                    default:
                        isInvalidAttribute = true;
                        break;
                }

                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidLexiconAttribute, reader.Name);
                }
            }

            if (!fFoundUri)
            {
                ThrowSrgsException(SRID.MissingRequiredAttribute, "uri", "lexicon");
            }
        }

        /// <summary>
        /// Parse the Meta Element
        ///
        /// Attributes:
        ///     name and http-equiv: one or the other but not both
        ///     content: required
        /// </summary>
        private static void ParseMeta(XmlReader reader)
        {
            bool fFoundContent = false;
            bool fFoundNameOrEquiv = false;
            bool isInvalidAttribute = false;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                    case "http-equiv":
                        if (fFoundNameOrEquiv)
                        {
                            ThrowSrgsException(SRID.MetaNameHTTPEquiv);
                        }
                        fFoundNameOrEquiv = true;
                        break;

                    case "content":
                        isInvalidAttribute = fFoundContent;
                        fFoundContent = true;
                        break;

                    default:
                        isInvalidAttribute = true;
                        break;
                }

                if (isInvalidAttribute)
                {
                    ThrowSrgsException(SRID.InvalidMetaAttribute, reader.Name);
                }
            }

            if (!fFoundContent)
            {
                ThrowSrgsException(SRID.MissingRequiredAttribute, "content", "meta");
            }
            if (!fFoundNameOrEquiv)
            {
                ThrowSrgsException(SRID.MissingRequiredAttribute, "name or http-equiv", "meta");
            }
        }

        private void ParseScript(XmlReader reader, IGrammar grammar)
        {
            int line = _filename != null ? _xmlTextReader.LineNumber : -1;
            string sRule = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        ThrowSrgsException(SRID.InvalidScriptAttribute);
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "rule":
                                if (string.IsNullOrEmpty(sRule))
                                {
                                    sRule = reader.Value;
                                }
                                else
                                {
                                    ThrowSrgsException(SRID.RuleAttributeDefinedMultipeTimes);
                                }
                                break;

                            default:
                                ThrowSrgsException(SRID.InvalidScriptAttribute);
                                break;
                        }
                        break;
                }
            }
            // if no rule or method defined - add the content to the generic _scrip
            if (string.IsNullOrEmpty(sRule))
            {
                _parser.AddScript(grammar, GetStringContent(reader), _filename, line);
            }
            else
            {
                // Adds a placeholder for the rule.
                // Once all the rules and scripts are read, the placeholder will be replaced with the proper rule.
                _scripts.Add(new ForwardReference(sRule, _parser.AddScript(grammar, sRule, GetStringContent(reader), _filename, line)));
            }
        }

        private static void ParseAssemblyReference(XmlReader reader, IGrammar grammar)
        {
            while (reader.MoveToNextAttribute())
            {
                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        ThrowSrgsException(SRID.InvalidScriptAttribute);
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "assembly":
                                grammar.AssemblyReferences.Add(reader.Value);
                                break;

                            default:
                                ThrowSrgsException(SRID.InvalidAssemblyReferenceAttribute);
                                break;
                        }
                        break;
                }
            }
        }

        private static void ParseImportNamespace(XmlReader reader, IGrammar grammar)
        {
            while (reader.MoveToNextAttribute())
            {
                switch (reader.NamespaceURI)
                {
                    case emptyNamespace:
                        ThrowSrgsException(SRID.InvalidScriptAttribute);
                        break;

                    case sapiNamespace:
                        switch (reader.LocalName)
                        {
                            case "namespace":
                                grammar.ImportNamespaces.Add(reader.Value);
                                break;

                            default:
                                ThrowSrgsException(SRID.InvalidImportNamespaceAttribute);
                                break;
                        }
                        break;
                }
            }
        }

        private bool ProcessChildNodes(XmlReader reader, IElement parent, IRule rule, string parentName)
        {
            bool fFirstElement = true;

            // Create a list of name value tags for this scope
            List<IPropertyTag> tags = null;

            reader.MoveToElement();                                 // Move to containing parent of attributes
            if (!reader.IsEmptyElement)
            {
                reader.Read();                                      // Move to first child parent
                while (reader.NodeType != XmlNodeType.EndElement)   // Process each child parent while not at end parent
                {
                    bool isInvalidNode = false;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        // Null if no children are allowed
                        if (parent == null)
                        {
                            ThrowSrgsException(SRID.InvalidNotEmptyElement, parentName);
                        }

                        IElement child = null;
                        switch (reader.NamespaceURI)
                        {
                            case srgsNamespace:

                                switch (reader.LocalName)
                                {
                                    case "example":
                                        if (!(parent is IRule) || !fFirstElement)
                                        {
                                            ThrowSrgsException(SRID.InvalidExampleOrdering);
                                        }
                                        else
                                        {
                                            reader.Skip();
                                            continue;
                                        }

                                        break;

                                    case "ruleref":
                                        child = ParseRuleRef(parent, reader);
                                        break;

                                    case "one-of":
                                        child = ParseOneOf(parent, rule, reader);
                                        break;

                                    case "item":
                                        child = ParseItem(parent, rule, reader);
                                        break;

                                    case "token":
                                        child = ParseToken(parent, reader);
                                        break;

                                    case "tag":
                                        child = ParseTag(parent, reader);
                                        IPropertyTag tag = child as IPropertyTag;
                                        if (tag != null)
                                        {
                                            // The tag list is delayed as it might not be necessary
                                            if (tags == null)
                                            {
                                                tags = new List<IPropertyTag>();
                                            }
                                            tags.Add(tag);
                                        }
                                        break;

                                    case "rule":
                                    default:
                                        isInvalidNode = true;
                                        break;
                                }
                                break;

                            case sapiNamespace:
                                switch (reader.LocalName)
                                {
                                    case "subset":
                                        if ((parent is IRule) || (parent is IItem))
                                        {
                                            child = ParseSubset(parent, reader);
                                        }
                                        else
                                        {
                                            isInvalidNode = true;
                                        }
                                        break;

                                    default:
                                        isInvalidNode = true;
                                        break;
                                }
                                break;

                            default:
                                reader.Skip();                      // Skip over parents in unknown namespaces
                                break;
                        }
                        isInvalidNode = ParseChildNodeElement(parent, isInvalidNode, child);
                        fFirstElement = false;
                    }
                    else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                    {
                        // Null if no children are allowed
                        if (parent == null)
                        {
                            ThrowSrgsException(SRID.InvalidNotEmptyElement, parentName);
                        }

                        isInvalidNode = ParseChildNodeText(reader, parent);
                        fFirstElement = false;
                    }
                    else
                    {
                        reader.Skip();                              // Skip over non-parent/text node types
                    }

                    if (isInvalidNode)
                    {
                        ThrowSrgsException(SRID.InvalidElement, reader.Name);
                    }
                }
            }

            reader.Read();                                          // Move to next sibling

            // Generate the tags for this scope
            if (tags != null)
            {
                foreach (IPropertyTag tag in tags)
                {
                    tag.PostParse(parent);
                }
            }
            return !fFirstElement;
        }

        private bool ParseChildNodeText(XmlReader reader, IElement parent)
        {
            bool isInvalidNode = false;
            string content = reader.Value;

            // Create the SrgsElement for the text
            IElementText srgsText = _parser.CreateText(parent, content);

            // Split it in pieces
            ParseText(parent, content, null, null, -1f);

            // if the parent is a one of, then the children must be an Item
            if (parent is IOneOf)
            {
                isInvalidNode = true;
            }
            else
            {
                IRule parentRule = parent as IRule;
                if (parentRule != null)
                {
                    _parser.AddElement(parentRule, srgsText);
                }
                else
                {
                    IItem parentItem = parent as IItem;
                    if (parentItem != null)
                    {
                        _parser.AddElement(parentItem, srgsText);
                    }
                    else
                    {
                        isInvalidNode = true;
                    }
                }
            }

            reader.Read();
            return isInvalidNode;
        }

        private bool ParseChildNodeElement(IElement parent, bool isInvalidNode, IElement child)
        {
            // The child parent has not been processed yet
            if (child != null)
            {
                // if the parent is a one of, then the children must be an Item
                IOneOf parentOneOf = parent as IOneOf;
                if (parentOneOf != null)
                {
                    IItem childItem = child as IItem;
                    if (childItem != null)
                    {
                        _parser.AddItem(parentOneOf, childItem);
                    }
                    else
                    {
                        isInvalidNode = true;
                    }
                }
                else
                {
                    IRule parentRule = parent as IRule;
                    if (parentRule != null)
                    {
                        _parser.AddElement(parentRule, child);
                    }
                    else
                    {
                        IItem parentItem = parent as IItem;
                        if (parentItem != null)
                        {
                            _parser.AddElement(parentItem, child);
                        }
                        else
                        {
                            isInvalidNode = true;
                        }
                    }
                }
            }

            return isInvalidNode;
        }

        private void ProcessRulesAndScriptsNodes(XmlReader reader, IGrammar grammar)
        {
            bool fProcessedRules = false;

            // Move to containing element of attributes
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
            {
                // Move to first child element
                reader.Read();

                // Process each child element while not at end element
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    bool isInvalidNode = false;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.NamespaceURI)
                        {
                            case srgsNamespace:
                                switch (reader.LocalName)
                                {
                                    case "lexicon":
                                        if (fProcessedRules)
                                        {
                                            ThrowSrgsException(SRID.InvalidGrammarOrdering);
                                        }
                                        ParseLexicon(reader);
                                        break;

                                    case "meta":
                                        if (fProcessedRules)
                                        {
                                            ThrowSrgsException(SRID.InvalidGrammarOrdering);
                                        }
                                        ParseMeta(reader);
                                        break;

                                    case "metadata":
                                        if (fProcessedRules)
                                        {
                                            ThrowSrgsException(SRID.InvalidGrammarOrdering);
                                        }
                                        reader.Skip();
                                        break;

                                    case "rule":
                                        IRule rule = ParseRule(grammar, reader);
                                        rule.PostParse(grammar);
                                        fProcessedRules = true;
                                        break;

                                    case "tag":
                                        if (fProcessedRules || _hasTagFormat && grammar.TagFormat != SrgsTagFormat.W3cV1)
                                        {
                                            ThrowSrgsException(SRID.InvalidGrammarOrdering);
                                        }
                                        grammar.GlobalTags.Add(GetTagContent(grammar, reader));
                                        break;

                                    default:
                                        isInvalidNode = true;
                                        break;
                                }
                                break;

                            case sapiNamespace:
                                switch (reader.LocalName)
                                {
                                    case "script":
                                        ParseScript(reader, grammar);
                                        fProcessedRules = true;
                                        break;

                                    case "assemblyReference":
                                        ParseAssemblyReference(reader, grammar);
                                        fProcessedRules = true;
                                        break;

                                    case "importNamespace":
                                        ParseImportNamespace(reader, grammar);
                                        fProcessedRules = true;
                                        break;
                                    default:
                                        isInvalidNode = true;
                                        break;
                                }
                                break;

                            default:
                                // Skip over elements in unknown namespaces
                                reader.Skip();
                                break;
                        }
                    }
                    else
                    {
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            ThrowSrgsException(SRID.InvalidElement, "text");
                        }
                        // Skip over non-element/text node types
                        reader.Skip();
                    }

                    if (isInvalidNode)
                    {
                        ThrowSrgsException(SRID.InvalidElement, reader.Name);
                    }
                }
            }

            // Move to next sibling
            reader.Read();
        }

        private static string GetStringContent(XmlReader reader)
        {
            StringBuilder sb = new();

            reader.MoveToElement();                                 // Move to containing element of attributes
            if (!reader.IsEmptyElement)
            {
                reader.Read();                                      // Move to first child element
                while (reader.NodeType != XmlNodeType.EndElement)   // Process each child element while not at end element
                {
                    sb.Append(reader.ReadString());

                    bool isInvalidNode = false;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.NamespaceURI)
                        {
                            case srgsNamespace:
                            case sapiNamespace:
                                isInvalidNode = true;
                                break;

                            default:
                                reader.Skip();                      // Skip over elements in unknown namespaces
                                break;
                        }
                    }
                    else if (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Skip();                              // Skip over non-end element node types
                    }

                    if (isInvalidNode)
                    {
                        ThrowSrgsException(SRID.InvalidElement, reader.Name);
                    }
                }
            }

            reader.Read();                                          // Move to next sibling
            return sb.ToString();
        }
        private static void ParsePropertyTag(string sTag, out string name, out object value)
        {
            // Default value
            name = null;
            value = string.Empty;

            // <tag>Name=</tag>             pszValue = null     vValue = VT_EMPTY
            // <tag>Name="string"</tag>     pszValue = "string" vValue = VT_EMPTY
            // <tag>Name=true</tag>         pszValue = null     vValue = VT_BOOL
            // <tag>Name=123</tag>          pszValue = null     vValue = VT_I4
            // <tag>Name=3.14</tag>         pszValue = null     vValue = VT_R8
            int iEqual = sTag.IndexOf('=');

            if (iEqual >= 0)
            {
                // Set property name
                name = sTag.Substring(0, iEqual).Trim(Helpers._achTrimChars);
                iEqual++;
            }
            else
            {
                iEqual = 0;
            }

            // Set property value
            int cLenProperty = sTag.Length;

            if (iEqual < cLenProperty)
            {
                if (sTag[iEqual] == '"')
                {
                    // Name="string"
                    iEqual++;

                    int iEndQuote = sTag.IndexOf('"', iEqual + 1);

                    if (iEndQuote + 1 != cLenProperty)
                    {
                        // Invalid string value
                        XmlParser.ThrowSrgsException(SRID.IncorrectAttributeValue, name, sTag.Substring(iEqual));
                    }

                    value = sTag.Substring(iEqual, iEndQuote - iEqual);
                }
                else
                {
                    string sValue = sTag.Substring(iEqual);
                    int iValue;

                    if (int.TryParse(sValue, out iValue))
                    {
                        // propInfo.pszValue = null
                        // Name=123
                        // propInfo.vValue = VT_I4
                        value = iValue;
                    }
                    else
                    {
                        double flValue;

                        if (double.TryParse(sValue, out flValue))
                        {
                            // propInfo.pszValue = null
                            // propInfo.vValue   = VT_R8
                            value = flValue;
                        }
                        else
                        {
                            bool fValue;

                            if (bool.TryParse(sValue, out fValue))
                            {
                                // Name=true
                                // propInfo.pszValue = null
                                // propInfo.vValue   = VT_BOOL
                                value = fValue;
                            }
                            else
                            {
                                XmlParser.ThrowSrgsException(SRID.InvalidNameValueProperty, name, sValue);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert integer range string to MinValue and MaxValue.
        /// For n- format, MaxValue = Int32.MaxValue
        /// Valid formats: n|n-|n-m     n,m integers
        ///                integer = [whitespace] [+] [0[{x|X}]] [digits]
        /// </summary>
        private static void SetRepeatValues(string repeat, out int minRepeat, out int maxRepeat)
        {
            minRepeat = maxRepeat = 1;
            if (!string.IsNullOrEmpty(repeat))
            {
                int sep = repeat.IndexOf("-", StringComparison.Ordinal);

                if (sep < 0)
                {
                    int minmax = Convert.ToInt32(repeat, CultureInfo.InvariantCulture);

                    // Limit the range of valid values
                    if (minmax < 0 || minmax > 255)
                    {
                        XmlParser.ThrowSrgsException(SRID.MinMaxOutOfRange, minmax, minmax);
                    }
                    minRepeat = maxRepeat = minmax;
                }
                else if (0 < sep)
                {
                    minRepeat = Convert.ToInt32(repeat.Substring(0, sep), CultureInfo.InvariantCulture);
                    if (sep < (repeat.Length - 1))
                    {
                        maxRepeat = Convert.ToInt32(repeat.Substring(sep + 1), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        maxRepeat = int.MaxValue;
                    }
                    // Limit the range of valid values
                    if (minRepeat < 0 || minRepeat > 255 || (maxRepeat != int.MaxValue && (maxRepeat < 0 || maxRepeat > 255)))
                    {
                        XmlParser.ThrowSrgsException(SRID.MinMaxOutOfRange, minRepeat, maxRepeat);
                    }

                    // Max be greater or equal to min
                    if (minRepeat > maxRepeat)
                    {
                        throw new ArgumentException(SR.Get(SRID.MinGreaterThanMax));
                    }
                }
                else
                {
                    ThrowSrgsException(SRID.InvalidItemRepeatAttribute, repeat);
                }
            }
            else
            {
                ThrowSrgsException(SRID.InvalidItemAttribute2);
            }
        }

        private static void CheckForDuplicates(ref string dest, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(dest))
            {
                StringBuilder attribute = new(reader.LocalName);
                if (reader.NamespaceURI.Length > 0)
                {
                    attribute.Append(reader.NamespaceURI);
                    attribute.Append(':');
                }
                XmlParser.ThrowSrgsException(SRID.InvalidAttributeDefinedTwice, reader.Value, attribute);
            }
            dest = reader.Value;
        }

        // Throws exception if the specified Rule does not have a valid Id.
        internal static void ValidateRuleId(string id)
        {
            Helpers.ThrowIfEmptyOrNull(id, nameof(id));

            if (!XmlReader.IsName(id) || (id == "NULL") || (id == "VOID") || (id == "GARBAGE") || (id.IndexOfAny(s_invalidRuleIdChars) != -1))
            {
                XmlParser.ThrowSrgsException(SRID.InvalidRuleId, id);
            }
        }

        private void ValidateRulerefNotPointingToSelf(string uri)
        {
            // Check that the uri pointed to in the ruleref does not point this file
            // in srgs.xml: ... <ruleref uri="srgs.xml#rule>
            // in srgs.xml: or  <ruleref uri="srgs.xml>
            if (_filename != null)
            {
                if (uri.IndexOf(_shortFilename, StringComparison.Ordinal) == 0 && (uri.Length > _shortFilename.Length && uri[_shortFilename.Length] == '#' || uri.Length == _shortFilename.Length))
                {
                    ThrowSrgsException(SRID.InvalidRuleRefSelf);
                }
            }
        }

        private void ValidateScripts()
        {
            // Check that the rule and methods are defined for a script
            foreach (ForwardReference script in _scripts)
            {
                if (!_rules.Contains(script._name))
                {
                    ThrowSrgsException(SRID.InvalidScriptDefinition, script._name);
                }
            }
            // Validate for unique rule names
            List<string> ruleNames = new();

            foreach (string rule in _rules)
            {
                if (ruleNames.Contains(rule))
                {
                    XmlParser.ThrowSrgsException(SRID.RuleAttributeDefinedMultipeTimes, rule);
                }

                ruleNames.Add(rule);
            }
        }

        #endregion

        #region Private Fields

        private IElementFactory _parser;

        // Avoid to do a cast many times
        private XmlReader _reader;

        // Avoid to do a cast many times
        private XmlTextReader _xmlTextReader;

        // Save the filename
        private string _filename;

        // Save the filename without the path
        private string _shortFilename;

        // Language Id for this grammar
        private CultureInfo _langId;

        // Has the Grammar element a FormatTag
        private bool _hasTagFormat;

        // All defined rules
        private List<string> _rules = new();

        private List<ForwardReference> _scripts = new();

        private static readonly char[] s_invalidRuleIdChars = new char[] { '.', ':', '-', '#' };

        private static readonly char[] s_slashBackSlash = new char[] { '\\', '/' };

        #endregion
    }
}
