// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal;

namespace System.Composition
{
    internal static class ExceptionBuilder
    {
        public static ArgumentException Argument_ExpressionMustBeNew(string parameterName)
        {
            return CreateArgumentException(SR.Argument_ExpressionMustBeNew, parameterName);
        }

        public static ArgumentException Argument_ExpressionMustBePropertyMember(string parameterName)
        {
            return CreateArgumentException(SR.Argument_ExpressionMustBePropertyMember, parameterName);
        }

        public static ArgumentException Argument_ExpressionMustBeVoidMethodWithNoArguments(string methodName)
        {
            return CreateArgumentException(SR.Argument_ExpressionMustBeVoidMethodWithNoArguments, methodName);
        }

        private static ArgumentException CreateArgumentException(string message, string parameterName)
        {
            Assumes.NotNull(parameterName);

            return new ArgumentException(SR.Format(message, parameterName), parameterName);
        }
    }
}
