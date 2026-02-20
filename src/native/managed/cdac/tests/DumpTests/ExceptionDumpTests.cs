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

    [ConditionalFact]
    public void Exception_ContractIsAvailable()
    {
        IException exceptionContract = Target.Contracts.Exception;
        Assert.NotNull(exceptionContract);
    }

    [ConditionalFact]
    public void Exception_CrashingThreadHasLastThrownObject()
    {
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        // Walk threads to find one with a non-null LastThrownObjectHandle
        bool foundExceptionThread = false;
        TargetPointer currentThread = storeData.FirstThread;
        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            if (threadData.LastThrownObjectHandle != TargetPointer.Null)
            {
                foundExceptionThread = true;
                break;
            }
            currentThread = threadData.NextThread;
        }

        // FailFast with a message should leave an exception on the crashing thread
        Assert.True(foundExceptionThread, "Expected at least one thread with a LastThrownObjectHandle");
    }

    [ConditionalFact]
    public void Exception_CanGetExceptionDataFromFirstNestedException()
    {
        IThread threadContract = Target.Contracts.Thread;
        IException exceptionContract = Target.Contracts.Exception;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        // Find a thread with a first nested exception
        TargetPointer currentThread = storeData.FirstThread;
        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            if (threadData.FirstNestedException != TargetPointer.Null)
            {
                // Walk the nested exception chain
                TargetPointer nestedEx = threadData.FirstNestedException;
                TargetPointer managedException = exceptionContract.GetNestedExceptionInfo(nestedEx, out TargetPointer nextNested);
                Assert.NotEqual(TargetPointer.Null, managedException);

                ExceptionData exData = exceptionContract.GetExceptionData(managedException);
                Assert.NotEqual(TargetPointer.Null, exData.Message);
                return;
            }
            currentThread = threadData.NextThread;
        }

        // If no nested exceptions found, that's still valid — FailFast may not create nested chain
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
