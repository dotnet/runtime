// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates text layout information (such as alignment and linespacing), display manipulations (such as
    /// ellipsis insertion and national digit substitution) and OpenType features.
    /// </summary>
    public sealed class StringFormat : MarshalByRefObject, ICloneable, IDisposable
    {
        internal IntPtr nativeFormat;

        private StringFormat(IntPtr format)
        {
            nativeFormat = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='StringFormat'/> class.
        /// </summary>
        public StringFormat() : this(0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='StringFormat'/> class with the specified <see cref='System.Drawing.StringFormatFlags'/>.
        /// </summary>
        public StringFormat(StringFormatFlags options) :
        this(options, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='StringFormat'/> class with the specified
        /// <see cref='System.Drawing.StringFormatFlags'/> and language.
        /// </summary>
        public StringFormat(StringFormatFlags options, int language)
        {
            int status = Gdip.GdipCreateStringFormat(options, language, out nativeFormat);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='StringFormat'/> class from the specified
        /// existing <see cref='System.Drawing.StringFormat'/>.
        /// </summary>
        public StringFormat(StringFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);

            int status = Gdip.GdipCloneStringFormat(new HandleRef(format, format.nativeFormat), out nativeFormat);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='StringFormat'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (nativeFormat != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDeleteStringFormat(new HandleRef(this, nativeFormat));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, $"GDI+ returned an error status: {status.ToString(CultureInfo.InvariantCulture)}");
#endif
                }
                catch (Exception ex)
                {
                    if (ClientUtils.IsCriticalException(ex))
                    {
                        throw;
                    }

                    Debug.Fail("Exception thrown during Dispose: " + ex.ToString());
                }
                finally
                {
                    nativeFormat = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Creates an exact copy of this <see cref='StringFormat'/>.
        /// </summary>
        public object Clone()
        {
            IntPtr cloneFormat;

            int status = Gdip.GdipCloneStringFormat(new HandleRef(this, nativeFormat), out cloneFormat);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            StringFormat newCloneStringFormat = new StringFormat(cloneFormat);

            return newCloneStringFormat;
        }


        /// <summary>
        /// Gets or sets a <see cref='StringFormatFlags'/> that contains formatting information.
        /// </summary>
        public StringFormatFlags FormatFlags
        {
            get
            {
                StringFormatFlags format;

                int status = Gdip.GdipGetStringFormatFlags(new HandleRef(this, nativeFormat), out format);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return format;
            }
            set
            {
                int status = Gdip.GdipSetStringFormatFlags(new HandleRef(this, nativeFormat), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Sets the measure of characters to the specified range.
        /// </summary>
        public void SetMeasurableCharacterRanges(CharacterRange[] ranges)
        {
            int status = Gdip.GdipSetStringFormatMeasurableCharacterRanges(new HandleRef(this, nativeFormat), ranges.Length, ranges);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        // For English, this is horizontal alignment
        /// <summary>
        /// Specifies text alignment information.
        /// </summary>
        public StringAlignment Alignment
        {
            get
            {
                StringAlignment alignment;
                int status = Gdip.GdipGetStringFormatAlign(new HandleRef(this, nativeFormat), out alignment);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return alignment;
            }
            set
            {
                if (value < StringAlignment.Near || value > StringAlignment.Far)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(StringAlignment));
                }

                int status = Gdip.GdipSetStringFormatAlign(new HandleRef(this, nativeFormat), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        // For English, this is vertical alignment
        /// <summary>
        /// Gets or sets the line alignment.
        /// </summary>
        public StringAlignment LineAlignment
        {
            get
            {
                StringAlignment alignment;
                int status = Gdip.GdipGetStringFormatLineAlign(new HandleRef(this, nativeFormat), out alignment);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return alignment;
            }
            set
            {
                if (value < 0 || value > StringAlignment.Far)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(StringAlignment));
                }

                int status = Gdip.GdipSetStringFormatLineAlign(new HandleRef(this, nativeFormat), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref='HotkeyPrefix'/> for this <see cref='StringFormat'/> .
        /// </summary>
        public HotkeyPrefix HotkeyPrefix
        {
            get
            {
                HotkeyPrefix hotkeyPrefix;
                int status = Gdip.GdipGetStringFormatHotkeyPrefix(new HandleRef(this, nativeFormat), out hotkeyPrefix);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return hotkeyPrefix;
            }
            set
            {
                if (value < HotkeyPrefix.None || value > HotkeyPrefix.Hide)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(HotkeyPrefix));
                }

                int status = Gdip.GdipSetStringFormatHotkeyPrefix(new HandleRef(this, nativeFormat), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Sets tab stops for this <see cref='StringFormat'/>.
        /// </summary>
        public void SetTabStops(float firstTabOffset, float[] tabStops)
        {
            if (firstTabOffset < 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidArgumentValue, nameof(firstTabOffset), firstTabOffset));
            }

            foreach (float tabStop in tabStops) // Emulate Windows GDI+ behavior.
            {
                if (float.IsNegativeInfinity(tabStop))
                {
                    throw new NotImplementedException();
                }
            }

            int status = Gdip.GdipSetStringFormatTabStops(new HandleRef(this, nativeFormat), firstTabOffset, tabStops.Length, tabStops);

            if (status != Gdip.Ok)
            {
                throw Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Gets the tab stops for this <see cref='StringFormat'/>.
        /// </summary>
        public float[] GetTabStops(out float firstTabOffset)
        {
            int count;
            int status = Gdip.GdipGetStringFormatTabStopCount(new HandleRef(this, nativeFormat), out count);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            float[] tabStops = new float[count];
            status = Gdip.GdipGetStringFormatTabStops(new HandleRef(this, nativeFormat), count, out firstTabOffset, tabStops);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return tabStops;
        }


        // String trimming. How to handle more text than can be displayed
        // in the limits available.

        /// <summary>
        /// Gets or sets the <see cref='StringTrimming'/> for this <see cref='StringFormat'/>.
        /// </summary>
        public StringTrimming Trimming
        {
            get
            {
                StringTrimming trimming;
                int status = Gdip.GdipGetStringFormatTrimming(new HandleRef(this, nativeFormat), out trimming);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
                return trimming;
            }

            set
            {
                if (value < StringTrimming.None || value > StringTrimming.EllipsisPath)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(StringTrimming));
                }

                int status = Gdip.GdipSetStringFormatTrimming(new HandleRef(this, nativeFormat), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Gets a generic default <see cref='StringFormat'/>.
        /// Remarks from MSDN: A generic, default StringFormat object has the following characteristics:
        /// - No string format flags are set.
        /// - Character alignment and line alignment are set to StringAlignmentNear.
        /// - Language ID is set to neutral language, which means that the current language associated with the calling thread is used.
        /// - String digit substitution is set to StringDigitSubstituteUser.
        /// - Hot key prefix is set to HotkeyPrefixNone.
        /// - Number of tab stops is set to zero.
        /// - String trimming is set to StringTrimmingCharacter.
        /// </summary>
        public static StringFormat GenericDefault
        {
            get
            {
                IntPtr format;
                int status = Gdip.GdipStringFormatGetGenericDefault(out format);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return new StringFormat(format);
            }
        }

        /// <summary>
        /// Gets a generic typographic <see cref='StringFormat'/>.
        /// Remarks from MSDN: A generic, typographic StringFormat object has the following characteristics:
        /// - String format flags StringFormatFlagsLineLimit, StringFormatFlagsNoClip, and StringFormatFlagsNoFitBlackBox are set.
        /// - Character alignment and line alignment are set to StringAlignmentNear.
        /// - Language ID is set to neutral language, which means that the current language associated with the calling thread is used.
        /// - String digit substitution is set to StringDigitSubstituteUser.
        /// - Hot key prefix is set to HotkeyPrefixNone.
        /// - Number of tab stops is set to zero.
        /// - String trimming is set to StringTrimmingNone.
        /// </summary>
        public static StringFormat GenericTypographic
        {
            get
            {
                IntPtr format;
                int status = Gdip.GdipStringFormatGetGenericTypographic(out format);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return new StringFormat(format);
            }
        }

        public void SetDigitSubstitution(int language, StringDigitSubstitute substitute)
        {
            int status = Gdip.GdipSetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), language, substitute);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        /// <summary>
        /// Gets the <see cref='StringDigitSubstitute'/> for this <see cref='StringFormat'/>.
        /// </summary>
        public StringDigitSubstitute DigitSubstitutionMethod
        {
            get
            {
                StringDigitSubstitute digitSubstitute;

                int status = Gdip.GdipGetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), out _, out digitSubstitute);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return digitSubstitute;
            }
        }

        /// <summary>
        /// Gets the language of <see cref='StringDigitSubstitute'/> for this <see cref='StringFormat'/>.
        /// </summary>
        public int DigitSubstitutionLanguage
        {
            get
            {
                int language;
                int status = Gdip.GdipGetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), out language, out _);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return language;
            }
        }

        internal int GetMeasurableCharacterRangeCount()
        {
            int cnt;
            int status = Gdip.GdipGetStringFormatMeasurableCharacterRangeCount(new HandleRef(this, nativeFormat), out cnt);

            Gdip.CheckStatus(status);
            return cnt;
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='StringFormat'/>.
        /// </summary>
        ~StringFormat()
        {
            Dispose(false);
        }

        /// <summary>
        /// Converts this <see cref='StringFormat'/> to a human-readable string.
        /// </summary>
        public override string ToString() => $"[StringFormat, FormatFlags={FormatFlags}]";
    }
}
