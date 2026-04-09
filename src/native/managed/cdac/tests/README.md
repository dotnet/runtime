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

## Preferred unit test shape

The preferred shape for these tests is:

1. Use a helper that creates the contract interface or legacy interface under
   test.
2. Pass that helper a callback that configures the backing state through a
   builder or other focused setup object.
3. In the test body, call APIs on the interface returned by the helper and
   assert on the results.

An example:

```csharp
[Theory]
[ClassData(typeof(MockTarget.StdArch))]
public void GetSimpleComCallWrapperData_ReturnsRefCountMasked(MockTarget.Architecture arch)
{
    ulong wrapperAddress = 0;
    ulong expectedRefCount = 0x1234_5678;

    IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
    {
        MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
        MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
        simpleWrapper.RefCount = expectedRefCount;
        simpleWrapper.MainWrapper = wrapper.Address;
        wrapper.SimpleWrapper = simpleWrapper.Address;
        wrapper.Next = ulong.MaxValue;
        wrapperAddress = wrapper.Address;
    });

    SimpleComCallWrapperData result =
        contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));

    Assert.Equal(expectedRefCount, result.RefCount);
}
```

Tests run on all four architecture combinations using `[ClassData(typeof(MockTarget.StdArch))]`:
- 64-bit little-endian, 64-bit big-endian
- 32-bit little-endian, 32-bit big-endian

On 32-bit targets, `ClrDataAddress` values are sign-extended. When writing
SOSDacImpl-level tests or asserting on address values, prefer addresses below
`0x8000_0000` or account for the sign extension in the expected value.
## Implementing the helper that returns the contract

Helpers should hide target construction and return the contract or legacy
interface that the test will call. The `BuiltInCOM` example shows a good shape
for these helpers:

1. Accept the target architecture and a callback that configures backing state.
2. Create the low-level memory builder and any subsystem-specific builder.
3. Let the callback populate the backing state.
4. Build a `TestPlaceholderTarget` from the builder's memory reader, types, and
   globals.
5. Register the contract(s) the test needs.
6. Return the interface under test.

Keep the helper focused on one subsystem and one main interface surface. If a
test needs additional collaborating contracts, accept them as optional
parameters or wire them up inside the helper in one place.

```csharp
private static IBuiltInCOM CreateBuiltInCOM(
    MockTarget.Architecture arch,
    Action<MockBuiltInComBuilder> configure,
    ISyncBlock? syncBlock = null)
{
    TargetTestHelpers helpers = new(arch);
    MockMemorySpace.Builder builder = new(helpers);
    MockMemorySpace.BumpAllocator allocator =
        builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd, minAlign: 16);
    MockBuiltInComBuilder builtInCom = new(builder, allocator, arch);

    configure(builtInCom);

    var target = new TestPlaceholderTarget(
        arch,
        builder.GetMemoryContext().ReadFromTarget,
        CreateContractTypes(builtInCom),
        CreateContractGlobals(builtInCom));

    target.SetContracts(Mock.Of<ContractRegistry>(
        c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 1)
          && c.SyncBlock == (syncBlock ?? Mock.Of<ISyncBlock>())));

    return target.Contracts.BuiltInCOM;
}
```

## Mocking target memory

`MockDescriptors/` contains helpers that set up mock target memory for each
subsystem.

### Sub-system builder

For structure-heavy subsystems, prefer a subsystem-specific builder in the
style of `MockBuiltInComBuilder`. That builder should use the shared mock
memory builder and allocator to populate subsystem state, while also exposing
reusable `Layout<TView>` instances and any subsystem globals or constants that
the contract helper needs when it creates the `TestPlaceholderTarget`.

The builder should expose:

- allocation helpers that return named wrappers such as `AddComCallWrapper()`
  or `AddSimpleComCallWrapper()`
- layout properties that the contract helper can convert into `Target.TypeInfo`
- global properties that the contract helper can surface through the target

```csharp
internal sealed class MockBuiltInComBuilder
{
    internal Layout<MockSimpleComCallWrapper> SimpleComCallWrapperLayout { get; }
    internal Layout<MockComCallWrapper> ComCallWrapperLayout { get; }
    internal Layout<MockComMethodTable> ComMethodTableLayout { get; }

    public ulong TearOffAddRefGlobalAddress { get; }
    public ulong TearOffAddRefSimpleGlobalAddress { get; }

    public MockSimpleComCallWrapper AddSimpleComCallWrapper()
        => SimpleComCallWrapperLayout.Create(
            AllocateAndAdd((ulong)SimpleComCallWrapperLayout.Size, "SimpleComCallWrapper"));

    public MockComCallWrapper AddComCallWrapper()
        => ComCallWrapperLayout.Create(
            AllocateAndAdd((ulong)ComCallWrapperLayout.Size, "ComCallWrapper"));
}
```

This lets setup code compose structures through named operations, while keeping
type/global registration close to the same builder. Because the underlying
memory builder and allocator can be shared, multiple subsystem builders can
participate in a more complex mock target when needed.

When one subsystem needs to materialize another subsystem's structures, prefer
builder-to-builder composition over re-implementing the same heap layout logic.
For example, if an object-focused helper needs `SyncBlock`-shaped memory, let
the object builder depend on `MockSyncBlockBuilder` for that portion of the
setup and keep object-specific work limited to object headers, globals, and
relationships such as sync-table entries. This keeps the canonical memory shape
for each structure in one place.

Sub-system builders, `Layout<TView>` definitions, and `TypedView` types should
not take direct dependencies on cDAC assembly types. Keep them expressed in
terms of mock-memory concepts and primitive layout information so they can be
extracted into a separate assembly later if needed.

In practice, that means builders should not expose or cache cDAC-facing types
such as `Dictionary<DataType, Target.TypeInfo>`, `TargetPointer`, or other cDAC
wrapper types. Keep layout ownership in the builder, but perform the
`DataType`/`Target.TypeInfo` translation in the contract helper or test layer
that constructs the `TestPlaceholderTarget`, and use primitive values such as
`ulong` for target pointers on builder surfaces.

Apply the same rule to globals: builders should expose one property per global
value they own, and the helper/test layer should translate those properties into
the `(string Name, ulong Value)` globals table passed to `TestPlaceholderTarget`.

#### Keep low-level mock memory setup out of tests

Prefer reusable helpers that let tests describe the state they care about in
terms of named structures, properties, and relationships. When a test needs
exact memory control, concentrate the low-level details in the helper callback,
builder, or typed wrapper rather than repeating `Slice(...)`, offset
arithmetic, and pointer writes in each test body.

#### TypedViews for mock structures

For tests that need to build up native-looking structures in mock memory, use
the `Layout` / `TypedView` helpers inside the helper or builder layer when the
memory shape is reused or the wrapper makes the setup clearer. These helpers
are useful when a mock structure has:

- named fields with target-dependent pointer sizes
- embedded inline arrays
- a header followed by variable-sized trailing data

The usual pattern is:

1. Add a static `CreateLayout` method on the `TypedView` type that defines the
   layout once for that structure.
2. Follow it with typed property accessors that read and write named fields
   through `ReadPointerField`, `WriteUInt32Field`, and similar helpers.
3. Allocate mock memory for that layout (plus any trailing data, if needed).
4. Create a typed wrapper and populate fields through properties, collection
   wrappers, or indexed accessors.
5. Prefer APIs that return the typed wrapper itself rather than just its address
   when callers will continue populating or inspecting that structure.
6. Return the typed wrapper from the helper or builder when setup code needs to
   connect multiple structures together.

```csharp
internal sealed class MockComMethodTable : TypedView
{
    private const string FlagsFieldName = "Flags";
    private const string MethodTableFieldName = "MethodTable";

    public static Layout<MockComMethodTable> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ComMethodTable", architecture)
            .AddPointerField(FlagsFieldName)
            .AddPointerField(MethodTableFieldName)
            .Build<MockComMethodTable>();

    public ulong Flags
    {
        get => ReadPointerField(FlagsFieldName);
        set => WritePointerField(FlagsFieldName, value);
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }
}
```

Prefer adding typed accessors when multiple tests need to interpret the same
memory shape. This keeps helper code focused on the contract behavior being
exercised instead of repeating offset arithmetic. Fall back to direct span or
offset writes for one-off cases where a wrapper would add indirection without
improving readability.

#### Composite (embedded) fields

When a layout contains an embedded struct (not a pointer to it), reserve space
for the embedded value explicitly in the layout:

```csharp
Layout<MockAllocContext> allocContextLayout =
    MockAllocContext.CreateLayout(architecture);

Layout<MockGeneration> generationLayout =
    new SequentialLayoutBuilder("Generation", architecture)
        .AddField("AllocationContext", allocContextLayout.Size)
        .AddPointerField("StartSegment")
        .Build<MockGeneration>();
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
