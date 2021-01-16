// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;

namespace System.Speech.Recognition
{
    // This represents the attributes various speech recognizers may, or may not support.

    public class RecognizerInfo : IDisposable
    {
        #region Constructors

        private RecognizerInfo(ObjectToken token, CultureInfo culture)
        {
            // Retrieve the token name
            _id = token.Name;

            // Retrieve default display name
            _description = token.Description;

            // Store full object token id for internal use.
            // NOTE - SAPI returns the wrong hive for tokenenum tokens (always HKLM).
            // Do not rely on the path to be correct in all cases.
            _sapiObjectTokenId = token.Id;

            _name = token.TokenName();

            _culture = culture;

            // Enum all values and add to custom table
            Dictionary<string, string> attrs = new();
            foreach (string keyName in token.Attributes.GetValueNames())
            {
                string attributeValue;
                if (token.Attributes.TryGetString(keyName, out attributeValue))
                {
                    attrs[keyName] = attributeValue;
                }
            }
            _attributes = new ReadOnlyDictionary<string, string>(attrs);

            string audioFormats;
            if (token.Attributes.TryGetString("AudioFormats", out audioFormats))
            {
                _supportedAudioFormats = new ReadOnlyCollection<SpeechAudioFormatInfo>(SapiAttributeParser.GetAudioFormatsFromString(audioFormats));
            }
            else
            {
                _supportedAudioFormats = new ReadOnlyCollection<SpeechAudioFormatInfo>(new List<SpeechAudioFormatInfo>());
            }

            _objectToken = token;
        }

        internal static RecognizerInfo Create(ObjectToken token)
        {
            // Token for recognizer should have Attributes.
            if (token.Attributes == null)
            {
                return null;
            }

            // Get other attributes
            string langId;

            // must have a language id
            if (!token.Attributes.TryGetString("Language", out langId))
            {
                return null;
            }
            CultureInfo cultureInfo = SapiAttributeParser.GetCultureInfoFromLanguageString(langId);
            if (cultureInfo != null)
            {
                return new RecognizerInfo(token, cultureInfo);
            }
            else
            {
                return null;
            }
        }

        internal ObjectToken GetObjectToken()
        {
            return _objectToken;
        }

        /// <summary>
        /// For IDisposable.
        /// RecognizerInfo can be constructed through creating a new object token (usage of _recognizerInfo in RecognizerBase),
        /// so dispose needs to be called.
        /// </summary>
        public void Dispose()
        {
            _objectToken.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region public Properties
        public string Id
        {
            get { return _id; }
        }
        public string Name
        {
            get { return _name; }
        }
        public string Description
        {
            get { return _description; }
        }
        public CultureInfo Culture
        {
            get { return _culture; }
        }
        public ReadOnlyCollection<SpeechAudioFormatInfo> SupportedAudioFormats
        {
            get { return _supportedAudioFormats; }
        }
        public IDictionary<string, string> AdditionalInfo
        {
            get { return _attributes; }
        }

        #endregion

        #region Internal Properties

        #endregion

        #region Private Fields

        // This table stores each attribute
        private ReadOnlyDictionary<string, string> _attributes;

        // Named attributes - these get initialized in constructor
        private string _id;
        private string _name;
        private string _description;
        private string _sapiObjectTokenId;
        private CultureInfo _culture;

        private ReadOnlyCollection<SpeechAudioFormatInfo> _supportedAudioFormats;

        private ObjectToken _objectToken;

        #endregion
    }
}
