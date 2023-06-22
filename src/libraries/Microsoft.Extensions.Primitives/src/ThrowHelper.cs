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

#pragma warning disable CS8524 // "switch is not exhaustive". It actually is.
            return resource switch
            {
                ExceptionResource.Argument_InvalidOffsetLength => SR.Argument_InvalidOffsetLength,
                ExceptionResource.Argument_InvalidOffsetLengthStringSegment => SR.Argument_InvalidOffsetLengthStringSegment,
                ExceptionResource.Capacity_CannotChangeAfterWriteStarted => SR.Capacity_CannotChangeAfterWriteStarted,
                ExceptionResource.Capacity_NotEnough => SR.Capacity_NotEnough,
                ExceptionResource.Capacity_NotUsedEntirely => SR.Capacity_NotUsedEntirely,
            };
#pragma warning restore CS8524
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
