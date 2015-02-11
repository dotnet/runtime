// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

namespace System.Collections.StackTests
{
    public class StackCloneTests
    {
        [Fact]
        public void TestCloneBasic()
        {
            Stack stk;
            Stack stkClone;

            A a1;
            A a2;

            //[]vanila 

            stk = new Stack();

            for (int i = 0; i < 100; i++)
                stk.Push(i);

            stkClone = (Stack)stk.Clone();

            Assert.Equal(100, stkClone.Count);

            for (int i = 0; i < 100; i++)
            {
                Assert.True(stkClone.Contains(i));
            }

            //[]making sure that this is shallow

            stk = new Stack();
            stk.Push(new A(10));
            stkClone = (Stack)stk.Clone();
            Assert.Equal(1, stkClone.Count);

            a1 = (A)stk.Pop();
            a1.I = 50;

            Assert.Equal(1, stkClone.Count);
            a2 = (A)stkClone.Pop();
            Assert.Equal(50, a2.I);

            //[]vanila with synchronized stack
            stk = new Stack();

            for (int i = 0; i < 100; i++)
                stk.Push(i);

            stkClone = (Stack)(Stack.Synchronized(stk)).Clone();
            Assert.Equal(100, stkClone.Count);
            Assert.True(stkClone.IsSynchronized);

            for (int i = 0; i < 100; i++)
            {
                Assert.True(stkClone.Contains(i));
            }
        }
    }

    class A
    {
        private int _i;
        public A(int i)
        {
            _i = i;
        }
        
        internal int I
        {
            set { _i = value; }
            get { return _i; }
        }
    }
}
