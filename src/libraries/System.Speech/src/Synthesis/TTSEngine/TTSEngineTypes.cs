// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Internal;

namespace System.Speech.Synthesis.TtsEngine
{
    #region Public Interface
    public abstract class TtsEngineSsml
    {
        /// <summary>
        /// Constructor for the TTS engine
        /// </summary>
        /// <param name="registryKey">Voice token registry entry
        /// from where this engine was created from</param>
        protected TtsEngineSsml(string registryKey) { }

        /// <summary>
        /// Queries the engine about the output format it supports.
        /// </summary>
        /// <param name="speakOutputFormat">Wave or Text</param>
        /// <param name="targetWaveFormat">Wave format header</param>
        /// <returns>Returns the closest format that it supports</returns>
        public abstract IntPtr GetOutputFormat(SpeakOutputFormat speakOutputFormat, IntPtr targetWaveFormat);

        /// <summary>
        /// Add a lexicon for this engine
        /// </summary>
        /// <param name="uri">uri</param>
        /// <param name="mediaType">media type</param>
        /// <param name="site">Engine site</param>
        public abstract void AddLexicon(Uri uri, string mediaType, ITtsEngineSite site);

        /// <summary>
        /// Removes a lexicon for this engine
        /// </summary>
        /// <param name="uri">uri</param>
        /// <param name="site">Engine site</param>
        public abstract void RemoveLexicon(Uri uri, ITtsEngineSite site);

        /// <summary>
        /// Renders the specified text fragments array in the
        /// specified output format.
        /// </summary>
        /// <param name="fragment">Text fragment with SSML
        /// attributes information</param>
        /// <param name="waveHeader">Wave format header</param>
        /// <param name="site">Engine site</param>
        public abstract void Speak(TextFragment[] fragment, IntPtr waveHeader, ITtsEngineSite site);
    }

    [StructLayout(LayoutKind.Sequential)]
    [ImmutableObject(true)]
    public struct SpeechEventInfo : IEquatable<SpeechEventInfo>
    {
        public short EventId { get { return _eventId; } internal set { _eventId = value; } }
        public short ParameterType { get { return _parameterType; } internal set { _parameterType = value; } }

        /// <summary>
        /// Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        /// </summary>
        public int Param1 { get { return _param1; } internal set { _param1 = value; } }

        /// <summary>
        /// Can be a numeric type, or pointer to string.
        /// </summary>
        public IntPtr Param2 { get { return _param2; } internal set { _param2 = value; } }

        public SpeechEventInfo(short eventId,
                               short parameterType,
                               int param1,
                               IntPtr param2)
        {
            _eventId = eventId;
            _parameterType = parameterType;
            _param1 = param1;
            _param2 = param2;
        }
        public static bool operator ==(SpeechEventInfo event1, SpeechEventInfo event2)
        {
            return event1.EventId == event2.EventId && event1.ParameterType == event2.ParameterType && event1.Param1 == event2.Param1 && event1.Param2 == event2.Param2;
        }
        public static bool operator !=(SpeechEventInfo event1, SpeechEventInfo event2)
        {
            return !(event1 == event2);
        }
        public bool Equals(SpeechEventInfo other)
        {
            return this == other;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is SpeechEventInfo))
            {
                return false;
            }

            return Equals((SpeechEventInfo)obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private short _eventId;
        private short _parameterType;
        private int _param1;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        private IntPtr _param2;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.
    }
    public interface ITtsEngineSite
    {
        void AddEvents(SpeechEventInfo[] events, int count);
        int Write(IntPtr data, int count);
        SkipInfo GetSkipInfo();
        void CompleteSkip(int skipped);
        Stream LoadResource(Uri uri, string mediaType);
        int EventInterest { get; }
        int Actions { get; }
        int Rate { get; }
        int Volume { get; }
    }
    public class SkipInfo
    {
        internal SkipInfo(int type, int count)
        {
            _type = type;
            _count = count;
        }
        public int Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
            }
        }
        public SkipInfo()
        {
        }

        private int _type;
        private int _count;
    }

    #endregion

    #region Public Types
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{State.Action} {TextToSpeak!=null?TextToSpeak:\"\"}")]
    public class TextFragment
    {
        public TextFragment()
        {
        }
        public FragmentState State { get { return _state; } set { _state = value; } }
        public string TextToSpeak { get { return _textToSpeak; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _textToSpeak = value; } }
        public int TextOffset { get { return _textOffset; } set { _textOffset = value; } }
        public int TextLength { get { return _textLength; } set { _textLength = value; } }

        internal TextFragment(FragmentState fragState)
            : this(fragState, null, null, 0, 0)
        {
        }

        internal TextFragment(FragmentState fragState, string textToSpeak)
            : this(fragState, textToSpeak, textToSpeak, 0, textToSpeak.Length)
        {
        }

        internal TextFragment(FragmentState fragState, string textToSpeak, string textFrag, int offset, int length)
        {
            if (fragState.Action == TtsEngineAction.Speak || fragState.Action == TtsEngineAction.Pronounce)
            {
                textFrag = textToSpeak;
            }
            if (!string.IsNullOrEmpty(textFrag))
            {
                TextToSpeak = textFrag;
            }
            State = fragState;
            TextOffset = offset;
            TextLength = length;
        }

        private FragmentState _state;
        [MarshalAs(UnmanagedType.LPWStr)]
        private string _textToSpeak = string.Empty;
        private int _textOffset;
        private int _textLength;
    }
    [ImmutableObject(true)]

    public struct FragmentState : IEquatable<FragmentState>
    {
        public TtsEngineAction Action { get { return _action; } internal set { _action = value; } }
        public int LangId { get { return _langId; } internal set { _langId = value; } }
        public int Emphasis { get { return _emphasis; } internal set { _emphasis = value; } }
        public int Duration { get { return _duration; } internal set { _duration = value; } }
        public SayAs SayAs { get { return _sayAs; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _sayAs = value; } }
        public Prosody Prosody { get { return _prosody; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _prosody = value; } }
        public char[] Phoneme { get { return _phoneme; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _phoneme = value; } }
        public FragmentState(TtsEngineAction action,
                             int langId,
                             int emphasis,
                             int duration,
                             SayAs sayAs,
                             Prosody prosody,
                             char[] phonemes)
        {
            _action = action;
            _langId = langId;
            _emphasis = emphasis;
            _duration = duration;
            _sayAs = sayAs;
            _prosody = prosody;
            _phoneme = phonemes;
        }
        public static bool operator ==(FragmentState state1, FragmentState state2)
        {
            return state1.Action == state2.Action && state1.LangId == state2.LangId && state1.Emphasis == state2.Emphasis && state1.Duration == state2.Duration && state1.SayAs == state2.SayAs && state1.Prosody == state2.Prosody && Array.Equals(state1.Phoneme, state2.Phoneme);
        }
        public static bool operator !=(FragmentState state1, FragmentState state2)
        {
            return !(state1 == state2);
        }
        public bool Equals(FragmentState other)
        {
            return this == other;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is FragmentState))
            {
                return false;
            }

            return Equals((FragmentState)obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private TtsEngineAction _action;
        private int _langId;
        private int _emphasis;
        private int _duration;
        private SayAs _sayAs;
        private Prosody _prosody;
        private char[] _phoneme;
    }
    [StructLayout(LayoutKind.Sequential)]
    public class Prosody
    {
        public ProsodyNumber Pitch { get { return _pitch; } set { _pitch = value; } }
        public ProsodyNumber Range { get { return _range; } set { _range = value; } }
        public ProsodyNumber Rate { get { return _rate; } set { _rate = value; } }
        public int Duration { get { return _duration; } set { _duration = value; } }
        public ProsodyNumber Volume { get { return _volume; } set { _volume = value; } }
        public ContourPoint[] GetContourPoints() { return _contourPoints; }
        public void SetContourPoints(ContourPoint[] points)
        {
            Helpers.ThrowIfNull(points, nameof(points));

            _contourPoints = (ContourPoint[])points.Clone();
        }
        public Prosody()
        {
            Pitch = new ProsodyNumber((int)ProsodyPitch.Default);
            Range = new ProsodyNumber((int)ProsodyRange.Default);
            Rate = new ProsodyNumber((int)ProsodyRate.Default);
            Volume = new ProsodyNumber((int)ProsodyVolume.Default);
        }

        internal Prosody Clone()
        {
            Prosody cloned = new();
            cloned._pitch = _pitch;
            cloned._range = _range;
            cloned._rate = _rate;
            cloned._duration = _duration;
            cloned._volume = _volume;
            return cloned;
        }

        internal ProsodyNumber _pitch;
        internal ProsodyNumber _range;
        internal ProsodyNumber _rate; // can be casted to a Prosody Rate
        internal int _duration;
        internal ProsodyNumber _volume;
        internal ContourPoint[] _contourPoints;
    }
    [ImmutableObject(true)]

    public struct ContourPoint : IEquatable<ContourPoint>
    {
        public float Start { get { return _start; } /* internal set { _start = value; }  */}
        public float Change { get { return _change; } /* internal set { _change = value; } */ }
        public ContourPointChangeType ChangeType { get { return _changeType; } /* internal set { _changeType = value; } */ }
        public ContourPoint(float start, float change, ContourPointChangeType changeType)
        {
            _start = start;
            _change = change;
            _changeType = changeType;
        }
        public static bool operator ==(ContourPoint point1, ContourPoint point2)
        {
            return point1.Start.Equals(point2.Start) && point1.Change.Equals(point2.Change) && point1.ChangeType.Equals(point2.ChangeType);
        }
        public static bool operator !=(ContourPoint point1, ContourPoint point2)
        {
            return !(point1 == point2);
        }
        public bool Equals(ContourPoint other)
        {
            return this == other;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is ContourPoint))
            {
                return false;
            }

            return Equals((ContourPoint)obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private float _start;
        private float _change;
        private ContourPointChangeType _changeType;
    }
    [ImmutableObject(true)]
    public struct ProsodyNumber : IEquatable<ProsodyNumber>
    {
        public int SsmlAttributeId { get { return _ssmlAttributeId; } internal set { _ssmlAttributeId = value; } }
        public bool IsNumberPercent { get { return _isPercent; } internal set { _isPercent = value; } }
        public float Number { get { return _number; } internal set { _number = value; } }
        public ProsodyUnit Unit { get { return _unit; } internal set { _unit = value; } }
        public const int AbsoluteNumber = int.MaxValue;
        public ProsodyNumber(int ssmlAttributeId)
        {
            _ssmlAttributeId = ssmlAttributeId;
            _number = 1.0f;
            _isPercent = true;
            _unit = ProsodyUnit.Default;
        }
        public ProsodyNumber(float number)
        {
            _ssmlAttributeId = int.MaxValue;
            _number = number;
            _isPercent = false;
            _unit = ProsodyUnit.Default;
        }
        public static bool operator ==(ProsodyNumber prosodyNumber1, ProsodyNumber prosodyNumber2)
        {
            return prosodyNumber1._ssmlAttributeId == prosodyNumber2._ssmlAttributeId && prosodyNumber1.Number.Equals(prosodyNumber2.Number) && prosodyNumber1.IsNumberPercent == prosodyNumber2.IsNumberPercent && prosodyNumber1.Unit == prosodyNumber2.Unit;
        }
        public static bool operator !=(ProsodyNumber prosodyNumber1, ProsodyNumber prosodyNumber2)
        {
            return !(prosodyNumber1 == prosodyNumber2);
        }
        public bool Equals(ProsodyNumber other)
        {
            return this == other;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is ProsodyNumber))
            {
                return false;
            }

            return Equals((ProsodyNumber)obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private int _ssmlAttributeId;
        private bool _isPercent;
        private float _number;
        private ProsodyUnit _unit;
    }
    [StructLayout(LayoutKind.Sequential)]
    public class SayAs
    {
        public string InterpretAs { get { return _interpretAs; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _interpretAs = value; } }
        public string Format { get { return _format; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _format = value; } }
        public string Detail { get { return _detail; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _detail = value; } }

        [MarshalAs(UnmanagedType.LPWStr)]
        private string _interpretAs;

        [MarshalAs(UnmanagedType.LPWStr)]
        private string _format;

        [MarshalAs(UnmanagedType.LPWStr)]
        private string _detail;
    }

    #endregion

    #region Public Enums
    public enum TtsEngineAction
    {
        Speak,
        Silence,
        Pronounce,
        Bookmark,
        SpellOut,
        StartSentence,
        StartParagraph,
        ParseUnknownTag,
    }
    public enum EmphasisWord : int
    {
        Default,
        Strong,
        Moderate,
        None,
        Reduced
    }
    public enum EmphasisBreak : int
    {
        None = -1,
        ExtraWeak = -2,
        Weak = -3,
        Medium = -4,
        Strong = -5,
        ExtraStrong = -6,
        /// <summary>
        /// Equivalent to the empty <Break />
        /// </summary>
        Default = -7,
    }
    public enum ProsodyPitch
    {
        Default,
        ExtraLow,
        Low,
        Medium,
        High,
        ExtraHigh
    }
    public enum ProsodyRange
    {
        Default,
        ExtraLow,
        Low,
        Medium,
        High,
        ExtraHigh
    }
    public enum ProsodyRate
    {
        Default,
        ExtraSlow,
        Slow,
        Medium,
        Fast,
        ExtraFast
    }
    public enum ProsodyVolume : int
    {
        Default = -1,
        Silent = -2,
        ExtraSoft = -3,
        Soft = -4,
        Medium = -5,
        Loud = -6,
        ExtraLoud = -7
    }
    public enum ProsodyUnit : int
    {
        Default,
        Hz,
        Semitone
    }
    public enum TtsEventId
    {
        StartInputStream = 1,
        EndInputStream = 2,
        VoiceChange = 3,   // lparam_is_token
        Bookmark = 4,   // lparam_is_string
        WordBoundary = 5,
        Phoneme = 6,
        SentenceBoundary = 7,
        Viseme = 8,
        AudioLevel = 9,   // wparam contains current output audio level
    }
    public enum EventParameterType
    {
        Undefined = 0x0000,
        Token = 0x0001,
        Object = 0x0002,
        Pointer = 0x0003,
        String = 0x0004
    }
    public enum SpeakOutputFormat
    {
        WaveFormat = 0,
        Text = 1
    }
    public enum ContourPointChangeType
    {
        Hz = 0,
        Percentage = 1
    }

    #endregion
}
