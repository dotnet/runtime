// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Used as generic argument for <see cref="JSMarshalAsAttribute{T}"/> to express expected JavaScript type, which should be source or result of argument marshaling.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public abstract class JSType
    {
        internal JSType() { }
        /// <summary>
        /// Asserts that a JavaScript function should have no return value.
        /// </summary>
        public sealed class Void : JSType
        {
            internal Void() { }
        }

        /// <summary>
        /// Suppresses marshaling of the JavaScript function's return value and discards it.
        /// </summary>
        public sealed class Discard : JSType
        {
            internal Discard() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Boolean">Boolean</see> type.
        /// </summary>
        public sealed class Boolean : JSType
        {
            internal Boolean() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number">Number</see> type.
        /// </summary>
        public sealed class Number : JSType
        {
            internal Number() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt">BigInt</see> type.
        /// </summary>
        public sealed class BigInt : JSType
        {
            internal BigInt() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date">Date</see> type.
        /// </summary>
        public sealed class Date : JSType
        {
            internal Date() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String">String</see> type.
        /// </summary>
        public sealed class String : JSType
        {
            internal String() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object">Object</see> type.
        /// </summary>
        public sealed class Object : JSType
        {
            internal Object() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Error">Error</see> type.
        /// </summary>
        public sealed class Error : JSType
        {
            internal Error() { }
        }

        /// <summary>
        /// Marshals a managed Span or ArraySegment as a JavaScript wrapper object that provides access to the managed memory.
        /// </summary>
        public sealed class MemoryView : JSType
        {
            internal MemoryView() { }
        }

        /// <summary>
        /// Marshal as copy of JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array">Array</see> or <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/TypedArray">TypedArray</see> type.
        /// </summary>
        public sealed class Array<T> : JSType where T : JSType
        {
            internal Array() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise">Promise</see> type.
        /// </summary>
        public sealed class Promise<T> : JSType where T : JSType
        {
            internal Promise() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see> type.
        /// </summary>
        public sealed class Function : JSType
        {
            internal Function() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see> type.
        /// </summary>
        public sealed class Function<T> : JSType where T : JSType
        {
            internal Function() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see> type.
        /// </summary>
        public sealed class Function<T1, T2> : JSType where T1 : JSType where T2 : JSType
        {
            internal Function() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see> type.
        /// </summary>
        public sealed class Function<T1, T2, T3> : JSType where T1 : JSType where T2 : JSType where T3 : JSType
        {
            internal Function() { }
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see> type.
        /// </summary>
        public sealed class Function<T1, T2, T3, T4> : JSType where T1 : JSType where T2 : JSType where T3 : JSType where T4 : JSType
        {
            internal Function() { }
        }

        /// <summary>
        /// Dynamically selects the most appropriate JavaScript or managed type for each argument value at runtime.
        /// </summary>
        public sealed class Any : JSType
        {
            internal Any() { }
        }
    }
}
