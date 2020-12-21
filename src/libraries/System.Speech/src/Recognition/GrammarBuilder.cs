// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Internal.SrgsParser;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.GrammarBuilding;
using System.Speech.Internal;
using System.Text;
using System.IO;

namespace System.Speech.Recognition
{
    /// <summary>
    ///
    /// </summary>
    [DebuggerDisplay("{DebugSummary}")]
    public class GrammarBuilder
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public GrammarBuilder()
        {
            _grammarBuilder = new InternalGrammarBuilder();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        public GrammarBuilder(string phrase)
            : this()
        {
            Append(phrase);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="subsetMatchingCriteria"></param>
        public GrammarBuilder(string phrase, SubsetMatchingMode subsetMatchingCriteria)
            : this()
        {
            Append(phrase, subsetMatchingCriteria);
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        public GrammarBuilder(string phrase, int minRepeat, int maxRepeat)
            : this()
        {
            Append(phrase, minRepeat, maxRepeat);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        public GrammarBuilder(GrammarBuilder builder, int minRepeat, int maxRepeat)
            : this()
        {
            Append(builder, minRepeat, maxRepeat);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="alternateChoices"></param>
        public GrammarBuilder(Choices alternateChoices)
            : this()
        {
            Append(alternateChoices);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        public GrammarBuilder(SemanticResultKey key)
            : this()
        {
            Append(key);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public GrammarBuilder(SemanticResultValue value)
            : this()
        {
            Append(value);
        }

        #endregion Constructors


        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        // Append connecting words
        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        public void Append(string phrase)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));

            AddItem(new GrammarBuilderPhrase(phrase));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="subsetMatchingCriteria"></param>
        public void Append(string phrase, SubsetMatchingMode subsetMatchingCriteria)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));
            GrammarBuilder.ValidateSubsetMatchingCriteriaArgument(subsetMatchingCriteria, nameof(subsetMatchingCriteria));

            AddItem(new GrammarBuilderPhrase(phrase, subsetMatchingCriteria));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
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
        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
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
        /// <summary>
        ///
        /// </summary>
        /// <param name="alternateChoices"></param>
        public void Append(Choices alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            AddItem(alternateChoices.OneOf);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        public void Append(SemanticResultKey key)
        {
            Helpers.ThrowIfNull(key, "builder");

            AddItem(key.SemanticKeyElement);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public void Append(SemanticResultValue value)
        {
            Helpers.ThrowIfNull(value, "builder");

            AddItem(value.Tag);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
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
        /// <summary>
        ///
        /// </summary>
        public void AppendDictation()
        {
            AddItem(new GrammarBuilderDictation());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        public void AppendDictation(string category)
        {
            Helpers.ThrowIfEmptyOrNull(category, nameof(category));

            AddItem(new GrammarBuilderDictation(category));
        }

        // Append wildcard element
        /// <summary>
        ///
        /// </summary>
        public void AppendWildcard()
        {
            AddItem(new GrammarBuilderWildcard());
        }

        /// <summary>
        /// TODOC
        /// Append external rule ref
        /// </summary>
        /// <param name="path"></param>
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
        /// TODOC
        /// Append external rule ref
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rule"></param>
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

        /// <summary>
        /// TODOC
        /// </summary>
        /// <returns></returns>
        public string DebugShowPhrases
        {
            get
            {
                return DebugSummary;
            }
        }

        #endregion Constructors

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region Public Properties

        /// <summary>
        /// TODOC
        /// </summary>
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

        //*******************************************************************
        //
        // Operator Overloads
        //
        //*******************************************************************

        #region Operator Overloads

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static GrammarBuilder operator +(string phrase, GrammarBuilder builder)
        {
            return Add(phrase, builder);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static GrammarBuilder Add(string phrase, GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = new(phrase);
            grammar.Append(builder);
            return grammar;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public static GrammarBuilder operator +(GrammarBuilder builder, string phrase)
        {
            return Add(builder, phrase);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public static GrammarBuilder Add(GrammarBuilder builder, string phrase)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = builder.Clone();
            grammar.Append(phrase);
            return grammar;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="choices"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static GrammarBuilder operator +(Choices choices, GrammarBuilder builder)
        {
            return Add(choices, builder);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="choices"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static GrammarBuilder Add(Choices choices, GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(choices, nameof(choices));
            Helpers.ThrowIfNull(builder, nameof(builder));

            GrammarBuilder grammar = new(choices);
            grammar.Append(builder);
            return grammar;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="choices"></param>
        /// <returns></returns>
        public static GrammarBuilder operator +(GrammarBuilder builder, Choices choices)
        {
            return Add(builder, choices);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="choices"></param>
        /// <returns></returns>
        public static GrammarBuilder Add(GrammarBuilder builder, Choices choices)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));
            Helpers.ThrowIfNull(choices, nameof(choices));

            GrammarBuilder grammar = builder.Clone();
            grammar.Append(choices);
            return grammar;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder1"></param>
        /// <param name="builder2"></param>
        /// <returns></returns>
        public static GrammarBuilder operator +(GrammarBuilder builder1, GrammarBuilder builder2)
        {
            return Add(builder1, builder2);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder1"></param>
        /// <param name="builder2"></param>
        /// <returns></returns>
        public static GrammarBuilder Add(GrammarBuilder builder1, GrammarBuilder builder2)
        {
            Helpers.ThrowIfNull(builder1, nameof(builder1));
            Helpers.ThrowIfNull(builder2, nameof(builder2));

            GrammarBuilder grammar = builder1.Clone();
            grammar.Append(builder2);
            return grammar;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public static implicit operator GrammarBuilder(string phrase) { return new GrammarBuilder(phrase); }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="choices"></param>
        /// <returns></returns>
        public static implicit operator GrammarBuilder(Choices choices) { return new GrammarBuilder(choices); }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="semanticKey"></param>
        /// <returns></returns>
        public static implicit operator GrammarBuilder(SemanticResultKey semanticKey) { return new GrammarBuilder(semanticKey); }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="semanticValue"></param>
        /// <returns></returns>
        public static implicit operator GrammarBuilder(SemanticResultValue semanticValue) { return new GrammarBuilder(semanticValue); }

        #endregion


        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        /// <param name="minParamName"></param>
        /// <param name="maxParamName"></param>
        static internal void ValidateRepeatArguments(int minRepeat, int maxRepeat, string minParamName, string maxParamName)
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="subsetMatchingCriteria"></param>
        /// <param name="paramName"></param>
        static internal void ValidateSubsetMatchingCriteriaArgument(SubsetMatchingMode subsetMatchingCriteria, string paramName)
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="elementFactory"></param>
        internal void CreateGrammar(IElementFactory elementFactory)
        {
            // Create a new Identifier Collection which will provide unique ids
            // for each rule
            IdentifierCollection ruleIds = new();
            elementFactory.Grammar.Culture = Culture;

            _grammarBuilder.CreateElement(elementFactory, null, null, ruleIds);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stream"></param>
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


        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

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

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

        #region Private Methods

        /// <summary>
        ///
        /// </summary>
        private void AddItem(GrammarBuilderBase item)
        {
            InternalBuilder.Items.Add(item.Clone());
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private InternalGrammarBuilder _grammarBuilder;

        private CultureInfo _culture = CultureInfo.CurrentUICulture;

        #endregion

        //*******************************************************************
        //
        // Private Type
        //
        //*******************************************************************

        #region Private Type

        /// <summary>
        ///
        /// </summary>
        private class InternalGrammarBuilder : BuilderElements
        {
            //*******************************************************************
            //
            // Internal Methods
            //
            //*******************************************************************

            #region Internal Methods

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            internal override GrammarBuilderBase Clone()
            {
                InternalGrammarBuilder newGrammarbuilder = new();
                foreach (GrammarBuilderBase i in Items)
                {
                    newGrammarbuilder.Items.Add(i.Clone());
                }
                return newGrammarbuilder;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="elementFactory"></param>
            /// <param name="parent"></param>
            /// <param name="rule"></param>
            /// <param name="ruleIds"></param>
            /// <returns></returns>
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
