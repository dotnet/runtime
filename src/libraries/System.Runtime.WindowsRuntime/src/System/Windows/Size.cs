// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning disable 436   // Redefining types from Windows.Foundation
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

#if FEATURE_SLJ_PROJECTION_COMPAT
namespace System.Windows
#else // !FEATURE_SLJ_PROJECTION_COMPAT


namespace Windows.Foundation
#endif // FEATURE_SLJ_PROJECTION_COMPAT
{
    //
    // Size is the managed projection of Windows.Foundation.Size.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Size
    {
        private float _width;
        private float _height;

        private static readonly Size s_empty = CreateEmptySize();

        public Size(double width, double height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), SR.ArgumentOutOfRange_NeedNonNegNum);
            _width = (float)width;
            _height = (float)height;
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), SR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), SR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public static Size Empty
        {
            get { return s_empty; }
        }


        public bool IsEmpty
        {
            get { return Width < 0; }
        }

        private static Size CreateEmptySize()
        {
            Size size = new Size();
            // We can't set these via the property setters because negatives widths
            // are rejected in those APIs.
            size._width = Single.NegativeInfinity;
            size._height = Single.NegativeInfinity;
            return size;
        }

        public static bool operator ==(Size size1, Size size2)
        {
            return size1.Width == size2.Width &&
                   size1.Height == size2.Height;
        }

        public static bool operator !=(Size size1, Size size2)
        {
            return !(size1 == size2);
        }

        public override bool Equals(object o)
        {
            return o is Size && Size.Equals(this, (Size)o);
        }

        public bool Equals(Size value)
        {
            return Size.Equals(this, value);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return Width.GetHashCode() ^
                       Height.GetHashCode();
            }
        }

        private static bool Equals(Size size1, Size size2)
        {
            if (size1.IsEmpty)
            {
                return size2.IsEmpty;
            }
            else
            {
                return size1.Width.Equals(size2.Width) &&
                       size1.Height.Equals(size2.Height);
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            return String.Format("{0},{1}", _width, _height);
        }
    }
}

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning restore 436
#endif // !FEATURE_SLJ_PROJECTION_COMPAT
