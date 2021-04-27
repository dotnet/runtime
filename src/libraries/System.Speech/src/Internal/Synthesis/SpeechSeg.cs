// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Speech.Synthesis.TtsEngine;

namespace System.Speech.Internal.Synthesis
{

    internal class SpeechSeg
    {
        #region Constructors

        internal SpeechSeg(TTSVoice voice, AudioData audio)
        {
            _voice = voice;
            _audio = audio;
        }

        #endregion

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

        #region private Fields

        private TTSVoice _voice;
        private List<TextFragment> _textFragments = new();
#pragma warning disable 56524 // The _audio are not created in this module and should not be disposed
        private AudioData _audio;
#pragma warning restore 56524

        #endregion

    }
}
