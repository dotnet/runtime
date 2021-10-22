// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal readonly struct ParamsArray<TArr> where TArr : IValueArray<object?>
    {
        // NOTE: arguments in an array form are stored in element #0 of object?[MaxInlineArgs + 1].
        // alternatively we could use object?[2] with element #0 storing a sentinel object and #2 the actual array
        // that will take less space, but sentinel check is an actual compare vs. length compare that can be done at JIT time.
        // This is a size/cycles tradeoff, which I did not have time to measure, but either way would work.
        private const int MaxInlineArgs = 3;

        private readonly TArr _args;

        internal ParamsArray(TArr args)
        {
            _args = args;
        }

        // NB: _args.Length is a JIT-time constant.
        public int Length => _args.Length > MaxInlineArgs ?
                             ((object?[])_args[0]!).Length :
                             _args.Length;

        public object? this[int index] => _args.Length > MaxInlineArgs ?
                             ((object?[])_args[0]!)[index] :
                             _args[index];
    }

    internal static class ParamsArray
    {
        private static ParamsArray<TArr> Create<TArr>(TArr args) where TArr : IValueArray<object?>
            => new ParamsArray<TArr>(args);

        public static ParamsArray<ValueArray<object?, object[]>> Create(object? arg0)
        {
            ValueArray<object?, object[]> args = default;
            args[0] = arg0;
            return Create(args);
        }

        public static ParamsArray<ValueArray<object?, object[,]>> Create(object? arg0, object? arg1)
        {
            ValueArray<object?, object[,]> args = default;
            args[0] = arg0;
            args[1] = arg1;
            return Create(args);
        }

#if NICE_SYNTAX
        public static ParamsArray<object?[3]> Create(object? arg0, object? arg1, object? arg2)
            => Create(new object?[3] { arg0, arg1, arg2 });
#else
        public static ParamsArray<ValueArray<object?, object[,,]>> Create(object? arg0, object? arg1, object? arg2)
        {
            ValueArray<object?, object[,,]> args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            return Create(args);
        }
#endif

        public static ParamsArray<ValueArray<object?, object[,,,]>> Create(object?[] args)
        {
            ValueArray<object?, object[,,,]> argsArr = default;
            argsArr[0] = args;
            return Create(argsArr);
        }
    }
}
