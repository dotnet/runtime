// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    [Serializable]
    public class PromptStyle
    {
        #region Constructors
        public PromptStyle()
        {
        }
        public PromptStyle(PromptRate rate)
        {
            Rate = rate;
        }
        public PromptStyle(PromptVolume volume)
        {
            Volume = volume;
        }
        public PromptStyle(PromptEmphasis emphasis)
        {
            Emphasis = emphasis;
        }

        #endregion

        #region public Properties

        // <prosody pitch, contour, range, rate, duration, volume>
        public PromptRate Rate
        {
            get
            {
                return _rate;
            }
            set
            {
                _rate = value;
            }
        }
        public PromptVolume Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
            }
        }
        public PromptEmphasis Emphasis
        {
            get
            {
                return _emphasis;
            }
            set
            {
                _emphasis = value;
            }
        }

        #endregion

        #region Private Fields

        private PromptRate _rate;
        private PromptVolume _volume;
        private PromptEmphasis _emphasis;

        #endregion
    }

    #region Public Enums
    public enum SayAs
    {
        SpellOut,
        NumberOrdinal,
        NumberCardinal,
        Date,
        DayMonthYear,
        MonthDayYear,
        YearMonthDay,
        YearMonth,
        MonthYear,
        MonthDay,
        DayMonth,
        Year,
        Month,
        Day,
        Time,
        Time24,
        Time12,
        Telephone,
        Text
    }
    public enum VoiceGender
    {
        NotSet,
        Male,
        Female,
        Neutral
    }
    public enum VoiceAge
    {
        NotSet,
        Child = 10,
        Teen = 15,
        Adult = 30,
        Senior = 65
    }
    public enum PromptRate
    {
        NotSet,
        ExtraFast,
        Fast,
        Medium,
        Slow,
        ExtraSlow
    }
    public enum PromptVolume
    {
        NotSet,
        Silent,
        ExtraSoft,
        Soft,
        Medium,
        Loud,
        ExtraLoud,
        Default
    }
    public enum PromptEmphasis
    {
        NotSet,
        Strong,
        Moderate,
        None,
        Reduced
    }
    public enum PromptBreak
    {
        None,
        ExtraSmall,
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    #endregion
}
