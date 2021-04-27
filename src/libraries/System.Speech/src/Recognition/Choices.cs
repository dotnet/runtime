// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.GrammarBuilding;

namespace System.Speech.Recognition
{
    [DebuggerDisplay("{_oneOf.DebugSummary}")]
    public class Choices
    {
        #region Constructors

        public Choices()
        {
        }

        public Choices(params string[] phrases)
        {
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            Add(phrases);
        }

        public Choices(params GrammarBuilder[] alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            Add(alternateChoices);
        }

        #endregion

        #region Public Methods

        public void Add(params string[] phrases)
        {
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            foreach (string phrase in phrases)
            {
                Helpers.ThrowIfEmptyOrNull(phrase, "phrase");

                _oneOf.Add(phrase);
            }
        }

        public void Add(params GrammarBuilder[] alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            foreach (GrammarBuilder alternateChoice in alternateChoices)
            {
                Helpers.ThrowIfNull(alternateChoice, "alternateChoice");

                _oneOf.Items.Add(new ItemElement(alternateChoice));
            }
        }
        public GrammarBuilder ToGrammarBuilder()
        {
            return new GrammarBuilder(this);
        }

        #endregion

        #region Internal Properties

        internal OneOfElement OneOf
        {
            get
            {
                return _oneOf;
            }
        }

        #endregion

        #region Private Fields

        private OneOfElement _oneOf = new();

        #endregion
    }
}
