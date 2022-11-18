// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Recognition.SrgsGrammar
{
    internal class SrgsElementFactory : IElementFactory
    {
        internal SrgsElementFactory(SrgsGrammar grammar)
        {
            _grammar = grammar;
        }

        /// <summary>
        /// Clear all the rules
        /// </summary>
        void IElementFactory.RemoveAllRules()
        {
        }

        IPropertyTag IElementFactory.CreatePropertyTag(IElement parent)
        {
            return new SrgsNameValueTag();
        }

        ISemanticTag IElementFactory.CreateSemanticTag(IElement parent)
        {
            return new SrgsSemanticInterpretationTag();
        }

        IElementText IElementFactory.CreateText(IElement parent, string value)
        {
            return new SrgsText(value);
        }

        IToken IElementFactory.CreateToken(IElement parent, string content, string pronunciation, string display, float reqConfidence)
        {
            SrgsToken token = new(content);
            if (!string.IsNullOrEmpty(pronunciation))
            {
                // Check if the pronunciations are ok
                string sPron = pronunciation;
                for (int iCurPron = 0, iDeliminator = 0; iCurPron < sPron.Length; iCurPron = iDeliminator + 1)
                {
                    // Find semi-colon delimiter and replace with null
                    iDeliminator = pronunciation.IndexOfAny(s_pronSeparator, iCurPron);
                    if (iDeliminator == -1)
                    {
                        iDeliminator = sPron.Length;
                    }

                    string sSubPron = sPron.Substring(iCurPron, iDeliminator - iCurPron);

                    // make sure this goes through
                    switch (_grammar.PhoneticAlphabet)
                    {
                        case AlphabetType.Sapi:
                            sSubPron = PhonemeConverter.ConvertPronToId(sSubPron, _grammar.Culture.LCID);
                            break;

                        case AlphabetType.Ipa:
                            PhonemeConverter.ValidateUpsIds(sSubPron);
                            break;

                        case AlphabetType.Ups:
                            sSubPron = PhonemeConverter.UpsConverter.ConvertPronToId(sSubPron);
                            break;
                    }
                }

                token.Pronunciation = pronunciation;
            }

            if (!string.IsNullOrEmpty(display))
            {
                token.Display = display;
            }

            if (reqConfidence >= 0)
            {
                throw new NotSupportedException(SR.Get(SRID.ReqConfidenceNotSupported));
            }
            return token;
        }

        IItem IElementFactory.CreateItem(IElement parent, IRule rule, int minRepeat, int maxRepeat, float repeatProbability, float weight)
        {
            SrgsItem item = new();
            if (minRepeat != 1 || maxRepeat != 1)
            {
                item.SetRepeat(minRepeat, maxRepeat);
            }
            item.RepeatProbability = repeatProbability;
            item.Weight = weight;
            return item;
        }

        IRuleRef IElementFactory.CreateRuleRef(IElement parent, Uri srgsUri)
        {
            return new SrgsRuleRef(srgsUri);
        }

        IRuleRef IElementFactory.CreateRuleRef(IElement parent, Uri srgsUri, string semanticKey, string parameters)
        {
            return new SrgsRuleRef(semanticKey, parameters, srgsUri);
        }

        IOneOf IElementFactory.CreateOneOf(IElement parent, IRule rule)
        {
            return new SrgsOneOf();
        }

        ISubset IElementFactory.CreateSubset(IElement parent, string text, MatchMode matchMode)
        {
            SubsetMatchingMode matchingMode = SubsetMatchingMode.Subsequence;

            switch (matchMode)
            {
                case MatchMode.OrderedSubset:
                    matchingMode = SubsetMatchingMode.OrderedSubset;
                    break;

                case MatchMode.OrderedSubsetContentRequired:
                    matchingMode = SubsetMatchingMode.OrderedSubsetContentRequired;
                    break;

                case MatchMode.Subsequence:
                    matchingMode = SubsetMatchingMode.Subsequence;
                    break;

                case MatchMode.SubsequenceContentRequired:
                    matchingMode = SubsetMatchingMode.SubsequenceContentRequired;
                    break;
            }
            return new SrgsSubset(text, matchingMode);
        }

        void IElementFactory.InitSpecialRuleRef(IElement parent, IRuleRef special)
        {
        }

        void IElementFactory.AddScript(IGrammar grammar, string sRule, string code)
        {
            SrgsGrammar srgsGrammar = (SrgsGrammar)grammar;
            SrgsRule rule = srgsGrammar.Rules[sRule];
            if (rule != null)
            {
                rule.Script += code;
            }
            else
            {
                srgsGrammar.AddScript(sRule, code);
            }
        }

        string IElementFactory.AddScript(IGrammar grammar, string sRule, string code, string filename, int line)
        {
            return code;
        }

        void IElementFactory.AddScript(IGrammar grammar, string script, string filename, int line)
        {
            SrgsGrammar srgsGrammar = (SrgsGrammar)grammar;
            srgsGrammar.AddScript(null, script);
        }

        void IElementFactory.AddItem(IOneOf oneOf, IItem value)
        {
            ((SrgsOneOf)oneOf).Add((SrgsItem)value);
        }

        void IElementFactory.AddElement(IRule rule, IElement value)
        {
            ((SrgsRule)rule).Elements.Add((SrgsElement)value);
        }

        void IElementFactory.AddElement(IItem item, IElement value)
        {
            ((SrgsItem)item).Elements.Add((SrgsElement)value);
        }

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
                return SrgsRuleRef.Null;
            }
        }
        IRuleRef IElementFactory.Void
        {
            get
            {
                return SrgsRuleRef.Void;
            }
        }
        IRuleRef IElementFactory.Garbage
        {
            get
            {
                return SrgsRuleRef.Garbage;
            }
        }
        private SrgsGrammar _grammar;

        private static readonly char[] s_pronSeparator = new char[] { ' ', '\t', '\n', '\r', ';' };
    }
}
