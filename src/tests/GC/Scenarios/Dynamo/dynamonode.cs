// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamo {
    using System;

    public class DynamoNode : BaseNode
    {

        protected RandomNode [] DynamicArray;

        public DynamoNode( int LowNum, int HighNum, int Value)
        {

            if (HighNum<LowNum)
                throw new FormatException("HighNum must be <= LowNum");

            iType = 1;
            this.Value = Value;

            DynamicArray = new RandomNode[ HighNum - LowNum ];

            for( int iCounter = 0, i = LowNum; i < HighNum; i++,  iCounter++)
                DynamicArray[ iCounter ] = new RandomNode( i * ( HighNum - LowNum ), i );

        }

        public int Length
        {
            get { return DynamicArray.Length; }
        }

        public RandomNode this[int index]
        {
            get {
                try {
                    return DynamicArray[ index ];
                }
                catch (IndexOutOfRangeException) {
                    return null;
                }
            }
        }

    }
}

