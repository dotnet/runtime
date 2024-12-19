// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SR = System.SR;

namespace System
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException_DestinationTooShort()
        {
            throw new ArgumentException(SR.Argument_DestinationTooShort, "destination");
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException_CannotExtractScalar(ExceptionArgument argument)
        {
            throw GetArgumentException(ExceptionResource.Argument_CannotExtractScalar, argument);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRange_IndexMustBeLessException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index,
                                                    ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
        }

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

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
        }

        private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
        }

        private static string GetArgumentName(ExceptionArgument argument)
        {
            switch (argument)
            {
                case ExceptionArgument.ch:
                    return nameof(ExceptionArgument.ch);
                case ExceptionArgument.culture:
                    return nameof(ExceptionArgument.culture);
                case ExceptionArgument.index:
                    return nameof(ExceptionArgument.index);
                case ExceptionArgument.input:
                    return nameof(ExceptionArgument.input);
                case ExceptionArgument.value:
                    return nameof(ExceptionArgument.value);
                default:
                    Debug.Fail("The enum value is not defined, please check the ExceptionArgument Enum.");
                    return "";

            };
        }

        private static string GetResourceString(ExceptionResource resource)
        {
            switch (resource)
            {
                case ExceptionResource.ArgumentOutOfRange_IndexMustBeLess:
                    return SR.ArgumentOutOfRange_IndexMustBeLess;
                case ExceptionResource.Argument_CannotExtractScalar:
                    return SR.Argument_CannotExtractScalar;
                default:
                    Debug.Fail("The enum value is not defined, please check the ExceptionResource Enum.");
                    return "";
            }
        }
    }

    //
    // The convention for this enum is using the argument name as the enum name
    //
    internal enum ExceptionArgument
    {
        ch,
        culture,
        index,
        input,
        value,
    }

    //
    // The convention for this enum is using the resource name as the enum name
    //
    internal enum ExceptionResource
    {
        Argument_CannotExtractScalar,
        ArgumentOutOfRange_IndexMustBeLess
    }
}
