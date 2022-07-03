// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

#endregion

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    /// AudioFormatConverter takes its conversion tables from ...\scg\tts\common\vapiio\alaw_ULaw.cpp
    /// </summary>
    internal static class AudioFormatConverter
    {
        #region Internal Methods

        /// <summary>
        /// Finds the converting method based on the specified formats.
        /// </summary>
        /// <param name="data">Reference to the buffer of audio data.</param>
        /// <param name="from">Audio format that the data will be converted from.</param>
        /// <param name="to">Audio format that the data will be converted to.</param>
        /// <returns>New array with the audio data in requested format.</returns>
        internal static short[] Convert(byte[] data, AudioCodec from, AudioCodec to)
        {
            ConvertByteShort cnvDlgt = null;

            switch (from)
            {
                case AudioCodec.PCM8:
                    switch (to)
                    {
                        case AudioCodec.PCM16: cnvDlgt = new ConvertByteShort(ConvertLinear8LinearByteShort); break;
                    }
                    break;
                case AudioCodec.PCM16:
                    switch (to)
                    {
                        case AudioCodec.PCM16: cnvDlgt = new ConvertByteShort(ConvertLinear2LinearByteShort); break;
                    }
                    break;

                case AudioCodec.G711U:
                    switch (to)
                    {
                        case AudioCodec.PCM16: cnvDlgt = new ConvertByteShort(ConvertULaw2Linear); break;
                    }
                    break;
                case AudioCodec.G711A:
                    switch (to)
                    {
                        case AudioCodec.PCM16: cnvDlgt = new ConvertByteShort(ConvertALaw2Linear); break;
                    }
                    break;

                default:
                    throw new FormatException();
            }

            if (cnvDlgt == null)
            {
                throw new FormatException();
            }

            return cnvDlgt(data, data.Length);
        }

        /// <summary>
        /// Finds the converting method based on the specified formats.
        /// </summary>
        /// <param name="data">Reference to the buffer of audio data.</param>
        /// <param name="from">Audio format that the data will be converted from.</param>
        /// <param name="to">Audio format that the data will be converted to.</param>
        /// <returns>New array with the audio data in requested format.</returns>
        internal static byte[] Convert(short[] data, AudioCodec from, AudioCodec to)
        {
            ConvertShortByte cnvDlgt = null;

            switch (from)
            {
                case AudioCodec.PCM16:
                    switch (to)
                    {
                        case AudioCodec.PCM8: cnvDlgt = new ConvertShortByte(ConvertLinear8LinearShortByte); break;
                        case AudioCodec.PCM16: cnvDlgt = new ConvertShortByte(ConvertLinear2LinearShortByte); break;
                        case AudioCodec.G711U: cnvDlgt = new ConvertShortByte(ConvertLinear2ULaw); break;
                        case AudioCodec.G711A: cnvDlgt = new ConvertShortByte(ConvertLinear2ALaw); break;
                    }
                    break;

                default:
                    throw new FormatException();
            }

            return cnvDlgt(data, data.Length);
        }

        internal static AudioCodec TypeOf(WAVEFORMATEX format)
        {
            AudioCodec codec = AudioCodec.Undefined;

            switch ((WaveFormatTag)format.wFormatTag)
            {
                case WaveFormatTag.WAVE_FORMAT_PCM:
                    switch (format.nBlockAlign / format.nChannels)
                    {
                        case 1:
                            codec = AudioCodec.PCM8;
                            break;
                        case 2:
                            codec = AudioCodec.PCM16;
                            break;
                    }
                    break;

                case WaveFormatTag.WAVE_FORMAT_ALAW:
                    codec = AudioCodec.G711A;
                    break;

                case WaveFormatTag.WAVE_FORMAT_MULAW:
                    codec = AudioCodec.G711U;
                    break;
            }

            return codec;
        }

        #endregion

        #region Private Methods

        #region Converters between Linear and ULaw

        /// <summary>
        /// This routine converts from 16 bit linear to ULaw by direct access to the conversion table.
        /// </summary>
        /// <param name="data">Array of 16 bit linear samples.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>New buffer of 8 bit ULaw samples.</returns>
        internal static byte[] ConvertLinear2ULaw(short[] data, int size)
        {
            byte[] newData = new byte[size];
            s_uLawCompTableCached ??= CalcLinear2ULawTable();

            for (int i = 0; i < size; i++)
            {
                unchecked
                {
                    // Extend the sign bit for the sample that is constructed from two bytes
                    newData[i] = s_uLawCompTableCached[(ushort)data[i] >> 2];
                }
            }
            return newData;
        }

        /// <summary>
        /// This routine converts from ULaw to 16 bit linear by direct access to the conversion table.
        /// </summary>
        /// <param name="data">Array of 8 bit ULaw samples.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>New buffer of signed 16 bit linear samples</returns>
        internal static short[] ConvertULaw2Linear(byte[] data, int size)
        {
            short[] newData = new short[size];
            for (int i = 0; i < size; i++)
            {
                int sample = s_ULaw_exp_table[data[i]];

                newData[i] = unchecked((short)sample);
            }

            return newData;
        }

        /// <summary>
        /// This routine converts from linear to ULaw.
        ///
        /// Craig Reese: IDA/Supercomputing Research Center
        /// Joe Campbell: Department of Defense
        /// 29 September 1989
        ///
        /// References:
        /// 1) CCITT Recommendation G.711  (very difficult to follow)
        /// 2) "A New Digital Technique for Implementation of Any
        ///     Continuous PCM Companding Law," Villeret, Michel,
        ///     et al. 1973 IEEE Int. Conf. on Communications, Vol 1,
        ///     1973, pg. 11.12-11.17
        /// 3) MIL-STD-188-113,"Interoperability and Performance Standards
        ///     for Analog-to_Digital Conversion Techniques,"
        ///     17 February 1987
        /// </summary>
        /// <returns>New buffer of 8 bit ULaw samples</returns>
        private static byte[] CalcLinear2ULawTable()
        {
            /*const*/
            bool ZEROTRAP = false;      // turn off the trap as per the MIL-STD
            const byte uBIAS = 0x84;              // define the add-in bias for 16 bit samples
            const int uCLIP = 32635;

            byte[] table = new byte[(ushort.MaxValue + 1) >> 2];

            for (int i = 0; i < ushort.MaxValue; i += 4)
            {
                short data = unchecked((short)i);

                int sample;
                int sign, exponent, mantissa;
                byte ULawbyte;

                unchecked
                {
                    // Extend the sign bit for the sample that is constructed from two bytes
                    sample = (data >> 2) << 2;

                    // Get the sample into sign-magnitude.
                    sign = (sample >> 8) & 0x80;          // set aside the sign
                    if (sign != 0)
                    {
                        sample = -sample;
                    }
                    if (sample > uCLIP) sample = uCLIP;   // clip the magnitude

                    // Convert from 16 bit linear to ULaw.
                    sample += uBIAS;
                    exponent = s_exp_lut_linear2ulaw[(sample >> 7) & 0xFF];
                    mantissa = (sample >> (exponent + 3)) & 0x0F;

                    ULawbyte = (byte)(~(sign | (exponent << 4) | mantissa));
                }

                if (ZEROTRAP)
                {
                    if (ULawbyte == 0) ULawbyte = 0x02; // optional CCITT trap
                }

                table[i >> 2] = ULawbyte;
            }

            return table;
        }

        #endregion

        #region Converters between Linear and ALaw

        /// <summary>
        /// This routine converts from 16 bit linear to ALaw by direct access to the conversion table.
        /// </summary>
        /// <param name="data">Array of 16 bit linear samples.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>New buffer of 8 bit ALaw samples.</returns>
        internal static byte[] ConvertLinear2ALaw(short[] data, int size)
        {
            byte[] newData = new byte[size];
            s_aLawCompTableCached ??= CalcLinear2ALawTable();

            for (int i = 0; i < size; i++)
            {
                unchecked
                {
                    //newData [i] = ALaw_comp_table [(data [i] / 4) & 0x3fff];
                    newData[i] = s_aLawCompTableCached[(ushort)data[i] >> 2];
                }
            }

            return newData;
        }

        /// <summary>
        /// This routine converts from ALaw to 16 bit linear by direct access to the conversion table.
        /// </summary>
        /// <param name="data">Array of 8 bit ALaw samples.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>New buffer of signed 16 bit linear samples</returns>
        internal static short[] ConvertALaw2Linear(byte[] data, int size)
        {
            short[] newData = new short[size];
            for (int i = 0; i < size; i++)
            {
                int sample = s_ALaw_exp_table[data[i]];

                newData[i] = unchecked((short)sample);
            }

            return newData;
        }

        /// <summary>
        /// This routine converts from linear to ALaw.
        ///
        /// Craig Reese: IDA/Supercomputing Research Center
        /// Joe Campbell: Department of Defense
        /// 29 September 1989
        ///
        /// References:
        /// 1) CCITT Recommendation G.711  (very difficult to follow)
        /// 2) "A New Digital Technique for Implementation of Any
        ///     Continuous PCM Companding Law," Villeret, Michel,
        ///     et al. 1973 IEEE Int. Conf. on Communications, Vol 1,
        ///     1973, pg. 11.12-11.17
        /// 3) MIL-STD-188-113,"Interoperability and Performance Standards
        ///     for Analog-to_Digital Conversion Techniques,"
        ///     17 February 1987
        /// </summary>
        /// <returns>New buffer of 8 bit ALaw samples</returns>
        private static byte[] CalcLinear2ALawTable()
        {
            const int ACLIP = 31744;

            byte[] table = new byte[(ushort.MaxValue + 1) >> 2];

            for (int i = 0; i < ushort.MaxValue; i += 4)
            {
                short data = unchecked((short)i);

                int sample, sign, exponent, mantissa;
                byte ALawbyte;

                unchecked
                {
                    // Extend the sign bit for the sample that is constructed from two bytes
                    sample = (data >> 2) << 2;

                    // Get the sample into sign-magnitude.
                    sign = ((~sample) >> 8) & 0x80;     // set aside the sign
                    if (sign == 0) sample = -sample;    // get magnitude
                    if (sample > ACLIP) sample = ACLIP; // clip the magnitude
                }

                // Convert from 16 bit linear to ULaw.
                if (sample >= 256)
                {
                    exponent = s_exp_lut_linear2alaw[(sample >> 8) & 0x7F];
                    mantissa = (sample >> (exponent + 3)) & 0x0F;
                    ALawbyte = (byte)((exponent << 4) | mantissa);
                }
                else
                {
                    ALawbyte = (byte)(sample >> 4);
                }

                ALawbyte ^= (byte)(sign ^ 0x55);

                table[i >> 2] = ALawbyte;
            }

            return table;
        }

        #endregion

        #region PCM to PCM

        /// <summary>
        /// Empty linear conversion (does nothing, for table consistency).
        /// </summary>
        /// <param name="data">Array of audio data in linear format.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>The same array in linear format.</returns>
        private static short[] ConvertLinear2LinearByteShort(byte[] data, int size)
        {
            short[] as1 = new short[size / 2];
            unchecked
            {
                for (int i = 0; i < size; i += 2)
                {
                    as1[i / 2] = (short)(data[i] + (short)(data[i + 1] << 8));
                }
            }
            return as1;
        }

        /// <summary>
        /// Empty linear conversion (does nothing, for table consistency).
        /// </summary>
        /// <param name="data">Array of audio data in linear format.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>The same array in linear format.</returns>
        private static short[] ConvertLinear8LinearByteShort(byte[] data, int size)
        {
            short[] as1 = new short[size];
            unchecked
            {
                for (int i = 0; i < size; i++)
                {
                    as1[i] = (short)((data[i] - 128) << 8);
                }
            }
            return as1;
        }

        /// <summary>
        /// Empty linear conversion (does nothing, for table consistency).
        /// </summary>
        /// <param name="data">Array of audio data in linear format.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>The same array in linear format.</returns>
        private static byte[] ConvertLinear2LinearShortByte(short[] data, int size)
        {
            byte[] ab = new byte[size * 2];
            for (int i = 0; i < size; i++)
            {
                short s = data[i];
                ab[2 * i] = unchecked((byte)s);
                ab[2 * i + 1] = unchecked((byte)(s >> 8));
            }
            return ab; // the same format: do nothing
        }

        /// <summary>
        /// Empty linear conversion (does nothing, for table consistency).
        /// </summary>
        /// <param name="data">Array of audio data in linear format.</param>
        /// <param name="size">Size of the data in the array.</param>
        /// <returns>The same array in linear format.</returns>
        private static byte[] ConvertLinear8LinearShortByte(short[] data, int size)
        {
            byte[] ab = new byte[size];
            for (int i = 0; i < size; i++)
            {
                ab[i] = unchecked((byte)(((ushort)((data[i] + 127) >> 8)) + 128));
            }
            return ab; // the same format: do nothing
        }

        #endregion

        #endregion

        #region Private Members

        #region Conversion tables for direct conversions

        // Cached table for aLaw and uLaw conversion (16K * 2 bytes each)
        private static byte[] s_uLawCompTableCached;
        private static byte[] s_aLawCompTableCached;

        #endregion

        #region Conversion tables for algorithmic conversions

        private static readonly int[] s_exp_lut_linear2alaw = new int[128]
        {
            1, 1, 2, 2, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7
        };

        private static int[] s_exp_lut_linear2ulaw = new int[256]
        {
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
        };

        #endregion

        #region Conversion tables for 'byte' to 'short' conversion

        /// <summary>
        /// Table to converts ULaw values to Linear
        /// </summary>
        private static int[] s_ULaw_exp_table = new int[256]
        {
            -32124, -31100, -30076, -29052, -28028, -27004, -25980, -24956,
            -23932, -22908, -21884, -20860, -19836, -18812, -17788, -16764,
            -15996, -15484, -14972, -14460, -13948, -13436, -12924, -12412,
            -11900, -11388, -10876, -10364, -9852, -9340, -8828, -8316,
            -7932, -7676, -7420, -7164, -6908, -6652, -6396, -6140,
            -5884, -5628, -5372, -5116, -4860, -4604, -4348, -4092,
            -3900, -3772, -3644, -3516, -3388, -3260, -3132, -3004,
            -2876, -2748, -2620, -2492, -2364, -2236, -2108, -1980,
            -1884, -1820, -1756, -1692, -1628, -1564, -1500, -1436,
            -1372, -1308, -1244, -1180, -1116, -1052,  -988,  -924,
            -876,  -844,  -812,  -780,  -748,  -716,  -684,  -652,
            -620,  -588,  -556,  -524,  -492,  -460,  -428,  -396,
            -372,  -356,  -340,  -324,  -308,  -292,  -276,  -260,
            -244,  -228,  -212,  -196,  -180,  -164,  -148,  -132,
            -120,  -112,  -104,   -96,   -88,   -80,   -72,   -64,
            -56,   -48,   -40,   -32,   -24,   -16,    -8,     0,
            32124, 31100, 30076, 29052, 28028, 27004, 25980, 24956,
            23932, 22908, 21884, 20860, 19836, 18812, 17788, 16764,
            15996, 15484, 14972, 14460, 13948, 13436, 12924, 12412,
            11900, 11388, 10876, 10364,  9852,  9340,  8828,  8316,
            7932,  7676,  7420,  7164,  6908,  6652,  6396,  6140,
            5884,  5628,  5372,  5116,  4860,  4604,  4348,  4092,
            3900,  3772,  3644,  3516,  3388,  3260,  3132,  3004,
            2876,  2748,  2620,  2492,  2364,  2236,  2108,  1980,
            1884,  1820,  1756,  1692,  1628,  1564,  1500,  1436,
            1372,  1308,  1244,  1180,  1116,  1052,   988,   924,
            876,   844,   812,   780,   748,   716,   684,   652,
            620,   588,   556,   524,   492,   460,   428,   396,
            372,   356,   340,   324,   308,   292,   276,   260,
            244,   228,   212,   196,   180,   164,   148,   132,
            120,   112,   104,    96,    88,    80,    72,    64,
            56,    48,    40,    32,    24,    16,     8,     0
        };

        /// <summary>
        /// Table to converts ALaw values to Linear
        /// </summary>
        private static int[] s_ALaw_exp_table = new int[256]
        {
            -5504, -5248, -6016, -5760, -4480, -4224, -4992, -4736,
            -7552, -7296, -8064, -7808, -6528, -6272, -7040, -6784,
            -2752, -2624, -3008, -2880, -2240, -2112, -2496, -2368,
            -3776, -3648, -4032, -3904, -3264, -3136, -3520, -3392,
            -22016, -20992, -24064, -23040, -17920, -16896, -19968, -18944,
            -30208, -29184, -32256, -31232, -26112, -25088, -28160, -27136,
            -11008, -10496, -12032, -11520, -8960, -8448, -9984, -9472,
            -15104, -14592, -16128, -15616, -13056, -12544, -14080, -13568,
            -344,  -328,  -376,  -360,  -280,  -264,  -312,  -296,
            -472,  -456,  -504,  -488,  -408,  -392,  -440,  -424,
            -88,   -72,  -120,  -104,   -24,    -8,   -56,   -40,
            -216,  -200,  -248,  -232,  -152,  -136,  -184,  -168,
            -1376, -1312, -1504, -1440, -1120, -1056, -1248, -1184,
            -1888, -1824, -2016, -1952, -1632, -1568, -1760, -1696,
            -688,  -656,  -752,  -720,  -560,  -528,  -624,  -592,
            -944,  -912, -1008,  -976,  -816,  -784,  -880,  -848,
            5504,  5248,  6016,  5760,  4480,  4224,  4992,  4736,
            7552,  7296,  8064,  7808,  6528,  6272,  7040,  6784,
            2752,  2624,  3008,  2880,  2240,  2112,  2496,  2368,
            3776,  3648,  4032,  3904,  3264,  3136,  3520,  3392,
            22016, 20992, 24064, 23040, 17920, 16896, 19968, 18944,
            30208, 29184, 32256, 31232, 26112, 25088, 28160, 27136,
            11008, 10496, 12032, 11520,  8960,  8448,  9984,  9472,
            15104, 14592, 16128, 15616, 13056, 12544, 14080, 13568,
            344,   328,   376,   360,   280,   264,   312,   296,
            472,   456,   504,   488,   408,   392,   440,   424,
            88,    72,   120,   104,    24,     8,    56,    40,
            216,   200,   248,   232,   152,   136,   184,   168,
            1376,  1312,  1504,  1440,  1120,  1056,  1248,  1184,
            1888,  1824,  2016,  1952,  1632,  1568,  1760,  1696,
            688,   656,   752,   720,   560,   528,   624,   592,
            944,   912,  1008,   976,   816,   784,   880,   848
        };

        #endregion

        internal enum WaveFormatTag
        {
            WAVE_FORMAT_PCM = 1,
            WAVE_FORMAT_ALAW = 0x0006,
            WAVE_FORMAT_MULAW = 0x0007
        }
        // delegates
        private delegate short[] ConvertByteShort(byte[] data, int size);
        private delegate byte[] ConvertShortByte(short[] data, int size);

        #endregion
    }

    #region Internal Types

    /// <summary>
    /// Supported formats for audio transcoding in SES
    /// </summary>
    internal enum AudioCodec
    {
        /// <summary>
        /// Audio format PCM 16 bit
        /// </summary>
        PCM16 = 128,

        /// <summary>
        /// Audio format PCM 16 bit
        /// </summary>
        PCM8 = 127,

        /// <summary>
        /// Audio format G.711 mu-law
        /// </summary>
        G711U = 0,

        /// <summary>
        /// AudioFormat G.711 A-law
        /// </summary>
        G711A = 8,

        /// <summary>
        /// No audio format specified
        /// </summary>
        Undefined = -1
    }

    #endregion
}
