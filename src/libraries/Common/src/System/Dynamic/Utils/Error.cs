// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Dynamic.Utils
{
    /// <summary>
    ///    Strongly-typed and parameterized exception factory.
    /// </summary>
    internal static partial class Error
    {
        /// <summary>
        /// InvalidOperationException with message like "Enumeration has either not started or has already finished."
        /// </summary>
        internal static Exception EnumerationIsDone()
        {
            return new InvalidOperationException(Strings.EnumerationIsDone);
        }

        /// <summary>
        /// InvalidOperationException with message like "Collection was modified; enumeration operation may not execute."
        /// </summary>
        internal static Exception CollectionModifiedWhileEnumerating()
        {
            return new InvalidOperationException(Strings.CollectionModifiedWhileEnumerating);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} contains generic parameters"
        /// </summary>
        internal static Exception TypeContainsGenericParameters(object p0, string paramName)
        {
            return new ArgumentException(Strings.TypeContainsGenericParameters(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} contains generic parameters"
        /// </summary>
        internal static Exception TypeContainsGenericParameters(object p0, string paramName, int index)
        {
            return TypeContainsGenericParameters(p0, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} is a generic type definition"
        /// </summary>
        internal static Exception TypeIsGeneric(object p0, string paramName)
        {
            return new ArgumentException(Strings.TypeIsGeneric(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Type {0} is a generic type definition"
        /// </summary>
        internal static Exception TypeIsGeneric(object p0, string paramName, int index)
        {
            return TypeIsGeneric(p0, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments for constructor"
        /// </summary>
        internal static Exception IncorrectNumberOfConstructorArguments()
        {
            return new ArgumentException(Strings.IncorrectNumberOfConstructorArguments);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2, string paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2, string paramName, int index)
        {
            return ExpressionTypeDoesNotMatchMethodParameter(p0, p1, p2, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchParameter(object p0, object p1, string paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchParameter(p0, p1), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchParameter(object p0, object p1, string paramName, int index)
        {
            return ExpressionTypeDoesNotMatchParameter(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// InvalidOperationException with message like "Incorrect number of arguments supplied for lambda invocation"
        /// </summary>
        internal static Exception IncorrectNumberOfLambdaArguments()
        {
            return new InvalidOperationException(Strings.IncorrectNumberOfLambdaArguments);
        }

        /// <summary>
        /// ArgumentException with message like "Incorrect number of arguments supplied for call to method '{0}'"
        /// </summary>
        internal static Exception IncorrectNumberOfMethodCallArguments(object p0, string paramName)
        {
            return new ArgumentException(Strings.IncorrectNumberOfMethodCallArguments(p0), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1, string paramName)
        {
            return new ArgumentException(Strings.ExpressionTypeDoesNotMatchConstructorParameter(p0, p1), paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static Exception ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1, string paramName, int index)
        {
            return ExpressionTypeDoesNotMatchConstructorParameter(p0, p1, GetParamName(paramName, index));
        }

        /// <summary>
        /// ArgumentException with message like "Expression must be readable"
        /// </summary>
        internal static Exception ExpressionMustBeReadable(string paramName)
        {
            return new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
        }

        /// <summary>
        /// ArgumentException with message like "Expression must be readable"
        /// </summary>
        internal static Exception ExpressionMustBeReadable(string paramName, int index)
        {
            return ExpressionMustBeReadable(GetParamName(paramName, index));
        }

        private static string GetParamName(string paramName, int index)
        {
            if (index >= 0)
            {
                return $"{paramName}[{index}]";
            }

            return paramName;
        }
    }
}
