// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class FirefoxExecutionContext : ExecutionContext
{
    public string? ActorName { get; set; }
    public string? ThreadName { get; set; }
    public string? GlobalName { get; set; }
    public Task<Result>? LastDebuggerAgentBufferReceived { get; set; }

    public FirefoxExecutionContext(MonoSDBHelper sdbAgent, int id, string actorName) : base(sdbAgent, id, actorName, PauseOnExceptionsKind.Unset)
    {
        ActorName = actorName;
    }

    private int evaluateExpressionResultId;

    public int GetResultID()
    {
        return Interlocked.Increment(ref evaluateExpressionResultId);
    }
}
