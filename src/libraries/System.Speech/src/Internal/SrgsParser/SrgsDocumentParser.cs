// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;

namespace System.Speech.Internal.SrgsParser
{
    internal class SrgsDocumentParser : ISrgsParser
    {
        #region Constructors

        internal SrgsDocumentParser(SrgsGrammar grammar)
        {
            _grammar = grammar;
        }

        #endregion

        #region Internal Methods

        // Initializes the object from a stream containing SRGS in XML
        public void Parse()
        {
            try
            {
                ProcessGrammarElement(_grammar, _parser.Grammar);
            }
            catch
            {
                // clear all the rules
                _parser.RemoveAllRules();
                throw;
            }
        }

        #endregion

        #region Internal Properties

        public IElementFactory ElementFactory
        {
            set
            {
                _parser = value;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Process the top level grammar element
        /// </summary>
        private void ProcessGrammarElement(SrgsGrammar source, IGrammar grammar)
        {
            grammar.Culture = source.Culture;
            grammar.Mode = source.Mode;
            if (source.Root != null)
            {
                grammar.Root = source.Root.Id;
            }
            grammar.TagFormat = source.TagFormat;
            grammar.XmlBase = source.XmlBase;
            grammar.GlobalTags = source.GlobalTags;
            grammar.PhoneticAlphabet = source.PhoneticAlphabet;

            // Process child elements.
            foreach (SrgsRule srgsRule in source.Rules)
            {
                IRule rule = ParseRule(grammar, srgsRule);
                rule.PostParse(grammar);
            }
            grammar.AssemblyReferences = source.AssemblyReferences;
            grammar.CodeBehind = source.CodeBehind;
            grammar.Debug = source.Debug;
            grammar.ImportNamespaces = source.ImportNamespaces;
            grammar.Language = source.Language ?? "C#";
            grammar.Namespace = source.Namespace;

            // if add the content to the generic _scrip
            _parser.AddScript(grammar, source.Script, null, -1);
            // Finish all initialization - should check for the Root and the all
            // rules are defined
            grammar.PostParse(null);
        }

        /// <summary>
        /// Parse a rule
        /// </summary>
        private IRule ParseRule(IGrammar grammar, SrgsRule srgsRule)
        {
            string id = srgsRule.Id;
            bool hasScript = srgsRule.OnInit != null || srgsRule.OnParse != null || srgsRule.OnError != null || srgsRule.OnRecognition != null;
            IRule rule = grammar.CreateRule(id, srgsRule.Scope == SrgsRuleScope.Public ? RulePublic.True : RulePublic.False, srgsRule.Dynamic, hasScript);

            if (srgsRule.OnInit != null)
            {
                rule.CreateScript(grammar, id, srgsRule.OnInit, RuleMethodScript.onInit);
            }

            if (srgsRule.OnParse != null)
            {
                rule.CreateScript(grammar, id, srgsRule.OnParse, RuleMethodScript.onParse);
            }

            if (srgsRule.OnError != null)
            {
                rule.CreateScript(grammar, id, srgsRule.OnError, RuleMethodScript.onError);
            }

            if (srgsRule.OnRecognition != null)
            {
                rule.CreateScript(grammar, id, srgsRule.OnRecognition, RuleMethodScript.onRecognition);
            }

            // Add the code to the backend
            if (srgsRule.Script.Length > 0)
            {
                _parser.AddScript(grammar, id, srgsRule.Script);
            }

            rule.BaseClass = srgsRule.BaseClass;

            foreach (SrgsElement srgsElement in GetSortedTagElements(srgsRule.Elements))
            {
                ProcessChildNodes(srgsElement, rule, rule);
            }
            return rule;
        }

        /// <summary>
        /// Parse a ruleref
        /// </summary>
        private IRuleRef ParseRuleRef(SrgsRuleRef srgsRuleRef, IElement parent)
        {
            IRuleRef ruleRef = null;
            bool fSpecialRuleRef = true;

            if (srgsRuleRef == SrgsRuleRef.Null)
            {
                ruleRef = _parser.Null;
            }
            else if (srgsRuleRef == SrgsRuleRef.Void)
            {
                ruleRef = _parser.Void;
            }
            else if (srgsRuleRef == SrgsRuleRef.Garbage)
            {
                ruleRef = _parser.Garbage;
            }
            else
            {
                ruleRef = _parser.CreateRuleRef(parent, srgsRuleRef.Uri, srgsRuleRef.SemanticKey, srgsRuleRef.Params);
                fSpecialRuleRef = false;
            }

            if (fSpecialRuleRef)
            {
                _parser.InitSpecialRuleRef(parent, ruleRef);
            }

            ruleRef.PostParse(parent);
            return ruleRef;
        }

        /// <summary>
        /// Parse a One-Of
        /// </summary>
        private IOneOf ParseOneOf(SrgsOneOf srgsOneOf, IElement parent, IRule rule)
        {
            IOneOf oneOf = _parser.CreateOneOf(parent, rule);

            // Process child elements.
            foreach (SrgsItem item in srgsOneOf.Items)
            {
                ProcessChildNodes(item, oneOf, rule);
            }
            oneOf.PostParse(parent);
            return oneOf;
        }

        /// <summary>
        /// Parse Item
        /// </summary>
        private IItem ParseItem(SrgsItem srgsItem, IElement parent, IRule rule)
        {
            IItem item = _parser.CreateItem(parent, rule, srgsItem.MinRepeat, srgsItem.MaxRepeat, srgsItem.RepeatProbability, srgsItem.Weight);

            // Process child elements.
            foreach (SrgsElement srgsElement in GetSortedTagElements(srgsItem.Elements))
            {
                ProcessChildNodes(srgsElement, item, rule);
            }

            item.PostParse(parent);
            return item;
        }

        /// <summary>
        /// Parse Token
        /// </summary>
        private IToken ParseToken(SrgsToken srgsToken, IElement parent)
        {
            return _parser.CreateToken(parent, srgsToken.Text, srgsToken.Pronunciation, srgsToken.Display, -1);
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

            XmlParser.ParseText(parent, sChars, pronunciation, display, reqConfidence, new CreateTokenCallback(_parser.CreateToken));
        }

        /// <summary>
        /// Parse tag
        /// </summary>
        private ISubset ParseSubset(SrgsSubset srgsSubset, IElement parent)
        {
            MatchMode matchMode = MatchMode.Subsequence;

            switch (srgsSubset.MatchingMode)
            {
                case SubsetMatchingMode.OrderedSubset:
                    matchMode = MatchMode.OrderedSubset;
                    break;

                case SubsetMatchingMode.OrderedSubsetContentRequired:
                    matchMode = MatchMode.OrderedSubsetContentRequired;
                    break;

                case SubsetMatchingMode.Subsequence:
                    matchMode = MatchMode.Subsequence;
                    break;

                case SubsetMatchingMode.SubsequenceContentRequired:
                    matchMode = MatchMode.SubsequenceContentRequired;
                    break;
            }
            return _parser.CreateSubset(parent, srgsSubset.Text, matchMode);
        }

        /// <summary>
        /// Parse tag
        /// </summary>
        private ISemanticTag ParseSemanticTag(SrgsSemanticInterpretationTag srgsTag, IElement parent)
        {
            ISemanticTag tag = _parser.CreateSemanticTag(parent);

            tag.Content(parent, srgsTag.Script, 0);
            tag.PostParse(parent);
            return tag;
        }

        /// <summary>
        /// ParseNameValueTag tag
        /// </summary>
        private IPropertyTag ParseNameValueTag(SrgsNameValueTag srgsTag, IElement parent)
        {
            IPropertyTag tag = _parser.CreatePropertyTag(parent);

            // Initialize the tag
            tag.NameValue(parent, srgsTag.Name, srgsTag.Value);

            // Since the tag are always pushed at the end of the element list, they can be committed right away
            tag.PostParse(parent);
            return tag;
        }

        /// <summary>
        /// Calls the appropriate Parsing function based on the element type
        /// </summary>
        private void ProcessChildNodes(SrgsElement srgsElement, IElement parent, IRule rule)
        {
            Type elementType = srgsElement.GetType();
            IElement child = null;
            IRule parentRule = parent as IRule;
            IItem parentItem = parent as IItem;

            if (elementType == typeof(SrgsRuleRef))
            {
                child = ParseRuleRef((SrgsRuleRef)srgsElement, parent);
            }
            else if (elementType == typeof(SrgsOneOf))
            {
                child = ParseOneOf((SrgsOneOf)srgsElement, parent, rule);
            }
            else if (elementType == typeof(SrgsItem))
            {
                child = ParseItem((SrgsItem)srgsElement, parent, rule);
            }
            else if (elementType == typeof(SrgsToken))
            {
                child = ParseToken((SrgsToken)srgsElement, parent);
            }
            else if (elementType == typeof(SrgsNameValueTag))
            {
                child = ParseNameValueTag((SrgsNameValueTag)srgsElement, parent);
            }
            else if (elementType == typeof(SrgsSemanticInterpretationTag))
            {
                child = ParseSemanticTag((SrgsSemanticInterpretationTag)srgsElement, parent);
            }
            else if (elementType == typeof(SrgsSubset))
            {
                child = ParseSubset((SrgsSubset)srgsElement, parent);
            }
            else if (elementType == typeof(SrgsText))
            {
                SrgsText srgsText = (SrgsText)srgsElement;
                string content = srgsText.Text;

                // Create the SrgsElement for the text
                IElementText textChild = _parser.CreateText(parent, content);

                // Split it in pieces
                ParseText(parent, content, null, null, -1f);

                if (parentRule != null)
                {
                    _parser.AddElement(parentRule, textChild);
                }
                else
                {
                    if (parentItem != null)
                    {
                        _parser.AddElement(parentItem, textChild);
                    }
                    else
                    {
                        XmlParser.ThrowSrgsException(SRID.InvalidElement);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "Unsupported Srgs element");
                XmlParser.ThrowSrgsException(SRID.InvalidElement);
            }

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
                    XmlParser.ThrowSrgsException(SRID.InvalidElement);
                }
            }
            else
            {
                if (parentRule != null)
                {
                    _parser.AddElement(parentRule, child);
                }
                else
                {
                    if (parentItem != null)
                    {
                        _parser.AddElement(parentItem, child);
                    }
                    else
                    {
                        XmlParser.ThrowSrgsException(SRID.InvalidElement);
                    }
                }
            }
        }

        private IEnumerable<SrgsElement> GetSortedTagElements(Collection<SrgsElement> elements)
        {
            if (_grammar.TagFormat == SrgsTagFormat.KeyValuePairs)
            {
                List<SrgsElement> list = new();
                foreach (SrgsElement element in elements)
                {
                    if (!(element is SrgsNameValueTag))
                    {
                        list.Add(element);
                    }
                }
                foreach (SrgsElement element in elements)
                {
                    if ((element is SrgsNameValueTag))
                    {
                        list.Add(element);
                    }
                }
                return list;
            }
            else
            {
                // Semantic Interpretation, the order for the tag element does not matter
                return elements;
            }
        }

        #endregion

        #region Private Fields

        private SrgsGrammar _grammar;

        private IElementFactory _parser;

        #endregion
    }
}
