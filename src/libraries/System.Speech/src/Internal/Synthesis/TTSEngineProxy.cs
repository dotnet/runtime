// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Speech.Synthesis.TtsEngine;

namespace System.Speech.Internal.Synthesis
{
    internal abstract class ITtsEngineProxy
    {
        internal ITtsEngineProxy(int lcid)
        {
            _alphabetConverter = new AlphabetConverter(lcid);
        }

        internal abstract IntPtr GetOutputFormat(IntPtr targetFormat);
        internal abstract void AddLexicon(Uri lexicon, string mediaType);
        internal abstract void RemoveLexicon(Uri lexicon);
        internal abstract void Speak(List<TextFragment> frags, byte[] wfx);
        internal abstract void ReleaseInterface();
        internal abstract char[] ConvertPhonemes(char[] phones, AlphabetType alphabet);
        internal abstract AlphabetType EngineAlphabet { get; }
        internal AlphabetConverter AlphabetConverter { get { return _alphabetConverter; } }

        protected AlphabetConverter _alphabetConverter;
    }

    internal class TtsProxySsml : ITtsEngineProxy
    {
        #region Constructors

        internal TtsProxySsml(TtsEngineSsml ssmlEngine, ITtsEngineSite site, int lcid)
            : base(lcid)
        {
            _ssmlEngine = ssmlEngine;
            _site = site;
        }

        #endregion

        #region Internal Methods

        internal override IntPtr GetOutputFormat(IntPtr targetFormat)
        {
            return _ssmlEngine.GetOutputFormat(SpeakOutputFormat.WaveFormat, targetFormat);
        }

        internal override void AddLexicon(Uri lexicon, string mediaType)
        {
            _ssmlEngine.AddLexicon(lexicon, mediaType, _site);
        }

        internal override void RemoveLexicon(Uri lexicon)
        {
            _ssmlEngine.RemoveLexicon(lexicon, _site);
        }

        internal override void Speak(List<TextFragment> frags, byte[] wfx)
        {
            GCHandle gc = GCHandle.Alloc(wfx, GCHandleType.Pinned);
            try
            {
                IntPtr waveFormat = gc.AddrOfPinnedObject();
                _ssmlEngine.Speak(frags.ToArray(), waveFormat, _site);
            }
            finally
            {
                gc.Free();
            }
        }

        internal override char[] ConvertPhonemes(char[] phones, AlphabetType alphabet)
        {
            if (alphabet == AlphabetType.Ipa)
            {
                return phones;
            }
            else
            {
                return _alphabetConverter.SapiToIpa(phones);
            }
        }

        internal override AlphabetType EngineAlphabet
        {
            get
            {
                return AlphabetType.Ipa;
            }
        }

        /// <summary>
        /// Release the COM interface for COM object
        /// </summary>
        internal override void ReleaseInterface()
        {
        }

        #endregion

        #region private Fields

        private TtsEngineSsml _ssmlEngine;
        private ITtsEngineSite _site;

        #endregion
    }

    internal class TtsProxySapi : ITtsEngineProxy
    {
        #region Constructors

        internal TtsProxySapi(ITtsEngine sapiEngine, IntPtr iSite, int lcid)
            : base(lcid)
        {
            _iSite = iSite;
            _sapiEngine = sapiEngine;
        }

        #endregion

        #region Internal Methods

        internal override IntPtr GetOutputFormat(IntPtr preferredFormat)
        {
            // Initialize TTS Engine
            Guid formatId = SAPIGuids.SPDFID_WaveFormatEx;
            Guid guidNull = new();
            IntPtr coMem = IntPtr.Zero;

            _sapiEngine.GetOutputFormat(ref formatId, preferredFormat, out guidNull, out coMem);
            return coMem;
        }

        internal override void AddLexicon(Uri lexicon, string mediaType)
        {
            // SAPI: Ignore
        }

        internal override void RemoveLexicon(Uri lexicon)
        {
            // SAPI: Ignore
        }

        internal override void Speak(List<TextFragment> frags, byte[] wfx)
        {
            GCHandle gc = GCHandle.Alloc(wfx, GCHandleType.Pinned);
            try
            {
                IntPtr waveFormat = gc.AddrOfPinnedObject();
                GCHandle spvTextFragment = new();

                if (ConvertTextFrag.ToSapi(frags, ref spvTextFragment))
                {
                    Guid formatId = SAPIGuids.SPDFID_WaveFormatEx;
                    try
                    {
                        _sapiEngine.Speak(0, ref formatId, waveFormat, spvTextFragment.AddrOfPinnedObject(), _iSite);
                    }
                    finally
                    {
                        ConvertTextFrag.FreeTextSegment(ref spvTextFragment);
                    }
                }
            }
            finally
            {
                gc.Free();
            }
        }

        internal override AlphabetType EngineAlphabet
        {
            get
            {
                return AlphabetType.Sapi;
            }
        }

        internal override char[] ConvertPhonemes(char[] phones, AlphabetType alphabet)
        {
            if (alphabet == AlphabetType.Ipa)
            {
                return _alphabetConverter.IpaToSapi(phones);
            }
            else
            {
                return phones;
            }
        }

        /// <summary>
        /// Release the COM interface for COM object
        /// </summary>
        internal override void ReleaseInterface()
        {
            Marshal.ReleaseComObject(_sapiEngine);
        }

        #endregion

        #region private Fields

        private ITtsEngine _sapiEngine;

        // This variable is stored here but never created or deleted
        private IntPtr _iSite;

        #endregion
    }
}
