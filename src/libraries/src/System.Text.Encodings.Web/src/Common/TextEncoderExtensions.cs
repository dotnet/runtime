// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Provides access to <see cref="TextEncoder"/> APIs that aren't part of the ref asms.
    /// </summary>
    internal static class TextEncoderExtensions
    {
        private delegate OperationStatus EncodeUtf8Del(TextEncoder encoder, ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock);
        private delegate int FindFirstCharacterToEncodeUtf8Del(TextEncoder encoder, ReadOnlySpan<byte> utf8Text);

        private static readonly EncodeUtf8Del s_encodeUtf8Fn = CreateEncodeUtf8Fn();
        private static readonly FindFirstCharacterToEncodeUtf8Del s_findFirstCharToEncodeUtf8Fn = CreateFindFirstCharToEncodeUtf8Fn();

        private static EncodeUtf8Del CreateEncodeUtf8Fn()
        {
            // Locate the shim method, which is able to perform fast virtual dispatch,
            // then create a delegate to it.

            MethodInfo methodInfo = typeof(TextEncoder).GetMethod("EncodeUtf8Shim", BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(methodInfo != null);
            EncodeUtf8Del del = (EncodeUtf8Del)methodInfo.CreateDelegate(typeof(EncodeUtf8Del));

            // Now invoke the delegate once. The reason for this is that the delegate probably
            // points to the pre-jit stub rather than the final codegen for the method, which
            // means that invocations of this delegate will incur an unnecessary call back into
            // the VM. Invoking the delegate forces JIT to take place now, so a future delegate
            // will point directly to the codegen rather than the pre-jit stub.

            del(HtmlEncoder.Default, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out _, out _, false);

            // Now create the delegate again and return it to the caller.
            // The delegate should now be pointing directly to the static method's codegen.

            return (EncodeUtf8Del)methodInfo.CreateDelegate(typeof(EncodeUtf8Del));
        }

        private static FindFirstCharacterToEncodeUtf8Del CreateFindFirstCharToEncodeUtf8Fn()
        {
            // See the comments in CreateEncodeUtf8Fn for an overview of how this logic works.

            MethodInfo methodInfo = typeof(TextEncoder).GetMethod("FindFirstCharacterToEncodeUtf8Shim", BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(methodInfo != null);

            FindFirstCharacterToEncodeUtf8Del del = (FindFirstCharacterToEncodeUtf8Del)methodInfo.CreateDelegate(typeof(FindFirstCharacterToEncodeUtf8Del));
            del(HtmlEncoder.Default, ReadOnlySpan<byte>.Empty);

            return (FindFirstCharacterToEncodeUtf8Del)methodInfo.CreateDelegate(typeof(FindFirstCharacterToEncodeUtf8Del));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OperationStatus EncodeUtf8(this TextEncoder encoder, ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            return s_encodeUtf8Fn(encoder, utf8Source, utf8Destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindFirstCharacterToEncodeUtf8(this TextEncoder encoder, ReadOnlySpan<byte> utf8Text)
        {
            return s_findFirstCharToEncodeUtf8Fn(encoder, utf8Text);
        }
    }
}
