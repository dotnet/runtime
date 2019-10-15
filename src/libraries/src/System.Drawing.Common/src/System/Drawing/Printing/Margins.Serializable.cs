// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file isn't built into the .csproj in corefx but is consumed by Mono.

using System.Runtime.Serialization;

namespace System.Drawing.Printing
{
    [Serializable]
    partial class Margins
    {
        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            if (_doubleLeft == 0 && _left != 0)
            {
                _doubleLeft = (double)_left;
            }

            if (_doubleRight == 0 && _right != 0)
            {
                _doubleRight = (double)_right;
            }

            if (_doubleTop == 0 && _top != 0)
            {
                _doubleTop = (double)_top;
            }

            if (_doubleBottom == 0 && _bottom != 0)
            {
                _doubleBottom = (double)_bottom;
            }
        }
    }
}
