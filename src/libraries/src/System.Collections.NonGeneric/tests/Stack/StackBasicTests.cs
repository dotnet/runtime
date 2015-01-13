// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class StackBasicTests
{
    [Fact]
    public static void EmptyStackSizeIsZero()
    {
        Stack stack = new Stack();
        Assert.Equal(stack.Count, 0);
    }

    [Fact]
    public static void DefaultStackIsNotSynchronized()
    {
        Stack stack = new Stack();
        Assert.False(stack.IsSynchronized);
    }

    [Fact]
    public static void NumberOfElementsAddedIsEqualToStackSize()
    {
        int iNumElementsAdded = 1975;
        Stack stack = new Stack();

        for (int i = 0; i < iNumElementsAdded; i++)
        {
            stack.Push(new Object());
        }

        Assert.Equal(stack.Count, iNumElementsAdded);
    }

    [Fact]
    public static void ClearResetsNumberOfElementsToZero()
    {
        int iNumElementsAdded = 2;
        Stack stack = new Stack();

        for (int i = 0; i < iNumElementsAdded; i++)
        {
            stack.Push(new Object());
        }

        stack.Clear();

        Assert.Equal(stack.Count, 0);
    }

    [Fact]
    public static void PopDecrementsStackSize()
    {
        int iNumElementsAdded = 25;
        Stack stack = new Stack();

        for (int i = 0; i < iNumElementsAdded; i++)
        {
            stack.Push(i);
        }

        for (int i = 0; i < iNumElementsAdded; i++)
        {
            Assert.Equal(stack.Count, iNumElementsAdded - i);
            Object objTop = stack.Pop();
            Assert.Equal(stack.Count, iNumElementsAdded - i - 1);
        }
    }

    [Fact]
    public static void PeekingEmptyStackThrows()
    {
        Stack stack = new Stack();
        Assert.Throws<InvalidOperationException>(() => { var x = stack.Peek(); });
    }

    [Fact]
    public static void PeekingEmptyStackAfterRemovingElementsThrows()
    {
        object objRet;
        Stack stack = new Stack();
        for (int i = 0; i < 1000; i++)
        {
            stack.Push(i);
        }

        for (int i = 0; i < 1000; i++)
        {
            objRet = stack.Pop();
        }

        Assert.Throws<InvalidOperationException>(() => { objRet = stack.Peek(); });
    }

    [Fact]
    public static void ICollectionCanBeGivenToStack()
    {
        int iNumElements = 10000;

        var objArr = new Object[iNumElements];
        for (int i = 0; i < iNumElements; i++)
        {
            objArr[i] = i;
        }

        Stack stack = new Stack(objArr);

        for (int i = 0; i < iNumElements; i++)
        {
            var objRet = stack.Pop();
            Assert.True(objRet.Equals(iNumElements - i - 1));
        }
    }

    [Fact]
    public static void PeekingAtElementTwiceGivesSameResults()
    {
        Stack stack = new Stack();
        stack.Push(1);
        Assert.True(stack.Peek().Equals(stack.Peek()));
    }

    [Fact]
    public static void PushAndPopWorkOnNullElements()
    {
        Stack stack = new Stack();
        stack.Push(null);
        stack.Push(-1);
        stack.Push(null);

        Assert.Equal(stack.Pop(), null);
        Assert.True((-1).Equals(stack.Pop()));
        Assert.Equal(stack.Pop(), null);
    }

    [Fact]
    public static void CopyingToNullArrayThrows()
    {
        Stack stack = new Stack();
        stack.Push("hey");
        Assert.Throws<ArgumentNullException>(() => stack.CopyTo(null, 0));
    }

    [Fact]
    public static void CopyingToMultiDimArrayThrows()
    {
        Stack stack = new Stack();
        stack.Push("hey");
        Assert.Throws<ArgumentException>(() => stack.CopyTo(new Object[8, 8], 0));
    }

    [Fact]
    public static void CopyingOutOfRangeThrows_1()
    {
        Stack stack = new Stack();
        var objArr = new Object[0];
        Assert.Throws<ArgumentException>(() => stack.CopyTo(objArr, 1));

        stack = new Stack();
        Assert.Throws<ArgumentException>(() => stack.CopyTo(objArr, Int32.MaxValue));

        stack = new Stack();
        Assert.Throws<ArgumentOutOfRangeException>(() => stack.CopyTo(objArr, Int32.MinValue));

        stack = new Stack();
        Assert.Throws<ArgumentOutOfRangeException>(() => stack.CopyTo(objArr, -1));
    }

    [Fact]
    public static void CopyingOutOfRangeThrows_2()
    {
        Stack stack = new Stack();
        stack.Push("MyString");

        var objArr = new Object[0];

        Assert.Throws<ArgumentException>(() => stack.CopyTo(objArr, 0));
    }

    [Fact]
    public static void GettingEnumeratorAndLoopingThroughWorks()
    {
        Stack stack = new Stack();
        stack.Push("hey");
        stack.Push("hello");

        IEnumerator ienum = stack.GetEnumerator();
        int iCounter = 0;

        while (ienum.MoveNext())
        {
            iCounter++;
        }

        Assert.Equal(iCounter, stack.Count);
    }

    [Fact]
    public static void GetBeforeStartingEnumerator()
    {
        // NOTE: The docs say this behaviour is undefined so if test fails it might be ok
        Stack stack = new Stack();
        stack.Push("a");
        stack.Push("b");

        IEnumerator ienum = stack.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => { Object obj = ienum.Current; });
    }

    [Fact]
    public static void EnumeratingBeyondEndOfListThenGetObject()
    {
        Stack stack = new Stack();
        stack.Push(new Object());
        stack.Push(stack);

        IEnumerator ienum = stack.GetEnumerator();

        Assert.True(ienum.MoveNext());

        for (int i = 0; i < 100; i++)
        {
            Object objTemp1 = ienum.Current;
            Assert.True(objTemp1.Equals(stack));
        }

        Assert.True(ienum.MoveNext());

        for (int i = 0; i < 100; i++)
        {
            Assert.False(ienum.MoveNext());
        }

        Assert.Throws<InvalidOperationException>(() => { var o = ienum.Current; });
    }

    [Fact]
    public static void PassingNegativeCapacityThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { Stack stack = new Stack(Int32.MinValue); });
    }

    [Fact]
    public static void CreatingStackWithZeroCapacityDoesntThrow()
    {
        Stack stack = new Stack(0);
    }

    [Fact]
    public static void PassingValidCapacityCreatesZeroElementsStack()
    {
        Stack stack = new Stack(1);
        Assert.Equal(stack.Count, 0);
    }

    [Fact]
    public static void SynchronizedStacksIsSynchronizedPropertyReturnsTrue()
    {
        Stack stack = Stack.Synchronized(new Stack());
        Assert.True(stack.IsSynchronized);
    }

    [Fact]
    public static void SynchronizingNullStackThrows()
    {
        Assert.Throws<ArgumentNullException>(() => { Stack stack = Stack.Synchronized(null); });
    }

    [Fact]
    public static void TestingAllMethodsOfSynchronizedStack()
    {
        Stack q1 = new Stack();
        for (int i = 0; i < 10; i++)
        {
            q1.Push("String_" + i);
        }

        Stack q2 = Stack.Synchronized(q1);
        Assert.Equal(q2.Count, q1.Count);
        q2.Clear();
        Assert.Equal(q2.Count, 0);
        for (int i = 0; i < 10; i++)
        {
            q2.Push("String_" + i);
        }

        for (int i = 0, j = 9; i < 10; i++, j--)
        {
            Assert.True(((String)q2.Peek()).Equals("String_" + j));
            Assert.True(((String)q2.Pop()).Equals("String_" + j));
        }

        Assert.Equal(q2.Count, 0);
        Assert.True(q2.IsSynchronized);

        for (int i = 0; i < 10; i++)
            q2.Push("String_" + i);
        Stack q3 = Stack.Synchronized(q2);

        Assert.True(q3.IsSynchronized);
        Assert.Equal(q2.Count, q3.Count);

        var strArr = new String[10];
        q2.CopyTo(strArr, 0);
        for (int i = 0, j = 9; i < 10; i++, j--)
        {
            Assert.True(strArr[i].Equals("String_" + j));
        }

        strArr = new String[10 + 10];
        q2.CopyTo(strArr, 10);
        for (int i = 0, j = 9; i < 10; i++, j--)
        {
            Assert.True(strArr[i + 10].Equals("String_" + j));
        }

        Assert.Throws<ArgumentNullException>(() => q2.CopyTo(null, 0));

        var oArr = q2.ToArray();
        for (int i = 0, j = 9; i < 10; i++, j--)
        {
            Assert.True(((String)oArr[i]).Equals("String_" + j));
        }

        var ienm1 = q2.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => { var oValue = ienm1.Current; });

        var iCount = 9;
        while (ienm1.MoveNext())
        {
            Assert.True(((String)ienm1.Current).Equals("String_" + iCount));
            iCount--;
        }

        ienm1.Reset();
        iCount = 9;
        while (ienm1.MoveNext())
        {
            Assert.True(((String)ienm1.Current).Equals("String_" + iCount));
            iCount--;
        }

        ienm1.Reset();
        q2.Pop();

        Assert.Throws<InvalidOperationException>(() => { var oValue = ienm1.Current; });
        Assert.Throws<InvalidOperationException>(() => ienm1.MoveNext());
        Assert.Throws<InvalidOperationException>(() => ienm1.Reset());
    }

    [Fact]
    public static void PassingNullCollectionToConstructorThrows()
    {
        Assert.Throws<ArgumentNullException>(() => { Stack stack = new Stack(null); });
    }
}