// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

[CollectionDefinition(nameof(GlobalTypeValidationCollection), DisableParallelization = true)]
public sealed class GlobalTypeValidationCollection;

[Collection(nameof(GlobalTypeValidationCollection))]
public sealed class GlobalTypeValidationTests
{
#if DEBUG
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GlobalReads_ValidateDescriptorType(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetGlobals(
            [
                ("numeric", (ulong?)1, (string?)null, "uint32"),
                ("pointer", (ulong?)2, (string?)null, "pointer"),
            ])
            .SetContracts([]);

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        using ThrowingTraceListener listener = new();

        Assert.Contains("declared as 'uint32'", Assert.Throws<DebugAssertException>(() => target.ReadGlobal<byte>("numeric")).Message);
        Assert.Contains("expected pointer", Assert.Throws<DebugAssertException>(() => target.ReadGlobalPointer("numeric")).Message);
        Assert.Contains("reading as UInt32", Assert.Throws<DebugAssertException>(() => target.ReadGlobal<uint>("pointer")).Message);
    }

    private sealed class ThrowingTraceListener : System.Diagnostics.TraceListener, IDisposable
    {
        private readonly System.Diagnostics.TraceListener[] _savedListeners;

        public ThrowingTraceListener()
        {
            _savedListeners = new System.Diagnostics.TraceListener[System.Diagnostics.Trace.Listeners.Count];
            System.Diagnostics.Trace.Listeners.CopyTo(_savedListeners, 0);
            System.Diagnostics.Trace.Listeners.Clear();
            System.Diagnostics.Trace.Listeners.Add(this);
        }

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
        }

        public override void Fail(string? message, string? detailMessage)
            => throw new DebugAssertException($"{message} {detailMessage}");

        public new void Dispose()
        {
            System.Diagnostics.Trace.Listeners.Clear();
            System.Diagnostics.Trace.Listeners.AddRange(_savedListeners);
            base.Dispose();
        }
    }

    private sealed class DebugAssertException(string message) : Exception(message);
#endif
}
