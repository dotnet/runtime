// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{

    // Interface that all recognizers must implement in order to connect to Grammar and RecognitionResult.
    internal interface IRecognizerInternal
    {

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        void SetGrammarState(Grammar grammar, bool enabled);

        void SetGrammarWeight(Grammar grammar, float weight);

        void SetGrammarPriority(Grammar grammar, int priority);

        Grammar GetGrammarFromId(ulong id);

        void SetDictationContext(Grammar grammar, string precedingText, string subsequentText);

        #endregion

    }

}
