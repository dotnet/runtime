// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Internal;

namespace System.Speech.Recognition
{
    [Serializable]
    public class RecognizedAudio
    {
        internal RecognizedAudio(byte[] rawAudioData, SpeechAudioFormatInfo audioFormat, DateTime startTime, TimeSpan audioPosition, TimeSpan audioDuration)
        {
            _audioFormat = audioFormat;
            _startTime = startTime;
            _audioPosition = audioPosition;
            _audioDuration = audioDuration;
            _rawAudioData = rawAudioData;
        }
        public SpeechAudioFormatInfo Format
        {
            get { return _audioFormat; }
        }

        // Chronological "wall-clock" time the user started speaking the result at. This is useful for latency calculations etc.
        public DateTime StartTime
        {
            get { return _startTime; }
        }

        // Position in the audio stream this audio starts at.
        // Note: the stream starts at zero when the engine first starts processing audio.
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        // Length of this audio fragment
        public TimeSpan Duration
        {
            get { return _audioDuration; }
        }

        // Different ways to store the audio, either as a binary data stream or as a wave file.
        public void WriteToWaveStream(Stream outputStream)
        {
            Helpers.ThrowIfNull(outputStream, nameof(outputStream));

            using (StreamMarshaler sm = new(outputStream))
            {
                WriteWaveHeader(sm);
            }

            // now write the raw data
            outputStream.Write(_rawAudioData, 0, _rawAudioData.Length);

            outputStream.Flush();
        }

        // Different ways to store the audio, either as a binary data stream or as a wave file.
        public void WriteToAudioStream(Stream outputStream)
        {
            Helpers.ThrowIfNull(outputStream, nameof(outputStream));

            // now write the raw data
            outputStream.Write(_rawAudioData, 0, _rawAudioData.Length);

            outputStream.Flush();
        }

        // Get another audio object from this one representing a range of audio.
        public RecognizedAudio GetRange(TimeSpan audioPosition, TimeSpan duration)
        {
            if (audioPosition.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(audioPosition), SR.Get(SRID.NegativeTimesNotSupported));
            }
            if (duration.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), SR.Get(SRID.NegativeTimesNotSupported));
            }
            if (audioPosition > _audioDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(audioPosition));
            }

            if (duration > audioPosition + _audioDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            // Get the position and length in bytes offset and bytes length.
            int startPosition = (int)((_audioFormat.BitsPerSample * _audioFormat.SamplesPerSecond * audioPosition.Ticks) / (TimeSpan.TicksPerSecond * 8));
            int length = (int)((_audioFormat.BitsPerSample * _audioFormat.SamplesPerSecond * duration.Ticks) / (TimeSpan.TicksPerSecond * 8));
            if (startPosition + length > _rawAudioData.Length)
            {
                length = _rawAudioData.Length - startPosition;
            }

            // Extract the data from the original stream
            byte[] audioBytes = new byte[length];
            Array.Copy(_rawAudioData, startPosition, audioBytes, 0, length);
            return new RecognizedAudio(audioBytes, _audioFormat, _startTime + audioPosition, audioPosition, duration);
        }

        #region Private Methods

        private void WriteWaveHeader(StreamMarshaler sm)
        {
            char[] riff = new char[4] { 'R', 'I', 'F', 'F' };
            byte[] formatSpecificData = _audioFormat.FormatSpecificData();
            sm.WriteArray<char>(riff, riff.Length);

            sm.WriteStream((uint)(_rawAudioData.Length + 38 + formatSpecificData.Length)); // Must be four bytes

            char[] wave = new char[4] { 'W', 'A', 'V', 'E' };
            sm.WriteArray(wave, wave.Length);

            char[] fmt = new char[4] { 'f', 'm', 't', ' ' };
            sm.WriteArray(fmt, fmt.Length);

            sm.WriteStream(18 + formatSpecificData.Length);

            sm.WriteStream((ushort)_audioFormat.EncodingFormat);
            sm.WriteStream((ushort)_audioFormat.ChannelCount);
            sm.WriteStream(_audioFormat.SamplesPerSecond);
            sm.WriteStream(_audioFormat.AverageBytesPerSecond);
            sm.WriteStream((ushort)_audioFormat.BlockAlign);
            sm.WriteStream((ushort)_audioFormat.BitsPerSample);
            sm.WriteStream((ushort)formatSpecificData.Length);

            // write codec specific data
            if (formatSpecificData.Length > 0)
            {
                sm.WriteStream(formatSpecificData);
            }

            char[] data = new char[4] { 'd', 'a', 't', 'a' };
            sm.WriteArray(data, data.Length);
            sm.WriteStream(_rawAudioData.Length);
        }

        #endregion

        #region Private Fields

        private DateTime _startTime;
        private TimeSpan _audioPosition;
        private TimeSpan _audioDuration;
        private SpeechAudioFormatInfo _audioFormat;
        private byte[] _rawAudioData;

        #endregion
    }
}
