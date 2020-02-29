// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System.Text
{
    [StructLayout(LayoutKind.Auto)]
    public readonly ref partial struct Utf8Span
    {
        public Utf8Span this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length);

                // Check for a split across a multi-byte subsequence on the way out.
                // Reminder: Unlike Utf8String, we can't safely dereference past the end of the span.

                ref byte newRef = ref DangerousGetMutableReference(offset);
                if (length > 0 && Utf8Utility.IsUtf8ContinuationByte(newRef))
                {
                    Utf8String.ThrowImproperStringSplit();
                }

                int endIdx = offset + length;
                if (endIdx < Length && Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(endIdx)))
                {
                    Utf8String.ThrowImproperStringSplit();
                }

                return UnsafeCreateWithoutValidation(new ReadOnlySpan<byte>(ref newRef, length));
            }
        }
    }
}
