// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Speech.Synthesis.TtsEngine;

namespace System.Speech.Internal.Synthesis
{
    internal static class ConvertTextFrag
    {
        #region internal Methods

        internal static bool ToSapi(List<TextFragment> ssmlFrags, ref GCHandle sapiFragLast)
        {
            bool fFirst = true;

            for (int iFrag = ssmlFrags.Count - 1; iFrag >= 0; iFrag--)
            {
                TextFragment textFragment = ssmlFrags[iFrag];

                // Remove the start and end paragraph fragments
                if (textFragment.State.Action == TtsEngineAction.StartParagraph || textFragment.State.Action == TtsEngineAction.StartSentence)
                {
                    continue;
                }

                SPVTEXTFRAG sapiFrag = new();

                // start with the text fragment
                sapiFrag.gcNext = fFirst ? new GCHandle() : sapiFragLast;
                sapiFrag.pNext = fFirst ? IntPtr.Zero : sapiFragLast.AddrOfPinnedObject();
                sapiFrag.gcText = GCHandle.Alloc(textFragment.TextToSpeak, GCHandleType.Pinned);
                sapiFrag.pTextStart = sapiFrag.gcText.AddrOfPinnedObject();
                sapiFrag.ulTextSrcOffset = textFragment.TextOffset;
                sapiFrag.ulTextLen = textFragment.TextLength;

                // State
                SPVSTATE sapiState = new();
                FragmentState ssmlState = textFragment.State;
                sapiState.eAction = (SPVACTIONS)ssmlState.Action;
                sapiState.LangID = (short)ssmlState.LangId;
                sapiState.EmphAdj = ssmlState.Emphasis != 1 ? 0 : 1;
                if (ssmlState.Prosody != null)
                {
                    sapiState.RateAdj = SapiRate(ssmlState.Prosody.Rate);
                    sapiState.Volume = SapiVolume(ssmlState.Prosody.Volume);
                    sapiState.PitchAdj.MiddleAdj = SapiPitch(ssmlState.Prosody.Pitch);
                }
                else
                {
                    sapiState.Volume = 100;
                }

                sapiState.ePartOfSpeech = SPPARTOFSPEECH.SPPS_Unknown;

                // Set the silence if any
                if (sapiState.eAction == SPVACTIONS.SPVA_Silence)
                {
                    sapiState.SilenceMSecs = SapiSilence(ssmlState.Duration, (EmphasisBreak)ssmlState.Emphasis);
                }

                // Set the phonemes if any
                if (ssmlState.Phoneme != null)
                {
                    sapiState.eAction = SPVACTIONS.SPVA_Pronounce;
                    sapiFrag.gcPhoneme = GCHandle.Alloc(ssmlState.Phoneme, GCHandleType.Pinned);
                    sapiState.pPhoneIds = sapiFrag.gcPhoneme.AddrOfPinnedObject();

                    // Get rid of the text if phonemes are defined. This is to be compatible with existing
                    // TTS engines.
                }
                else
                {
                    sapiFrag.gcPhoneme = new GCHandle();
                    sapiState.pPhoneIds = IntPtr.Zero;
                }

                // Set the say-as if any
                if (ssmlState.SayAs != null)
                {
                    string format = ssmlState.SayAs.Format;
                    string interpretAs;
                    switch (interpretAs = ssmlState.SayAs.InterpretAs)
                    {
                        case "spellout":
                        case "spell-out":
                        case "characters":
                        case "letters":
                            sapiState.eAction = SPVACTIONS.SPVA_SpellOut;
                            break;

                        case "time":
                        case "date":
                            if (!string.IsNullOrEmpty(format))
                            {
                                interpretAs = interpretAs + ':' + format;
                            }
                            sapiState.Context.pCategory = SapiCategory(sapiFrag, interpretAs, null);
                            break;

                        default:
                            sapiState.Context.pCategory = SapiCategory(sapiFrag, interpretAs, format);
                            break;
                    }
                }

                sapiFrag.State = sapiState;
                sapiFragLast = GCHandle.Alloc(sapiFrag, GCHandleType.Pinned);

                fFirst = false;
            }
            return !fFirst;
        }

        private static IntPtr SapiCategory(SPVTEXTFRAG sapiFrag, string interpretAs, string format)
        {
            int posSayAsFormat = Array.BinarySearch<string>(s_asSayAsFormat, interpretAs);
            string sFormat = posSayAsFormat >= 0 ? s_asContextFormat[posSayAsFormat] : format;
            sapiFrag.gcSayAsCategory = GCHandle.Alloc(sFormat, GCHandleType.Pinned);
            return sapiFrag.gcSayAsCategory.AddrOfPinnedObject();
        }

        internal static void FreeTextSegment(ref GCHandle fragment)
        {
            SPVTEXTFRAG sapiFrag = (SPVTEXTFRAG)fragment.Target;
            if (sapiFrag.gcNext.IsAllocated)
            {
                FreeTextSegment(ref sapiFrag.gcNext);
            }

            // free the references to the optional elements
            if (sapiFrag.gcPhoneme.IsAllocated)
            {
                sapiFrag.gcPhoneme.Free();
            }

            if (sapiFrag.gcSayAsCategory.IsAllocated)
            {
                sapiFrag.gcSayAsCategory.Free();
            }

            // Free the text associated with this fragment
            sapiFrag.gcText.Free();
            fragment.Free();
        }

        #endregion

        #region Private Methods

        private static int SapiVolume(ProsodyNumber volume)
        {
            int sapiVolume = 100;
            if (volume.SsmlAttributeId != ProsodyNumber.AbsoluteNumber)
            {
                switch ((ProsodyVolume)volume.SsmlAttributeId)
                {
                    case ProsodyVolume.ExtraLoud:
                        sapiVolume = 100;
                        break;

                    case ProsodyVolume.Loud:
                        sapiVolume = 80;
                        break;

                    case ProsodyVolume.Medium:
                        sapiVolume = 60;
                        break;

                    case ProsodyVolume.Soft:
                        sapiVolume = 40;
                        break;

                    case ProsodyVolume.ExtraSoft:
                        sapiVolume = 20;
                        break;

                    case ProsodyVolume.Silent:
                        sapiVolume = 0;
                        break;
                }
                // add the relative information
                sapiVolume = (int)((volume.IsNumberPercent ? sapiVolume * volume.Number : volume.Number) + 0.5);
            }
            else
            {
                sapiVolume = (int)(volume.Number + 0.5);
            }

            // Check the range.
            if (sapiVolume > 100)
            {
                sapiVolume = 100;
            }
            if (sapiVolume < 0)
            {
                sapiVolume = 0;
            }
            return sapiVolume;
        }

        private static int SapiSilence(int duration, EmphasisBreak emphasis)
        {
            int sapiSilence = 1000;

            if (duration > 0)
            {
                sapiSilence = duration;
            }
            else
            {
                switch (emphasis)
                {
                    // No break, arbitrarily defined as 10 milliseconds
                    case EmphasisBreak.None:
                        sapiSilence = 10;
                        break;

                    // Extra small break, arbitrarily defined as 125 milliseconds
                    case EmphasisBreak.ExtraWeak:
                        sapiSilence = 125;
                        break;

                    // Small break, arbitrarily defined as 250 milliseconds
                    case EmphasisBreak.Weak:
                        sapiSilence = 250;
                        break;

                    // Medium break, arbitrarily defined as 1000 milliseconds
                    case EmphasisBreak.Medium:
                        sapiSilence = 1000;
                        break;

                    // Large break, arbitrarily defined as 1750 milliseconds
                    case EmphasisBreak.Strong:
                        sapiSilence = 1750;
                        break;

                    // Extra large break, arbitrarily defined as 3000 milliseconds
                    case EmphasisBreak.ExtraStrong:
                        sapiSilence = 3000;
                        break;
                }
            }
            if (sapiSilence < 0 || sapiSilence > 0xffff)
            {
                sapiSilence = 1000;
            }
            return sapiSilence;
        }

        /// <summary>
        /// Produces the SAPI "RATE" tag
        /// </summary>
        private static int SapiRate(ProsodyNumber rate)
        {
            // Okay, we have a RATE element, but what do we set the rate to?
            // Rate varies on a scale from -10 to 10 for us.
            // There isn't a defined mapping between Words per Minute and rate.
            // For percentage changes, we will require that -10 maps to one third the default rate,
            // and +10 to three times the default, on a log scale.
            // But for absolute or relative (not percent) we can't support this without a defined base-line rate
            // We could get away with 180 for this in English, but very variable across languages.

            int sapiRate = 0;
            if (rate.SsmlAttributeId != ProsodyNumber.AbsoluteNumber)
            {
                switch ((ProsodyRate)rate.SsmlAttributeId)
                {
                    case ProsodyRate.ExtraSlow:
                        sapiRate = -9;
                        break;

                    case ProsodyRate.Slow:
                        sapiRate = -4;
                        break;

                    case ProsodyRate.Fast:
                        sapiRate = 4;
                        break;

                    case ProsodyRate.ExtraFast:
                        sapiRate = 9;
                        break;
                }

                // add the relative information
                sapiRate = (int)((rate.IsNumberPercent ? ScaleNumber(rate.Number, sapiRate, 10) : sapiRate) + 0.5);
            }
            else
            {
                sapiRate = ScaleNumber(rate.Number, 0, 10);
            }
            // Check the range.
            if (sapiRate > 10)
            {
                sapiRate = 10;
            }
            if (sapiRate < -10)
            {
                sapiRate = -10;
            }
            return sapiRate;
        }

        private static int SapiPitch(ProsodyNumber pitch)
        {
            int sapiPitch = 0;

            if (pitch.SsmlAttributeId != ProsodyNumber.AbsoluteNumber)
            {
                switch ((ProsodyPitch)pitch.SsmlAttributeId)
                {
                    case ProsodyPitch.ExtraHigh:
                        sapiPitch = 9;
                        break;

                    case ProsodyPitch.High:
                        sapiPitch = 4;
                        break;

                    case ProsodyPitch.Low:
                        sapiPitch = -4;
                        break;

                    case ProsodyPitch.ExtraLow:
                        sapiPitch = -9;
                        break;
                }
                // add the relative information
                sapiPitch = (int)((pitch.IsNumberPercent ? sapiPitch * pitch.Number : pitch.Number) + 0.5);
            }

            // Check the range.
            if (sapiPitch > 10)
            {
                sapiPitch = 10;
            }
            if (sapiPitch < -10)
            {
                sapiPitch = -10;
            }
            return sapiPitch;
        }

        private static int ScaleNumber(float value, int currentValue, int max)
        {
            int rate = 0;
            // Because we are on a logarithmic scale, can handle percentage changes
            // 300% --> multiply by 3.0 --> sapi rate change of +max.0
            // 100%    --> multiply by 1.0 --> sapi rate change of 0.0
            // 33%  --> multiply by 0.33 --> sapi rate change of -max.0
            if (value >= 0.01)
            {
                rate = (int)(((Math.Log(value) / Math.Log(3.0)) * max) + 0.5);
                rate += currentValue;
                if (rate > max)
                {
                    rate = max;
                }
                else if (rate < -max)
                {
                    rate = -max;
                }
            }
            else
            {
                rate = -max;
            }
            return rate;
        }

        #endregion

        #region Private Methods

        private static readonly string[] s_asSayAsFormat = new string[]
        {
            "acronym",
            "address",
            "cardinal",
            "currency",
            "date",
            "date:d",
            "date:dm",
            "date:dmy",
            "date:m",
            "date:md",
            "date:mdy",
            "date:my",
            "date:ym",
            "date:ymd",
            "date:y",
            "digits",
            "name",
            "net",
            "net:email",
            "net:uri",
            "ordinal",
            "spellout",
            "telephone",
            "time",
            "time:hms12",
            "time:hms24"
        };

        private static readonly string[] s_asContextFormat = new string[]
        {
            "name",
            "address",
            "number_cardinal",
            "currency",
            "date_md",
            "date_dm",
            "date_dm",
            "date_dmy",
            "date_md",
            "date_md",
            "date_mdy",
            "date_my",
            "date_ym",
            "date_ymd",
            "date_year",
            "number_digit",
            "name",
            "web_url",
            "E-mail_address",
            "web_url",
            "number_ordinal",
            "",
            "phone_number",
            "time",
            "time",
            "time"
        };

        #endregion
    }
}
