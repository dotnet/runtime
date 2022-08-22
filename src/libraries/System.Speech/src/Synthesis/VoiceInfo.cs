// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Internal.Synthesis;

namespace System.Speech.Synthesis
{
    [DebuggerDisplay("{(_name != null ? \"'\" + _name + \"' \" : \"\") +  (_culture != null ? \" '\" + _culture.ToString () + \"' \" : \"\") + (_gender != VoiceGender.NotSet ? \" '\" + _gender.ToString () + \"' \" : \"\") + (_age != VoiceAge.NotSet ? \" '\" + _age.ToString () + \"' \" : \"\") + (_variant > 0 ? \" \" + _variant.ToString () : \"\")}")]
    [Serializable]
    public class VoiceInfo
    {
        #region Constructors
        internal VoiceInfo(string name)
        {
            Helpers.ThrowIfEmptyOrNull(name, nameof(name));
            _name = name;
        }
        internal VoiceInfo(CultureInfo culture)
        {
            // Fails if no culture is provided
            Helpers.ThrowIfNull(culture, nameof(culture));

            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
            }
            _culture = culture;
        }

        internal VoiceInfo(ObjectToken token)
        {
            _registryKeyPath = token._sKeyId;

            // Retrieve the token name
            _id = token.Name;

            // Retrieve default display name
            _description = token.Description;

            // Get other attributes
            _name = token.TokenName();
            SsmlParserHelpers.TryConvertAge(token.Age.ToLowerInvariant(), out _age);
            SsmlParserHelpers.TryConvertGender(token.Gender.ToLowerInvariant(), out _gender);

            string langId;
            if (token.Attributes.TryGetString("Language", out langId))
            {
                _culture = SapiAttributeParser.GetCultureInfoFromLanguageString(langId);
            }

            string assemblyName;
            if (token.TryGetString("Assembly", out assemblyName))
            {
                _assemblyName = assemblyName;
            }

            string clsid;
            if (token.TryGetString("CLSID", out clsid))
            {
                _clsid = clsid;
            }

            if (token.Attributes != null)
            {
                // Enum all values and add to custom table
                Dictionary<string, string> attrs = new();
                foreach (string keyName in token.Attributes.GetValueNames())
                {
                    string attributeValue;
                    if (token.Attributes.TryGetString(keyName, out attributeValue))
                    {
                        attrs.Add(keyName, attributeValue);
                    }
                }
                _attributes = new ReadOnlyDictionary<string, string>(attrs);
            }

            string audioFormats;
            if (token.Attributes != null && token.Attributes.TryGetString("AudioFormats", out audioFormats))
            {
                _audioFormats = new ReadOnlyCollection<SpeechAudioFormatInfo>(SapiAttributeParser.GetAudioFormatsFromString(audioFormats));
            }
            else
            {
                _audioFormats = new ReadOnlyCollection<SpeechAudioFormatInfo>(new List<SpeechAudioFormatInfo>());
            }
        }
        internal VoiceInfo(VoiceGender gender)
        {
            _gender = gender;
        }
        internal VoiceInfo(VoiceGender gender, VoiceAge age)
        {
            _gender = gender;
            _age = age;
        }
        internal VoiceInfo(VoiceGender gender, VoiceAge age, int voiceAlternate)
        {
            if (voiceAlternate < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(voiceAlternate), SR.Get(SRID.PromptBuilderInvalidVariant));
            }

            _gender = gender;
            _age = age;
            _variant = voiceAlternate + 1;
        }

        #endregion

        #region public Methods

        /// <summary>
        /// Tests whether two AutomationIdentifier objects are equivalent
        /// </summary>
        public override bool Equals(object obj)
        {
#pragma warning disable 6506
            VoiceInfo voice = obj as VoiceInfo;
            return voice != null
                && _name == voice._name
                && (_age == voice._age || _age == VoiceAge.NotSet || voice._age == VoiceAge.NotSet)
                && (_gender == voice._gender || _gender == VoiceGender.NotSet || voice._gender == VoiceGender.NotSet)
                && (_culture == null || voice._culture == null || _culture.Equals(voice._culture));
#pragma warning restore 6506
        }

        /// <summary>
        /// Overrides Object.GetHashCode()
        /// </summary>
        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        #endregion

        #region public Properties
        public VoiceGender Gender
        {
            get
            {
                return _gender;
            }
        }
        public VoiceAge Age
        {
            get
            {
                return _age;
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        ///
        /// Return a copy of the internal Language set. This disable client
        /// applications to modify the internal languages list.
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }
        public string Id
        {
            get
            {
                return _id;
            }
        }
        public string Description
        {
            get
            {
                return _description ?? string.Empty;
            }
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ReadOnlyCollection<SpeechAudioFormatInfo> SupportedAudioFormats
        {
            get
            {
                return _audioFormats;
            }
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public IDictionary<string, string> AdditionalInfo => _attributes ??= new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0));
        #endregion

        #region Internal Methods

        internal static bool ValidateGender(VoiceGender gender)
        {
            return gender == VoiceGender.Female || gender == VoiceGender.Male || gender == VoiceGender.Neutral || gender == VoiceGender.NotSet;
        }

        internal static bool ValidateAge(VoiceAge age)
        {
            return age == VoiceAge.Adult || age == VoiceAge.Child || age == VoiceAge.NotSet || age == VoiceAge.Senior || age == VoiceAge.Teen;
        }

        #endregion

        #region Internal Property
        internal int Variant
        {
            get
            {
                return _variant;
            }
        }
        internal string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }
        internal string Clsid
        {
            get
            {
                return _clsid;
            }
        }
        internal string RegistryKeyPath
        {
            get
            {
                return _registryKeyPath;
            }
        }

        #endregion

        #region Private Fields

        private string _name;

        private CultureInfo _culture;

        private VoiceGender _gender;

        private VoiceAge _age;

        private int _variant = -1;

        [NonSerialized]
        private string _id;

        [NonSerialized]
        private string _registryKeyPath;

        [NonSerialized]
        private string _assemblyName;

        [NonSerialized]
        private string _clsid;

        [NonSerialized]
        private string _description;

        [NonSerialized]
        private ReadOnlyDictionary<string, string> _attributes;

        [NonSerialized]
        private ReadOnlyCollection<SpeechAudioFormatInfo> _audioFormats;

        #endregion
    }
}
