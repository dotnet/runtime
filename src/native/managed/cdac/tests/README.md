# cDAC Tests

Unit tests for the cDAC data contract reader. Tests use mock memory to simulate
a target process without needing a real runtime.

## Building and running

```bash
export PATH="$(pwd)/.dotnet:$PATH"   # from repo root
dotnet build src/native/managed/cdac/tests
dotnet test src/native/managed/cdac/tests
```

To run a subset:
```bash
dotnet test src/native/managed/cdac/tests --filter "FullyQualifiedName~GCTests"
```

## Test layers

Tests can validate behavior at two layers:

- **Contract-level tests** (e.g., `GCTests.cs`): Call contract APIs like
  `IGC.GetHeapData()` directly. Use these to verify that contracts correctly
  read and interpret mock target memory.
- **SOSDacImpl-level tests** (e.g., `SOSDacInterface8Tests.cs`): Call through
  `ISOSDacInterface*` on `SOSDacImpl`. Use these to verify the full API surface
  including HResult protocols, pointer conversions, and buffer sizing.

When implementing a new `SOSDacImpl` method backed by an existing contract, write
tests at the SOSDacImpl level. When implementing a new contract, write tests at
the contract level.

## Architecture support

Tests run on all four architecture combinations using `[ClassData(typeof(MockTarget.StdArch))]`:
- 64-bit little-endian, 64-bit big-endian
- 32-bit little-endian, 32-bit big-endian

Be aware that `ClrDataAddress` values are **sign-extended** on 32-bit targets
(see `ConversionExtensions.ToClrDataAddress`). A value like `0xAA000000` becomes
`0xFFFFFFFF_AA000000` on 32-bit. Either use values below `0x80000000` or account
for sign extension in assertions.

## Creating a test target

Use `TestPlaceholderTarget.Builder` to construct a mock target. Extension methods
like `AddGCHeapWks` add subsystem-specific mock data, types, globals, and
contracts in a single call. Each extension accepts an `Action<>` to configure
only the data the test needs — everything else defaults to zero.

```csharp
// Contract-level test
Target target = new TestPlaceholderTarget.Builder(arch)
    .AddGCHeapWks(gc =>
    {
        gc.Generations = [gen0, gen1, gen2, gen3];
        gc.FillPointers = [0x1000, 0x2000, 0x3000];
    })
    .Build();
IGC gc = target.Contracts.GC;

// SOSDacImpl-level test
ISOSDacInterface8 dac8 = new SOSDacImpl(
    new TestPlaceholderTarget.Builder(arch)
        .AddGCHeapWks(gc =>
        {
            gc.Generations = generations;
            gc.FillPointers = fillPointers;
        })
        .Build(),
    legacyObj: null);

// Server GC — heap address returned via out parameter
ISOSDacInterface8 dac8 = new SOSDacImpl(
    new TestPlaceholderTarget.Builder(arch)
        .AddGCHeapSvr(gc =>
        {
            gc.Generations = generations;
            gc.FillPointers = fillPointers;
        }, out var heapAddr)
        .Build(),
    legacyObj: null);
```

The builder owns the `MockMemorySpace.Builder` internally, accumulates types
and globals from each `Add*` call, and wires up contracts automatically at
`Build()` time via `TestContractRegistry`.

## MockDescriptors

`MockDescriptors/` contains helpers that set up mock target memory for each
subsystem. The preferred pattern is an **extension method on
`TestPlaceholderTarget.Builder`** that takes an `Action<ConfigObject>` parameter:

1. A configuration class (e.g., `GCHeapBuilder`) accumulates test data via
   fluent `Set*()` methods.
2. The extension method allocates mock memory, registers types/globals/contracts
   directly on the target builder, and returns the builder for chaining.
3. Unset arrays default to zero-length or zero-initialized so tests only
   configure the data they care about.

See `MockDescriptors.GC.cs` for a complete example (`GCHeapBuilder` +
`GCHeapBuilderExtensions`).

### Key patterns

#### Composite (embedded) fields

When a type contains an embedded struct (not a pointer to it), you must specify
the size explicitly in the field definition:

```csharp
// Get the size of the embedded type first
uint allocContextSize = MockDescriptors.GetTypesForTypeFields(helpers, [GCAllocContextFields])
    [DataType.GCAllocContext].Size!.Value;

// Then reference it with an explicit size
new(nameof(Data.Generation.AllocationContext), DataType.GCAllocContext, allocContextSize)
```

Without the explicit size, the layout engine won't know how much space to reserve.

#### Embedded arrays vs pointer fields

Some fields are pointers to external data, others are the start of an inline array.
Check how the `Data.*` constructor reads the field:

- **Pointer**: `target.ReadPointer(address + offset)` → allocate separate memory,
  write a pointer to it.
- **Embedded/inline**: `address + offset` (no ReadPointer) → write array elements
  directly into the struct at that offset.

#### Global indirection levels

Globals have different indirection patterns depending on the subsystem.
Check how the contract reads the global:

- **`ReadGlobal<T>(name)`**: reads a primitive value directly from the global.
  The global value in the mock is the value itself.
- **`ReadGlobalPointer(name)`**: reads a pointer stored at the global address.
  The global value in the mock is the *address* of a memory fragment containing
  the actual pointer value.
- **Double indirection** (`ReadPointer(ReadGlobalPointer(name))`): the global
  points to memory that contains a pointer to the actual data.
