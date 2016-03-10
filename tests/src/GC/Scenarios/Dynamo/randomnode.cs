// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Dynamo {
    using System;

    public class RandomNode : BaseNode
    {
        public static int TotalMemory = 0;

        protected byte [] SimpleSize;
        protected int size = 0;


        public RandomNode( int Size, int Value )
        {
            this.Value = Value;
            iType = 2;

            if ( Size < 0 )
                throw new FormatException("Size must >= 0");
            else if ( Size > 0 ) {
                SimpleSize = new byte[ Size ];
                SimpleSize[0] = (byte)255;
                SimpleSize[Size - 1] = (byte)255;

                TotalMemory+=Size;
                size = Size;
            }

        }

    }
}
