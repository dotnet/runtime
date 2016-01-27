// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
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
