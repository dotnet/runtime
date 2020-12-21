// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Internal;

using System.Diagnostics;

#pragma warning disable 56504 // The public API is not that public so remove all the parameter validation.

namespace System.Speech.Synthesis.TtsEngine
{
    #region Public Interface

    /// <summary>
    /// TODOC
    /// </summary>
    public
 abstract class TtsEngineSsml
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

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [ImmutableObject(true)]

    public
 struct SpeechEventInfo : IEquatable<SpeechEventInfo>
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public short EventId { get { return _eventId; } internal set { _eventId = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public short ParameterType { get { return _parameterType; } internal set { _parameterType = value; } }
        /// <summary>
        /// Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        /// </summary>
        public int Param1 { get { return _param1; } internal set { _param1 = value; } }   //
        /// <summary>
        /// Can be a numeric type, or pointer to string.
        /// </summary>
        public IntPtr Param2 { get { return _param2; } internal set { _param2 = value; } }

        /// TODOC
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

        /// TODOC
        public static bool operator ==(SpeechEventInfo event1, SpeechEventInfo event2)
        {
            return event1.EventId == event2.EventId && event1.ParameterType == event2.ParameterType && event1.Param1 == event2.Param1 && event1.Param2 == event2.Param2;
        }

        /// TODOC
        public static bool operator !=(SpeechEventInfo event1, SpeechEventInfo event2)
        {
            return !(event1 == event2);
        }

        /// TODOC
        public bool Equals(SpeechEventInfo other)
        {
            return this == other;
        }

        /// TODOC
        public override bool Equals(object obj)
        {
            if (!(obj is SpeechEventInfo))
            {
                return false;
            }

            return Equals((SpeechEventInfo)obj);
        }

        /// TODOC
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private short _eventId;
        private short _parameterType;
        private int _param1;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        private IntPtr _param2;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 interface ITtsEngineSite
    {
        /// <summary>
        /// TODOC
        /// </summary>
        void AddEvents(SpeechEventInfo[] events, int count);
        /// <summary>
        /// TODOC
        /// </summary>
        int Write(IntPtr data, int count);
        /// <summary>
        /// TODOC
        /// </summary>
        SkipInfo GetSkipInfo();
        /// <summary>
        /// TODOC
        /// </summary>
        void CompleteSkip(int skipped);
        /// <summary>
        /// TODOC
        /// </summary>
        Stream LoadResource(Uri uri, string mediaType);

        /// <summary>
        /// TODOC
        /// </summary>
        int EventInterest { get; }
        /// <summary>
        /// TODOC
        /// </summary>
        int Actions { get; }
        /// <summary>
        /// TODOC
        /// </summary>
        int Rate { get; }
        /// <summary>
        /// TODOC
        /// </summary>
        int Volume { get; }
    }

    /// <summary>
    /// TODOC
    /// </summary>

    public
 class SkipInfo
    {
        internal SkipInfo(int type, int count)
        {
            _type = type;
            _count = count;
        }

        /// <summary>
        /// TODOC
        /// </summary>
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
        /// <summary>
        /// TODOC
        /// </summary>
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

        /// <summary>
        /// TODOC
        /// </summary>
        public SkipInfo()
        {
        }

        private int _type;
        private int _count;
    }

    #endregion

    #region Public Types

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]

    [DebuggerDisplay("{State.Action} {TextToSpeak!=null?TextToSpeak:\"\"}")]
    public
 class TextFragment
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public TextFragment()
        {
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public FragmentState State { get { return _state; } set { _state = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public string TextToSpeak { get { return _textToSpeak; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _textToSpeak = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public int TextOffset { get { return _textOffset; } set { _textOffset = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
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

    /// <summary>
    /// TODOC
    /// </summary>
    [ImmutableObject(true)]

    public
 struct FragmentState : IEquatable<FragmentState>
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public TtsEngineAction Action { get { return _action; } internal set { _action = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public int LangId { get { return _langId; } internal set { _langId = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public int Emphasis { get { return _emphasis; } internal set { _emphasis = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public int Duration { get { return _duration; } internal set { _duration = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public SayAs SayAs { get { return _sayAs; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _sayAs = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public Prosody Prosody { get { return _prosody; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _prosody = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public char[] Phoneme { get { return _phoneme; } internal set { Helpers.ThrowIfNull(value, nameof(value)); _phoneme = value; } }

        /// TODOC
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

        /// TODOC
        public static bool operator ==(FragmentState state1, FragmentState state2)
        {
            return state1.Action == state2.Action && state1.LangId == state2.LangId && state1.Emphasis == state2.Emphasis && state1.Duration == state2.Duration && state1.SayAs == state2.SayAs && state1.Prosody == state2.Prosody && Array.Equals(state1.Phoneme, state2.Phoneme);
        }

        /// TODOC
        public static bool operator !=(FragmentState state1, FragmentState state2)
        {
            return !(state1 == state2);
        }

        /// TODOC
        public bool Equals(FragmentState other)
        {
            return this == other;
        }

        /// TODOC
        public override bool Equals(object obj)
        {
            if (!(obj is FragmentState))
            {
                return false;
            }

            return Equals((FragmentState)obj);
        }

        /// TODOC
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

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public
 class Prosody
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber Pitch { get { return _pitch; } set { _pitch = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber Range { get { return _range; } set { _range = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber Rate { get { return _rate; } set { _rate = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public int Duration { get { return _duration; } set { _duration = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber Volume { get { return _volume; } set { _volume = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public ContourPoint[] GetContourPoints() { return _contourPoints; }
        /// <summary>
        /// TODOC
        /// </summary>
        public void SetContourPoints(ContourPoint[] points)
        {
            Helpers.ThrowIfNull(points, nameof(points));

            _contourPoints = (ContourPoint[])points.Clone();
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public Prosody()
        {
            Pitch = new ProsodyNumber((int)ProsodyPitch.Default); ;
            Range = new ProsodyNumber((int)ProsodyRange.Default); ;
            Rate = new ProsodyNumber((int)ProsodyRate.Default);
            Volume = new ProsodyNumber((int)ProsodyVolume.Default); ;
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

    /// <summary>
    /// TODOC
    /// </summary>
    [ImmutableObject(true)]

    public
 struct ContourPoint : IEquatable<ContourPoint>
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public float Start { get { return _start; } /* internal set { _start = value; }  */}
        /// <summary>
        /// TODOC
        /// </summary>
        public float Change { get { return _change; } /* internal set { _change = value; } */ }
        /// <summary>
        /// TODOC
        /// </summary>
        public ContourPointChangeType ChangeType { get { return _changeType; } /* internal set { _changeType = value; } */ }
        /// <summary>
        /// TODOC
        /// </summary>
        public ContourPoint(float start, float change, ContourPointChangeType changeType)
        {
            _start = start;
            _change = change;
            _changeType = changeType;
        }

        /// TODOC
        public static bool operator ==(ContourPoint point1, ContourPoint point2)
        {
            return point1.Start.Equals(point2.Start) && point1.Change.Equals(point2.Change) && point1.ChangeType.Equals(point2.ChangeType);
        }

        /// TODOC
        public static bool operator !=(ContourPoint point1, ContourPoint point2)
        {
            return !(point1 == point2);
        }

        /// TODOC
        public bool Equals(ContourPoint other)
        {
            return this == other;
        }

        /// TODOC
        public override bool Equals(object obj)
        {
            if (!(obj is ContourPoint))
            {
                return false;
            }

            return Equals((ContourPoint)obj);
        }

        /// TODOC
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private float _start;
        private float _change;
        private ContourPointChangeType _changeType;
    }

    /// <summary>
    /// TODOC
    /// </summary>
    [ImmutableObject(true)]
    public
 struct ProsodyNumber : IEquatable<ProsodyNumber>
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public int SsmlAttributeId { get { return _ssmlAttributeId; } internal set { _ssmlAttributeId = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public bool IsNumberPercent { get { return _isPercent; } internal set { _isPercent = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public float Number { get { return _number; } internal set { _number = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyUnit Unit { get { return _unit; } internal set { _unit = value; } }

        /// <summary>
        /// TODOC
        /// </summary>
        public const int AbsoluteNumber = int.MaxValue;

        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber(int ssmlAttributeId)
        {
            _ssmlAttributeId = ssmlAttributeId;
            _number = 1.0f;
            _isPercent = true;
            _unit = ProsodyUnit.Default;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public ProsodyNumber(float number)
        {
            _ssmlAttributeId = int.MaxValue;
            _number = number;
            _isPercent = false;
            _unit = ProsodyUnit.Default;
        }

        /// TODOC
        public static bool operator ==(ProsodyNumber prosodyNumber1, ProsodyNumber prosodyNumber2)
        {
            return prosodyNumber1._ssmlAttributeId == prosodyNumber2._ssmlAttributeId && prosodyNumber1.Number.Equals(prosodyNumber2.Number) && prosodyNumber1.IsNumberPercent == prosodyNumber2.IsNumberPercent && prosodyNumber1.Unit == prosodyNumber2.Unit;
        }

        /// TODOC
        public static bool operator !=(ProsodyNumber prosodyNumber1, ProsodyNumber prosodyNumber2)
        {
            return !(prosodyNumber1 == prosodyNumber2);
        }

        /// TODOC
        public bool Equals(ProsodyNumber other)
        {
            return this == other;
        }

        /// TODOC
        public override bool Equals(object obj)
        {
            if (!(obj is ProsodyNumber))
            {
                return false;
            }

            return Equals((ProsodyNumber)obj);
        }

        /// TODOC
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private int _ssmlAttributeId;
        private bool _isPercent;
        private float _number;
        private ProsodyUnit _unit;
    }

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public
 class SayAs
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public string InterpretAs { get { return _interpretAs; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _interpretAs = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
        public string Format { get { return _format; } set { Helpers.ThrowIfEmptyOrNull(value, nameof(value)); _format = value; } }
        /// <summary>
        /// TODOC
        /// </summary>
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

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum TtsEngineAction
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Speak,
        /// <summary>
        /// TODOC
        /// </summary>
        Silence,
        /// <summary>
        /// TODOC
        /// </summary>
        Pronounce,
        /// <summary>
        /// TODOC
        /// </summary>
        Bookmark,
        /// <summary>
        /// TODOC
        /// </summary>
        SpellOut,
        /// <summary>
        /// TODOC
        /// </summary>
        StartSentence,
        /// <summary>
        /// TODOC
        /// </summary>
        StartParagraph,
        /// <summary>
        /// TODOC
        /// </summary>
        ParseUnknownTag,
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum EmphasisWord : int
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default,
        /// <summary>
        /// TODOC
        /// </summary>
        Strong,
        /// <summary>
        /// TODOC
        /// </summary>
        Moderate,
        /// <summary>
        /// TODOC
        /// </summary>
        None,
        /// <summary>
        /// TODOC
        /// </summary>
        Reduced
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum EmphasisBreak : int
    {
        /// <summary>
        /// TODOC
        /// </summary>
        None = -1,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraWeak = -2,
        /// <summary>
        /// TODOC
        /// </summary>
        Weak = -3,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium = -4,
        /// <summary>
        /// TODOC
        /// </summary>
        Strong = -5,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraStrong = -6,
        /// <summary>
        /// Equivalent to the empty <Break />
        /// </summary>
        Default = -7,
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ProsodyPitch
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraLow,
        /// <summary>
        /// TODOC
        /// </summary>
        Low,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        High,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraHigh
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ProsodyRange
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraLow,
        /// <summary>
        /// TODOC
        /// </summary>
        Low,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        High,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraHigh
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ProsodyRate
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraSlow,
        /// <summary>
        /// TODOC
        /// </summary>
        Slow,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        Fast,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraFast
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ProsodyVolume : int
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default = -1,
        /// <summary>
        /// TODOC
        /// </summary>
        Silent = -2,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraSoft = -3,
        /// <summary>
        /// TODOC
        /// </summary>
        Soft = -4,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium = -5,
        /// <summary>
        /// TODOC
        /// </summary>
        Loud = -6,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraLoud = -7
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ProsodyUnit : int
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Default,
        /// <summary>
        /// TODOC
        /// </summary>
        Hz,
        /// <summary>
        /// TODOC
        /// </summary>
        Semitone
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum TtsEventId
    {
        /// <summary>
        /// TODOC
        /// </summary>
        StartInputStream = 1,
        /// <summary>
        /// TODOC
        /// </summary>
        EndInputStream = 2,
        /// <summary>
        /// TODOC
        /// </summary>
        VoiceChange = 3,   // lparam_is_token
        /// <summary>
        /// TODOC
        /// </summary>
        Bookmark = 4,   // lparam_is_string
        /// <summary>
        /// TODOC
        /// </summary>
        WordBoundary = 5,
        /// <summary>
        /// TODOC
        /// </summary>
        Phoneme = 6,
        /// <summary>
        /// TODOC
        /// </summary>
        SentenceBoundary = 7,
        /// <summary>
        /// TODOC
        /// </summary>
        Viseme = 8,
        /// <summary>
        /// TODOC
        /// </summary>
        AudioLevel = 9,   // wparam contains current output audio level
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum EventParameterType
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Undefined = 0x0000,
        /// <summary>
        /// TODOC
        /// </summary>
        Token = 0x0001,
        /// <summary>
        /// TODOC
        /// </summary>
        Object = 0x0002,
        /// <summary>
        /// TODOC
        /// </summary>
        Pointer = 0x0003,
        /// <summary>
        /// TODOC
        /// </summary>
        String = 0x0004
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum SpeakOutputFormat
    {
        /// <summary>
        /// TODOC
        /// </summary>
        WaveFormat = 0,
        /// <summary>
        /// TODOC
        /// </summary>
        Text = 1
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public
 enum ContourPointChangeType
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Hz = 0,
        /// <summary>
        /// TODOC
        /// </summary>
        Percentage = 1
    }

    #endregion

    #region Internal Interface

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TextFragmentInterop
    {
        internal FragmentStateInterop _state;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _textToSpeak;
        internal int _textOffset;
        internal int _textLength;
        internal static IntPtr FragmentToPtr(List<TextFragment> textFragments, Collection<IntPtr> memoryBlocks)
        {
            TextFragmentInterop fragInterop = new();
            int len = textFragments.Count;
            int sizeOfFrag = Marshal.SizeOf(fragInterop);
            IntPtr ret = Marshal.AllocCoTaskMem(sizeOfFrag * len);
            memoryBlocks.Add(ret);
            for (int i = 0; i < len; i++)
            {
                fragInterop._state.FragmentStateToPtr(textFragments[i].State, memoryBlocks);
                fragInterop._textToSpeak = textFragments[i].TextToSpeak;
                fragInterop._textOffset = textFragments[i].TextOffset;
                fragInterop._textLength = textFragments[i].TextLength;
                Marshal.StructureToPtr(fragInterop, (IntPtr)((ulong)ret + (ulong)(i * sizeOfFrag)), false);
            }

            return ret;
        }
    }

    /// <summary>
    /// TODOC
    /// </summary>
    internal struct FragmentStateInterop
    {
        internal TtsEngineAction _action;
        internal int _langId;
        internal int _emphasis;
        internal int _duration;
        internal IntPtr _sayAs;
        internal IntPtr _prosody;
        internal IntPtr _phoneme;
        internal void FragmentStateToPtr(FragmentState state, Collection<IntPtr> memoryBlocks)
        {
            _action = state.Action;
            _langId = state.LangId;
            _emphasis = state.Emphasis;
            _duration = state.Duration;

            if (state.SayAs != null)
            {
                _sayAs = Marshal.AllocCoTaskMem(Marshal.SizeOf(state.SayAs));
                memoryBlocks.Add(_sayAs);
                Marshal.StructureToPtr(state.SayAs, _sayAs, false);
            }
            else
            {
                _sayAs = IntPtr.Zero;
            }
            if (state.Phoneme != null)
            {
                short[] phonemes = new short[state.Phoneme.Length + 1];
                for (uint i = 0; i < state.Phoneme.Length; i++)
                {
                    phonemes[i] = unchecked((short)state.Phoneme[i]);
                }
                phonemes[state.Phoneme.Length] = 0;
                int sizeOfShort = Marshal.SizeOf(phonemes[0]);
                _phoneme = Marshal.AllocCoTaskMem(sizeOfShort * phonemes.Length);
                memoryBlocks.Add(_phoneme);
                for (uint i = 0; i < phonemes.Length; i++)
                {
                    Marshal.Copy(phonemes, 0, _phoneme, phonemes.Length);
                }
            }
            else
            {
                _phoneme = IntPtr.Zero;
            }

            _prosody = ProsodyInterop.ProsodyToPtr(state.Prosody, memoryBlocks);
        }
    }

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProsodyInterop
    {
        internal ProsodyNumber _pitch;
        internal ProsodyNumber _range;
        internal ProsodyNumber _rate; // can be casted to a Prosody Rate
        internal int _duration;
        internal ProsodyNumber _volume;
        internal IntPtr _contourPoints;
        internal static IntPtr ProsodyToPtr(Prosody prosody, Collection<IntPtr> memoryBlocks)
        {
            if (prosody == null)
            {
                return IntPtr.Zero;
            }

            ProsodyInterop prosodyInterop = new();
            prosodyInterop._pitch = prosody.Pitch;
            prosodyInterop._range = prosody.Range;
            prosodyInterop._rate = prosody.Rate;
            prosodyInterop._duration = prosody.Duration;
            prosodyInterop._volume = prosody.Volume;

            ContourPoint[] points = prosody.GetContourPoints();

            if (points != null)
            {
                int sizeOfPoint = Marshal.SizeOf(points[0]);
                prosodyInterop._contourPoints = Marshal.AllocCoTaskMem(points.Length * sizeOfPoint);
                memoryBlocks.Add(prosodyInterop._contourPoints);
                for (uint i = 0; i < points.Length; i++)
                {
                    Marshal.StructureToPtr(points[i], (IntPtr)((ulong)prosodyInterop._contourPoints + (ulong)sizeOfPoint * i), false);
                }
            }
            else
            {
                prosodyInterop._contourPoints = IntPtr.Zero;
            }
            IntPtr ret = Marshal.AllocCoTaskMem(Marshal.SizeOf(prosodyInterop));
            memoryBlocks.Add(ret);
            Marshal.StructureToPtr(prosodyInterop, ret, false);
            return ret;
        }
    }

    #endregion
}
