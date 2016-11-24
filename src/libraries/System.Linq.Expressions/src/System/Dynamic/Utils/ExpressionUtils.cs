// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading;

namespace System.Dynamic.Utils
{
    internal static partial class ExpressionUtils
    {
        /// <summary>
        /// See overload with <see cref="IArgumentProvider"/> for more information.
        /// </summary>
        public static ReadOnlyCollection<ParameterExpression> ReturnReadOnly(IParameterProvider provider, ref object collection)
        {
            ParameterExpression tObj = collection as ParameterExpression;
            if (tObj != null)
            {
                // otherwise make sure only one readonly collection ever gets exposed
                Interlocked.CompareExchange(
                    ref collection,
                    new ReadOnlyCollection<ParameterExpression>(new ListParameterProvider(provider, tObj)),
                    tObj
                );
            }

            // and return what is not guaranteed to be a readonly collection
            return (ReadOnlyCollection<ParameterExpression>)collection;
        }
    }
}
