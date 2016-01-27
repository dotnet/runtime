// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if EVENTSOURCE_GENERICS
?using System;
using System.Reflection;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides support for casting enums to their underlying type
    /// from within generic context.
    /// </summary>
    /// <typeparam name="UnderlyingType">
    /// The underlying type of the enum.
    /// </typeparam>
    internal static class EnumHelper<UnderlyingType>
    {
        public static UnderlyingType Cast<ValueType>(ValueType value)
        {
            return (UnderlyingType)(object)value;
        }
    }

}
#endif
