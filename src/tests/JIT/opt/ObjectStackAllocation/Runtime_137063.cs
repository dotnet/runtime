// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_137063
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/134949", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsWasi), nameof(TestLibrary.PlatformDetection.IsReadyToRunCompiled))]
    [Fact]
    public static void Test()
    {
        IEnumerable<int> small = new SmallEnumerable();
        IEnumerable<int> other = new OtherEnumerable();
        Stack<int> stack = new();

        // Factory calls favor SmallEnumerator, but loop iterations favor OtherEnumerator.
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                int first = Count(small, stack);
                int second = Count(small, stack);
                int third = Count(other, stack);
                Assert.Equal(2, first);
                Assert.Equal(2, second);
                Assert.Equal(8, third);
            }

            Thread.Sleep(5);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count(IEnumerable<int> from, Stack<int> stack)
    {
        stack.Clear();
        IEnumerator<int> enumerator = from.GetEnumerator();
        while (enumerator.MoveNext())
        {
            stack.Push(enumerator.Current);
        }

        return stack.Count;
    }

    private sealed class SmallEnumerable : IEnumerable<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmallEnumerator GetEnumerator() => new SmallEnumerator(2);

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private struct SmallEnumerator : IEnumerator<int>
    {
        private readonly int _count;
        private int _cursor;
        private int _current;

        public SmallEnumerator(int count)
        {
            _count = count;
            _cursor = 0;
            _current = 0;
        }

        public int Current => _current;

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_cursor < _count)
            {
                _cursor++;
                _current = 1;
                return true;
            }

            return false;
        }

        public void Reset() => _cursor = 0;

        public void Dispose()
        {
        }
    }

    private sealed class OtherEnumerable : IEnumerable<int>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IEnumerator<int> GetEnumerator() => new OtherEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private abstract class OtherEnumeratorBase
    {
        protected int _index = -1;
        protected readonly int _endIndex = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if ((uint)index < (uint)_endIndex)
            {
                _index = index;
                return true;
            }

            _index = _endIndex;

            return false;
        }
    }

    private sealed class OtherEnumerator : OtherEnumeratorBase, IEnumerator<int>
    {
        public int Current => 1;

        object IEnumerator.Current => Current;

        public void Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}
