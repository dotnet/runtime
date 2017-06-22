// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Drawing.Text;
    using System.Runtime.InteropServices;
    using System.Globalization;

    /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange"]/*' />
    [StructLayout(LayoutKind.Sequential)]
    public struct CharacterRange
    {
        private int _first;
        private int _length;

        /**
        * Create a new CharacterRange object
        */
        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.CharacterRange"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.CharacterRange'/> class
        ///       with the specified coordinates.
        ///    </para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public CharacterRange(int First, int Length)
        {
            _first = First;
            _length = Length;
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.First"]/*' />
        /// <devdoc>
        ///    Gets the First character position of this <see cref='System.Drawing.CharacterRange'/>.
        /// </devdoc>
        public int First
        {
            get
            {
                return _first;
            }
            set
            {
                _first = value;
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.Length"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the Length of this <see cref='System.Drawing.CharacterRange'/>.
        ///    </para>
        /// </devdoc>
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(CharacterRange))
                return false;

            CharacterRange cr = (CharacterRange)obj;
            return ((_first == cr.First) && (_length == cr.Length));
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.EqualOperator"]/*' />
        public static bool operator ==(CharacterRange cr1, CharacterRange cr2)
        {
            return ((cr1.First == cr2.First) && (cr1.Length == cr2.Length));
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.InequalOperator"]/*' />
        public static bool operator !=(CharacterRange cr1, CharacterRange cr2)
        {
            return !(cr1 == cr2);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="CharacterRange.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return unchecked(_first << 8 + _length);
        }
    }

    /**
     * Represent a Stringformat object
     */
    /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat"]/*' />
    /// <devdoc>
    ///    Encapsulates text layout information (such
    ///    as alignment and linespacing), display manipulations (such as ellipsis insertion
    ///    and national digit substitution) and OpenType features.
    /// </devdoc>
    public sealed class StringFormat : MarshalByRefObject, ICloneable, IDisposable
    {
        internal IntPtr nativeFormat;

        private StringFormat(IntPtr format)
        {
            nativeFormat = format;
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.StringFormat"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.StringFormat'/>
        ///    class.
        /// </devdoc>
        public StringFormat() : this(0, 0)
        {
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.StringFormat1"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.StringFormat'/>
        ///    class with the specified <see cref='System.Drawing.StringFormatFlags'/>.
        /// </devdoc>
        public StringFormat(StringFormatFlags options) :
        this(options, 0)
        {
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.StringFormat2"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.StringFormat'/>
        ///    class with the specified <see cref='System.Drawing.StringFormatFlags'/> and language.
        /// </devdoc>
        public StringFormat(StringFormatFlags options, int language)
        {
            int status = SafeNativeMethods.Gdip.GdipCreateStringFormat(options, language, out nativeFormat);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.StringFormat3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.StringFormat'/> class from the specified
        ///       existing <see cref='System.Drawing.StringFormat'/>.
        ///    </para>
        /// </devdoc>
        public StringFormat(StringFormat format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            int status = SafeNativeMethods.Gdip.GdipCloneStringFormat(new HandleRef(format, format.nativeFormat), out nativeFormat);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Dispose"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Dispose2"]/*' />
        private void Dispose(bool disposing)
        {
            if (nativeFormat != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteStringFormat(new HandleRef(this, nativeFormat));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
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

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public object Clone()
        {
            IntPtr cloneFormat = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneStringFormat(new HandleRef(this, nativeFormat), out cloneFormat);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            StringFormat newCloneStringFormat = new StringFormat(cloneFormat);

            return newCloneStringFormat;
        }


        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.FormatFlags"]/*' />
        /// <devdoc>
        ///    Gets or sets a <see cref='System.Drawing.StringFormatFlags'/> that contains formatting information.
        /// </devdoc>
        public StringFormatFlags FormatFlags
        {
            get
            {
                StringFormatFlags format;

                int status = SafeNativeMethods.Gdip.GdipGetStringFormatFlags(new HandleRef(this, nativeFormat), out format);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return format;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetStringFormatFlags(new HandleRef(this, nativeFormat), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.SetMeasurableCharacterRanges"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Sets the measure of characters to the specified
        ///       range.
        ///    </para>
        /// </devdoc>
        public void SetMeasurableCharacterRanges(CharacterRange[] ranges)
        {
            int status = SafeNativeMethods.Gdip.GdipSetStringFormatMeasurableCharacterRanges(new HandleRef(this, nativeFormat), ranges.Length, ranges);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        // For English, this is horizontal alignment
        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Alignment"]/*' />
        /// <devdoc>
        ///    Specifies text alignment information.
        /// </devdoc>
        public StringAlignment Alignment
        {
            get
            {
                StringAlignment alignment = 0;
                int status = SafeNativeMethods.Gdip.GdipGetStringFormatAlign(new HandleRef(this, nativeFormat), out alignment);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return alignment;
            }
            set
            {
                //valid values are 0x0 to 0x2
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)StringAlignment.Near, (int)StringAlignment.Far))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(StringAlignment));
                }

                int status = SafeNativeMethods.Gdip.GdipSetStringFormatAlign(new HandleRef(this, nativeFormat), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        // For English, this is vertical alignment
        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.LineAlignment"]/*' />
        /// <devdoc>
        ///    Gets or sets the line alignment.
        /// </devdoc>
        public StringAlignment LineAlignment
        {
            get
            {
                StringAlignment alignment = 0;
                int status = SafeNativeMethods.Gdip.GdipGetStringFormatLineAlign(new HandleRef(this, nativeFormat), out alignment);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return alignment;
            }
            set
            {
                if (value < 0 || value > StringAlignment.Far)
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(StringAlignment));
                }

                int status = SafeNativeMethods.Gdip.GdipSetStringFormatLineAlign(new HandleRef(this, nativeFormat), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.HotkeyPrefix"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Drawing.StringFormat.HotkeyPrefix'/> for this <see cref='System.Drawing.StringFormat'/> .
        ///    </para>
        /// </devdoc>
        public HotkeyPrefix HotkeyPrefix
        {
            get
            {
                HotkeyPrefix hotkeyPrefix;
                int status = SafeNativeMethods.Gdip.GdipGetStringFormatHotkeyPrefix(new HandleRef(this, nativeFormat), out hotkeyPrefix);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return hotkeyPrefix;
            }
            set
            {
                //valid values are 0x0 to 0x2
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)HotkeyPrefix.None, (int)HotkeyPrefix.Hide))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(HotkeyPrefix));
                }

                int status = SafeNativeMethods.Gdip.GdipSetStringFormatHotkeyPrefix(new HandleRef(this, nativeFormat), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.SetTabStops"]/*' />
        /// <devdoc>
        ///    Sets tab stops for this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public void SetTabStops(float firstTabOffset, float[] tabStops)
        {
            if (firstTabOffset < 0)
                throw new ArgumentException(SR.Format(SR.InvalidArgument, "firstTabOffset", firstTabOffset));

            int status = SafeNativeMethods.Gdip.GdipSetStringFormatTabStops(new HandleRef(this, nativeFormat), firstTabOffset, tabStops.Length, tabStops);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.GetTabStops"]/*' />
        /// <devdoc>
        ///    Gets the tab stops for this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public float[] GetTabStops(out float firstTabOffset)
        {
            int count = 0;
            int status = SafeNativeMethods.Gdip.GdipGetStringFormatTabStopCount(new HandleRef(this, nativeFormat), out count);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            float[] tabStops = new float[count];
            status = SafeNativeMethods.Gdip.GdipGetStringFormatTabStops(new HandleRef(this, nativeFormat), count, out firstTabOffset, tabStops);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return tabStops;
        }


        // String trimming. How to handle more text than can be displayed
        // in the limits available.

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Trimming"]/*' />
        /// <devdoc>
        ///    Gets or sets the <see cref='System.Drawing.StringTrimming'/>
        ///    for this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public StringTrimming Trimming
        {
            get
            {
                StringTrimming trimming;
                int status = SafeNativeMethods.Gdip.GdipGetStringFormatTrimming(new HandleRef(this, nativeFormat), out trimming);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
                return trimming;
            }

            set
            {
                //valid values are 0x0 to 0x5
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)StringTrimming.None, (int)StringTrimming.EllipsisPath))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(StringTrimming));
                }

                int status = SafeNativeMethods.Gdip.GdipSetStringFormatTrimming(new HandleRef(this, nativeFormat), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.GenericDefault"]/*' />
        /// <devdoc>
        ///    Gets a generic default <see cref='System.Drawing.StringFormat'/>.
        ///    Remarks from MSDN: A generic, default StringFormat object has the following characteristics: 
        ///         - No string format flags are set. 
        ///         - Character alignment and line alignment are set to StringAlignmentNear. 
        ///         - Language ID is set to neutral language, which means that the current language associated with the calling thread is used. 
        ///         - String digit substitution is set to StringDigitSubstituteUser. 
        ///         - Hot key prefix is set to HotkeyPrefixNone. 
        ///         - Number of tab stops is set to zero. 
        ///         - String trimming is set to StringTrimmingCharacter. 
        /// </devdoc>
        public static StringFormat GenericDefault
        {
            get
            {
                IntPtr format;
                int status = SafeNativeMethods.Gdip.GdipStringFormatGetGenericDefault(out format);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return new StringFormat(format);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.GenericTypographic"]/*' />
        /// <devdoc>
        ///    Gets a generic typographic <see cref='System.Drawing.StringFormat'/>.
        ///    Remarks from MSDN: A generic, typographic StringFormat object has the following characteristics: 
        ///         - String format flags StringFormatFlagsLineLimit, StringFormatFlagsNoClip, and StringFormatFlagsNoFitBlackBox are set. 
        ///         - Character alignment and line alignment are set to StringAlignmentNear. 
        ///         - Language ID is set to neutral language, which means that the current language associated with the calling thread is used.
        ///         - String digit substitution is set to StringDigitSubstituteUser. 
        ///         - Hot key prefix is set to HotkeyPrefixNone. 
        ///         - Number of tab stops is set to zero. 
        ///         - String trimming is set to StringTrimmingNone. 
        /// </devdoc>
        public static StringFormat GenericTypographic
        {
            get
            {
                IntPtr format;
                int status = SafeNativeMethods.Gdip.GdipStringFormatGetGenericTypographic(out format);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return new StringFormat(format);
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.SetDigitSubstitution"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void SetDigitSubstitution(int language, StringDigitSubstitute substitute)
        {
            int status = SafeNativeMethods.Gdip.GdipSetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), language, substitute);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.DigitSubstitutionMethod"]/*' />
        /// <devdoc>
        ///    Gets the <see cref='System.Drawing.StringDigitSubstitute'/>
        ///    for this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public StringDigitSubstitute DigitSubstitutionMethod
        {
            get
            {
                StringDigitSubstitute digitSubstitute;
                int lang = 0;

                int status = SafeNativeMethods.Gdip.GdipGetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), out lang, out digitSubstitute);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return digitSubstitute;
            }
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.DigitSubstitutionLanguage"]/*' />
        /// <devdoc>
        ///    Gets the language of <see cref='System.Drawing.StringDigitSubstitute'/>
        ///    for this <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        public int DigitSubstitutionLanguage
        {
            get
            {
                StringDigitSubstitute digitSubstitute;
                int language = 0;
                int status = SafeNativeMethods.Gdip.GdipGetStringFormatDigitSubstitution(new HandleRef(this, nativeFormat), out language, out digitSubstitute);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return language;
            }
        }

        /**
          * Object cleanup
          */
        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.StringFormat'/>.
        /// </devdoc>
        ~StringFormat()
        {
            Dispose(false);
        }

        /// <include file='doc\StringFormat.uex' path='docs/doc[@for="StringFormat.ToString"]/*' />
        /// <devdoc>
        ///    Converts this <see cref='System.Drawing.StringFormat'/> to
        ///    a human-readable string.
        /// </devdoc>
        public override string ToString()
        {
            return "[StringFormat, FormatFlags=" + FormatFlags.ToString() + "]";
        }
    }
}

