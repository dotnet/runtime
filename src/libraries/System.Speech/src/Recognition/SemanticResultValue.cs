// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.GrammarBuilding;

namespace System.Speech.Recognition
{
    [DebuggerDisplay("{_tag.DebugSummary}")]
    public class SemanticResultValue
    {
        #region Constructors
        public SemanticResultValue(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            _tag = new TagElement(value);
        }
        public SemanticResultValue(string phrase, object value)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, nameof(phrase));
            ArgumentNullException.ThrowIfNull(value);

            _tag = new TagElement(new GrammarBuilderPhrase(phrase), value);
        }
        public SemanticResultValue(GrammarBuilder builder, object value)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(value);

            _tag = new TagElement(builder.Clone(), value);
        }

        #endregion

        #region Public Methods
        public GrammarBuilder ToGrammarBuilder()
        {
            return new GrammarBuilder(this);
        }

        #endregion

        #region Internal Properties

        internal TagElement Tag
        {
            get
            {
                return _tag;
            }
        }

        #endregion

        #region Private Fields

        private TagElement _tag;

        #endregion
    }
}
