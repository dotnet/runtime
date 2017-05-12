// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

    class Program
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        static int Main(string[] args)
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
