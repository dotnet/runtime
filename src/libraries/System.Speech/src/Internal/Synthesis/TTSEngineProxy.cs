// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Threading;

namespace System.Speech.Internal.Synthesis
{
    internal abstract class ITtsEngineProxy
    {
        internal ITtsEngineProxy(int lcid)
        {
            _alphabetConverter = new AlphabetConverter(lcid);
        }

        abstract internal IntPtr GetOutputFormat(IntPtr targetFormat);
        abstract internal void AddLexicon(Uri lexicon, string mediaType);
        abstract internal void RemoveLexicon(Uri lexicon);
        abstract internal void Speak(List<TextFragment> frags, byte[] wfx);
        abstract internal void ReleaseInterface();
        abstract internal char[] ConvertPhonemes(char[] phones, AlphabetType alphabet);
        abstract internal AlphabetType EngineAlphabet { get; }
        internal AlphabetConverter AlphabetConverter { get { return _alphabetConverter; } }


        protected AlphabetConverter _alphabetConverter;
    }

    internal class TtsProxySsml : ITtsEngineProxy
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal TtsProxySsml(TtsEngineSsml ssmlEngine, ITtsEngineSite site, int lcid)
            : base(lcid)
        {
            _ssmlEngine = ssmlEngine;
            _site = site;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        override internal IntPtr GetOutputFormat(IntPtr targetFormat)
        {
            return _ssmlEngine.GetOutputFormat(SpeakOutputFormat.WaveFormat, targetFormat);
        }

        override internal void AddLexicon(Uri lexicon, string mediaType)
        {
            _ssmlEngine.AddLexicon(lexicon, mediaType, _site);
        }

        override internal void RemoveLexicon(Uri lexicon)
        {
            _ssmlEngine.RemoveLexicon(lexicon, _site);
        }

        override internal void Speak(List<TextFragment> frags, byte[] wfx)
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

        override internal char[] ConvertPhonemes(char[] phones, AlphabetType alphabet)
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

        override internal AlphabetType EngineAlphabet
        {
            get
            {
                return AlphabetType.Ipa;
            }
        }

        /// <summary>
        /// Release the COM interface for COM object
        /// </summary>
        override internal void ReleaseInterface()
        {
        }


        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        private TtsEngineSsml _ssmlEngine;
        private ITtsEngineSite _site;

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal class TtsProxySapi : ITtsEngineProxy
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal TtsProxySapi(ITtsEngine sapiEngine, IntPtr iSite, int lcid)
            : base(lcid)
        {
            _iSite = iSite;
            _sapiEngine = sapiEngine;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        override internal IntPtr GetOutputFormat(IntPtr preferedFormat)
        {
            // Initialize TTS Engine
            Guid formatId = SAPIGuids.SPDFID_WaveFormatEx;
            Guid guidNull = new Guid();
            IntPtr coMem = IntPtr.Zero;

            _sapiEngine.GetOutputFormat(ref formatId, preferedFormat, out guidNull, out coMem);
            return coMem;
        }

        override internal void AddLexicon(Uri lexicon, string mediaType)
        {
            // SAPI: Ignore
        }

        override internal void RemoveLexicon(Uri lexicon)
        {
            // SAPI: Ignore
        }

        override internal void Speak(List<TextFragment> frags, byte[] wfx)
        {
            GCHandle gc = GCHandle.Alloc(wfx, GCHandleType.Pinned);
            try
            {
                IntPtr waveFormat = gc.AddrOfPinnedObject();
                GCHandle spvTextFragment = new GCHandle();

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

        override internal AlphabetType EngineAlphabet
        {
            get
            {
                return AlphabetType.Sapi;
            }
        }

        override internal char[] ConvertPhonemes(char[] phones, AlphabetType alphabet)
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
        override internal void ReleaseInterface()
        {
            Marshal.ReleaseComObject(_sapiEngine);
        }


        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        private ITtsEngine _sapiEngine;

        // This variable is stored here but never created or deleted
        private IntPtr _iSite;

        #endregion
    }
}
