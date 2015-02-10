// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

namespace System.Collections.StackTests
{
    public class StackToArrayTests
    {
        [Fact]
        public void TestToArrayBasic()
        {
            Stack stk1;
            Object[] oArr;
            Int32 iNumberOfElements;
            String strValue;

            //[] Vanila test case - this gives an object array of the values in the stack
            stk1 = new Stack();
            oArr = stk1.ToArray();

            
            Assert.Equal(0, oArr.Length);

            iNumberOfElements = 10;
            for (int i = 0; i < iNumberOfElements; i++)
                stk1.Push(i);

            oArr = stk1.ToArray();
            Array.Sort(oArr);

            for (int i = 0; i < oArr.Length; i++)
            {
                strValue = "Value_" + i;
                
            Assert.Equal((Int32)oArr[i], i);
            }
        }
    }
}
