// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Speech.Synthesis;
using System.Collections.Generic;
using System.Speech.AudioFormat;
using System.Speech.Internal.ObjectTokens;
using System.Runtime.InteropServices;

namespace System.Speech.Internal.Synthesis
{
    internal class TTSVoice
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal TTSVoice(ITtsEngineProxy engine, VoiceInfo voiceId)
        {
            _engine = engine;
            _voiceId = voiceId;
        }

        #endregion

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region public Methods

        /// <summary>
        /// Tests whether two objects are equivalent
        /// </summary>
        public override bool Equals(object obj)
        {
            TTSVoice voice = obj as TTSVoice;
            return voice != null && (_voiceId.Equals(voice.VoiceInfo));
        }

        /// <summary>
        /// Overrides Object.GetHashCode()
        /// </summary>
        public override int GetHashCode()
        {
            return _voiceId.GetHashCode();
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        internal void UpdateLexicons(List<LexiconEntry> lexicons)
        {
            // Remove the lexicons that are defined in this voice but are not in the list
            for (int i = _lexicons.Count - 1; i >= 0; i--)
            {
                LexiconEntry entry = _lexicons[i];
                if (!lexicons.Contains(entry))
                {
                    // Remove the entry first, just in case the RemoveLexicon throws
                    _lexicons.RemoveAt(i);
                    TtsEngine.RemoveLexicon(entry._uri);
                }
            }

            // Addd the lexicons that are defined in this voice but are not in the list
            foreach (LexiconEntry entry in lexicons)
            {
                if (!_lexicons.Contains(entry))
                {
                    // Remove the entry first, just in case the RemoveLexicon throws
                    TtsEngine.AddLexicon(entry._uri, entry._mediaType);
                    _lexicons.Add(entry);
                }
            }
        }

        internal byte[] WaveFormat(byte[] targetWaveFormat)
        {
            // Get the Wave header if it has not been set by the user
            if (targetWaveFormat == null && _waveFormat == null)
            {
                // The registry values contains a default rate 
                if (VoiceInfo.SupportedAudioFormats.Count > 0)
                {
                    // Create the array of bytes containing the format
                    targetWaveFormat = VoiceInfo.SupportedAudioFormats[0].WaveFormat;
                }
            }

            // No input specified and we already got the default
            if (targetWaveFormat == null && _waveFormat != null)
            {
                return _waveFormat;
            }

            // New waveFormat provided?
            if (_waveFormat == null || !Array.Equals(targetWaveFormat, _waveFormat))
            {
                IntPtr waveFormat = IntPtr.Zero;
                GCHandle targetFormat = new GCHandle();

                if (targetWaveFormat != null)
                {
                    targetFormat = GCHandle.Alloc(targetWaveFormat, GCHandleType.Pinned);
                }
                try
                {
                    waveFormat = _engine.GetOutputFormat(targetWaveFormat != null ? targetFormat.AddrOfPinnedObject() : IntPtr.Zero);
                }
                finally
                {
                    if (targetWaveFormat != null)
                    {
                        targetFormat.Free();
                    }
                }

                if (waveFormat != IntPtr.Zero)
                {
                    _waveFormat = WAVEFORMATEX.ToBytes(waveFormat);

                    // Free the buffer
                    Marshal.FreeCoTaskMem(waveFormat);
                }
                else
                {
                    _waveFormat = WAVEFORMATEX.Default.ToBytes();
                }
            }
            return _waveFormat;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        internal ITtsEngineProxy TtsEngine
        {
            get
            {
                return _engine;
            }
        }

        internal VoiceInfo VoiceInfo
        {
            get
            {
                return _voiceId;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        private ITtsEngineProxy _engine;
        private VoiceInfo _voiceId;
        private List<LexiconEntry> _lexicons = new List<LexiconEntry>();
        private byte[] _waveFormat;

        #endregion
    }
}
