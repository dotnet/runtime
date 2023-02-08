// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestApp
{
    public struct StructWithValue : IEquatable<StructWithValue>
    {
        private ushort value;

        public StructWithValue(ushort v)
        {
            value = v;
        }


        public bool Equals(StructWithValue other)
        {
            if (value.Equals (other.value))
                return true;
            else
                return false;
        }
    }

    public class Program
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Fact]
        public static int TestEntryPoint()
        {
            var comparer = EqualityComparer<StructWithValue>.Default;

            for (ushort i = 0; ; i++)
            {
                var a = new StructWithValue(i);
                var b = new StructWithValue(i);

                if (!comparer.Equals(a, b))
                {
                    return 0;
                }

                if (i == ushort.MaxValue)
                {
                    break;
                }
            }

            return 100;
        }
    }
}
