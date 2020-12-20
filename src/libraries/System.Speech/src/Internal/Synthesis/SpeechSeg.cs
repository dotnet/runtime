// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Speech.Synthesis.TtsEngine;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    ///
    /// </summary>
    internal class SpeechSeg
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal SpeechSeg(TTSVoice voice, AudioData audio)
        {
            _voice = voice;
            _audio = audio;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        internal List<TextFragment> FragmentList
        {
            get
            {
                return _textFragments;
            }
        }

        internal AudioData Audio
        {
            get
            {
                return _audio;
            }
        }

        internal TTSVoice Voice
        {
            get
            {
                return _voice;
            }
        }

        internal bool IsText
        {
            get
            {
                return _audio == null;
            }
        }


        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        internal void AddFrag(TextFragment textFragment)
        {
            if (_audio != null)
            {
                throw new InvalidOperationException();
            }

            _textFragments.Add(textFragment);
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        private TTSVoice _voice;
        private List<TextFragment> _textFragments = new();
#pragma warning disable 56524 // The _audio are not created in this module and should not be disposed
        private AudioData _audio;
#pragma warning enable 56524


        #endregion

    }
}
