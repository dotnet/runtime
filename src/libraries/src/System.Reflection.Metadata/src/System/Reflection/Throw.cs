// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // This file defines an internal class used to throw exceptions. The main purpose is to reduce code size.
    // Also it improves the likelihood that callers will be inlined.
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HeapHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataHeapHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntityOrUserStringHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataTableOrUserStringHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidToken()
        {
            throw new ArgumentException(SR.InvalidToken, "token");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TableIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OutOfBounds()
        {
            throw new BadImageFormatException(SR.OutOfBoundsRead);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCodedIndex()
        {
            throw new BadImageFormatException(SR.InvalidCodedIndex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidHandle()
        {
            throw new BadImageFormatException(SR.InvalidHandle);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCompressedInteger()
        {
            throw new BadImageFormatException(SR.InvalidCompressedInteger);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidSerializedString()
        {
            throw new BadImageFormatException(SR.InvalidSerializedString);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ImageTooSmall()
        {
            throw new BadImageFormatException(SR.ImageTooSmall);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
        {
            throw new BadImageFormatException(SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReferenceOverflow()
        {
            throw new BadImageFormatException(SR.RowIdOrHeapOffsetTooLarge);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TableNotSorted(TableIndex tableIndex)
        {
            throw new BadImageFormatException(SR.Format(SR.MetadataTableNotSorted, (int)tableIndex));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TooManySubnamespaces()
        {
            throw new BadImageFormatException(SR.TooManySubnamespaces);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCast()
        {
            throw new InvalidCastException();
        }
    }
}
