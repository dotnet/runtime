// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning disable 436   // Redefining types from Windows.Foundation
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

#if FEATURE_SLJ_PROJECTION_COMPAT
namespace System.Windows
#else // !FEATURE_SLJ_PROJECTION_COMPAT


namespace Windows.UI
#endif // FEATURE_SLJ_PROJECTION_COMPAT
{
    //
    // Color is the managed projection of Windows.Foundation.Color. Any changes to the layout of
    // this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Color : IFormattable
    {
        private byte _A;
        private byte _R;
        private byte _G;
        private byte _B;

        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            Color c1 = new Color();

            c1.A = a;
            c1.R = r;
            c1.G = g;
            c1.B = b;

            return c1;
        }

        public byte A
        {
            get { return _A; }
            set { _A = value; }
        }

        public byte R
        {
            get { return _R; }
            set { _R = value; }
        }

        public byte G
        {
            get { return _G; }
            set { _G = value; }
        }

        public byte B
        {
            get { return _B; }
            set { _B = value; }
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null, null);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        internal string ConvertToString(string format, IFormatProvider provider)
        {
            StringBuilder sb = new StringBuilder();

            if (format == null)
            {
                sb.AppendFormat(provider, "#{0:X2}", _A);
                sb.AppendFormat(provider, "{0:X2}", _R);
                sb.AppendFormat(provider, "{0:X2}", _G);
                sb.AppendFormat(provider, "{0:X2}", _B);
            }
            else
            {
                // Helper to get the numeric list separator for a given culture.
                char separator = TokenizerHelper.GetNumericListSeparator(provider);

                sb.AppendFormat(provider,
                    "sc#{1:" + format + "}{0} {2:" + format + "}{0} {3:" + format + "}{0} {4:" + format + "}",
                    separator, _A, _R, _G, _B);
            }

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return _A.GetHashCode() ^ _R.GetHashCode() ^ _G.GetHashCode() ^ _B.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return o is Color && this == (Color)o;
        }

        public bool Equals(Color color)
        {
            return this == color;
        }

        public static bool operator ==(Color color1, Color color2)
        {
            return
                color1.R == color2.R &&
                color1.G == color2.G &&
                color1.B == color2.B &&
                color1.A == color2.A;
        }

        public static bool operator !=(Color color1, Color color2)
        {
            return (!(color1 == color2));
        }
    }
}

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning restore 436
#endif // !FEATURE_SLJ_PROJECTION_COMPAT
