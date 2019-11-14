// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Globalization;
using System.Runtime.InteropServices;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml.Controls.Primitives
{
    //
    // GeneratorPosition is the managed projection of Windows.UI.Xaml.Controls.Primitives.GeneratorPosition.
    // Any changes to the layout of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct GeneratorPosition
    {
        private int _index;
        private int _offset;

        public int Index { get { return _index; } set { _index = value; } }
        public int Offset { get { return _offset; } set { _offset = value; } }

        public GeneratorPosition(int index, int offset)
        {
            _index = index;
            _offset = offset;
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode() + _offset.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat("GeneratorPosition (", _index.ToString(CultureInfo.InvariantCulture), ",", _offset.ToString(CultureInfo.InvariantCulture), ")");
        }

        public override bool Equals(object o)
        {
            if (o is GeneratorPosition)
            {
                GeneratorPosition that = (GeneratorPosition)o;
                return _index == that._index &&
                        _offset == that._offset;
            }
            return false;
        }

        public static bool operator ==(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return gp1._index == gp2._index &&
                    gp1._offset == gp2._offset;
        }

        public static bool operator !=(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return !(gp1 == gp2);
        }
    }
}

#pragma warning restore 436
