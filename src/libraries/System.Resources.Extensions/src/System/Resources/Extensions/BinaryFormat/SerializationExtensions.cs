// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace System.Resources.Extensions.BinaryFormat;

internal static class SerializationExtensions
{
    /// <summary>
    ///  Converts the given exception to a <see cref="SerializationException"/> if needed, nesting the original exception
    ///  and assigning the original stack trace.
    /// </summary>
    public static SerializationException ConvertToSerializationException(this Exception ex)
        => ex is SerializationException serializationException
            ? serializationException
#if NETCOREAPP
            : (SerializationException)ExceptionDispatchInfo.SetRemoteStackTrace(
                new SerializationException(ex.Message, ex),
                ex.StackTrace ?? string.Empty);
#else
            : new SerializationException(ex.Message, ex);
#endif
}
