// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.GrammarBuilding;

namespace System.Speech.Recognition
{
    [DebuggerDisplay("{_semanticKey.DebugSummary}")]
    public class SemanticResultKey
    {
        #region Constructors

        private SemanticResultKey(string semanticResultKey)
            : base()
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));

            _semanticKey = new SemanticKeyElement(semanticResultKey);
        }

        public SemanticResultKey(string semanticResultKey, params string[] phrases)
            : this(semanticResultKey)
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            // Build a grammar builder with all the phrases
            foreach (string phrase in phrases)
            {
                _semanticKey.Add(phrase);
            }
        }

        public SemanticResultKey(string semanticResultKey, params GrammarBuilder[] builders)
            : this(semanticResultKey)
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));
            Helpers.ThrowIfNull(builders, "phrases");

            // Build a grammar builder with all the grammar builders
            foreach (GrammarBuilder builder in builders)
            {
                _semanticKey.Add(builder.Clone());
            }
        }

        #endregion

        #region Public Methods
        public GrammarBuilder ToGrammarBuilder()
        {
            return new GrammarBuilder(this);
        }

        #endregion

        #region Internal Properties

        internal SemanticKeyElement SemanticKeyElement
        {
            get
            {
                return _semanticKey;
            }
        }

        #endregion

        #region Private Fields

        private readonly SemanticKeyElement _semanticKey;

        #endregion
    }
}
