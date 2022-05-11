// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Speech.Internal.Synthesis;

namespace System.Speech.AudioFormat
{
    [Serializable]
    public class SpeechAudioFormatInfo
    {
        #region Constructors

        private SpeechAudioFormatInfo(EncodingFormat encodingFormat, int samplesPerSecond, short bitsPerSample, short channelCount, byte[] formatSpecificData)
        {
            if (encodingFormat == 0)
            {
                throw new ArgumentException(SR.Get(SRID.CannotUseCustomFormat), nameof(encodingFormat));
            }
            if (samplesPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(samplesPerSecond), SR.Get(SRID.MustBeGreaterThanZero));
            }
            if (bitsPerSample <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerSample), SR.Get(SRID.MustBeGreaterThanZero));
            }
            if (channelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channelCount), SR.Get(SRID.MustBeGreaterThanZero));
            }

            _encodingFormat = encodingFormat;
            _samplesPerSecond = samplesPerSecond;
            _bitsPerSample = bitsPerSample;
            _channelCount = channelCount;
            if (formatSpecificData == null)
            {
                _formatSpecificData = Array.Empty<byte>();
            }
            else
            {
                _formatSpecificData = (byte[])formatSpecificData.Clone();
            }

            switch (encodingFormat)
            {
                case EncodingFormat.ALaw:
                case EncodingFormat.ULaw:
                    if (bitsPerSample != 8)
                    {
                        throw new ArgumentOutOfRangeException(nameof(bitsPerSample));
                    }
                    if (formatSpecificData != null && formatSpecificData.Length != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(formatSpecificData));
                    }
                    break;
            }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SpeechAudioFormatInfo(EncodingFormat encodingFormat, int samplesPerSecond, int bitsPerSample, int channelCount, int averageBytesPerSecond, int blockAlign, byte[] formatSpecificData)
            : this(encodingFormat, samplesPerSecond, (short)bitsPerSample, (short)channelCount, formatSpecificData)
        {
            // Don't explicitly check these are sensible values - allow flexibility here as some formats may do unexpected things here.
            if (averageBytesPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(averageBytesPerSecond), SR.Get(SRID.MustBeGreaterThanZero));
            }
            if (blockAlign <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockAlign), SR.Get(SRID.MustBeGreaterThanZero));
            }
            _averageBytesPerSecond = averageBytesPerSecond;
            _blockAlign = (short)blockAlign;
        }
        public SpeechAudioFormatInfo(int samplesPerSecond, AudioBitsPerSample bitsPerSample, AudioChannel channel)
            : this(EncodingFormat.Pcm, samplesPerSecond, (short)bitsPerSample, (short)channel, null)
        {
            // Don't explicitly check these are sensible values - allow flexibility here as some formats may do unexpected things here.
            _blockAlign = (short)(_channelCount * (_bitsPerSample / 8));
            _averageBytesPerSecond = _samplesPerSecond * _blockAlign;
        }

        #endregion

        #region Public Properties
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public int AverageBytesPerSecond { get { return _averageBytesPerSecond; } }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public int BitsPerSample { get { return _bitsPerSample; } }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public int BlockAlign { get { return _blockAlign; } }
        public EncodingFormat EncodingFormat { get { return _encodingFormat; } }
        public int ChannelCount { get { return _channelCount; } }
        public int SamplesPerSecond { get { return _samplesPerSecond; } }

        #endregion

        #region Public Methods
        public byte[] FormatSpecificData() { return (byte[])_formatSpecificData.Clone(); }
        public override bool Equals(object obj)
        {
            SpeechAudioFormatInfo refObj = obj as SpeechAudioFormatInfo;
            if (refObj == null)
            {
                return false;
            }

            if (!(_averageBytesPerSecond.Equals(refObj._averageBytesPerSecond) &&
                _bitsPerSample.Equals(refObj._bitsPerSample) &&
                _blockAlign.Equals(refObj._blockAlign) &&
                _encodingFormat.Equals(refObj._encodingFormat) &&
                _channelCount.Equals(refObj._channelCount) &&
                _samplesPerSecond.Equals(refObj._samplesPerSecond)))
            {
                return false;
            }
            if (_formatSpecificData.Length != refObj._formatSpecificData.Length)
            {
                return false;
            }
            for (int i = 0; i < _formatSpecificData.Length; i++)
            {
                if (_formatSpecificData[i] != refObj._formatSpecificData[i])
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            return _averageBytesPerSecond.GetHashCode();
        }

        #endregion

        #region Internal Methods
        internal byte[] WaveFormat
        {
            get
            {
                WAVEFORMATEX wfx = new();
                wfx.wFormatTag = (short)EncodingFormat;
                wfx.nChannels = (short)ChannelCount;
                wfx.nSamplesPerSec = SamplesPerSecond;
                wfx.nAvgBytesPerSec = AverageBytesPerSecond;
                wfx.nBlockAlign = (short)BlockAlign;
                wfx.wBitsPerSample = (short)BitsPerSample;
                wfx.cbSize = (short)FormatSpecificData().Length;

                byte[] abWfx = wfx.ToBytes();
                if (wfx.cbSize > 0)
                {
                    byte[] wfxTemp = new byte[abWfx.Length + wfx.cbSize];
                    Array.Copy(abWfx, wfxTemp, abWfx.Length);
                    Array.Copy(FormatSpecificData(), 0, wfxTemp, abWfx.Length, wfx.cbSize);
                    abWfx = wfxTemp;
                }
                return abWfx;
            }
        }
        #endregion

        #region Private Fields

        private int _averageBytesPerSecond;
        private short _bitsPerSample;
        private short _blockAlign;
        private EncodingFormat _encodingFormat;
        private short _channelCount;
        private int _samplesPerSecond;
        private byte[] _formatSpecificData;

        #endregion
    }

    #region Public Properties
    public enum AudioChannel
    {
        Mono = 1,
        Stereo = 2
    }
    public enum AudioBitsPerSample
    {
        Eight = 8,
        Sixteen = 16
    }

    #endregion
}
