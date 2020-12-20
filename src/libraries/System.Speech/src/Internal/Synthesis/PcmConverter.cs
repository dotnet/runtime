// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Internal.Synthesis
{
    internal class PcmConverter
    {
        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        ///   Description:
        ///   first read samples into VAPI_PCM16, then judge cases : 
        ///   1. STEREO -> mono + resampling
        ///      STEREO  -> 1 mono -> reSampling
        ///   2. mono  -> STEREO + resampling
        ///      mono   -> reSampling -> STEREO
        ///   3. STEREO  -> STEREO + resampling
        ///      STEREO  -> 2 MONO - > reSampling -> 2 MONO -> STEREO
        ///   4. mono  -> mono + resampling
        ///      mono  -> reSampling -> mono
        /// </summary>
        /// <param name="inWavFormat"></param>
        /// <param name="outWavFormat"></param>
        /// <returns></returns>
        internal bool PrepareConverter (ref WAVEFORMATEX inWavFormat, ref WAVEFORMATEX outWavFormat)
        {
            bool convert = true;
            // Check if we can deal with the format
            if (!(inWavFormat.nSamplesPerSec > 0 && inWavFormat.nChannels <= 2 && inWavFormat.nChannels > 0 && outWavFormat.nChannels > 0 && outWavFormat.nSamplesPerSec > 0 && outWavFormat.nChannels <= 2))
            {
                throw new FormatException ();
            }

            _iInFormatType = AudioFormatConverter.TypeOf (inWavFormat);
            _iOutFormatType = AudioFormatConverter.TypeOf (outWavFormat);
            if (_iInFormatType < 0 || _iOutFormatType < 0)
            {
                throw new FormatException ();
            }

            // Check if Format in == Format out
            if (outWavFormat.nSamplesPerSec == inWavFormat.nSamplesPerSec && _iOutFormatType == _iInFormatType && outWavFormat.nChannels == inWavFormat.nChannels)
            {
                convert = false;
            }
            else
            {
                //--- need reset filter
                if (inWavFormat.nSamplesPerSec != outWavFormat.nSamplesPerSec)
                {
                    CreateResamplingFilter (inWavFormat.nSamplesPerSec, outWavFormat.nSamplesPerSec);
                }

                // Keep a reference to the WaveHeaderformat
                _inWavFormat = inWavFormat;
                _outWavFormat = outWavFormat;
            }
            return convert;
        }

        /// <summary>
        ///   Description:
        ///   first read samples into VAPI_PCM16, then judge cases : 
        ///   1. STEREO -> mono + resampling
        ///      STEREO  -> 1 mono -> reSampling
        ///   2. mono  -> STEREO + resampling
        ///      mono   -> reSampling -> STEREO
        ///   3. STEREO  -> STEREO + resampling
        ///      STEREO  -> 2 MONO - > reSampling -> 2 MONO -> STEREO
        ///   4. mono  -> mono + resampling
        ///      mono  -> reSampling -> mono
        /// </summary>
        /// <param name="pvInSamples"></param>
        /// <returns></returns>
        internal byte [] ConvertSamples (byte [] pvInSamples)
        {
            short [] pnBuff = null;

            //--- Convert samples to VAPI_PCM16
            short [] inSamples = AudioFormatConverter.Convert (pvInSamples, _iInFormatType, AudioCodec.PCM16);

            //--- case 1
            if (_inWavFormat.nChannels == 2 && _outWavFormat.nChannels == 1)
            {
                pnBuff = Resample (_inWavFormat, _outWavFormat, Stereo2Mono (inSamples), _leftMemory);
            }

            //--- case 2
            else if (_inWavFormat.nChannels == 1 && _outWavFormat.nChannels == 2)
            {
                //--- resampling
                pnBuff = Mono2Stereo (Resample (_inWavFormat, _outWavFormat, inSamples, _leftMemory));
            }

            //--- case 3
            if (_inWavFormat.nChannels == 2 && _outWavFormat.nChannels == 2)
            {
                if (_inWavFormat.nSamplesPerSec != _outWavFormat.nSamplesPerSec)
                {
                    short [] leftChannel;
                    short [] rightChannel;
                    SplitStereo (inSamples, out leftChannel, out rightChannel);
                    pnBuff = MergeStereo (Resample (_inWavFormat, _outWavFormat, leftChannel, _leftMemory), Resample (_inWavFormat, _outWavFormat, rightChannel, _rightMemory));
                }
                else
                {
                    pnBuff = inSamples;
                }
            }

            //--- case 4
            if (_inWavFormat.nChannels == 1 && _outWavFormat.nChannels == 1)
            {
                pnBuff = Resample (_inWavFormat, _outWavFormat, inSamples, _leftMemory);
            }

            _eChunkStatus = Block.Middle;
            //---Convert samples to output format
            return AudioFormatConverter.Convert (pnBuff, AudioCodec.PCM16, _iOutFormatType);
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        /// <summary>
        /// Convert the data from one sample rate to an another
        /// </summary>
        /// <param name="inWavFormat"></param>
        /// <param name="outWavFormat"></param>
        /// <param name="pnBuff"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private short [] Resample (WAVEFORMATEX inWavFormat, WAVEFORMATEX outWavFormat, short [] pnBuff, float [] memory)
        {
            if (inWavFormat.nSamplesPerSec != outWavFormat.nSamplesPerSec)
            {
                float [] pdBuff = Short2Float (pnBuff);

                //--- resample
                pdBuff = Resampling (pdBuff, memory);

                pnBuff = Float2Short (pdBuff);
            }
            return pnBuff;
        }


        /// <summary>
        /// convert short array to float array
        /// </summary>
        /// <param name="inSamples"></param>
        /// <returns></returns>
        static private float [] Short2Float (short [] inSamples)
        {
            float [] pdOut = new float [inSamples.Length];

            for (int i = 0; i < inSamples.Length; i++)
            {
                pdOut [i] = (float) inSamples [i];
            }

            return pdOut;
        }

        /// <summary>
        /// convert float array to short array
        /// </summary>
        /// <param name="inSamples"></param>
        /// <returns></returns>
        static private short [] Float2Short (float [] inSamples)
        {
            short [] outSamples = new short [inSamples.Length];
            float dtmp;

            for (int i = 0; i < inSamples.Length; i++)
            {
                if (inSamples [i] >= 0)
                {
                    dtmp = inSamples [i] + 0.5f;
                    if (dtmp > short.MaxValue)
                    {
                        dtmp = short.MaxValue;
                    }
                }
                else
                {
                    dtmp = inSamples [i] - 0.5f;
                    if (dtmp < short.MinValue)
                    {
                        dtmp = short.MinValue;
                    }
                }
                outSamples [i] = (short) (dtmp);
            }
            return outSamples;
        }

        /// <summary>
        /// convert mono speech to stereo speech
        /// </summary>
        /// <param name="inSamples"></param>
        /// <returns></returns>
        static private short [] Mono2Stereo (short [] inSamples)
        {
            short [] outSamples = new short [inSamples.Length * 2];

            for (int i = 0, k = 0; i < inSamples.Length; i++, k += 2)
            {
                outSamples [k] = inSamples [i];
                outSamples [k + 1] = inSamples [i];
            }

            return outSamples;
        }

        /// <summary>
        /// convert stereo speech to mono speech
        /// </summary>
        /// <param name="inSamples"></param>
        /// <returns></returns>
        static private short [] Stereo2Mono (short [] inSamples)
        {
            short [] outSamples = new short [inSamples.Length / 2];

            for (int i = 0, k = 0; i < inSamples.Length; i += 2, k++)
            {
                outSamples [k] = unchecked ((short) (((int) inSamples [i] + (int) inSamples [i + 1]) / 2));
            }

            return outSamples;
        }

        /// <summary>
        /// merge 2 channel signals into one signal
        /// </summary>
        /// <param name="leftSamples"></param>
        /// <param name="rightSamples"></param>
        /// <returns></returns>
        static private short [] MergeStereo (short [] leftSamples, short [] rightSamples)
        {
            short [] outSamples = new short [leftSamples.Length * 2];

            for (int i = 0, k = 0; i < leftSamples.Length; i++, k += 2)
            {
                outSamples [k] = leftSamples [i];
                outSamples [k + 1] = rightSamples [i];
            }

            return outSamples;
        }

        /// <summary>
        /// split stereo signals into 2 channel mono signals
        /// </summary>
        /// <param name="inSamples"></param>
        /// <param name="leftSamples"></param>
        /// <param name="rightSamples"></param>
        static private void SplitStereo (short [] inSamples, out short [] leftSamples, out short [] rightSamples)
        {
            int length = inSamples.Length / 2;

            leftSamples = new short [length];
            rightSamples = new short [length];

            for (int i = 0, k = 0; i < inSamples.Length; i += 2)
            {
                leftSamples [k] = inSamples [i];
                rightSamples [k] = inSamples [i + 1];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inHz"></param>
        /// <param name="outHz"></param>
        private void CreateResamplingFilter (int inHz, int outHz)
        {
            int iLimitFactor;

            if (inHz <= 0)
            {
                throw new ArgumentOutOfRangeException ("inHz");
            }

            if (outHz <= 0)
            {
                throw new ArgumentOutOfRangeException ("outHz");
            }

            FindResampleFactors (inHz, outHz);
            iLimitFactor = (_iUpFactor > _iDownFactor) ? _iUpFactor : _iDownFactor;

            _iFilterHalf = (int) (inHz * iLimitFactor * _dHalfFilterLen);
            _iFilterLen = 2 * _iFilterHalf + 1;

            _filterCoeff = WindowedLowPass (.5f / (float) iLimitFactor, (float) _iUpFactor);

            _iBuffLen = (int) ((float) _iFilterLen / (float) _iUpFactor);

            _leftMemory = new float [_iBuffLen];
            _rightMemory = new float [_iBuffLen];

            _eChunkStatus = Block.First; // first chunk
        }

        /// <summary>
        /// Creates a low pass filter using the windowing method.
        /// dCutOff is spec. in normalized frequency
        /// </summary>
        /// <param name="dCutOff"></param>
        /// <param name="dGain"></param>
        /// <returns></returns>
        private float [] WindowedLowPass (float dCutOff, float dGain)
        {
            float [] pdCoeffs = null;
            float [] pdWindow = null;
            double dArg;
            double dSinc;

            System.Diagnostics.Debug.Assert (dCutOff > 0.0 && dCutOff < 0.5);

            pdWindow = Blackman (_iFilterLen, true);

            pdCoeffs = new float [_iFilterLen];

            dArg = 2.0f * Math.PI * dCutOff;
            pdCoeffs [_iFilterHalf] = (float) (dGain * 2.0 * dCutOff);

            for (long i = 1; i <= _iFilterHalf; i++)
            {
                dSinc = dGain * Math.Sin (dArg * i) / (Math.PI * i) * pdWindow [_iFilterHalf - i];
                pdCoeffs [_iFilterHalf + i] = (float) dSinc;
                pdCoeffs [_iFilterHalf - i] = (float) dSinc;
            }

            return pdCoeffs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inHz"></param>
        /// <param name="outHz"></param>
        private void FindResampleFactors (int inHz, int outHz)
        {
            int iDiv = 1;
            int i;

            while (iDiv != 0)
            {
                iDiv = 0;
                for (i = 0; i < piPrimes.Length; i++)
                {
                    if ((inHz % piPrimes [i]) == 0 && (outHz % piPrimes [i]) == 0)
                    {
                        inHz /= piPrimes [i];
                        outHz /= piPrimes [i];
                        iDiv = 1;
                        break;
                    }
                }
            }

            _iUpFactor = outHz;
            _iDownFactor = inHz;
        }


        /*****************************************************************************
        * Resampling *
        *------------*
        *   Description:
        *
        ******************************************************************* DING  ***/
        private float [] Resampling (float [] inSamples, float [] pdMemory)
        {
            int cInSamples = inSamples.Length;
            int cOutSamples;
            int iPhase;
            int j;
            int n;
            int iAddHalf;

            if (_eChunkStatus == Block.First)
            {
                cOutSamples = (cInSamples * _iUpFactor - _iFilterHalf) / _iDownFactor;
                iAddHalf = 1;
            }
            else if (_eChunkStatus == Block.Middle)
            {
                cOutSamples = (cInSamples * _iUpFactor) / _iDownFactor;
                iAddHalf = 2;
            }
            else
            {
                System.Diagnostics.Debug.Assert (_eChunkStatus == Block.Last);
                cOutSamples = (_iFilterHalf * _iUpFactor) / _iDownFactor;
                iAddHalf = 2;
            }

            if (cOutSamples < 0)
            {
                cOutSamples = 0;
            }
            float [] outSamples = new float [cOutSamples];

            for (int i = 0; i < cOutSamples; i++)
            {
                double dAcum = 0.0;

                n = ((i * _iDownFactor - iAddHalf * _iFilterHalf) / _iUpFactor);
                iPhase = (i * _iDownFactor) - (n * _iUpFactor + iAddHalf * _iFilterHalf);

                for (j = 0; j < _iFilterLen / _iUpFactor; j++)
                {
                    if (_iUpFactor * j > iPhase)
                    {
                        if (n + j >= 0 && n + j < cInSamples)
                        {
                            dAcum += inSamples [n + j] * _filterCoeff [_iUpFactor * j - iPhase];
                        }
                        else if (n + j < 0)
                        {
                            dAcum += pdMemory [_iBuffLen + n + j] * _filterCoeff [_iUpFactor * j - iPhase];
                        }
                    }
                }

                outSamples [i] = (float) dAcum;
            }

            //--- store samples into buffer
            if (_eChunkStatus != Block.Last)
            {
                n = cInSamples - (_iBuffLen + 1);
                for (int i = 0; i < _iBuffLen; i++)
                {
                    if (n >= 0)
                    {
                        pdMemory [i] = inSamples [n++];
                    }
                    else
                    {
                        n++;
                        pdMemory [i] = 0.0f;
                    }
                }
            }

            return outSamples;
        }

        /// <summary>
        /// Returns a vector with a Blackman window of the specified length.
        /// </summary>
        /// <param name="iLength"></param>
        /// <param name="bSymmetric"></param>
        /// <returns></returns>
        static private float [] Blackman (int iLength, bool bSymmetric)
        {
            float [] pdWindow = new float [iLength];
            double dArg, dArg2;

            dArg = 2.0 * Math.PI;
            if (bSymmetric)
            {
                dArg /= (float) (iLength - 1);
            }
            else
            {
                dArg /= (float) iLength;
            }

            dArg2 = 2.0 * dArg;

            for (int i = 0; i < iLength; i++)
            {
                pdWindow [i] = (float) (0.42 - (0.5 * Math.Cos (dArg * i)) + (0.08 * Math.Cos (dArg2 * i)));
            }

            return pdWindow;
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region private Fields

        private enum Block { First, Middle, Last };

        private WAVEFORMATEX _inWavFormat;
        private WAVEFORMATEX _outWavFormat;
        AudioCodec _iInFormatType;
        AudioCodec _iOutFormatType;

        private Block _eChunkStatus;
        private int _iUpFactor;
        private int _iFilterHalf;
        private int _iDownFactor;
        private int _iFilterLen;
        private int _iBuffLen;
        private float [] _filterCoeff;

        private float [] _leftMemory;
        private float [] _rightMemory;

        const float _dHalfFilterLen = 0.0005f;

        static readonly int [] piPrimes = new int [] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

        #endregion
    }
}

