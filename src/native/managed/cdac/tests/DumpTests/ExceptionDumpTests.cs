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
    protected ExceptionDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "ExceptionState";

    [Fact]
    public void Exception_ThreadHasCurrentException()
    {
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        Assert.True(storeData.ThreadCount > 0);
    }

    [Fact]
    public void Exception_ContractIsAvailable()
    {
        IException exceptionContract = Target.Contracts.Exception;
        Assert.NotNull(exceptionContract);
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
