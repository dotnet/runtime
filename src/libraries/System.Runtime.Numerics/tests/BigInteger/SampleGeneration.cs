// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tests
{
    public static partial class SampleGeneration
    {

        public static IEnumerable<ReadOnlyMemory<T>> EnumerateSequence<T>(IEnumerable<T> elementSource, int minLength, int maxLengthExclusive)
        {
            return EnumerateSequence(elementSource.ToArray(), minLength, maxLengthExclusive);
        }

        public static IEnumerable<ReadOnlyMemory<T>> EnumerateSequence<T>(T[] elementSource, int minLength, int maxLengthExclusive)
        {
            for (var i = minLength; maxLengthExclusive > i; ++i)
            {
                foreach (var item in EnumerateSequence(elementSource, i))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<ReadOnlyMemory<T>> EnumerateSequence<T>(IEnumerable<T> elementSource, int length)
        {
            return EnumerateSequence(elementSource.ToArray(), length);
        }

        public static IEnumerable<ReadOnlyMemory<T>> EnumerateSequence<T>(T[] elementSource, int length)
        {
            var a = new T[length];
            var r = new ReadOnlyMemory<T>(a);
            foreach (var _ in EnumerateSequenceYieldsCurrentCount(elementSource, a))
            {
                yield return r;
            }
        }

        private static IEnumerable<long> EnumerateSequenceYieldsCurrentCount<T>(T[] elementSource, T[] buffer)
        {
            var c = 0L;
            var b = elementSource.Length;
            if (b != 0)
            {
                var stack = new int[buffer.Length];
                for (var i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = elementSource[0];
                }
                {
                L:;
                    yield return c++;
                    for (var i = 0; stack.Length != i; ++i)
                    {
                        var en = ++stack[i];
                        if (b == en)
                        {
                        }
                        else
                        {
                            buffer[i] = elementSource[en];
                            for (; 0 <= --i;)
                            {
                                buffer[i] = elementSource[0];
                                stack[i] = 0;
                            }
                            goto L;
                        }
                    }
                }
            }
        }
    }
}
