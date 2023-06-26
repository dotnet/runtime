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

        private static string GetArgumentName(ExceptionArgument argument)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
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
