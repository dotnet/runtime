// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Linq
{
    /// <summary>
    ///    Strongly-typed and parameterized string resources.
    /// </summary>
    internal static class Strings
    {
        internal static string ArgumentNotIEnumerableGeneric(string message)
        {
            return SR.Format(SR.ArgumentNotIEnumerableGeneric, message);
        }

        internal static string ArgumentNotValid(string message)
        {
            return SR.Format(SR.ArgumentNotValid, message);
        }

        internal static string UnhandledExpressionType(object message)
        {
            return SR.Format(SR.UnhandledExpressionType, message);
        }

        internal static string UnhandledBindingType(object message)
        {
            return SR.Format(SR.UnhandledBindingType, message);
        }

        internal static string NoMethodOnType(string name, object type)
        {
            return SR.Format(SR.NoMethodOnType, name, type);
        }

        internal static string NoMethodOnTypeMatchingArguments(string name, object type)
        {
            return SR.Format(SR.NoMethodOnTypeMatchingArguments, name, type);
        }
    }
}
