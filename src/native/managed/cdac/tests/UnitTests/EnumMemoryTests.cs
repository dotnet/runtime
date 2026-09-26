// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Collector;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using MemoryRegionEmitter = Collector::Microsoft.Diagnostics.DataContractReader.EnumMemory.MemoryRegionEmitter;
using MemoryRegionEnumerator = Collector::Microsoft.Diagnostics.DataContractReader.EnumMemory.MemoryRegionEnumerator;
using MethodCollector = Collector::Microsoft.Diagnostics.DataContractReader.EnumMemory.MethodCollector;
using ObjectCollector = Collector::Microsoft.Diagnostics.DataContractReader.EnumMemory.ObjectCollector;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class EnumMemoryTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0x200, 3)]
    [InlineData(0x100000, 2)]
    [InlineData(0x100200, 3)]
    [InlineData(0x108000, 0)]
    [InlineData(0x108200, 3)]
    public void DumpMode_UsesNativeMiniDumpFlagPrecedence(uint miniDumpFlags, int expected)
    {
        Assert.Equal(expected, (int)MemoryRegionEnumerator.GetDumpFlags(miniDumpFlags));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   at Program.Main()", "   at Program.Main()")]
    [InlineData("   at Program.Main() in C:\\private\\Program.cs:line 42", "   at Program.Main()")]
    [InlineData("   at A.M() in /private/a.cs:line 1\n   at B.M() in /private/b.cs:line 2\n", "   at A.M()\n   at B.M()")]
    [InlineData("   at A.M() dans /private/a.cs:ligne 1\r\n   at B.M()\r\n", "   at A.M()\r\n   at B.M()")]
    [InlineData("   at A.M(F(Int32)) in secret.cs:line 1", "   at A.M(F(Int32))")]
    [InlineData("   at A.M(\uD83D\uDE00) in secret.cs:line 1", "   at A.M(\uD83D\uDE00)")]
    [InlineData("   at A.M()\n--- End of stack trace ---\n", "   at A.M()")]
    [InlineData("unstructured text", "")]
    public void TriageStackTrace_RemovesFileInfoAndZerosRemainingCharacters(string original, string expected)
    {
        char[] buffer = original.ToCharArray();
        ObjectCollector.StripFileInfoFromStackTrace(buffer);
        Assert.Equal(expected.PadRight(original.Length, '\0'), new string(buffer));
    }

    [Theory]
    [InlineData(false, false, false, true, true)]
    [InlineData(false, true, true, true, true)]
    [InlineData(true, false, false, true, true)]
    [InlineData(true, true, false, true, true)]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, true, true, true, false)]
    public void ExceptionCollection_UsesTriagePolicy(bool triage, bool derived, bool overridesStackTrace, bool littleEndian, bool supportsUpdates)
    {
        TargetPointer exception = new(0x1000);
        TargetPointer innerException = new(0x1100);
        TargetPointer message = new(0x2000);
        TargetPointer stackTrace = new(0x3000);
        TargetPointer remoteStackTrace = new(0x4000);
        TargetPointer exceptionTable = new(0x100);
        TargetPointer derivedTable = new(0x200);
        TargetPointer stringTable = new(0x300);
        TargetPointer objectTable = new(0x400);
        TargetPointer getter = new(0x500);
        const uint StringOffset = 12;
        const string Original = "   at Program.Main() in /private/Program.cs:line 42";
        const string Sanitized = "   at Program.Main()";
        Encoding encoding = littleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        byte[] stringBytes = encoding.GetBytes(Original);

        Mock<IObject> objects = new();
        objects.Setup(o => o.GetSize(It.IsAny<TargetPointer>())).Returns(128);
        objects.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(stringTable);
        objects.Setup(o => o.GetMethodTableAddress(exception)).Returns(derived ? derivedTable : exceptionTable);
        objects.Setup(o => o.GetMethodTableAddress(innerException)).Returns(exceptionTable);
        objects.Setup(o => o.GetStringValue(It.IsAny<TargetPointer>())).Returns(Original);
        uint stringLength = (uint)Original.Length;
        uint stringOffset = StringOffset;
        objects.Setup(o => o.GetStringData(It.IsAny<TargetPointer>(), out stringLength, out stringOffset));

        Mock<IRuntimeTypeSystem> types = new();
        types.Setup(t => t.GetTypeHandle(It.IsAny<TargetPointer>())).Returns((TargetPointer p) => new TestTypeHandle(p));
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.Exception)).Returns(exceptionTable);
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.String)).Returns(stringTable);
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.Object)).Returns(objectTable);
        types.Setup(t => t.GetParentMethodTable(It.Is<ITypeHandle>(t => t.Address == derivedTable))).Returns(exceptionTable);
        types.Setup(t => t.GetNumVtableSlots(It.Is<ITypeHandle>(t => t.Address == exceptionTable))).Returns(2);
        types.Setup(t => t.GetNumVtableSlots(It.Is<ITypeHandle>(t => t.Address == objectTable))).Returns(1);
        types.Setup(t => t.GetMethodDescForSlot(It.Is<ITypeHandle>(t => t.Address == exceptionTable), 1)).Returns(getter);
        types.Setup(t => t.GetMethodDescForSlot(It.Is<ITypeHandle>(t => t.Address == derivedTable), 1))
            .Returns(overridesStackTrace ? new TargetPointer(0x600) : getter);
        types.Setup(t => t.GetMethodDescHandle(getter)).Returns(new MethodDescHandle(getter));
        types.Setup(t => t.GetMethodToken(It.IsAny<MethodDescHandle>())).Returns(0x06000001);

        MetadataBuilder metadata = new();
        metadata.AddModule(0, metadata.GetOrAddString("TestModule"), default, default, default);
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Virtual, MethodImplAttributes.IL,
            metadata.GetOrAddString("get_StackTrace"), default, 0, default);
        BlobBuilder blob = new();
        new MetadataRootBuilder(metadata).Serialize(blob, 0, 0);
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
        Mock<IEcmaMetadata> ecmaMetadata = new();
        ecmaMetadata.Setup(m => m.GetMetadata(It.IsAny<Contracts.ModuleHandle>())).Returns(provider.GetMetadataReader());

        Mock<IException> exceptions = new();
        exceptions.Setup(e => e.GetExceptionData(exception))
            .Returns(new ExceptionData(message, innerException, TargetPointer.Null, TargetPointer.Null, stackTrace, remoteStackTrace, 0, 0));
        exceptions.Setup(e => e.GetExceptionData(innerException))
            .Returns(new ExceptionData(message, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, 0, 0));
        exceptions.Setup(e => e.GetExceptionStackFrames(It.IsAny<TargetPointer>())).Returns([]);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = littleEndian, Is64Bit = true })
            .AddMockContract(objects.Object)
            .AddMockContract(types.Object)
            .AddMockContract(exceptions.Object)
            .AddMockContract(ecmaMetadata.Object)
            .AddMockContract(new Mock<ILoader>().Object)
            .AddMockContract(new Mock<IFeatureFlags>().Object)
            .UseReader((ulong address, Span<byte> buffer) =>
            {
                Assert.True(address == stackTrace.Value + StringOffset || address == remoteStackTrace.Value + StringOffset);
                stringBytes.CopyTo(buffer);
                return 0;
            })
            .Build();

        using RecordingCallback callback = new(supportsUpdates);
        MemoryRegionEmitter emitter = new(callback.Address, 8);
        new ObjectCollector(target, emitter, new MethodCollector(target), triage).EnumerateObject(exception);

        Assert.Contains(exception.Value, callback.Regions);
        Assert.Contains(innerException.Value, callback.Regions);
        Assert.Equal(!triage, callback.Regions.Contains(message.Value));
        Assert.Contains(stackTrace.Value, callback.Regions);
        Assert.Equal(!triage || !overridesStackTrace, callback.Regions.Contains(remoteStackTrace.Value));
        if (triage && supportsUpdates)
        {
            byte[] expected = encoding.GetBytes(Sanitized.PadRight(Original.Length, '\0'));
            Assert.Equal(expected, callback.Updates[stackTrace.Value + StringOffset]);
            if (!overridesStackTrace)
                Assert.Equal(expected, callback.Updates[remoteStackTrace.Value + StringOffset]);
            Assert.Equal(overridesStackTrace ? 1 : 2, callback.Updates.Count);
        }
        else
        {
            Assert.Empty(callback.Updates);
        }
        Assert.Equal(0, emitter.Result);
    }

    private sealed record TestTypeHandle(TargetPointer Address) : ITypeHandle;

    private sealed class RecordingCallback : IDisposable
    {
        private readonly nint* _instance;
        private readonly nint* _vtable;
        private readonly GCHandle _handle;
        private readonly bool _supportsUpdates;
        public HashSet<ulong> Regions { get; } = [];
        public Dictionary<ulong, byte[]> Updates { get; } = [];
        public nint Address => (nint)_instance;

        public RecordingCallback(bool supportsUpdates)
        {
            _supportsUpdates = supportsUpdates;
            _handle = GCHandle.Alloc(this);
            _vtable = (nint*)NativeMemory.AllocZeroed(5, (nuint)sizeof(nint));
            _vtable[0] = (nint)(delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int>)&QueryInterface;
            _vtable[2] = (nint)(delegate* unmanaged[MemberFunction]<nint, uint>)&Release;
            _vtable[3] = (nint)(delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)&Enumerate;
            _vtable[4] = (nint)(delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int>)&Update;
            _instance = (nint*)NativeMemory.Alloc(2, (nuint)sizeof(nint));
            _instance[0] = (nint)_vtable;
            _instance[1] = GCHandle.ToIntPtr(_handle);
        }

        private static RecordingCallback Get(nint self) => (RecordingCallback)GCHandle.FromIntPtr(((nint*)self)[1]).Target!;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int QueryInterface(nint self, Guid* iid, nint* result)
        {
            *result = Get(self)._supportsUpdates && *iid == new Guid("3721A26F-8B91-4D98-A388-DB17B356FADB") ? self : 0;
            return *result != 0 ? 0 : unchecked((int)0x80004002);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint Release(nint self) => 1;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int Enumerate(nint self, ulong address, uint size)
        {
            Get(self).Regions.Add(address);
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int Update(nint self, ulong address, uint size, byte* bytes)
        {
            Get(self).Updates[address] = new ReadOnlySpan<byte>(bytes, checked((int)size)).ToArray();
            return 0;
        }

        public void Dispose()
        {
            NativeMemory.Free(_instance);
            NativeMemory.Free(_vtable);
            _handle.Free();
        }
    }
}
