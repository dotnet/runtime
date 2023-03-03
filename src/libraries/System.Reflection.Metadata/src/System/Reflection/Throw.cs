// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection
{
    // This file defines an internal class used to throw exceptions. The main purpose is to reduce code size.
    // Also it improves the likelihood that callers will be inlined.
    internal static class Throw
    {
        [DoesNotReturn]
        internal static void InvalidCast()
        {
            throw new InvalidCastException();
        }

        [DoesNotReturn]
        internal static void InvalidArgument(string message, string parameterName)
        {
            throw new ArgumentException(message, parameterName);
        }

        [DoesNotReturn]
        internal static void InvalidArgument_OffsetForVirtualHeapHandle()
        {
            throw new ArgumentException(SR.CantGetOffsetForVirtualHeapHandle, "handle");
        }

        [DoesNotReturn]
        internal static Exception InvalidArgument_UnexpectedHandleKind(HandleKind kind)
        {
            throw new ArgumentException(SR.Format(SR.UnexpectedHandleKind, kind));
        }

        [DoesNotReturn]
        internal static Exception InvalidArgument_Handle(string parameterName)
        {
            throw new ArgumentException(SR.InvalidHandle, parameterName);
        }

        [DoesNotReturn]
        internal static void SignatureNotVarArg()
        {
            throw new InvalidOperationException(SR.SignatureNotVarArg);
        }

        [DoesNotReturn]
        internal static void ControlFlowBuilderNotAvailable()
        {
            throw new InvalidOperationException(SR.ControlFlowBuilderNotAvailable);
        }

        [DoesNotReturn]
        internal static void InvalidOperationBuilderAlreadyLinked()
        {
            throw new InvalidOperationException(SR.BuilderAlreadyLinked);
        }

        [DoesNotReturn]
        internal static void InvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }

        [DoesNotReturn]
        internal static void InvalidOperation_LabelNotMarked(int id)
        {
            throw new InvalidOperationException(SR.Format(SR.LabelNotMarked, id));
        }

        [DoesNotReturn]
        internal static void LabelDoesntBelongToBuilder(string parameterName)
        {
            throw new ArgumentException(SR.LabelDoesntBelongToBuilder, parameterName);
        }

        [DoesNotReturn]
        internal static void HeapHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataHeapHandle, "handle");
        }

        [DoesNotReturn]
        internal static void EntityOrUserStringHandleRequired()
        {
            throw new ArgumentException(SR.NotMetadataTableOrUserStringHandle, "handle");
        }

        [DoesNotReturn]
        internal static void InvalidToken()
        {
            throw new ArgumentException(SR.InvalidToken, "token");
        }

        [DoesNotReturn]
        internal static void ArgumentNull(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        [DoesNotReturn]
        internal static void ArgumentEmptyString(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyString, parameterName);
        }

        [DoesNotReturn]
        internal static void ArgumentEmptyArray(string parameterName)
        {
            throw new ArgumentException(SR.ExpectedNonEmptyArray, parameterName);
        }

        [DoesNotReturn]
        internal static void ValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }

        [DoesNotReturn]
        internal static void BuilderArgumentNull()
        {
            throw new ArgumentNullException("builder");
        }

        [DoesNotReturn]
        internal static void ArgumentOutOfRange(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        [DoesNotReturn]
        internal static void ArgumentOutOfRange(string parameterName, string message)
        {
            throw new ArgumentOutOfRangeException(parameterName, message);
        }

        [DoesNotReturn]
        internal static void BlobTooLarge(string parameterName)
        {
            throw new ArgumentOutOfRangeException(parameterName, SR.BlobTooLarge);
        }

        [DoesNotReturn]
        internal static void IndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        [DoesNotReturn]
        internal static void TableIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }

        [DoesNotReturn]
        internal static void ValueArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("value");
        }

        [DoesNotReturn]
        internal static void OutOfBounds()
        {
            throw new BadImageFormatException(SR.OutOfBoundsRead);
        }

        [DoesNotReturn]
        internal static void WriteOutOfBounds()
        {
            throw new InvalidOperationException(SR.OutOfBoundsWrite);
        }

        [DoesNotReturn]
        internal static void InvalidCodedIndex()
        {
            throw new BadImageFormatException(SR.InvalidCodedIndex);
        }

        [DoesNotReturn]
        internal static void InvalidHandle()
        {
            throw new BadImageFormatException(SR.InvalidHandle);
        }

        [DoesNotReturn]
        internal static void InvalidCompressedInteger()
        {
            throw new BadImageFormatException(SR.InvalidCompressedInteger);
        }

        [DoesNotReturn]
        internal static void InvalidSerializedString()
        {
            throw new BadImageFormatException(SR.InvalidSerializedString);
        }

        [DoesNotReturn]
        internal static void ImageTooSmall()
        {
            throw new BadImageFormatException(SR.ImageTooSmall);
        }

        [DoesNotReturn]
        internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
        {
            throw new BadImageFormatException(SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
        }

        [DoesNotReturn]
        internal static void ReferenceOverflow()
        {
            throw new BadImageFormatException(SR.RowIdOrHeapOffsetTooLarge);
        }

        [DoesNotReturn]
        internal static void TableNotSorted(TableIndex tableIndex)
        {
            throw new BadImageFormatException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [DoesNotReturn]
        internal static void InvalidOperation_TableNotSorted(TableIndex tableIndex)
        {
            throw new InvalidOperationException(SR.Format(SR.MetadataTableNotSorted, tableIndex));
        }

        [DoesNotReturn]
        internal static void InvalidOperation_PEImageNotAvailable()
        {
            throw new InvalidOperationException(SR.PEImageNotAvailable);
        }

        [DoesNotReturn]
        internal static void TooManySubnamespaces()
        {
            throw new BadImageFormatException(SR.TooManySubnamespaces);
        }

        [DoesNotReturn]
        internal static void ValueOverflow()
        {
            throw new BadImageFormatException(SR.ValueTooLarge);
        }

        [DoesNotReturn]
        internal static void SequencePointValueOutOfRange()
        {
            throw new BadImageFormatException(SR.SequencePointValueOutOfRange);
        }

        [DoesNotReturn]
        internal static void HeapSizeLimitExceeded(HeapIndex heap)
        {
            throw new ImageFormatLimitationException(SR.Format(SR.HeapSizeLimitExceeded, heap));
        }

        [DoesNotReturn]
        internal static void PEReaderDisposed()
        {
            throw new ObjectDisposedException(nameof(PortableExecutable.PEReader));
        }
    }
}
