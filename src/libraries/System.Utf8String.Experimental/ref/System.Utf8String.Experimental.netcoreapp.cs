// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public sealed partial class Utf8String : System.IComparable<System.Utf8String?>, System.IEquatable<System.Utf8String?>
    {
        public static System.Utf8String Create<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public static System.Utf8String CreateRelaxed<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public static System.Utf8String UnsafeCreateWithoutValidation<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
    }
}
