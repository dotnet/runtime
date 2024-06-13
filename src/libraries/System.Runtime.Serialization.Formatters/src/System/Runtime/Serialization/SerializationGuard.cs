// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.Serialization
{
    internal sealed class SerializationGuard
    {
        private static readonly MethodInfo? s_startDeserialization =
            typeof(SerializationInfo).GetMethod(
                "StartDeserialization",
                BindingFlags.Public | BindingFlags.Static,
                Type.EmptyTypes);

        internal static IDisposable? StartDeserialization()
        {
            Debug.Assert(s_startDeserialization is not null);
            return (IDisposable?)s_startDeserialization.Invoke(null, null);
        }
    }
}
