# Contract PrecodeStubs

This contract provides support for examining [precode](../coreclr/botr/method-descriptor.md#precode): small fragments of code used to implement temporary entry points and an efficient wrapper for stubs.

## APIs of contract

```csharp
    // Gets a pointer to the MethodDesc for a given stub entrypoint
    TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint);

    // Given an interior address within a precode stub and the kind of stub (StubPrecode or FixupPrecode),
    // computes the entry point of the precode.
    TargetPointer GetPrecodeEntryPointFromInteriorAddress(TargetCodePointer interiorAddress, bool isFixupPrecode);

    // If the code pointer is an interpreter precode, returns the actual interpreter
    // code address (ByteCodeAddr). Otherwise returns the original address unchanged.
    // Mirrors GetInterpreterCodeFromInterpreterPrecodeIfPresent in native code (precode.cpp).
    TargetCodePointer GetInterpreterCodeFromInterpreterPrecodeIfPresent(TargetCodePointer entryPoint);
```

## Version 3

<!-- BEGIN GENERATED: usage contract=PrecodeStubs version=c3 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `FixupPrecodeData` | `MethodDesc` | `pointer` | pointer to the MethodDesc associated with this fixup precode |
| `InterpByteCodeStart` | `Method` | `pointer` | pointer to the InterpMethod associated with the bytecode |
| `InterpMethod` | `MethodDesc` | `pointer` | pointer to the MethodDesc for the interpreted method |
| `InterpreterPrecodeData` | `ByteCodeAddr` | `pointer` | pointer to the InterpByteCodeStart for the interpreter bytecode |
| `PrecodeMachineDescriptor` | `DynamicHelperPrecodeType` | `uint8` | Precode type byte for a dynamic helper precode |
| `PrecodeMachineDescriptor` | `FixupBytes` | `uint8[]` | Assembly code of a FixupStub |
| `PrecodeMachineDescriptor` | `FixupIgnoredBytes` | `uint8[]` | Bytes to ignore when comparing FixupBytes to an actual block of memory in the target process. |
| `PrecodeMachineDescriptor` | `FixupStubPrecodeSize` | `uint8` | Byte size of FixupBytes and FixupIgnoredBytes |
| `PrecodeMachineDescriptor` | `InterpreterPrecodeType` | `uint8` | Precode type byte for an interpreter precode |
| `PrecodeMachineDescriptor` | `PInvokeImportPrecodeType` | `uint8` | Precode type byte for a P/Invoke import precode |
| `PrecodeMachineDescriptor` | `StubBytes` | `uint8[]` | Assembly code of a StubPrecode |
| `PrecodeMachineDescriptor` | `StubCodePageSize` | `uint32` | Size of a precode code page (in bytes) |
| `PrecodeMachineDescriptor` | `StubIgnoredBytes` | `uint8[]` | Bytes to ignore when comparing StubBytes to an actual block of memory in the target process. |
| `PrecodeMachineDescriptor` | `StubPrecodeSize` | `uint8` | Byte size of StubBytes and StubIgnoredBytes |
| `PrecodeMachineDescriptor` | `StubPrecodeType` | `uint8` | precode sort byte for stub precodes |
| `PrecodeMachineDescriptor` | `ThisPointerRetBufPrecodeType` | `uint8` | Precode type byte for a this-pointer return-buffer precode |
| `PrecodeMachineDescriptor` | `UMEntryPrecodeType` | `uint8` | Precode type byte for a UMEntry precode |
| `StubPrecodeData` | `SecretParam` | `pointer` | pointer to the MethodDesc associated with this stub precode or a second stub data pointer for other types |
| `StubPrecodeData` | `Type` | `uint8` | precise sort of stub precode |
| `ThisPtrRetBufPrecodeData` | `MethodDesc` | `pointer` | pointer to the MethodDesc associated with the ThisPtrRetBufPrecode |

### Global variables used

_None._

### Contracts used

| Contract Name |
| --- |
| `PlatformMetadata` |
<!-- END GENERATED: usage contract=PrecodeStubs version=c3 -->

The `CodePointerToInstrPointerMask` converts IP values that may include an arm Thumb bit
(for example, extracted from disassembling a call instruction or from a snapshot of the
registers) into an address. On other architectures applying the mask is a no-op.

### Determining the precode type
``` csharp
    private bool ReadBytesAndCompare(TargetPointer instrAddress, byte[] expectedBytePattern, byte[] bytesToIgnore)
    {
        byte[] localCopy = new byte[expectedBytePattern.Length];
        for (int i = 0; i < expectedBytePattern.Length; i++)
        {
            if (bytesToIgnore[i] == 0)
            {
                byte targetBytePattern = _target.Read<byte>(instrAddress + i);
                if (expectedBytePattern[i] != targetBytePattern)
                {
                    return false;
                }
            }
        }

        return true;
    }
    private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
    {
        KnownPrecodeType? basicPrecodeType = default;
        if (ReadBytesAndCompare(instrAddress, MachineDescriptor.StubBytes, MachineDescriptor.StubIgnoredBytes))
        {
            // get the actual type from the StubPrecodeData
            Data.StubPrecodeData stubPrecodeData = GetStubPrecodeData(instrAddress);
            byte exactPrecodeType = stubPrecodeData.Type;
            if (exactPrecodeType == 0)
                return null;

            if (exactPrecodeType == MachineDescriptor.StubPrecodeType)
            {
                return KnownPrecodeType.Stub;
            }
            else if (MachineDescriptor.PInvokeImportPrecodeType is byte compareByte1 && compareByte1 == exactPrecodeType)
            {
                return KnownPrecodeType.PInvokeImport;
            }
            else if (MachineDescriptor.ThisPointerRetBufPrecodeType is byte compareByte2 && compareByte2 == exactPrecodeType)
            {
                return KnownPrecodeType.ThisPtrRetBuf;
            }
            else if (MachineDescriptor.UMEntryPrecodeType is byte compareByte3 && compareByte3 == exactPrecodeType)
            {
                return KnownPrecodeType.UMEntry;
            }
            else if (MachineDescriptor.InterpreterPrecodeType is byte compareByte4 && compareByte4 == exactPrecodeType)
            {
                return KnownPrecodeType.Interpreter;
            }
            else if (MachineDescriptor.DynamicHelperPrecodeType is byte compareByte5 && compareByte5 == exactPrecodeType)
            {
                return KnownPrecodeType.DynamicHelper;
            }
        }
        else if (ReadBytesAndCompare(instrAddress, MachineDescriptor.FixupBytes, MachineDescriptor.FixupIgnoredBytes))
        {
            return KnownPrecodeType.Fixup;
        }
        return null;
    }
```

### `MethodDescFromStubAddress`

```csharp
    internal enum KnownPrecodeType
    {
        Stub = 1,
        PInvokeImport,
        Fixup,
        ThisPtrRetBuf,
        UMEntry,
        DynamicHelper,
        Interpreter
    }

    internal abstract class ValidPrecode
    {
        public TargetPointer InstrPointer { get; }
        public KnownPrecodeType PrecodeType { get; }

        protected ValidPrecode(TargetPointer instrPointer, KnownPrecodeType precodeType)
        {
            InstrPointer = instrPointer;
            PrecodeType = precodeType;
        }

        internal abstract TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);

    }

    internal class StubPrecode : ValidPrecode
    {
        internal StubPrecode(TargetPointer instrPointer, KnownPrecodeType type = KnownPrecodeType.Stub) : base(instrPointer, type) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer stubPrecodeDataAddress = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            if (ContractVersion(PrecodeStubs) == 1)
                return target.ReadPointer (stubPrecodeDataAddress + /* offset of StubPrecodeData.MethodDesc */ );
            else
                return target.ReadPointer (stubPrecodeDataAddress + /* offset of StubPrecodeData.SecretParam */ );
        }
    }

    internal sealed class PInvokeImportPrecode : StubPrecode
    {
        internal PInvokeImportPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.PInvokeImport) { }
    }

    internal sealed class FixupPrecode : ValidPrecode
    {
        internal FixupPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.Fixup) { }
        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer fixupPrecodeDataAddress = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            return target.ReadPointer (fixupPrecodeDataAddress + /* offset of FixupPrecodeData.MethodDesc */);
        }
    }

    internal sealed class ThisPtrRetBufPrecode : ValidPrecode
    {
        internal ThisPtrRetBufPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.ThisPtrRetBuf) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            if (ContractVersion(PrecodeStubs) == 1)
                throw new NotImplementedException(); // TODO(cdac)
            else
                return target.ReadPointer(target.ReadPointer (stubPrecodeDataAddress + /* offset of StubPrecodeData.SecretParam */ ) + /*offset of ThisPtrRetBufPrecodeData.MethodDesc*/);
        }
    }

    // Resolves MethodDesc for interpreter precodes by following
    // the InterpreterPrecodeData -> InterpByteCodeStart -> InterpMethod -> MethodDesc chain.
    internal sealed class InterpreterPrecode : ValidPrecode
    {
        internal InterpreterPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.Interpreter) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer dataAddr = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            Data.InterpreterPrecodeData precodeData = target.ProcessedData.GetOrAdd<Data.InterpreterPrecodeData>(dataAddr);
            Data.InterpByteCodeStart byteCodeStart = target.ProcessedData.GetOrAdd<Data.InterpByteCodeStart>(precodeData.ByteCodeAddr);
            Data.InterpMethod interpMethod = target.ProcessedData.GetOrAdd<Data.InterpMethod>(byteCodeStart.Method);
            return interpMethod.MethodDesc;
        }
    }

    internal TargetPointer CodePointerReadableInstrPointer(TargetCodePointer codePointer)
    {
        // Mask off the thumb bit, if we're on arm32, to get the actual instruction pointer
        ulong instrPointer = (ulong)codePointer.AsTargetPointer & MachineDescriptor.CodePointerToInstrPointerMask.Value;
        return new TargetPointer(instrPointer);
    }


    internal ValidPrecode GetPrecodeFromEntryPoint(TargetCodePointer entryPoint)
    {
        TargetPointer instrPointer = CodePointerReadableInstrPointer(entryPoint);
        if (IsAlignedInstrPointer(instrPointer) && TryGetKnownPrecodeType(instrPointer) is KnownPrecodeType precodeType)
        {
            switch (precodeType)
            {
                case KnownPrecodeType.Stub:
                    return new StubPrecode(instrPointer);
                case KnownPrecodeType.Fixup:
                    return new FixupPrecode(instrPointer);
                case KnownPrecodeType.PInvokeImport:
                    return new PInvokeImportPrecode(instrPointer);
                case KnownPrecodeType.ThisPtrRetBuf:
                    return new ThisPtrRetBufPrecode(instrPointer);
                case KnownPrecodeType.Interpreter:
                    return new InterpreterPrecode(instrPointer);
                default:
                    break;
            }
        }
        throw new InvalidOperationException($"Invalid precode type 0x{instrPointer:x16}");
    }

    TargetPointer IPrecodeStubs.GetMethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, MachineDescriptor);
    }

    // Returns the interpreter bytecode address if the entry point is an interpreter precode,
    // otherwise returns the original entry point unchanged.
    // This method never throws - on any failure, the original address is returned.
    TargetCodePointer IPrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(TargetCodePointer entryPoint)
    {
        try
        {
            TargetPointer instrPointer = CodePointerReadableInstrPointer(entryPoint);
            if (!IsAlignedInstrPointer(instrPointer))
                return entryPoint;

            if (TryGetKnownPrecodeType(instrPointer) is not KnownPrecodeType.Interpreter)
                return entryPoint;

            TargetPointer dataAddr = instrPointer + MachineDescriptor.StubCodePageSize;
            Data.InterpreterPrecodeData precodeData = // read InterpreterPrecodeData at dataAddr
            if (precodeData.ByteCodeAddr == TargetPointer.Null)
                return entryPoint;

            return new TargetCodePointer(precodeData.ByteCodeAddr);
        }
        catch
        {
            return entryPoint;
        }
    }
```

### `GetPrecodeEntryPointFromInteriorAddress`

Given an interior address within a precode stub and the kind of stub (StubPrecode or FixupPrecode),
computes the entry point of the precode.

```csharp
    TargetPointer IPrecodeStubs.GetPrecodeEntryPointFromInteriorAddress(TargetCodePointer interiorAddress, bool isFixupPrecode)
    {
        TargetPointer instrPointer = CodePointerReadableInstrPointer(interiorAddress);

        uint stubSize;
        if (isFixupPrecode)
        {
            stubSize = MachineDescriptor.FixupStubPrecodeSize;
        }
        else
        {
            stubSize = MachineDescriptor.StubPrecodeSize;
        }

        ulong pageMask = MachineDescriptor.StubCodePageSize - 1;
        ulong pageBase = instrPointer.Value & ~pageMask;
        ulong offset = instrPointer.Value - pageBase;
        ulong entryPointAddress = pageBase + (offset / stubSize) * stubSize;

        return new TargetPointer(entryPointAddress);
    }
```
