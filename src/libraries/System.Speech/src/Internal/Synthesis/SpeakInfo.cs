// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;

#pragma warning disable 56524 // The _voiceSynthesis member is not created in this module and should not be disposed

namespace System.Speech.Internal.Synthesis
{
    internal sealed class SpeakInfo
    {
        #region Constructors
        /// <param name="voiceSynthesis">Voice synthesizer used</param>
        /// <param name="ttsVoice">Default engine to use</param>
        internal SpeakInfo(VoiceSynthesis voiceSynthesis, TTSVoice ttsVoice)
        {
            _voiceSynthesis = voiceSynthesis;
            _ttsVoice = ttsVoice;
        }

        #endregion

        #region Internal Properties

        internal TTSVoice Voice
        {
            get
            {
                return _ttsVoice;
            }
        }

        #endregion

        #region Internal Methods

        internal void SetVoice(string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant)
        {
            TTSVoice ttsVoice = _voiceSynthesis.GetEngine(name, culture, gender, age, variant, false);
            if (!ttsVoice.Equals(_ttsVoice))
            {
                _ttsVoice = ttsVoice;
                _fNotInTextSeg = true;
            }
        }

        internal void AddAudio(AudioData audio)
        {
            AddNewSeg(null, audio);
            _fNotInTextSeg = true;
        }

        internal void AddText(TTSVoice ttsVoice, TextFragment textFragment)
        {
            if (_fNotInTextSeg || ttsVoice != _ttsVoice)
            {
                AddNewSeg(ttsVoice, null);
                _fNotInTextSeg = false;
            }
            _lastSeg.AddFrag(textFragment);
        }

        internal SpeechSeg RemoveFirst()
        {
            SpeechSeg speechSeg = null;
            if (_listSeg.Count > 0)
            {
                speechSeg = _listSeg[0];
                _listSeg.RemoveAt(0);
            }
            return speechSeg;
        }

        #endregion

        #region Private Method

        private void AddNewSeg(TTSVoice pCurrVoice, AudioData audio)
        {
            SpeechSeg pNew = new(pCurrVoice, audio);

            _listSeg.Add(pNew);
            _lastSeg = pNew;
        }

        #endregion

        #region private Fields

        // default TTS voice
        private TTSVoice _ttsVoice;

        // If true then a new segment is required for the next Add Text
        private bool _fNotInTextSeg = true;

        // list of segments (text or audio)
        private List<SpeechSeg> _listSeg = new();

        // current segment
        private SpeechSeg _lastSeg;

        // Reference to the VoiceSynthesizer that created it
        private VoiceSynthesis _voiceSynthesis;

        #endregion
    }

    #region Private Types

    internal class AudioData : IDisposable
    {
        internal AudioData(Uri uri, ResourceLoader resourceLoader)
        {
            _uri = uri;
            _resourceLoader = resourceLoader;
            Uri baseAudio;
            _stream = _resourceLoader.LoadFile(uri, out _mimeType, out baseAudio, out _localFile);
        }

        /// <summary>
        /// Needed by IEnumerable!!!
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AudioData()
        {
            Dispose(false);
        }

        internal Uri _uri;
        internal string _mimeType;
        internal Stream _stream;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // unload the file from the cache
                if (_localFile != null)
                {
                    _resourceLoader.UnloadFile(_localFile);
                }

                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                    _localFile = null;
                    _uri = null;
                }
            }
        }

        private string _localFile;
        private ResourceLoader _resourceLoader;
    }

    #endregion
}
