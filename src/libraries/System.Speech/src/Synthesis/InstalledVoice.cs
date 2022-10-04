// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Internal;
using System.Speech.Internal.Synthesis;

namespace System.Speech.Synthesis
{
    [DebuggerDisplay("{VoiceInfo.Name} [{Enabled ? \"Enabled\" : \"Disabled\"}]")]
    public class InstalledVoice
    {
        #region Constructors

        internal InstalledVoice(VoiceSynthesis voiceSynthesizer, VoiceInfo voice)
        {
            _voiceSynthesizer = voiceSynthesizer;
            _voice = voice;
            _enabled = true;
        }

        #endregion

        #region public Properties
        public VoiceInfo VoiceInfo
        {
            get
            {
                return _voice;
            }
        }
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

        #region public Methods
        public override bool Equals(object obj)
        {
            InstalledVoice ti2 = obj as InstalledVoice;
            if (ti2 == null)
            {
                return false;
            }

            return VoiceInfo.Name == ti2.VoiceInfo.Name && VoiceInfo.Age == ti2.VoiceInfo.Age && VoiceInfo.Gender == ti2.VoiceInfo.Gender && VoiceInfo.Culture.Equals(ti2.VoiceInfo.Culture);
        }
        public override int GetHashCode()
        {
            return VoiceInfo.Name.GetHashCode();
        }

        #endregion Events

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
                    voiceFirst ??= ti;
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

        #region Private Fields

        private VoiceInfo _voice;
        private bool _enabled;

#pragma warning disable 6524 // The voice synthesizer cannot be disposed when this object is deleted.
        private VoiceSynthesis _voiceSynthesizer;
#pragma warning restore 6524

        #endregion
    }
}
