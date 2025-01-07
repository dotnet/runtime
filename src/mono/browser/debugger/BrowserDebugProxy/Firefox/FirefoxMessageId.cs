// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public class FirefoxMessageId : MessageId
{
    public readonly string toId;

    public FirefoxMessageId(string? sessionId, int id, string toId) : base(sessionId, id)
    {
        this.toId = toId;
    }

    public static implicit operator SessionId(FirefoxMessageId id) => new SessionId(id.sessionId);

    public override string ToString() => $"msg-{sessionId}:::{id}:::{toId}";

    public override int GetHashCode() => (sessionId?.GetHashCode() ?? 0) ^ (toId?.GetHashCode() ?? 0) ^ id.GetHashCode();

    public override bool Equals(object obj) => (obj is FirefoxMessageId) ? ((FirefoxMessageId)obj).sessionId == sessionId && ((FirefoxMessageId)obj).id == id && ((FirefoxMessageId)obj).toId == toId : false;
}
