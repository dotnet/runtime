// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Text;

// Exceptions cannot get through the COM code.
// The engine site saves the last exception before sending it back to the client.
#pragma warning disable 6500

namespace System.Speech.Internal.Synthesis
{
    internal class EngineSite : ITtsEngineSite, ITtsEventSink
    {
        #region Constructors

        internal EngineSite(ResourceLoader resourceLoader)
        {
            _resourceLoader = resourceLoader;
        }

        #endregion

        #region Internal Methods
        internal TtsEventMapper EventMapper
        {
            get
            {
                return _eventMapper;
            }
            set
            {
                _eventMapper = value;
            }
        }

        #region ISpTTSEngineStite implementation
        /// <summary>
        /// Adds events directly to an event sink.
        /// </summary>
        public void AddEvents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SpeechEventInfo[] events, int ulCount)
        {
            try
            {
                foreach (SpeechEventInfo sapiEvent in events)
                {
                    int evtMask = 1 << sapiEvent.EventId;

                    if (sapiEvent.EventId == (short)TtsEventId.EndInputStream && _eventMapper != null)
                    {
                        _eventMapper.FlushEvent();
                    }

                    if ((evtMask & _eventInterest) != 0)
                    {
                        TTSEvent ttsEvent = CreateTtsEvent(sapiEvent);
                        if (_eventMapper == null)
                        {
                            AddEvent(ttsEvent);
                        }
                        else
                        {
                            _eventMapper.AddEvent(ttsEvent);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _exception = e;
                _actions |= SPVESACTIONS.SPVES_ABORT;
            }
        }

        /// <summary>
        /// Queries the voice object to determine which real-time action(s) to perform.
        /// </summary>
        public int Write(IntPtr pBuff, int cb)
        {
            try
            {
                _audio.Play(pBuff, cb);
            }
            catch (Exception e)
            {
                _exception = e;
                _actions |= SPVESACTIONS.SPVES_ABORT;
            }
            return cb;
        }

        /// <summary>
        /// Retrieves the number and type of items to be skipped in the text stream.
        /// </summary>
        public SkipInfo GetSkipInfo()
        {
            return new SkipInfo(1 /*BSPVSKIPTYPE.SPVST_SENTENCE */, 1);
        }

        /// <summary>
        /// Notifies that the last skip request has been completed and to pass it the results.
        /// </summary>
        public void CompleteSkip(int ulNumSkipped)
        {
            return;
        }

        /// <summary>
        /// Passes back the event interest for the voice.
        /// </summary>
        public int EventInterest
        {
            get
            {
                return _eventInterest;
            }
        }

        /// <summary>
        ///  Queries the voice object to determine which real-time action(s) to perform
        /// </summary>
        public int Actions
        {
            get
            {
                return (int)_actions;
            }
        }

        /// <summary>
        ///  Retrieves the current TTS rendering rate adjustment that should be used by the engine.
        /// </summary>
        public int Rate
        {
            get
            {
                _actions &= ~SPVESACTIONS.SPVES_RATE;
                return _defaultRate;
            }
        }

        /// <summary>
        /// Retrieves the base output volume level the engine should use during synthesis.
        /// </summary>
        public int Volume
        {
            get
            {
                _actions &= ~SPVESACTIONS.SPVES_VOLUME;
                return _volume;
            }
        }

        /// <summary>
        /// Load a file either from a local network or from the Internet.
        /// </summary>
        public Stream LoadResource(Uri uri, string mediaType)
        {
            try
            {
                string localPath;
                string mediaTypeUnused; // TODO: Should this be passed out of this function?
                Uri baseUriUnused;
                using (Stream stream = _resourceLoader.LoadFile(uri, out mediaTypeUnused, out baseUriUnused, out localPath))
                {
                    // Read the file in memory for SES and release the original file immediately
                    // This scheme is really bad if the files being read are big but I would assume
                    // That it should not be the case.
                    int cLen = (int)stream.Length;
                    MemoryStream memStream = new(cLen);
                    byte[] ab = new byte[cLen];
                    stream.Read(ab, 0, ab.Length);
                    _resourceLoader.UnloadFile(localPath);
                    memStream.Write(ab, 0, cLen);
                    memStream.Position = 0;

                    return memStream;
                }
            }
            catch (Exception e)
            {
                _exception = e;
                _actions |= SPVESACTIONS.SPVES_ABORT;
            }
            return null;
        }

        #endregion

        public void AddEvent(TTSEvent evt)
        {
            _audio.InjectEvent(evt);
        }

        public void FlushEvent()
        {
        }

        internal void SetEventsInterest(int eventInterest)
        {
            _eventInterest = eventInterest;
            _eventMapper?.FlushEvent();
        }

        #endregion

        #region Internal Properties

        /// <summary>
        ///  Retrieves the current TTS rendering rate adjustment that should be used by the engine.
        /// </summary>
        internal int VoiceRate
        {
            get
            {
                return _defaultRate;
            }
            set
            {
                _defaultRate = value;
                _actions |= SPVESACTIONS.SPVES_RATE;
            }
        }

        /// <summary>
        /// Retrieves the base output volume level the engine should use during synthesis.
        /// </summary>
        internal int VoiceVolume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
                _actions |= SPVESACTIONS.SPVES_VOLUME;
            }
        }

        /// <summary>
        /// Set and reset the last exception
        /// </summary>
        internal Exception LastException
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
            }
        }

        internal void Abort()
        {
            _actions = SPVESACTIONS.SPVES_ABORT;
        }

        internal void InitRun(AudioBase audioDevice, int defaultRate, Prompt prompt)
        {
            _audio = audioDevice;
            _prompt = prompt;
            _defaultRate = defaultRate;
            _actions = SPVESACTIONS.SPVES_RATE | SPVESACTIONS.SPVES_VOLUME;
        }

        #endregion

        #region Private Members

        private TTSEvent CreateTtsEvent(SpeechEventInfo sapiEvent)
        {
            TTSEvent ttsEvent;
            switch ((TtsEventId)sapiEvent.EventId)
            {
                case TtsEventId.Phoneme:
                    ttsEvent = TTSEvent.CreatePhonemeEvent("" + (char)((uint)sapiEvent.Param2 & 0xFFFF), // current phoneme
                                                           "" + (char)(sapiEvent.Param1 & 0xFFFF), // next phoneme
                                                           TimeSpan.FromMilliseconds(sapiEvent.Param1 >> 16),
                                                           (SynthesizerEmphasis)((uint)sapiEvent.Param2 >> 16),
                                                           _prompt, _audio.Duration);
                    break;
                case TtsEventId.Bookmark:
                    // BookmarkDetected
                    string bookmark = Marshal.PtrToStringUni(sapiEvent.Param2);
                    ttsEvent = new TTSEvent((TtsEventId)sapiEvent.EventId, _prompt, null, null, _audio.Duration, _audio.Position, bookmark, (uint)sapiEvent.Param1, sapiEvent.Param2);
                    break;
                default:
                    ttsEvent = new TTSEvent((TtsEventId)sapiEvent.EventId, _prompt, null, null, _audio.Duration, _audio.Position, null, (uint)sapiEvent.Param1, sapiEvent.Param2);
                    break;
            }
            return ttsEvent;
        }

        #endregion

        #region private Fields

        private int _eventInterest;

        private SPVESACTIONS _actions = SPVESACTIONS.SPVES_RATE | SPVESACTIONS.SPVES_VOLUME;

        private AudioBase _audio;

        private Prompt _prompt;

        // Last Exception
        private Exception _exception;

        // Rate setup in the control panel
        private int _defaultRate;

        // Rate setup in the control panel
        private int _volume = 100;

        // Get a resource load
        private ResourceLoader _resourceLoader;

        // Map the TTS events to the right format
        private TtsEventMapper _eventMapper;

        #endregion
    }

    internal interface ITtsEventSink
    {
        void AddEvent(TTSEvent evt);
        void FlushEvent();
    }

    internal abstract class TtsEventMapper : ITtsEventSink
    {
        internal TtsEventMapper(ITtsEventSink sink)
        {
            _sink = sink;
        }

        protected virtual void SendToOutput(TTSEvent evt)
        {
            _sink?.AddEvent(evt);
        }

        public virtual void AddEvent(TTSEvent evt)
        {
            SendToOutput(evt);
        }

        public virtual void FlushEvent()
        {
            _sink?.FlushEvent();
        }

        private ITtsEventSink _sink;
    }

    internal class PhonemeEventMapper : TtsEventMapper
    {
        public enum PhonemeConversion
        {
            IpaToSapi,
            SapiToIpa,
            NoConversion
        }

        internal PhonemeEventMapper(ITtsEventSink sink, PhonemeConversion conversion, AlphabetConverter alphabetConverter) : base(sink)
        {
            _queue = new Queue();
            _phonemeQueue = new Queue();
            _conversion = conversion;
            _alphabetConverter = alphabetConverter;
            Reset();
        }

        public override void AddEvent(TTSEvent evt)
        {
            if (_conversion == PhonemeConversion.NoConversion)
            {
                SendToOutput(evt);
            }
            else if (evt.Id == TtsEventId.Phoneme)
            {
                _phonemeQueue.Enqueue(evt);

                int prefixSeek = _phonemes.Length + 1;
                _phonemes.Append(evt.Phoneme);
                do
                {
                    string prefix = _phonemes.ToString(0, prefixSeek);
                    if (_alphabetConverter.IsPrefix(prefix, _conversion == PhonemeConversion.SapiToIpa))
                    {
                        if (_alphabetConverter.IsConvertibleUnit(prefix, _conversion == PhonemeConversion.SapiToIpa))
                        {
                            _lastComplete = prefixSeek;
                        }
                        prefixSeek++;
                    }
                    else
                    {
                        if (_lastComplete == 0)
                        {
                            Trace.TraceError("Cannot convert the phonemes correctly. Attempt to start over...");
                            Reset();
                            break;
                        }
                        ConvertCompleteUnit();
                        _lastComplete = 0;
                        prefixSeek = 1;
                    }
                } while (prefixSeek <= _phonemes.Length);
            }
            else
            {
                SendToQueue(evt);
            }
        }

        public override void FlushEvent()
        {
            ConvertCompleteUnit();
            while (_queue.Count > 0)
            {
                SendToOutput((TTSEvent)_queue.Dequeue());
            }
            _phonemeQueue.Clear();
            _lastComplete = 0;

            base.FlushEvent();
        }

        private void ConvertCompleteUnit()
        {
            if (_lastComplete == 0)
            {
                return;
            }
            if (_phonemeQueue.Count == 0)
            {
                Trace.TraceError("Failed to convert phonemes. Phoneme queue is empty.");
                return;
            }

            char[] source = new char[_lastComplete];
            _phonemes.CopyTo(0, source, 0, _lastComplete);
            _phonemes.Remove(0, _lastComplete);
            char[] target;
            if (_conversion == PhonemeConversion.IpaToSapi)
            {
                target = _alphabetConverter.IpaToSapi(source);
            }
            else
            {
                target = _alphabetConverter.SapiToIpa(source);
            }

            //
            // Convert the audio duration
            // Update the next phoneme id
            // Retain any other information based on the first TTS phoneme event.
            //
            TTSEvent ttsEvent, targetEvent, basePhonemeEvent = null;
            long totalDuration = 0;
            basePhonemeEvent = (TTSEvent)_phonemeQueue.Peek();
            for (int i = 0; i < _lastComplete;)
            {
                ttsEvent = (TTSEvent)_phonemeQueue.Dequeue();
                totalDuration += ttsEvent.PhonemeDuration.Milliseconds;
                i += ttsEvent.Phoneme.Length;
            }

            targetEvent = TTSEvent.CreatePhonemeEvent(new string(target), "",
                                                      TimeSpan.FromMilliseconds(totalDuration),
                                                      basePhonemeEvent.PhonemeEmphasis,
                                                      basePhonemeEvent.Prompt,
                                                      basePhonemeEvent.AudioPosition);
            SendToQueue(targetEvent);
        }

        private void Reset()
        {
            _phonemeQueue.Clear();
            _phonemes = new StringBuilder();
            _lastComplete = 0;
        }

        private void SendToQueue(TTSEvent evt)
        {
            if (evt.Id == TtsEventId.Phoneme)
            {
                TTSEvent firstEvent;
                if (_queue.Count > 0)
                {
                    firstEvent = _queue.Dequeue() as TTSEvent;
                    if (firstEvent.Id == TtsEventId.Phoneme)
                    {
                        firstEvent.NextPhoneme = evt.Phoneme;
                    }
                    else
                    {
                        Trace.TraceError("First event in the queue of the phone mapper is not a PHONEME event");
                    }
                    SendToOutput(firstEvent);
                    while (_queue.Count > 0)
                    {
                        SendToOutput(_queue.Dequeue() as TTSEvent);
                    }
                }
                _queue.Enqueue(evt);
            }
            else
            {
                if (_queue.Count > 0)
                {
                    _queue.Enqueue(evt);
                }
                else
                {
                    SendToOutput(evt);
                }
            }
        }

        private PhonemeConversion _conversion;
        private StringBuilder _phonemes;
        private Queue _queue, _phonemeQueue;
        private AlphabetConverter _alphabetConverter;
        private int _lastComplete;
    }
}
