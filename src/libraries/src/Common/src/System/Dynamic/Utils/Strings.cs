// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Dynamic.Utils
{
    internal static partial class Strings
    {
        /// <summary>
        /// A string like "Invalid argument value"
        /// </summary>
        internal static string InvalidArgumentValue => SR.InvalidArgumentValue;

        /// <summary>
        /// A string like "Non-empty collection required"
        /// </summary>
        internal static string NonEmptyCollectionRequired => SR.NonEmptyCollectionRequired;

        /// <summary>
        /// A string like "The value null is not of type '{0}' and cannot be used in this collection."
        /// </summary>
        internal static string InvalidNullValue(object p0)
        {
            return SR.Format(SR.InvalidNullValue, p0);
        }

        /// <summary>
        /// A string like "The value '{0}' is not of type '{1}' and cannot be used in this collection."
        /// </summary>
        internal static string InvalidObjectType(object p0, object p1)
        {
            return SR.Format(SR.InvalidObjectType, p0, p1);
        }

        /// <summary>
        /// A string like "Type {0} contains generic parameters"
        /// </summary>
        internal static string TypeContainsGenericParameters(object p0)
        {
            return SR.Format(SR.TypeContainsGenericParameters, p0);
        }

        /// <summary>
        /// A string like "Type {0} is a generic type definition"
        /// </summary>
        internal static string TypeIsGeneric(object p0)
        {
            return SR.Format(SR.TypeIsGeneric, p0);
        }

        /// <summary>
        /// A string like "Collection was modified; enumeration operation may not execute."
        /// </summary>
        internal static string CollectionModifiedWhileEnumerating => SR.CollectionModifiedWhileEnumerating;

        /// <summary>
        /// A string like "Enumeration has either not started or has already finished."
        /// </summary>
        internal static string EnumerationIsDone => SR.EnumerationIsDone;

        /// <summary>
        /// A string like "Expression must be readable"
        /// </summary>
        internal static string ExpressionMustBeReadable => SR.ExpressionMustBeReadable;

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for parameter of type '{1}' of method '{2}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchMethodParameter, p0, p1, p2);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for parameter of type '{1}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchParameter(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchParameter, p0, p1);
        }

        /// <summary>
        /// A string like "Expression of type '{0}' cannot be used for constructor parameter of type '{1}'"
        /// </summary>
        internal static string ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1)
        {
            return SR.Format(SR.ExpressionTypeDoesNotMatchConstructorParameter, p0, p1);
        }
        /// <summary>
        /// A string like "Incorrect number of arguments supplied for call to method '{0}'"
        /// </summary>
        internal static string IncorrectNumberOfMethodCallArguments(object p0)
        {
            return SR.Format(SR.IncorrectNumberOfMethodCallArguments, p0);
        }

        /// <summary>
        /// A string like "Incorrect number of arguments supplied for lambda invocation"
        /// </summary>
        internal static string IncorrectNumberOfLambdaArguments => SR.IncorrectNumberOfLambdaArguments;

        /// <summary>
        /// A string like "Incorrect number of arguments for constructor"
        /// </summary>
        internal static string IncorrectNumberOfConstructorArguments => SR.IncorrectNumberOfConstructorArguments;
    }
}
