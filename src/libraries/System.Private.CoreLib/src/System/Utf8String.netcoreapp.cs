// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace System
{
    public sealed partial class Utf8String
    {
        /// <summary>
        /// Creates a new <see cref="Utf8String"/> instance, allowing the provided delegate to populate the
        /// instance data of the returned object.
        /// </summary>
        /// <typeparam name="TState">Type of the state object provided to <paramref name="action"/>.</typeparam>
        /// <param name="length">The length, in bytes, of the <see cref="Utf8String"/> instance to create.</param>
        /// <param name="state">The state object to provide to <paramref name="action"/>.</param>
        /// <param name="action">The callback which will be invoked to populate the returned <see cref="Utf8String"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="action"/> populates the buffer with ill-formed UTF-8 data.
        /// </exception>
        /// <remarks>
        /// The runtime will perform UTF-8 validation over the contents provided by the <paramref name="action"/> delegate.
        /// If an invalid UTF-8 subsequence is detected, an exception is thrown.
        /// </remarks>
        public static Utf8String Create<TState>(int length, TState state, SpanAction<byte, TState> action)
        {
            if (length < 0)
            {
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (action is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }

            if (length == 0)
            {
                return Empty; // special-case empty input
            }

            // Create and populate the Utf8String instance.
            // Can't use FastAllocateSkipZeroInit here because we're handing the raw buffer to user code.

            Utf8String newString = FastAllocate(length);
            action(newString.DangerousGetMutableSpan(), state);

            // Now perform validation.

            if (!Utf8Utility.IsWellFormedUtf8(newString.AsBytes()))
            {
                throw new ArgumentException(
                    message: SR.Utf8String_CallbackProvidedMalformedData,
                    paramName: nameof(action));
            }

            return newString;
        }

        /// <summary>
        /// Creates a new <see cref="Utf8String"/> instance, allowing the provided delegate to populate the
        /// instance data of the returned object.
        /// </summary>
        /// <typeparam name="TState">Type of the state object provided to <paramref name="action"/>.</typeparam>
        /// <param name="length">The length, in bytes, of the <see cref="Utf8String"/> instance to create.</param>
        /// <param name="state">The state object to provide to <paramref name="action"/>.</param>
        /// <param name="action">The callback which will be invoked to populate the returned <see cref="Utf8String"/>.</param>
        /// <remarks>
        /// The runtime will perform UTF-8 validation over the contents provided by the <paramref name="action"/> delegate.
        /// If an invalid UTF-8 subsequence is detected, the invalid subsequence is replaced with <see cref="Rune.ReplacementChar"/>
        /// in the returned <see cref="Utf8String"/> instance. This could result in the returned <see cref="Utf8String"/> instance
        /// having a different byte length than specified by the <paramref name="length"/> parameter.
        /// </remarks>
        public static Utf8String CreateRelaxed<TState>(int length, TState state, SpanAction<byte, TState> action)
        {
            if (length < 0)
            {
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (action is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }

            if (length == 0)
            {
                return Empty; // special-case empty input
            }

            // Create and populate the Utf8String instance.
            // Can't use FastAllocateSkipZeroInit here because we're handing the raw buffer to user code.

            Utf8String newString = FastAllocate(length);
            action(newString.DangerousGetMutableSpan(), state);

            // Now perform validation and fixup.

            return Utf8Utility.ValidateAndFixupUtf8String(newString);
        }

        /// <summary>
        /// Creates a new <see cref="Utf8String"/> instance, allowing the provided delegate to populate the
        /// instance data of the returned object. Please see remarks for important safety information about
        /// this method.
        /// </summary>
        /// <typeparam name="TState">Type of the state object provided to <paramref name="action"/>.</typeparam>
        /// <param name="length">The length, in bytes, of the <see cref="Utf8String"/> instance to create.</param>
        /// <param name="state">The state object to provide to <paramref name="action"/>.</param>
        /// <param name="action">The callback which will be invoked to populate the returned <see cref="Utf8String"/>.</param>
        /// <remarks>
        /// This factory method can be used as an optimization to skip the validation step that
        /// <see cref="Create{TState}(int, TState, SpanAction{byte, TState})"/> normally performs. The contract
        /// of this method requires that <paramref name="action"/> populate the buffer with well-formed UTF-8
        /// data, as <see cref="Utf8String"/> contractually guarantees that it contains only well-formed UTF-8 data,
        /// and runtime instability could occur if a caller violates this guarantee.
        /// </remarks>
        public static Utf8String UnsafeCreateWithoutValidation<TState>(int length, TState state, SpanAction<byte, TState> action)
        {
            if (length < 0)
            {
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (action is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }

            if (length == 0)
            {
                return Empty; // special-case empty input
            }

            // Create and populate the Utf8String instance.
            // Can't use FastAllocateSkipZeroInit here because we're handing the raw buffer to user code.

            Utf8String newString = FastAllocate(length);
            action(newString.DangerousGetMutableSpan(), state);

            // The line below is removed entirely in release builds.

            Debug.Assert(Utf8Utility.IsWellFormedUtf8(newString.AsBytes()), "Callback populated the buffer with ill-formed UTF-8 data.");

            return newString;
        }
    }
}
