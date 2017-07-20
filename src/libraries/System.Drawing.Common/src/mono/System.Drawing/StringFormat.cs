// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.StringFormat.cs
//
// Authors:
//   Dennis Hayes (dennish@Raytek.com)
//   Miguel de Icaza (miguel@ximian.com)
//   Jordi Mas i Hernandez (jordi@ximian.com)
//
// Copyright (C) 2002 Ximian, Inc (http://www.ximian.com)
// Copyright (C) 2004,2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.ComponentModel;
using System.Drawing.Text;

namespace System.Drawing
{

    public sealed class StringFormat : MarshalByRefObject, IDisposable, ICloneable
    {
        //		private static StringFormat genericDefault;
        private IntPtr nativeStrFmt = IntPtr.Zero;
        private int language = GDIPlus.LANG_NEUTRAL;

        public StringFormat() : this(0, GDIPlus.LANG_NEUTRAL)
        {
        }

        public StringFormat(StringFormatFlags options, int language)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateStringFormat(options, language, out nativeStrFmt);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        internal StringFormat(IntPtr native)
        {
            nativeStrFmt = native;
        }

        ~StringFormat()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (nativeStrFmt != IntPtr.Zero)
            {
                Status status = SafeNativeMethods.Gdip.GdipDeleteStringFormat(nativeStrFmt);
                nativeStrFmt = IntPtr.Zero;
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public StringFormat(StringFormat format)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            Status status = SafeNativeMethods.Gdip.GdipCloneStringFormat(format.NativeObject, out nativeStrFmt);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public StringFormat(StringFormatFlags options)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateStringFormat(options, GDIPlus.LANG_NEUTRAL, out nativeStrFmt);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public StringAlignment Alignment
        {
            get
            {
                StringAlignment align;
                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatAlign(nativeStrFmt, out align);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return align;
            }

            set
            {
                if ((value < StringAlignment.Near) || (value > StringAlignment.Far))
                    throw new InvalidEnumArgumentException("Alignment");

                Status status = SafeNativeMethods.Gdip.GdipSetStringFormatAlign(nativeStrFmt, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public StringAlignment LineAlignment
        {
            get
            {
                StringAlignment align;
                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatLineAlign(nativeStrFmt, out align);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return align;
            }

            set
            {
                if ((value < StringAlignment.Near) || (value > StringAlignment.Far))
                    throw new InvalidEnumArgumentException("Alignment");

                Status status = SafeNativeMethods.Gdip.GdipSetStringFormatLineAlign(nativeStrFmt, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public StringFormatFlags FormatFlags
        {
            get
            {
                StringFormatFlags flags;
                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatFlags(nativeStrFmt, out flags);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return flags;
            }

            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetStringFormatFlags(nativeStrFmt, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public HotkeyPrefix HotkeyPrefix
        {
            get
            {
                HotkeyPrefix hotkeyPrefix;
                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatHotkeyPrefix(nativeStrFmt, out hotkeyPrefix);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return hotkeyPrefix;
            }

            set
            {
                if ((value < HotkeyPrefix.None) || (value > HotkeyPrefix.Hide))
                    throw new InvalidEnumArgumentException("HotkeyPrefix");

                Status status = SafeNativeMethods.Gdip.GdipSetStringFormatHotkeyPrefix(nativeStrFmt, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }


        public StringTrimming Trimming
        {
            get
            {
                StringTrimming trimming;
                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatTrimming(nativeStrFmt, out trimming);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return trimming;
            }

            set
            {
                if ((value < StringTrimming.None) || (value > StringTrimming.EllipsisPath))
                    throw new InvalidEnumArgumentException("Trimming");

                Status status = SafeNativeMethods.Gdip.GdipSetStringFormatTrimming(nativeStrFmt, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public static StringFormat GenericDefault
        {
            get
            {
                IntPtr ptr;

                Status status = SafeNativeMethods.Gdip.GdipStringFormatGetGenericDefault(out ptr);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new StringFormat(ptr);
            }
        }


        public int DigitSubstitutionLanguage
        {
            get
            {
                return language;
            }
        }


        public static StringFormat GenericTypographic
        {
            get
            {
                IntPtr ptr;

                Status status = SafeNativeMethods.Gdip.GdipStringFormatGetGenericTypographic(out ptr);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new StringFormat(ptr);
            }
        }

        public StringDigitSubstitute DigitSubstitutionMethod
        {
            get
            {
                StringDigitSubstitute substitute;

                Status status = SafeNativeMethods.Gdip.GdipGetStringFormatDigitSubstitution(nativeStrFmt, language, out substitute);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return substitute;
            }
        }


        public void SetMeasurableCharacterRanges(CharacterRange[] ranges)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetStringFormatMeasurableCharacterRanges(nativeStrFmt,
                ranges.Length, ranges);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        internal int GetMeasurableCharacterRangeCount()
        {
            int cnt;
            Status status = SafeNativeMethods.Gdip.GdipGetStringFormatMeasurableCharacterRangeCount(nativeStrFmt, out cnt);

            SafeNativeMethods.Gdip.CheckStatus(status);
            return cnt;
        }

        public object Clone()
        {
            IntPtr native;

            Status status = SafeNativeMethods.Gdip.GdipCloneStringFormat(nativeStrFmt, out native);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new StringFormat(native);
        }

        public override string ToString()
        {
            return "[StringFormat, FormatFlags=" + this.FormatFlags.ToString() + "]";
        }

        internal IntPtr NativeObject
        {
            get
            {
                return nativeStrFmt;
            }
            set
            {
                nativeStrFmt = value;
            }
        }

        internal IntPtr nativeFormat => nativeStrFmt;

        public void SetTabStops(float firstTabOffset, float[] tabStops)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetStringFormatTabStops(nativeStrFmt, firstTabOffset, tabStops.Length, tabStops);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetDigitSubstitution(int language, StringDigitSubstitute substitute)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetStringFormatDigitSubstitution(nativeStrFmt, this.language, substitute);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public float[] GetTabStops(out float firstTabOffset)
        {
            int count = 0;
            firstTabOffset = 0;

            Status status = SafeNativeMethods.Gdip.GdipGetStringFormatTabStopCount(nativeStrFmt, out count);
            SafeNativeMethods.Gdip.CheckStatus(status);

            float[] tabStops = new float[count];

            if (count != 0)
            {
                status = SafeNativeMethods.Gdip.GdipGetStringFormatTabStops(nativeStrFmt, count, out firstTabOffset, tabStops);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }

            return tabStops;
        }

    }
}
