// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.Synthesis;
using System.Speech.Internal.ObjectTokens;
using System.Xml;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    [DebuggerDisplay("{(_name != null ? \"'\" + _name + \"' \" : \"\") +  (_culture != null ? \" '\" + _culture.ToString () + \"' \" : \"\") + (_gender != VoiceGender.NotSet ? \" '\" + _gender.ToString () + \"' \" : \"\") + (_age != VoiceAge.NotSet ? \" '\" + _age.ToString () + \"' \" : \"\") + (_variant > 0 ? \" \" + _variant.ToString () : \"\")}")]
    [Serializable]
    public class VoiceInfo
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="name"></param>
        internal VoiceInfo(string name)
        {
            Helpers.ThrowIfEmptyOrNull(name, nameof(name));
            _name = name;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="culture"></param>
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

            // Retriece the token name
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


        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        internal VoiceInfo(VoiceGender gender)
        {
            _gender = gender;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="age"></param>
        internal VoiceInfo(VoiceGender gender, VoiceAge age)
        {
            _gender = gender;
            _age = age;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="age"></param>
        /// <param name="voiceAlternate"></param>
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

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region public Methods

        /// <summary>
        /// Tests whether two AutomationIdentifier objects are equivalent
        /// </summary>
        public override bool Equals(object obj)
        {
            // PreSharp doesn't understand that if obj is null then this will return false.
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

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public VoiceGender Gender
        {
            get
            {
                return _gender;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public VoiceAge Age
        {
            get
            {
                return _age;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// TODOC
        ///
        /// Return a copy of the internal Language set. This disable client
        /// applications to modify the internal languages list.
        /// </summary>
        /// <value></value>
        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Description
        {
            get
            {
                return _description != null ? _description : string.Empty;
            }
        }


        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ReadOnlyCollection<SpeechAudioFormatInfo> SupportedAudioFormats
        {
            get
            {
                return _audioFormats;
            }
        }


        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public IDictionary<string, string> AdditionalInfo
        {
            get
            {
                if (_attributes == null)
                    _attributes = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0));
                return _attributes;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

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

        //*******************************************************************
        //
        // Internal Property
        //
        //*******************************************************************

        #region Internal Property

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        internal int Variant
        {
            get
            {
                return _variant;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        internal string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        internal string Clsid
        {
            get
            {
                return _clsid;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        internal string RegistryKeyPath
        {
            get
            {
                return _registryKeyPath;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private string _name;

        private CultureInfo _culture;

        private VoiceGender _gender = VoiceGender.NotSet;

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
