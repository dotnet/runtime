// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Speech.AudioFormat;

namespace System.Speech.Internal
{
    internal static class SapiAttributeParser
    {
        #region Internal Methods

        internal static CultureInfo GetCultureInfoFromLanguageString(string valueString)
        {
            string[] strings = valueString.Split(';');

            string langStringTrim = strings[0].Trim();

            if (!string.IsNullOrEmpty(langStringTrim))
            {
                try
                {
                    return new CultureInfo(int.Parse(langStringTrim, NumberStyles.HexNumber, CultureInfo.InvariantCulture), false);
                }
                catch (ArgumentException)
                {
                    return null; // If we have an invalid language id ignore it. Otherwise enumerating recognizers or voices would fail.
                }
            }

            return null;
        }

        internal static List<SpeechAudioFormatInfo> GetAudioFormatsFromString(string valueString)
        {
            List<SpeechAudioFormatInfo> formatList = new();
            string[] strings = valueString.Split(';');

            for (int i = 0; i < strings.Length; i++)
            {
                string formatString = strings[i].Trim();
                if (!string.IsNullOrEmpty(formatString))
                {
                    SpeechAudioFormatInfo formatInfo = AudioFormatConverter.ToSpeechAudioFormatInfo(formatString);
                    if (formatInfo != null) // Skip cases where a Guid is used.
                    {
                        formatList.Add(formatInfo);
                    }
                }
            }
            return formatList;
        }

        #endregion
    }
}
