// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;

using Microsoft.Internal;

namespace System.Composition
{
    internal static class ExceptionBuilder
    {
        public static ArgumentException Argument_ExpressionMustBeNew(string parameterName)
        {
            return CreateArgumentException(Strings.Argument_ExpressionMustBeNew, parameterName);
        }

        public static ArgumentException Argument_ExpressionMustBePropertyMember(string parameterName)
        {
            return CreateArgumentException(Strings.Argument_ExpressionMustBePropertyMember, parameterName);
        }


        public static ArgumentException Argument_ExpressionMustBeVoidMethodWithNoArguments(string methodName)
        {
            return CreateArgumentException(Strings.Argument_ExpressionMustBeVoidMethodWithNoArguments, methodName);
        }

        private static ArgumentException CreateArgumentException(string message, string parameterName)
        {
            Assumes.NotNull(parameterName);

            return new ArgumentException(Format(message, parameterName), parameterName);
        }

        private static string Format(string format, params string[] arguments)
        {
            return String.Format(CultureInfo.CurrentCulture, format, arguments);
        }
    }
}
