// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if EVENTSOURCE_GENERICS
?using System;
using System.Reflection;

namespace System.Diagnostics.Tracing
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
