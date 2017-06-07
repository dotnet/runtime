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
            Status status = GDIPlus.GdipCreateStringFormat(options, language, out nativeStrFmt);
            GDIPlus.CheckStatus(status);
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
                Status status = GDIPlus.GdipDeleteStringFormat(nativeStrFmt);
                nativeStrFmt = IntPtr.Zero;
                GDIPlus.CheckStatus(status);
            }
        }

        public StringFormat(StringFormat format)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            Status status = GDIPlus.GdipCloneStringFormat(format.NativeObject, out nativeStrFmt);
            GDIPlus.CheckStatus(status);
        }

        public StringFormat(StringFormatFlags options)
        {
            Status status = GDIPlus.GdipCreateStringFormat(options, GDIPlus.LANG_NEUTRAL, out nativeStrFmt);
            GDIPlus.CheckStatus(status);
        }

        public StringAlignment Alignment
        {
            get
            {
                StringAlignment align;
                Status status = GDIPlus.GdipGetStringFormatAlign(nativeStrFmt, out align);
                GDIPlus.CheckStatus(status);

                return align;
            }

            set
            {
                if ((value < StringAlignment.Near) || (value > StringAlignment.Far))
                    throw new InvalidEnumArgumentException("Alignment");

                Status status = GDIPlus.GdipSetStringFormatAlign(nativeStrFmt, value);
                GDIPlus.CheckStatus(status);
            }
        }

        public StringAlignment LineAlignment
        {
            get
            {
                StringAlignment align;
                Status status = GDIPlus.GdipGetStringFormatLineAlign(nativeStrFmt, out align);
                GDIPlus.CheckStatus(status);

                return align;
            }

            set
            {
                if ((value < StringAlignment.Near) || (value > StringAlignment.Far))
                    throw new InvalidEnumArgumentException("Alignment");

                Status status = GDIPlus.GdipSetStringFormatLineAlign(nativeStrFmt, value);
                GDIPlus.CheckStatus(status);
            }
        }

        public StringFormatFlags FormatFlags
        {
            get
            {
                StringFormatFlags flags;
                Status status = GDIPlus.GdipGetStringFormatFlags(nativeStrFmt, out flags);
                GDIPlus.CheckStatus(status);

                return flags;
            }

            set
            {
                Status status = GDIPlus.GdipSetStringFormatFlags(nativeStrFmt, value);
                GDIPlus.CheckStatus(status);
            }
        }

        public HotkeyPrefix HotkeyPrefix
        {
            get
            {
                HotkeyPrefix hotkeyPrefix;
                Status status = GDIPlus.GdipGetStringFormatHotkeyPrefix(nativeStrFmt, out hotkeyPrefix);
                GDIPlus.CheckStatus(status);

                return hotkeyPrefix;
            }

            set
            {
                if ((value < HotkeyPrefix.None) || (value > HotkeyPrefix.Hide))
                    throw new InvalidEnumArgumentException("HotkeyPrefix");

                Status status = GDIPlus.GdipSetStringFormatHotkeyPrefix(nativeStrFmt, value);
                GDIPlus.CheckStatus(status);
            }
        }


        public StringTrimming Trimming
        {
            get
            {
                StringTrimming trimming;
                Status status = GDIPlus.GdipGetStringFormatTrimming(nativeStrFmt, out trimming);
                GDIPlus.CheckStatus(status);
                return trimming;
            }

            set
            {
                if ((value < StringTrimming.None) || (value > StringTrimming.EllipsisPath))
                    throw new InvalidEnumArgumentException("Trimming");

                Status status = GDIPlus.GdipSetStringFormatTrimming(nativeStrFmt, value);
                GDIPlus.CheckStatus(status);
            }
        }

        public static StringFormat GenericDefault
        {
            get
            {
                IntPtr ptr;

                Status status = GDIPlus.GdipStringFormatGetGenericDefault(out ptr);
                GDIPlus.CheckStatus(status);

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

                Status status = GDIPlus.GdipStringFormatGetGenericTypographic(out ptr);
                GDIPlus.CheckStatus(status);

                return new StringFormat(ptr);
            }
        }

        public StringDigitSubstitute DigitSubstitutionMethod
        {
            get
            {
                StringDigitSubstitute substitute;

                Status status = GDIPlus.GdipGetStringFormatDigitSubstitution(nativeStrFmt, language, out substitute);
                GDIPlus.CheckStatus(status);

                return substitute;
            }
        }


        public void SetMeasurableCharacterRanges(CharacterRange[] ranges)
        {
            Status status = GDIPlus.GdipSetStringFormatMeasurableCharacterRanges(nativeStrFmt,
                ranges.Length, ranges);

            GDIPlus.CheckStatus(status);
        }

        internal int GetMeasurableCharacterRangeCount()
        {
            int cnt;
            Status status = GDIPlus.GdipGetStringFormatMeasurableCharacterRangeCount(nativeStrFmt, out cnt);

            GDIPlus.CheckStatus(status);
            return cnt;
        }

        public object Clone()
        {
            IntPtr native;

            Status status = GDIPlus.GdipCloneStringFormat(nativeStrFmt, out native);
            GDIPlus.CheckStatus(status);

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
            Status status = GDIPlus.GdipSetStringFormatTabStops(nativeStrFmt, firstTabOffset, tabStops.Length, tabStops);
            GDIPlus.CheckStatus(status);
        }

        public void SetDigitSubstitution(int language, StringDigitSubstitute substitute)
        {
            Status status = GDIPlus.GdipSetStringFormatDigitSubstitution(nativeStrFmt, this.language, substitute);
            GDIPlus.CheckStatus(status);
        }

        public float[] GetTabStops(out float firstTabOffset)
        {
            int count = 0;
            firstTabOffset = 0;

            Status status = GDIPlus.GdipGetStringFormatTabStopCount(nativeStrFmt, out count);
            GDIPlus.CheckStatus(status);

            float[] tabStops = new float[count];

            if (count != 0)
            {
                status = GDIPlus.GdipGetStringFormatTabStops(nativeStrFmt, count, out firstTabOffset, tabStops);
                GDIPlus.CheckStatus(status);
            }

            return tabStops;
        }

    }
}
