// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Tests;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public abstract class ArrayEnumeratorTests<T> : IList_Generic_Tests<T>
    {
        protected override IList<T> GenericIListFactory()
        {
            return new T[0];
        }

        protected override IList<T> GenericIListFactory(int count)
        {
            T[] array = new T[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = CreateT(i);
            }
            return array;
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override bool IsReadOnly_ValidityValue => true;
        protected override bool AddRemoveClear_ThrowsNotSupported => true;
    }

    public class ArrayEnumeratorTests_string : ArrayEnumeratorTests<string>
    {
        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class ArrayEnumeratorTests_int : ArraySegment_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }
    }
}
