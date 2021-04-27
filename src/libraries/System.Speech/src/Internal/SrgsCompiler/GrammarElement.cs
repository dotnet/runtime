// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal class GrammarElement : ParseElement, IGrammar
    {
        #region Constructors

        internal GrammarElement(Backend backend, CustomGrammar cg)
            : base(null)
        {
            _cg = cg;
            _backend = backend;
        }

        #endregion

        #region Internal Method

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

        IRule IGrammar.CreateRule(string id, RulePublic publicRule, RuleDynamic dynamic, bool hasScript)
        {
            SPCFGRULEATTRIBUTES dwRuleAttributes = 0;

            // Determine rule attributes to apply based on RuleScope, IsDynamic, and IsRootRule.
            //  IsRootRule  RuleScope   IsDynamic   Rule Attributes
            //  ----------------------------------------------------------------------
            //  true        *           true        Root | Active | TopLevel | Export | Dynamic
            //  true        *           false       Root | Active | TopLevel | Export
            //  false       internal    true        TopLevel | Export | Dynamic
            //  false       internal    false       TopLevel | Export
            //  false       private     true        Dynamic
            //  false       private     false       0
            if (id == _sRoot)
            {
                dwRuleAttributes |= SPCFGRULEATTRIBUTES.SPRAF_Root | SPCFGRULEATTRIBUTES.SPRAF_Active | SPCFGRULEATTRIBUTES.SPRAF_TopLevel;
                _hasRoot = true;
            }

            if (publicRule == RulePublic.True)
            {
                dwRuleAttributes |= SPCFGRULEATTRIBUTES.SPRAF_TopLevel | SPCFGRULEATTRIBUTES.SPRAF_Export;
            }

            if (dynamic == RuleDynamic.True)
            {
                // BackEnd supports exported dynamic rules for SRGS grammars.
                dwRuleAttributes |= SPCFGRULEATTRIBUTES.SPRAF_Dynamic;
            }

            // Create rule with specified attributes
            Rule rule = GetRule(id, dwRuleAttributes);

            // Add this rule to the list of rules of the STG list
            if (publicRule == RulePublic.True || id == _sRoot || hasScript)
            {
                _cg._rules.Add(rule);
            }
            return rule;
        }

        void IElement.PostParse(IElement parent)
        {
            if (_sRoot != null && !_hasRoot)
            {
                // "Root rule ""%s"" is undefined."
                XmlParser.ThrowSrgsException(SRID.RootNotDefined, _sRoot);
            }

            if (_undefRules.Count > 0)
            {
                // "Root rule ""%s"" is undefined."
                Rule rule = _undefRules[0];
                XmlParser.ThrowSrgsException(SRID.UndefRuleRef, rule.Name);
            }

            // SAPI semantics only for .NET Semantics
            bool containsCode = ((IGrammar)this).CodeBehind.Count > 0 || ((IGrammar)this).ImportNamespaces.Count > 0 || ((IGrammar)this).AssemblyReferences.Count > 0 || CustomGrammar._scriptRefs.Count > 0;
            if (containsCode && ((IGrammar)this).TagFormat != System.Speech.Recognition.SrgsGrammar.SrgsTagFormat.KeyValuePairs)
            {
                XmlParser.ThrowSrgsException(SRID.InvalidSemanticProcessingType);
            }
        }

        internal void AddScript(string name, string code)
        {
            foreach (Rule rule in _cg._rules)
            {
                if (rule.Name == name)
                {
                    rule.Script.Append(code);
                    break;
                }
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Base URI of grammar (xml:base)
        /// </summary>
        Uri IGrammar.XmlBase
        {
            set
            {
                if (value != null)
                {
                    _backend.SetBasePath(value.ToString());
                }
            }
        }

        /// <summary>
        /// GrammarElement language (xml:lang)
        /// </summary>
        CultureInfo IGrammar.Culture
        {
            set
            {
                Helpers.ThrowIfNull(value, nameof(value));

                _backend.LangId = value.LCID;
            }
        }

        /// <summary>
        /// GrammarElement mode.  voice or dtmf
        /// </summary>
        GrammarType IGrammar.Mode
        {
            set
            {
                _backend.GrammarMode = value;
            }
        }

        /// <summary>
        /// GrammarElement mode.  voice or dtmf
        /// </summary>
        AlphabetType IGrammar.PhoneticAlphabet
        {
            set
            {
                _backend.Alphabet = value;
            }
        }

        /// <summary>
        /// Tag format (srgs:tag-format)
        /// </summary>
        System.Speech.Recognition.SrgsGrammar.SrgsTagFormat IGrammar.TagFormat
        {
            get
            {
                return System.Speech.Recognition.SrgsGrammar.SrgsDocument.GrammarOptions2TagFormat(_backend.GrammarOptions);
            }
            set
            {
                _backend.GrammarOptions = System.Speech.Recognition.SrgsGrammar.SrgsDocument.TagFormat2GrammarOptions(value);
            }
        }

        /// <summary>
        /// Tag format (srgs:tag-format)
        /// </summary>
        Collection<string> IGrammar.GlobalTags
        {
            get
            {
                return _backend.GlobalTags;
            }
            set
            {
                _backend.GlobalTags = value;
            }
        }

        internal List<Rule> UndefRules
        {
            get
            {
                return _undefRules;
            }
        }

        internal Backend Backend
        {
            get
            {
                return _backend;
            }
        }

        /// <summary>
        /// language
        /// </summary>
        string IGrammar.Language
        {
            get
            {
                return _cg._language;
            }
            set
            {
                _cg._language = value;
            }
        }

        /// <summary>
        /// namespace
        /// </summary>
        string IGrammar.Namespace
        {
            get
            {
                return _cg._namespace;
            }
            set
            {
                _cg._namespace = value;
            }
        }

        /// <summary>
        /// CodeBehind
        /// </summary>
        Collection<string> IGrammar.CodeBehind
        {
            get
            {
                return _cg._codebehind;
            }
            set
            {
                _cg._codebehind = value;
            }
        }

        /// <summary>
        /// Add #line statements to the inline scripts if set
        /// </summary>
        bool IGrammar.Debug
        {
            set
            {
                _cg._fDebugScript = value;
            }
        }

        /// <summary>
        /// ImportNameSpaces
        /// </summary>
        Collection<string> IGrammar.ImportNamespaces
        {
            get
            {
                return _cg._importNamespaces;
            }
            set
            {
                _cg._importNamespaces = value;
            }
        }

        /// <summary>
        /// ImportNameSpaces
        /// </summary>
        Collection<string> IGrammar.AssemblyReferences
        {
            get
            {
                return _cg._assemblyReferences;
            }
            set
            {
                _cg._assemblyReferences = value;
            }
        }

        internal CustomGrammar CustomGrammar
        {
            get
            {
                return _cg;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create a new rule with the specified name and attribute, and return the initial state.
        /// Verify if Rule is unique.  A Rule may already have been created as a placeholder during RuleRef.
        /// </summary>
        /// <param name="sRuleId">Rule name</param>
        /// <param name="dwAttributes">Rule attributes</param>
        private Rule GetRule(string sRuleId, SPCFGRULEATTRIBUTES dwAttributes)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(sRuleId));

            // Check if RuleID is unique.
            Rule rule = _backend.FindRule(sRuleId);

            if (rule != null)
            {
                // Rule already defined.  Check if it is a placeholder.
                int iRule = _undefRules.IndexOf(rule);

                if (iRule != -1)
                {
                    // This is a UndefinedRule created as a placeholder for a RuleRef.
                    // - Update placeholder rule with correct attributes.
                    _backend.SetRuleAttributes(rule, dwAttributes);

                    // - Remove this now defined rule from UndefinedRules.
                    //   Swap top element with this rule and pop the top element.
                    _undefRules.RemoveAt(iRule);
                }
                else
                {
                    // Multiple definitions of the same Rule.
                    XmlParser.ThrowSrgsException(SRID.RuleRedefinition, sRuleId);    // "Redefinition of rule ""%s""."
                }
            }
            else
            {
                // Rule not yet defined.  Create a new rule and return the InitalState.
                rule = _backend.CreateRule(sRuleId, dwAttributes);
            }

            return rule;
        }

        #endregion

        #region Private Fields

        private Backend _backend;

        // Collection of referenced, but undefined, rules
        private List<Rule> _undefRules = new();
        private CustomGrammar _cg;

        private string _sRoot;

        private bool _hasRoot;

        #endregion
    }
}
