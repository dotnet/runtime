// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Primitives
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowArgumentNullException(ExceptionArgument argument)
        {
            throw new ArgumentNullException(GetArgumentName(argument));
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException(ExceptionResource resource)
        {
            throw new ArgumentException(GetResourceText(resource));
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException(ExceptionResource resource)
        {
            throw new InvalidOperationException(GetResourceText(resource));
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException(ExceptionResource resource, params object[] args)
        {
            string message = string.Format(GetResourceText(resource), args);

            throw new InvalidOperationException(message);
        }

        internal static ArgumentNullException GetArgumentNullException(ExceptionArgument argument)
        {
            return new ArgumentNullException(GetArgumentName(argument));
        }

        internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        internal static ArgumentException GetArgumentException(ExceptionResource resource)
        {
            return new ArgumentException(GetResourceText(resource));
        }

        private static string GetResourceText(ExceptionResource resource)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionResource), resource),
                "The enum value is not defined, please check the ExceptionResource Enum.");

            switch (resource)
            {
                case ExceptionResource.Argument_InvalidOffsetLength: return SR.Argument_InvalidOffsetLength;
                case ExceptionResource.Argument_InvalidOffsetLengthStringSegment: return SR.Argument_InvalidOffsetLengthStringSegment;
                case ExceptionResource.Capacity_CannotChangeAfterWriteStarted: return SR.Capacity_CannotChangeAfterWriteStarted;
                case ExceptionResource.Capacity_NotEnough: return SR.Capacity_NotEnough;
                case ExceptionResource.Capacity_NotUsedEntirely: return SR.Capacity_NotUsedEntirely;
                default:
                    Debug.Fail($"Unexpected resource {resource}");
                    return "";
            }
        }

        private static string GetArgumentString(ExceptionArgument argument)
        {
            switch (argument)
            {
                case ExceptionArgument.buffer: return nameof(ExceptionArgument.buffer);
                case ExceptionArgument.offset: return nameof(ExceptionArgument.offset);
                case ExceptionArgument.length: return nameof(ExceptionArgument.length);
                case ExceptionArgument.text: return nameof(ExceptionArgument.text);
                case ExceptionArgument.start: return nameof(ExceptionArgument.start);
                case ExceptionArgument.count: return nameof(ExceptionArgument.count);
                case ExceptionArgument.index: return nameof(ExceptionArgument.index);
                case ExceptionArgument.value: return nameof(ExceptionArgument.value);
                case ExceptionArgument.capacity: return nameof(ExceptionArgument.capacity);
                case ExceptionArgument.separators: return nameof(ExceptionArgument.separators);
                case ExceptionArgument.comparisonType: return nameof(ExceptionArgument.comparisonType);
                case ExceptionArgument.changeTokens: return nameof(ExceptionArgument.changeTokens);
                case ExceptionArgument.changeTokenProducer: return nameof(ExceptionArgument.changeTokenProducer);
                case ExceptionArgument.changeTokenConsumer: return nameof(ExceptionArgument.changeTokenConsumer);
                case ExceptionArgument.array: return nameof(ExceptionArgument.array);
                default:
                    Debug.Fail("The ExceptionArgument value is not defined.");
                    return string.Empty;
            }
        }
    }

    internal enum ExceptionArgument
    {
        buffer,
        offset,
        length,
        text,
        start,
        count,
        index,
        value,
        capacity,
        separators,
        comparisonType,
        changeTokens,
        changeTokenProducer,
        changeTokenConsumer,
        array,
    }

    internal enum ExceptionResource
    {
        Argument_InvalidOffsetLength,
        Argument_InvalidOffsetLengthStringSegment,
        Capacity_CannotChangeAfterWriteStarted,
        Capacity_NotEnough,
        Capacity_NotUsedEntirely
    }
}
