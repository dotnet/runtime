// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    /// <summary>
    /// Constructor arguments for objects with parameterized ctors with less than 5 parameters.
    /// This is to avoid boxing for small, immutable objects.
    /// </summary>
    internal sealed class Arguments<TArg0, TArg1, TArg2, TArg3>
    {
        public TArg0? Arg0;
        public TArg1? Arg1;
        public TArg2? Arg2;
        public TArg3? Arg3;
    }
}
