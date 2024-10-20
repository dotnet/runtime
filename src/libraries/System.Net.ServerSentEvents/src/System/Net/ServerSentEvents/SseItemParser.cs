// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.ServerSentEvents
{
    /// <summary>Encapsulates a method for parsing the bytes payload of a server-sent event.</summary>
    /// <typeparam name="T">Specifies the type of the return value of the parser.</typeparam>
    /// <param name="eventType">The event's type.</param>
    /// <param name="data">The event's payload bytes.</param>
    /// <returns>The parsed <typeparamref name="T"/>.</returns>
    public delegate T SseItemParser<out T>(string eventType, ReadOnlySpan<byte> data);
}
