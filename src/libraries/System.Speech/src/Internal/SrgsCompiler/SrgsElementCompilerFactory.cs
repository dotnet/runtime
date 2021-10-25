// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Globalization;
using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal class SrgsElementCompilerFactory : IElementFactory
    {
        #region Constructors

        internal SrgsElementCompilerFactory(Backend backend, CustomGrammar cg)
        {
            _backend = backend;
            _cg = cg;
            _grammar = new GrammarElement(backend, cg);
        }
        #endregion

        #region Internal Methods

        /// <summary>
        /// Clear all the rules
        /// </summary>
        void IElementFactory.RemoveAllRules()
        {
        }

        IPropertyTag IElementFactory.CreatePropertyTag(IElement parent)
        {
            return new PropertyTag((ParseElementCollection)parent, _backend);
        }

        ISemanticTag IElementFactory.CreateSemanticTag(IElement parent)
        {
            return new SemanticTag((ParseElementCollection)parent, _backend);
        }

        IElementText IElementFactory.CreateText(IElement parent, string value)
        {
            return null;
        }

        IToken IElementFactory.CreateToken(IElement parent, string content, string pronunciation, string display, float reqConfidence)
        {
            ParseToken((ParseElementCollection)parent, content, pronunciation, display, reqConfidence);
            return null;
        }

        IItem IElementFactory.CreateItem(IElement parent, IRule rule, int minRepeat, int maxRepeat, float repeatProbability, float weight)
        {
            return new Item(_backend, (Rule)rule, minRepeat, maxRepeat, repeatProbability, weight);
        }

        IRuleRef IElementFactory.CreateRuleRef(IElement parent, Uri srgsUri)
        {
            throw new NotImplementedException();
        }

        IRuleRef IElementFactory.CreateRuleRef(IElement parent, Uri srgsUri, string semanticKey, string parameters)
        {
            return new RuleRef((ParseElementCollection)parent, _backend, srgsUri, _grammar.UndefRules, semanticKey, parameters);
        }

        void IElementFactory.InitSpecialRuleRef(IElement parent, IRuleRef specialRule)
        {
            ((RuleRef)specialRule).InitSpecialRuleRef(_backend, (ParseElementCollection)parent);
        }

        IOneOf IElementFactory.CreateOneOf(IElement parent, IRule rule)
        {
            return new OneOf((Rule)rule, _backend);
        }

        ISubset IElementFactory.CreateSubset(IElement parent, string text, MatchMode mode)
        {
            return new Subset((ParseElementCollection)parent, _backend, text, mode);
        }

        void IElementFactory.AddScript(IGrammar grammar, string rule, string code)
        {
            ((GrammarElement)grammar).AddScript(rule, code);
        }

        string IElementFactory.AddScript(IGrammar grammar, string rule, string code, string filename, int line)
        {
            // add the #line information
            if (line >= 0)
            {
                if (_cg._language == "C#")
                {
                    // C#
                    return string.Format(CultureInfo.InvariantCulture, "#line {0} \"{1}\"\n{2}", line.ToString(CultureInfo.InvariantCulture), filename, code);
                }
                else
                {
                    // VB.Net
                    return string.Format(CultureInfo.InvariantCulture, "#ExternalSource (\"{1}\",{0}) \n{2}\n#End ExternalSource\n", line.ToString(CultureInfo.InvariantCulture), filename, code);
                }
            }
            return code;
        }

        void IElementFactory.AddScript(IGrammar grammar, string script, string filename, int line)
        {
            // add the #line information
            if (line >= 0)
            {
                if (_cg._language == "C#")
                {
                    // C#
                    _cg._script.Append("#line ");
                    _cg._script.Append(line.ToString(CultureInfo.InvariantCulture));
                    _cg._script.Append(" \"");
                    _cg._script.Append(filename);
                    _cg._script.Append("\"\n");
                    _cg._script.Append(script);
                }
                else
                {
                    // VB.Net
                    _cg._script.Append("#ExternalSource (");
                    _cg._script.Append(" \"");
                    _cg._script.Append(filename);
                    _cg._script.Append("\",");
                    _cg._script.Append(line.ToString(CultureInfo.InvariantCulture));
                    _cg._script.Append(")\n");
                    _cg._script.Append(script);
                    _cg._script.Append("#End #ExternalSource\n");
                }
            }
            else
            {
                _cg._script.Append(script);
            }
        }

        void IElementFactory.AddItem(IOneOf oneOf, IItem item)
        {
        }

        void IElementFactory.AddElement(IRule rule, IElement value)
        {
        }

        void IElementFactory.AddElement(IItem item, IElement value)
        {
        }

        #endregion

        #region Internal Properties

        IGrammar IElementFactory.Grammar
        {
            get
            {
                return _grammar;
            }
        }

        IRuleRef IElementFactory.Null
        {
            get
            {
                return RuleRef.Null;
            }
        }
        IRuleRef IElementFactory.Void
        {
            get
            {
                return RuleRef.Void;
            }
        }
        IRuleRef IElementFactory.Garbage
        {
            get
            {
                return RuleRef.Garbage;
            }
        }
        #endregion

        #region Private Methods

        // Disable parameter validation check

        /// <summary>
        /// Add transition representing the normalized token.
        ///
        /// White Space Normalization - Trim leading/trailing white spaces.
        ///                             Collapse white space sequences to a single ' '.
        /// Restrictions - Normalized token cannot be empty.
        ///                Normalized token cannot contain double-quote.
        ///
        /// If (Parent == Token) And (Parent.SAPIPron.Length > 0) Then
        ///     Escape normalized token.  "/" -> "\/", "\" -> "\\"
        ///     Build /D/L/P; form from the escaped token and SAPIPron.
        ///
        /// SAPIPron may be a semi-colon delimited list of pronunciations.
        /// In this case, a transition for each of the pronunciations will be added.
        ///
        /// AddTransition(NormalizedToken, Parent.EndState, NewState)
        /// Parent.EndState = NewState
        /// </summary>
        private void ParseToken(ParseElementCollection parent, string sToken, string pronunciation, string display, float reqConfidence)
        {
            int requiredConfidence = (parent != null) ? parent._confidence : CfgGrammar.SP_NORMAL_CONFIDENCE;

            // Performs white space normalization in place
            sToken = Backend.NormalizeTokenWhiteSpace(sToken);
            if (string.IsNullOrEmpty(sToken))
            {
                return;
            }

            // "sapi:reqconf" Attribute
            parent._confidence = CfgGrammar.SP_NORMAL_CONFIDENCE;  // Default to normal

            if (reqConfidence < 0 || reqConfidence.Equals(0.5f))
            {
                parent._confidence = CfgGrammar.SP_NORMAL_CONFIDENCE;  // Default to normal
            }
            else if (reqConfidence < 0.5)
            {
                parent._confidence = CfgGrammar.SP_LOW_CONFIDENCE;
            }
            else
            {
                parent._confidence = CfgGrammar.SP_HIGH_CONFIDENCE;
            }

            // If SAPIPron is specified, use /D/L/P; as the transition text, for each of the pronunciations.
            if (pronunciation != null || display != null)
            {
                // Escape normalized token.  "/" -> "\/", "\" -> "\\"
                string sEscapedToken = EscapeToken(sToken);
                string sDisplayToken = display == null ? sEscapedToken : EscapeToken(display);

                if (pronunciation != null)
                {
                    // Garbage transition is optional whereas Wildcard is not.  So we need additional epsilon transition.
                    OneOf oneOf = pronunciation.Contains(';') ? new OneOf(parent._rule, _backend) : null;

                    for (int iCurPron = 0, iDeliminator = 0; iCurPron < pronunciation.Length; iCurPron = iDeliminator + 1)
                    {
                        // Find semi-colon delimiter and replace with null
                        iDeliminator = pronunciation.IndexOf(';', iCurPron);
                        if (iDeliminator == -1)
                        {
                            iDeliminator = pronunciation.Length;
                        }

                        string pron = pronunciation.Substring(iCurPron, iDeliminator - iCurPron);
                        string sSubPron = null;
                        switch (_backend.Alphabet)
                        {
                            case AlphabetType.Sapi:
                                sSubPron = PhonemeConverter.ConvertPronToId(pron, _grammar.Backend.LangId);
                                break;

                            case AlphabetType.Ipa:
                                sSubPron = pron;
                                PhonemeConverter.ValidateUpsIds(sSubPron);
                                break;

                            case AlphabetType.Ups:
                                sSubPron = PhonemeConverter.UpsConverter.ConvertPronToId(pron);
                                break;
                        }

                        // Build /D/L/P; form for this pronunciation.
                        string sDLP = string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2};", sDisplayToken, sEscapedToken, sSubPron);

                        // Add /D/L/P; transition to the new state.
                        if (oneOf != null)
                        {
                            oneOf.AddArc(_backend.WordTransition(sDLP, 1.0f, requiredConfidence));
                        }
                        else
                        {
                            parent.AddArc(_backend.WordTransition(sDLP, 1.0f, requiredConfidence));
                        }
                    }

                    if (oneOf != null)
                    {
                        ((IOneOf)oneOf).PostParse(parent);
                    }
                }
                else
                {
                    // Build /D/L; form for this pronunciation.
                    string sDLP = string.Format(CultureInfo.InvariantCulture, "/{0}/{1};", sDisplayToken, sEscapedToken);

                    // Add /D/L; transition to the new state.
                    parent.AddArc(_backend.WordTransition(sDLP, 1.0f, requiredConfidence));
                }
            }
            else
            {
                // Add transition to the new state with normalized token.
                parent.AddArc(_backend.WordTransition(sToken, 1.0f, requiredConfidence));
            }
        }

        /// <summary>
        /// Escape token.  "/" -> "\/", "\" -> "\\"
        /// </summary>
        private static string EscapeToken(string sToken)                     // String to escape
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(sToken));

            // Easy out if no escape characters
            if (!sToken.Contains("\\/", StringComparison.Ordinal))
            {
                return sToken;
            }

            char[] achSrc = sToken.ToCharArray();
            char[] achDest = new char[achSrc.Length * 2];
            int iDest = 0;

            // Escape slashes and backslashes.
            for (int i = 0; i < achSrc.Length;)
            {
                if ((achSrc[i] == '\\') || (achSrc[i] == '/'))
                {
                    achDest[iDest++] = '\\';                            // Escape special character
                }

                achDest[iDest++] = achSrc[i++];
            }

            // null terminate and update string length
            return new string(achDest, 0, iDest);
        }

        #endregion

        #region Private Fields

        // Callers param
        private Backend _backend;

        // Grammar
        private GrammarElement _grammar;

        // Callers param
        private CustomGrammar _cg;

        #endregion
    }
}
