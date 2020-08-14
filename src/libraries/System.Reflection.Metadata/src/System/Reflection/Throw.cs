// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // This file defines an internal class used to throw exceptions. The main purpose is to reduce code size.
    // Also it improves the likelihood that callers will be inlined.
    internal static class Throw
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCast()
        {
            throw new InvalidCastException();
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidArgument(string message, string parameterName)
        {
            throw new ArgumentException(message, parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidArgument_OffsetForVirtualHeapHandle()
        {
            throw new ArgumentException(SR.CantGetOffsetForVirtualHeapHandle, "handle");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception InvalidArgument_UnexpectedHandleKind(HandleKind kind)
        {
            throw new ArgumentException(SR.Format(SR.UnexpectedHandleKind, kind));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception InvalidArgument_Handle(string parameterName)
        {
            throw new ArgumentException(SR.InvalidHandle, parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SignatureNotVarArg()
        {
            throw new InvalidOperationException(SR.SignatureNotVarArg);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ControlFlowBuilderNotAvailable()
        {
            throw new InvalidOperationException(SR.ControlFlowBuilderNotAvailable);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperationBuilderAlreadyLinked()
        {
            throw new InvalidOperationException(SR.BuilderAlreadyLinked);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_LabelNotMarked(int id)
        {
            throw new InvalidOperationException(SR.Format(SR.LabelNotMarked, id));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LabelDoesntBelongToBuilder(string parameterName)
        {
            throw new ArgumentException(SR.LabelDoesntBelongToBuilder, parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HeapHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataHeapHandle, "handle");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntityOrUserStringHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataTableOrUserStringHandle, "handle");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidToken()
        {
            throw new ArgumentException(SR.InvalidToken, "token");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentEmptyString(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyString, parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentEmptyArray(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyArray, parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BuilderArgumentNull()
        {
            throw new ArgumentNullException("builder");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(string parameterName, string message)
        {
            throw new ArgumentOutOfRangeException(parameterName, message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BlobTooLarge(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName, SR.BlobTooLarge);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TableIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("value");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OutOfBounds()
        {
            throw new BadImageFormatException(SR.OutOfBoundsRead);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WriteOutOfBounds()
        {
            throw new InvalidOperationException(SR.OutOfBoundsWrite);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCodedIndex()
        {
            throw new BadImageFormatException(SR.InvalidCodedIndex);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidHandle()
        {
            throw new BadImageFormatException(SR.InvalidHandle);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidCompressedInteger()
        {
            throw new BadImageFormatException(SR.InvalidCompressedInteger);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidSerializedString()
        {
            throw new BadImageFormatException(SR.InvalidSerializedString);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ImageTooSmall()
        {
            throw new BadImageFormatException(SR.ImageTooSmall);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
        {
            throw new BadImageFormatException(SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReferenceOverflow()
        {
            throw new BadImageFormatException(SR.RowIdOrHeapOffsetTooLarge);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TableNotSorted(TableIndex tableIndex)
        {
            throw new BadImageFormatException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_TableNotSorted(TableIndex tableIndex)
        {
            throw new InvalidOperationException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperation_PEImageNotAvailable()
        {
            throw new InvalidOperationException(SR.PEImageNotAvailable);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TooManySubnamespaces()
        {
            throw new BadImageFormatException(SR.TooManySubnamespaces);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ValueOverflow()
        {
            throw new BadImageFormatException(SR.ValueTooLarge);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SequencePointValueOutOfRange()
        {
            throw new BadImageFormatException(SR.SequencePointValueOutOfRange);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HeapSizeLimitExceeded(HeapIndex heap)
        {
            throw new ImageFormatLimitationException(SR.Format(SR.HeapSizeLimitExceeded, heap));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PEReaderDisposed()
        {
            throw new ObjectDisposedException(nameof(PortableExecutable.PEReader));
        }
    }
}
