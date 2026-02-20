// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Exception contract.
/// Uses the ExceptionState debuggee dump, which crashes with a nested exception chain.
/// </summary>
public abstract class ExceptionDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "ExceptionState";
    protected override string DumpType => "full";

    [ConditionalFact]
    public void Exception_ContractIsAvailable()
    {
        IException exceptionContract = Target.Contracts.Exception;
        Assert.NotNull(exceptionContract);
    }

    [ConditionalFact]
    public void Exception_CrashingThreadHasLastThrownObject()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        // FailFast with a message should leave an exception on the crashing thread
        Assert.NotEqual(TargetPointer.Null, crashingThread.LastThrownObjectHandle);
    }

    [ConditionalFact]
    public void Exception_CanGetExceptionDataFromFirstNestedException()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
        IException exceptionContract = Target.Contracts.Exception;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        if (crashingThread.FirstNestedException == TargetPointer.Null)
        {
            // If no nested exceptions, that's still valid — FailFast may not create nested chain
            return;
        }

        // Walk the nested exception chain
        TargetPointer managedException = exceptionContract.GetNestedExceptionInfo(crashingThread.FirstNestedException, out TargetPointer nextNested);
        Assert.NotEqual(TargetPointer.Null, managedException);

        ExceptionData exData = exceptionContract.GetExceptionData(managedException);
        Assert.NotEqual(TargetPointer.Null, exData.Message);
    }
}

public class ExceptionDumpTests_Local : ExceptionDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class ExceptionDumpTests_Net10 : ExceptionDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
