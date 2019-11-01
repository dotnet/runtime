// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Dynamo {
    using System;

    public class StaticNode : BaseNode
    {
        protected static int siValue = 200065;

        protected DynamoNode smallNode;
        protected DynamoNode largeNode;

        public StaticNode( int LowNum, int HighNum )
        {

            if (HighNum<LowNum)
                throw new FormatException("HighNum must be <= LowNum");

            Value = siValue++;
            iType = 0;

            int Half = (HighNum - LowNum) / 2;

            SmallNode = new DynamoNode( LowNum, (LowNum + Half), (siValue + 100000) );
            LargeNode = new DynamoNode( ( LowNum + Half ), HighNum, (siValue + 100001) );
        }

        public DynamoNode SmallNode {
            get { return smallNode; }
            set { smallNode = value; }
        }

        public DynamoNode LargeNode {
            get { return largeNode; }
            set { largeNode = value; }
        }

    }
}
