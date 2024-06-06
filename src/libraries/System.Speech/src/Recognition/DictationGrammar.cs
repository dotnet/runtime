// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Speech.Recognition
{
    // Class for grammars based on a statistical language model for doing dictation.

    public class DictationGrammar : Grammar
    {
        // The implementation of DictationGrammar stores a Uri in the Grammar.Uri field.
        // Then when LoadGrammar is called the Uri handling part of LoadGrammar is modified to check
        // if the grammar object is a DictationGrammar, in which case the SAPI dictation methods are called.
        // The Uri is "grammar:dictation" for regular dictation and "grammar:dictation#spelling" for a spelling.

        #region Constructors

        // Load the generic dictation language model.
        public DictationGrammar() : base(s_defaultDictationUri, null, null)
        {
        }

        // Load a specific topic. The topic is of the form "grammar:dictation#topic"
        public DictationGrammar(string topic) : base(new Uri(topic, UriKind.RelativeOrAbsolute), null, null)
        {
        }

        #endregion

        #region Public Methods
        public void SetDictationContext(string precedingText, string subsequentText)
        {
            if (State != GrammarState.Loaded)
            {
                throw new InvalidOperationException(SR.Get(SRID.GrammarNotLoaded));
            }
            // Note: You can only call this method after the Grammar is Loaded.
            // In theory we could support this more generally but there doesn't seem to be a lot of point.
            Debug.Assert(Recognizer != null);

            Recognizer.SetDictationContext(this, precedingText, subsequentText);
        }

        #endregion

        #region Internal Methods

        #endregion

        #region Private Fields

        private static readonly Uri s_defaultDictationUri = new("grammar:dictation");

        #endregion
    }
}
