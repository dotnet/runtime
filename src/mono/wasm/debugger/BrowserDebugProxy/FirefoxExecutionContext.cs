// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.WebAssembly.Diagnostics;

internal class FirefoxExecutionContext : ExecutionContext
{
    internal string ActorName { get; set; }
    internal string ThreadName { get; set; }
    internal string GlobalName { get; set; }

    public FirefoxExecutionContext(MonoSDBHelper sdbAgent, int id, string actorName) : base(sdbAgent, id, actorName)
    {
        ActorName = actorName;
    }

    private int evaluateExpressionResultId;

    public int GetResultID()
    {
        return Interlocked.Increment(ref evaluateExpressionResultId);
    }
}
