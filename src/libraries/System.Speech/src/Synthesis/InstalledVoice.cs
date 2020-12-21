// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Internal.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Threading;

using RegistryDataKey = System.Speech.Internal.ObjectTokens.RegistryDataKey;
using RegistryEntry = System.Collections.Generic.KeyValuePair<string, object>;


namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    [DebuggerDisplay("{VoiceInfo.Name} [{Enabled ? \"Enabled\" : \"Disabled\"}]")]
    public class InstalledVoice
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal InstalledVoice(VoiceSynthesis voiceSynthesizer, VoiceInfo voice)
        {
            _voiceSynthesizer = voiceSynthesizer;
            _voice = voice;
            _enabled = true;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        public VoiceInfo VoiceInfo
        {
            get
            {
                return _voice;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                SetEnabledFlag(value, true);
            }
        }

        #endregion Events

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region public Methods

        /// TODOC
        public override bool Equals(object obj)
        {
            InstalledVoice ti2 = obj as InstalledVoice;
            if (ti2 == null)
            {
                return false;
            }


            return VoiceInfo.Name == ti2.VoiceInfo.Name && VoiceInfo.Age == ti2.VoiceInfo.Age && VoiceInfo.Gender == ti2.VoiceInfo.Gender && VoiceInfo.Culture.Equals(ti2.VoiceInfo.Culture);
        }

        /// TODOC
        public override int GetHashCode()
        {
            return VoiceInfo.Name.GetHashCode();
        }

        #endregion Events

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        internal static InstalledVoice Find(List<InstalledVoice> list, VoiceInfo voiceId)
        {
            foreach (InstalledVoice ti in list)
            {
                if (ti.Enabled && ti.VoiceInfo.Equals(voiceId))
                {
                    return ti;
                }
            }
            return null;
        }

        internal static InstalledVoice FirstEnabled(List<InstalledVoice> list, CultureInfo culture)
        {
            InstalledVoice voiceFirst = null;
            foreach (InstalledVoice ti in list)
            {
                if (ti.Enabled)
                {
                    if (Helpers.CompareInvariantCulture(ti.VoiceInfo.Culture, culture))
                    {
                        return ti;
                    }
                    if (voiceFirst == null)
                    {
                        voiceFirst = ti;
                    }
                }
            }
            return voiceFirst;
        }

        internal void SetEnabledFlag(bool value, bool switchContext)
        {
            try
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (_enabled == false)
                    {
                        // reset the default voice if necessary
                        if (_voice.Equals(_voiceSynthesizer.CurrentVoice(switchContext).VoiceInfo))
                        {
                            _voiceSynthesizer.Voice = null;
                        }
                    }
                    else
                    {
                        // reset the default voice if necessary. This new voice could be the default
                        _voiceSynthesizer.Voice = null;
                    }
                }
            }
            // If no voice can be set, ignore the error
            catch (InvalidOperationException)
            {
                // reset to the default voice.
                _voiceSynthesizer.Voice = null;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private VoiceInfo _voice;
        private bool _enabled;

#pragma warning disable 6524 // The voice synthesizer cannot be disposed when this object is deleted.
        private VoiceSynthesis _voiceSynthesizer;
#pragma warning restore 6524

        #endregion
    }
}
