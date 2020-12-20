// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    [Serializable]
    public class PromptStyle
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        public PromptStyle ()
        {
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="rate"></param>
        public PromptStyle (PromptRate rate)
        {
            Rate = rate;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="volume"></param>
        public PromptStyle (PromptVolume volume)
        {
            Volume = volume;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="emphasis"></param>
        public PromptStyle (PromptEmphasis emphasis)
        {
            Emphasis = emphasis;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        // <prosody pitch, contour, range, rate, duration, volume>

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
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

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
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

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
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

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private PromptRate _rate = PromptRate.NotSet;
        private PromptVolume _volume = PromptVolume.NotSet;
        private PromptEmphasis _emphasis = PromptEmphasis.NotSet;

        #endregion
    }

    //*******************************************************************
    //
    // Public Enums
    //
    //*******************************************************************

    #region Public Enums

    /// <summary>
    /// TODOC
    /// </summary>
    public enum SayAs
    {
        /// <summary>
        /// TODOC
        /// </summary>
        SpellOut,
        /// <summary>
        /// TODOC
        /// </summary>
        NumberOrdinal,
        /// <summary>
        /// TODOC
        /// </summary>
        NumberCardinal,
        /// <summary>
        /// TODOC
        /// </summary>
        Date,
        /// <summary>
        /// TODOC
        /// </summary>
        DayMonthYear, 
        /// <summary>
        /// TODOC
        /// </summary>
        MonthDayYear,
        /// <summary>
        /// TODOC
        /// </summary>
        YearMonthDay,
        /// <summary>
        /// TODOC
        /// </summary>
        YearMonth,
        /// <summary>
        /// TODOC
        /// </summary>
        MonthYear,
        /// <summary>
        /// TODOC
        /// </summary>
        MonthDay,
        /// <summary>
        /// TODOC
        /// </summary>
        DayMonth,
        /// <summary>
        /// TODOC
        /// </summary>
        Year,
        /// <summary>
        /// TODOC
        /// </summary>
        Month,
        /// <summary>
        /// TODOC
        /// </summary>
        Day,
        /// <summary>
        /// TODOC
        /// </summary>
        Time,
        /// <summary>
        /// TODOC
        /// </summary>
        Time24,
        /// <summary>
        /// TODOC
        /// </summary>
        Time12,
        /// <summary>
        /// TODOC
        /// </summary>
        Telephone,

        /// <summary>
        /// TODOC
        /// </summary>
        Text
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum VoiceGender
    {
        /// <summary>
        /// TODOC
        /// </summary>
        NotSet,
        /// <summary>
        /// TODOC
        /// </summary>
        /// <summary>
        /// TODOC
        /// </summary>
        Male,
        /// <summary>
        /// TODOC
        /// </summary>
        Female,
        /// <summary>
        /// TODOC
        /// </summary>
        Neutral
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum VoiceAge
    {
        /// <summary>
        /// TODOC
        /// </summary>
        NotSet,
        /// <summary>
        /// TODOC
        /// </summary>
        Child=10,
        /// <summary>
        /// TODOC
        /// </summary>
        Teen=15,
        /// <summary>
        /// TODOC
        /// </summary>
        Adult=30,
        /// <summary>
        /// TODOC
        /// </summary>
        Senior=65
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum PromptRate
    {
        /// <summary>
        /// TODOC
        /// </summary>
        NotSet,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraFast,
        /// <summary>
        /// TODOC
        /// </summary>
        Fast,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        Slow,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraSlow
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum PromptVolume
    {
        /// <summary>
        /// TODOC
        /// </summary>
        NotSet,
        /// <summary>
        /// TODOC
        /// </summary>
        Silent,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraSoft,
        /// <summary>
        /// TODOC
        /// </summary>
        Soft,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        Loud,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraLoud,
        /// <summary>
        /// TODOC
        /// </summary>
        Default
    }

    /// <summary>
    /// TODOC
    /// </summary>
    public enum PromptEmphasis
    {
        /// <summary>
        /// TODOC
        /// </summary>
        NotSet,
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
    public enum PromptBreak
    {
        /// <summary>
        /// TODOC
        /// </summary>
        None,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraSmall,
        /// <summary>
        /// TODOC
        /// </summary>
        Small,
        /// <summary>
        /// TODOC
        /// </summary>
        Medium,
        /// <summary>
        /// TODOC
        /// </summary>
        Large,
        /// <summary>
        /// TODOC
        /// </summary>
        ExtraLarge
    }

    #endregion
}
