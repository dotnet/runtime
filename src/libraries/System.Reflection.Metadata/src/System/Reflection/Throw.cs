// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // This file defines an internal class used to throw exceptions. The main purpose is to reduce code size.
    // Also it improves the likelihood that callers will be inlined.
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCast()
        {
            throw new InvalidCastException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidArgument(string message, string parameterName)
        {
            throw new ArgumentException(message, parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidArgument_OffsetForVirtualHeapHandle()
        {
            throw new ArgumentException(SR.CantGetOffsetForVirtualHeapHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception InvalidArgument_UnexpectedHandleKind(HandleKind kind)
        {
            throw new ArgumentException(SR.Format(SR.UnexpectedHandleKind, kind));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception InvalidArgument_Handle(string parameterName)
        {
            throw new ArgumentException(SR.InvalidHandle, parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SignatureNotVarArg()
        {
            throw new InvalidOperationException(SR.SignatureNotVarArg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ControlFlowBuilderNotAvailable()
        {
            throw new InvalidOperationException(SR.ControlFlowBuilderNotAvailable);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperationBuilderAlreadyLinked()
        {
            throw new InvalidOperationException(SR.BuilderAlreadyLinked);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_LabelNotMarked(int id)
        {
            throw new InvalidOperationException(SR.Format(SR.LabelNotMarked, id));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LabelDoesntBelongToBuilder(string parameterName)
        {
            throw new ArgumentException(SR.LabelDoesntBelongToBuilder, parameterName);
        }

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
        internal static void ArgumentNull(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentEmptyString(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyString, parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentEmptyArray(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyArray, parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BuilderArgumentNull()
        {
            throw new ArgumentNullException("builder");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(string parameterName, string message)
        {
            throw new ArgumentOutOfRangeException(parameterName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BlobTooLarge(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName, SR.BlobTooLarge);
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
        internal static void ValueArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("value");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OutOfBounds()
        {
            throw new BadImageFormatException(SR.OutOfBoundsRead);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WriteOutOfBounds()
        {
            throw new InvalidOperationException(SR.OutOfBoundsWrite);
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
            throw new BadImageFormatException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_TableNotSorted(TableIndex tableIndex)
        {
            throw new InvalidOperationException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_PEImageNotAvailable()
        {
            throw new InvalidOperationException(SR.PEImageNotAvailable);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TooManySubnamespaces()
        {
            throw new BadImageFormatException(SR.TooManySubnamespaces);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueOverflow()
        {
            throw new BadImageFormatException(SR.ValueTooLarge);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SequencePointValueOutOfRange()
        {
            throw new BadImageFormatException(SR.SequencePointValueOutOfRange);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HeapSizeLimitExceeded(HeapIndex heap)
        {
            throw new ImageFormatLimitationException(SR.Format(SR.HeapSizeLimitExceeded, heap));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PEReaderDisposed()
        {
            throw new ObjectDisposedException(nameof(PortableExecutable.PEReader));
        }
    }
}
