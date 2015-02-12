// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace StackTests
{
    public class Stack_GetEnumeratorTests
    {
        [Fact]
        public static void GetEnumerator_FromNonEmptyStack()
        {
            int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            int[] expectedValues = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };

            Stack<int>.Enumerator enumerator = operandStack.GetEnumerator();
            Assert.True(HelperClass.VerifyEnumerator(enumerator, expectedValues)); //"Faild to get enumerator of stack"
        }

        [Fact]
        public static void GetEnumerator_FromEmptyStack()
        {
            int[] operands = new int[] { };
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            int[] expectedValues = new int[] { };

            Stack<int>.Enumerator enumerator = operandStack.GetEnumerator();
            Assert.True(HelperClass.VerifyEnumerator(enumerator, expectedValues)); //"Faild to get enumerator of an empty stack"
        }
    }

    /// Helper class 
    internal class HelperClass
    {
        public static bool VerifyEnumerator(Stack<int>.Enumerator enumerator, int[] expectedValues)
        {
            bool retVal = true;
            int i = 0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != expectedValues[i]) return false;
                ++i;
            };
            retVal = i == expectedValues.Length;
            return retVal;
        }
    }
}
