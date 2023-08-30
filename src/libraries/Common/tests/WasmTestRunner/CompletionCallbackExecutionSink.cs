// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit;

internal class CompletionCallbackExecutionSink : global::Xunit.Sdk.LongLivedMarshalByRefObject, IExecutionSink
{
    private readonly Action<ExecutionSummary> _completionCallback;
    private readonly IExecutionSink _innerSink;

    public ExecutionSummary ExecutionSummary => _innerSink.ExecutionSummary;

    public ManualResetEvent Finished => _innerSink.Finished;

    public CompletionCallbackExecutionSink(IExecutionSink innerSink, Action<ExecutionSummary> completionCallback)
    {
        _innerSink = innerSink;
        _completionCallback = completionCallback;
    }

    public void Dispose() => _innerSink.Dispose();

    public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
    {
        var result = _innerSink.OnMessageWithTypes(message, messageTypes);
        message.Dispatch<ITestAssemblyFinished>(messageTypes, args => _completionCallback(ExecutionSummary));
        return result;
    }
}
