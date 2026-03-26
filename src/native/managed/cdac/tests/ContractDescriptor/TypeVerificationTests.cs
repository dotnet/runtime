// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Data;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

/// <summary>
/// Verifies that the managed Data class field reads are type-compatible with the
/// declared types in the native data descriptor. Loads a real contract descriptor
/// JSON (generated from a checked build) and instantiates Data types against a
/// noop Target to trigger Debug.Assert type checks in TargetFieldExtensions.
/// </summary>
public class TypeVerificationTests
{
    [Fact]
    public void VerifyFieldTypes_CheckedDescriptor()
    {
        string descriptorPath = Path.Combine(
            Path.GetDirectoryName(typeof(TypeVerificationTests).Assembly.Location)!,
            "checked-descriptor.json");

        if (!File.Exists(descriptorPath))
            return;

        byte[] descriptorBytes = File.ReadAllBytes(descriptorPath);
        ContractDescriptorParser.ContractDescriptor? descriptor =
            ContractDescriptorParser.ParseCompact(descriptorBytes);

        Assert.NotNull(descriptor);

        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            descriptor,
            globalPointerValues: [],
            readFromTarget: static (address, buffer) => { buffer.Clear(); return 0; },
            writeToTarget: static (address, buffer) => 0,
            getThreadContext: static (threadId, contextFlags, buffer) => -1,
            isLittleEndian: BitConverter.IsLittleEndian,
            pointerSize: IntPtr.Size,
            additionalFactories: []);

        // Verify type info is present
        Target.TypeInfo threadType = target.GetTypeInfo(DataType.Thread);
        Assert.True(threadType.Fields.Count > 0);
        Assert.Equal("uint32", threadType.Fields["Id"].TypeName);

        // Install a trace listener that throws on Debug.Assert failures
        using ThrowingTraceListener listener = new();

        // Verify the listener catches Debug.Assert
        Assert.Throws<System.Exception>(() => System.Diagnostics.Debug.Assert(false, "test"));

        // Instantiate Data types — the ThrowingTraceListener will convert any
        // Debug.Assert type mismatch failures into exceptions that fail the test.
        TryCreate<Microsoft.Diagnostics.DataContractReader.Data.Thread>(target);
        TryCreate<ThreadStore>(target);
        TryCreate<MethodTable>(target);
        TryCreate<MethodDesc>(target);
        TryCreate<EEClass>(target);
        TryCreate<Module>(target);
        TryCreate<Assembly>(target);
        TryCreate<FieldDesc>(target);
        TryCreate<SyncBlock>(target);
        TryCreate<GCAllocContext>(target);
        TryCreate<ThreadStore>(target);
        TryCreate<MethodTable>(target);
        TryCreate<MethodDesc>(target);
        TryCreate<EEClass>(target);
        TryCreate<Module>(target);
        TryCreate<Assembly>(target);
        TryCreate<FieldDesc>(target);
        TryCreate<SyncBlock>(target);
        TryCreate<GCAllocContext>(target);
    }

    private static void CreateData<T>(Target target) where T : IData<T>
    {
        T.Create(target, default);
    }

    private static void TryCreate<T>(Target target) where T : IData<T>
    {
        try
        {
            T.Create(target, default);
        }
        catch (InvalidOperationException) { }
        catch (System.Collections.Generic.KeyNotFoundException) { }
    }

    /// <summary>
    /// A trace listener that converts Debug.Assert / Debug.Fail calls to exceptions.
    /// Replaces the default trace listener to prevent process termination on assert failure.
    /// </summary>
    private sealed class ThrowingTraceListener : TraceListener, IDisposable
    {
        private readonly TraceListener[] _savedListeners;

        public ThrowingTraceListener()
        {
            // Save and remove all existing listeners (including DefaultTraceListener
            // which would terminate the process on assert failure)
            _savedListeners = new TraceListener[Trace.Listeners.Count];
            Trace.Listeners.CopyTo(_savedListeners, 0);
            Trace.Listeners.Clear();
            Trace.Listeners.Add(this);
        }

        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }

        public override void Fail(string? message, string? detailMessage)
        {
            throw new System.Exception($"Debug.Assert failed: {message} {detailMessage}");
        }

        void IDisposable.Dispose()
        {
            Trace.Listeners.Remove(this);
            Trace.Listeners.AddRange(_savedListeners);
        }
    }
}
