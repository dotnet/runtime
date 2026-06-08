// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.TestInfrastructure.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for ISOSDacInterface APIs.
/// Uses the InterpreterStack debuggee dump to validate interpreter-specific behavior.
/// </summary>
public class ISOSDacInterfaceTests : DumpTestBase
{
    protected override string DebuggeeName => "InterpreterStack";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetCodeHeaderData_InterpreterMethod_ReturnsTypeInterpreter(TestConfiguration config)
    {
        InitializeDumpTest(config, "InterpreterStack", "full");

        try
        {
            Target.GetTypeInfo(DataType.InterpreterFrame);
        }
        catch (System.InvalidOperationException)
        {
            throw new Microsoft.DotNet.XUnitExtensions.SkipTestException("Interpreter support not available in this runtime build (FEATURE_INTERPRETER not enabled).");
        }

        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IPrecodeStubs precodeStubs = Target.Contracts.PrecodeStubs;
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        DumpTestStackWalker walker = DumpTestStackWalker.Walk(Target, crashingThread);

        ResolvedFrame interpFrame = walker.Frames
            .First(f => f.Name is "MethodA" or "MethodB" or "MethodC" or "MethodD");

        MethodDescHandle mdHandle = rts.GetMethodDescHandle(interpFrame.MethodDescPtr);
        TargetCodePointer precodeAddr = rts.GetNativeCode(mdHandle);
        Assert.NotEqual(TargetCodePointer.Null, precodeAddr);

        TargetCodePointer interpreterCodeAddr = precodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(precodeAddr);
        Assert.NotEqual(precodeAddr, interpreterCodeAddr);
        Assert.NotEqual(TargetCodePointer.Null, interpreterCodeAddr);

        DacpCodeHeaderData codeHeaderData;
        int hr = sosDac.GetCodeHeaderData(interpreterCodeAddr.ToClrDataAddress(Target), &codeHeaderData);
        AssertHResult(System.HResults.S_OK, hr);

        Assert.Equal(JitTypes.TYPE_INTERPRETER, codeHeaderData.JITType);
        Assert.Equal(interpFrame.MethodDescPtr.ToClrDataAddress(Target), codeHeaderData.MethodDescPtr);
        Assert.True(codeHeaderData.MethodSize > 0,
            $"Expected non-zero MethodSize for interpreter code, got {codeHeaderData.MethodSize}");
    }
}
