// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq
{
    /// <summary>
    ///    Strongly-typed and parameterized string resources.
    /// </summary>
    internal static class Strings
    {
        internal static string ArgumentNotIEnumerableGeneric(string message) =>
            SR.Format(SR.ArgumentNotIEnumerableGeneric, message);

        internal static string ArgumentNotValid(string message) =>
            SR.Format(SR.ArgumentNotValid, message);

        internal static string NoMethodOnType(string name, object type) =>
            SR.Format(SR.NoMethodOnType, name, type);

        internal static string NoMethodOnTypeMatchingArguments(string name, object type) =>
            SR.Format(SR.NoMethodOnTypeMatchingArguments, name, type);

        internal static string EnumeratingNullEnumerableExpression() => SR.EnumeratingNullEnumerableExpression;
    }
}
