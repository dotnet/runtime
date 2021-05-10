// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Speech.Internal;
using System.Speech.Internal.GrammarBuilding;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Recognition
{
    [DebuggerDisplay("{DebugSummary}")]
    public class GrammarBuilder
    {
        #region Constructors

        public GrammarBuilder()
        {
            _grammarBuilder = new InternalGrammarBuilder();
        }

        public GrammarBuilder(string phrase)
            : this()
        {
            Append(phrase);
        }

        public GrammarBuilder(string phrase, SubsetMatchingMode subsetMatchingCriteria)
            : this()
        {
            Append(phrase, subsetMatchingCriteria);
        }

        public GrammarBuilder(string phrase, int minRepeat, int maxRepeat)
            : this()
        {
            Append(phrase, minRepeat, maxRepeat);
        }

        public GrammarBuilder(GrammarBuilder builder, int minRepeat, int maxRepeat)
            : this()
        {
            Append(builder, minRepeat, maxRepeat);
        }

        public GrammarBuilder(Choices alternateChoices)
            : this()
        {
            Append(alternateChoices);
        }

        public GrammarBuilder(SemanticResultKey key)
            : this()
        {
            Append(key);
        }

        public GrammarBuilder(SemanticResultValue value)
            : this()
        {
            Append(value);
        }

        #endregion Constructors

        #region Public Methods

        // Append connecting words

        public void Append(string phrase)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));

            AddItem(new GrammarBuilderPhrase(phrase));
        }

        public void Append(string phrase, SubsetMatchingMode subsetMatchingCriteria)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));
            GrammarBuilder.ValidateSubsetMatchingCriteriaArgument(subsetMatchingCriteria, nameof(subsetMatchingCriteria));

            AddItem(new GrammarBuilderPhrase(phrase, subsetMatchingCriteria));
        }

        public void Append(string phrase, int minRepeat, int maxRepeat)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));
            GrammarBuilder.ValidateRepeatArguments(minRepeat, maxRepeat, "minRepeat", "maxRepeat");

            // Wrap the phrase in an item if min and max repeat are set
            GrammarBuilderPhrase elementPhrase = new(phrase);
            if (minRepeat != 1 || maxRepeat != 1)
            {
                AddItem(new ItemElement(elementPhrase, minRepeat, maxRepeat));
            }
            else
            {
                AddItem(elementPhrase);
            }
        }

        // Append list of rulerefs

        public void Append(GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            // Should never happens has it is a RO value
            Helpers.ThrowIfNull(builder.InternalBuilder, "builder.InternalBuilder");
            Helpers.ThrowIfNull(builder.InternalBuilder.Items, "builder.InternalBuilder.Items");

            // Clone the items if we are playing with the local list.
            foreach (GrammarBuilderBase item in builder.InternalBuilder.Items)
            {
                if (item == null)
                {
                    // This should never happen!
                    throw new ArgumentException(SR.Get(SRID.ArrayOfNullIllegal), nameof(builder));
                }
            }

            // Clone the items if we are playing with the local list.
            List<GrammarBuilderBase> items = builder == this ? builder.Clone().InternalBuilder.Items : builder.InternalBuilder.Items;

            foreach (GrammarBuilderBase item in items)
            {
                AddItem(item);
            }
        }

        // Append one-of

        public void Append(Choices alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            AddItem(alternateChoices.OneOf);
        }

        public void Append(SemanticResultKey key)
        {
            Helpers.ThrowIfNull(key, "builder");

            AddItem(key.SemanticKeyElement);
        }

        public void Append(SemanticResultValue value)
        {
            Helpers.ThrowIfNull(value, "builder");

            AddItem(value.Tag);
        }

        public void Append(GrammarBuilder builder, int minRepeat, int maxRepeat)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));
            GrammarBuilder.ValidateRepeatArguments(minRepeat, maxRepeat, "minRepeat", "maxRepeat");

            // Should never happens has it is a RO value
            Helpers.ThrowIfNull(builder.InternalBuilder, "builder.InternalBuilder");

            // Wrap the phrase in an item if min and max repeat are set
            if (minRepeat != 1 || maxRepeat != 1)
            {
                AddItem(new ItemElement(builder.InternalBuilder.Items, minRepeat, maxRepeat));
            }
            else
            {
                Append(builder);
            }
        }

        // Append dictation element

        public void AppendDictation()
        {
            AddItem(new GrammarBuilderDictation());
        }

        public void AppendDictation(string category)
        {
            Helpers.ThrowIfEmptyOrNull(category, nameof(category));

            AddItem(new GrammarBuilderDictation(category));
        }

        // Append wildcard element

        public void AppendWildcard()
        {
            AddItem(new GrammarBuilderWildcard());
        }

        /// <summary>
        /// Append external rule ref
        /// </summary>
        public void AppendRuleReference(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));
            Uri uri;

            try
            {
                uri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(e.Message, path, e);
            }

            AddItem(new GrammarBuilderRuleRef(uri, null));
        }

        /// <summary>
        /// Append external rule ref
        /// </summary>
        public void AppendRuleReference(string path, string rule)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));
            Helpers.ThrowIfEmptyOrNull(rule, nameof(rule));
            Uri uri;

            try
            {
                uri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(e.Message, path, e);
            }

            AddItem(new GrammarBuilderRuleRef(uri, rule));
        }
        public string DebugShowPhrases
        {
            get
            {
                return DebugSummary;
            }
        }

        #endregion Constructors

        #region Public Properties
        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _culture = value;
            }
        }

        #endregion

        #region Operator Overloads

        public static GrammarBuilder operator +(string phrase, GrammarBuilder builder)
        {
            return Add(phrase, builder);
        }

        public static GrammarBuilder Add(string phrase, GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = new(phrase);
            grammar.Append(builder);
            return grammar;
        }

        public static GrammarBuilder operator +(GrammarBuilder builder, string phrase)
        {
            return Add(builder, phrase);
        }

        public static GrammarBuilder Add(GrammarBuilder builder, string phrase)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = builder.Clone();
            grammar.Append(phrase);
            return grammar;
        }

        public static GrammarBuilder operator +(Choices choices, GrammarBuilder builder)
        {
            return Add(choices, builder);
        }

        public static GrammarBuilder Add(Choices choices, GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(choices, nameof(choices));
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = new(choices);
            grammar.Append(builder);
            return grammar;
        }

        public static GrammarBuilder operator +(GrammarBuilder builder, Choices choices)
        {
            return Add(builder, choices);
        }

        public static GrammarBuilder Add(GrammarBuilder builder, Choices choices)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));
            Helpers.ThrowIfNull(choices, nameof(choices));

            GrammarBuilder grammar = builder.Clone();
            grammar.Append(choices);
            return grammar;
        }

        public static GrammarBuilder operator +(GrammarBuilder builder1, GrammarBuilder builder2)
        {
            return Add(builder1, builder2);
        }

        public static GrammarBuilder Add(GrammarBuilder builder1, GrammarBuilder builder2)
        {
            Helpers.ThrowIfNull(builder1, nameof(builder1));
            Helpers.ThrowIfNull(builder2, nameof(builder2));

            GrammarBuilder grammar = builder1.Clone();
            grammar.Append(builder2);
            return grammar;
        }
        public static implicit operator GrammarBuilder(string phrase) { return new GrammarBuilder(phrase); }
        public static implicit operator GrammarBuilder(Choices choices) { return new GrammarBuilder(choices); }
        public static implicit operator GrammarBuilder(SemanticResultKey semanticKey) { return new GrammarBuilder(semanticKey); }
        public static implicit operator GrammarBuilder(SemanticResultValue semanticValue) { return new GrammarBuilder(semanticValue); }

        #endregion

        #region Internal Methods

        internal static void ValidateRepeatArguments(int minRepeat, int maxRepeat, string minParamName, string maxParamName)
        {
            if (minRepeat < 0)
            {
                throw new ArgumentOutOfRangeException(minParamName, SR.Get(SRID.InvalidMinRepeat, minRepeat));
            }
            if (minRepeat > maxRepeat)
            {
                throw new ArgumentException(SR.Get(SRID.MinGreaterThanMax), maxParamName);
            }
        }

        internal static void ValidateSubsetMatchingCriteriaArgument(SubsetMatchingMode subsetMatchingCriteria, string paramName)
        {
            switch (subsetMatchingCriteria)
            {
                case SubsetMatchingMode.OrderedSubset:
                case SubsetMatchingMode.OrderedSubsetContentRequired:
                case SubsetMatchingMode.Subsequence:
                case SubsetMatchingMode.SubsequenceContentRequired:
                    break;
                default:
                    throw new ArgumentException(SR.Get(SRID.EnumInvalid, paramName), paramName);
            }
        }

        internal void CreateGrammar(IElementFactory elementFactory)
        {
            // Create a new Identifier Collection which will provide unique ids
            // for each rule
            IdentifierCollection ruleIds = new();
            elementFactory.Grammar.Culture = Culture;

            _grammarBuilder.CreateElement(elementFactory, null, null, ruleIds);
        }

        internal void Compile(Stream stream)
        {
            Backend backend = new();
            CustomGrammar cg = new();
            SrgsElementCompilerFactory elementFactory = new(backend, cg);
            CreateGrammar(elementFactory);

            // Optimize in-memory graph representation of the grammar.
            backend.Optimize();

            using (StreamMarshaler streamHelper = new(stream))
            {
                backend.Commit(streamHelper);
            }

            stream.Position = 0;
        }

        internal GrammarBuilder Clone()
        {
            GrammarBuilder builder = new();
            builder._grammarBuilder = (InternalGrammarBuilder)_grammarBuilder.Clone();

            return builder;
        }

        #endregion

        #region Internal Properties

        internal virtual string DebugSummary
        {
            get
            {
                StringBuilder sb = new();

                foreach (GrammarBuilderBase item in InternalBuilder.Items)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(item.DebugSummary);
                }
                return sb.ToString();
            }
        }

        internal BuilderElements InternalBuilder
        {
            get
            {
                return _grammarBuilder;
            }
        }

        #endregion

        #region Private Methods

        private void AddItem(GrammarBuilderBase item)
        {
            InternalBuilder.Items.Add(item.Clone());
        }

        #endregion

        #region Private Fields

        private InternalGrammarBuilder _grammarBuilder;

        private CultureInfo _culture = CultureInfo.CurrentUICulture;

        #endregion

        #region Private Type

        private sealed class InternalGrammarBuilder : BuilderElements
        {
            #region Internal Methods

            internal override GrammarBuilderBase Clone()
            {
                InternalGrammarBuilder newGrammarbuilder = new();
                foreach (GrammarBuilderBase i in Items)
                {
                    newGrammarbuilder.Items.Add(i.Clone());
                }
                return newGrammarbuilder;
            }

            internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
            {
                Collection<RuleElement> newRules = new();
                CalcCount(null);
                Optimize(newRules);

                foreach (GrammarBuilderBase baseRule in newRules)
                {
                    Items.Add(baseRule);
                }

                // The id of the root rule
                string rootId = ruleIds.CreateNewIdentifier("root");

                // Set the grammar's root rule
                elementFactory.Grammar.Root = rootId;
                elementFactory.Grammar.TagFormat = System.Speech.Recognition.SrgsGrammar.SrgsTagFormat.KeyValuePairs;

                // Create the root rule
                IRule root = elementFactory.Grammar.CreateRule(rootId, RulePublic.False, RuleDynamic.NotSet, false);

                // Create all the rules
                foreach (GrammarBuilderBase item in Items)
                {
                    if (item is RuleElement)
                    {
                        item.CreateElement(elementFactory, root, root, ruleIds);
                    }
                }
                // Create an item which represents the grammar
                foreach (GrammarBuilderBase item in Items)
                {
                    if (!(item is RuleElement))
                    {
                        IElement element = item.CreateElement(elementFactory, root, root, ruleIds);

                        if (element != null)
                        {
                            element.PostParse(root);
                            elementFactory.AddElement(root, element);
                        }
                    }
                }
                // Post parse the root rule
                root.PostParse(elementFactory.Grammar);

                elementFactory.Grammar.PostParse(null);
                return null;
            }

            #endregion
        }

        #endregion
    }
}
