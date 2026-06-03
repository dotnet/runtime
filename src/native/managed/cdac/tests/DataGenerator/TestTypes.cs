// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data.GeneratorTests;

// Test-only IData<T> classes that exercise each codegen path the IData source
// generator can take. The DataGenerator analyzer is attached to this test
// project so ctors / Create factories / Write{Name} methods are generated
// alongside production code.

// 1. Single-source native baseline.
[CdacType("TestNative")]
internal sealed partial class TestNative : IData<TestNative>
{
    [Field] public uint A { get; }
    [Field] public TargetPointer B { get; }
}

// 2. Managed-only baseline (ManagedTypeSource pre-adjusts offsets for both
//    reference and value types, so the generated code is identical).
[CdacType("Test.Managed.Ref")]
internal sealed partial class TestManagedRef : IData<TestManagedRef>
{
    [Field("_a")] public uint A { get; }
}

// 3. Cross-source -- same field names used in both native and managed
//    descriptors. Tests source priority (native first, managed fallback).
[CdacType("TestCross", "Test.Cross")]
internal sealed partial class TestCross : IData<TestCross>
{
    [Field] public uint A { get; }
    [Field] public TargetPointer B { get; }
}

// 4. Native renamed across runtime versions. The unified Names list
//    contains both candidates; the cascade tries each against the native
//    descriptor (managed is absent for a single-source class).
[CdacType("TestNativeAlias")]
internal sealed partial class TestNativeAlias : IData<TestNativeAlias>
{
    [Field("A", "A_old")] public uint A { get; }
}

// 5. Managed renamed across BCL versions.
[CdacType("Test.ManagedAlias")]
internal sealed partial class TestManagedAlias : IData<TestManagedAlias>
{
    [Field("_a", "_a_old")] public uint A { get; }
}

// 6. Cross-source with multiple candidate names spanning both sides.
//    Tests both name fallback AND source priority together.
[CdacType("TestCrossAlias", "Test.CrossAlias")]
internal sealed partial class TestCrossAlias : IData<TestCrossAlias>
{
    [Field("A", "A_old", "_a", "_a_old")] public uint A { get; }
}

// 7. Writable, native-only.
[CdacType("TestWriteNative")]
internal sealed partial class TestWriteNative : IData<TestWriteNative>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
}

// 8. Writable, managed-only.
[CdacType("Test.WriteManaged")]
internal sealed partial class TestWriteManaged : IData<TestWriteManaged>
{
    [Field("_flags", Writable = true)] public uint Flags { get; private set; }
}

// 9. Writable, cross-source (write must hit whichever side resolved).
[CdacType("TestWriteCross", "Test.WriteCross")]
internal sealed partial class TestWriteCross : IData<TestWriteCross>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
}

// 10. Optional nullable (T? on a value type).
[CdacType("TestOptional")]
internal sealed partial class TestOptional : IData<TestOptional>
{
    [Field] public uint Required { get; }
    [Field] public uint? Optional { get; }
}

// 11. FieldAddress under cross-source fallback.
[CdacType("TestFieldAddr", "Test.FieldAddr")]
internal sealed partial class TestFieldAddr : IData<TestFieldAddr>
{
    [Field]      public uint A { get; }
    [FieldAddress("Anchor")] public TargetPointer AnchorAddress { get; }
}

// 11b. Optional nullable FieldAddress (TargetPointer?). The descriptor is
// allowed to omit the field; absent => null, present => base + offset.
[CdacType("TestOptionalFieldAddr")]
internal sealed partial class TestOptionalFieldAddr : IData<TestOptionalFieldAddr>
{
    [Field]        public uint Required { get; }
    [FieldAddress] public TargetPointer? OptionalAddress { get; }
}

// 12. UsePropertyName = false with explicit names -- suppresses property name.
[CdacType("TestNoPropertyName")]
internal sealed partial class TestNoPropertyName : IData<TestNoPropertyName>
{
    [Field("m_flags", UsePropertyName = false)] public uint Flags { get; }
}

// 13. DataPointer -- IData<T>-typed property with Pointer = true.
[CdacType("TestDataPointer")]
internal sealed partial class TestDataPointer : IData<TestDataPointer>
{
    [Field(Pointer = true)] public TestNative Inner { get; }
}

// 14. StaticAddress -- resolves a static field address via native global or managed metadata.
[CdacType("TestStaticAddr", "Test.StaticAddr")]
internal sealed partial class TestStaticAddr : IData<TestStaticAddr>
{
    [StaticAddress("s_instance")]
    public static partial TargetPointer Instance(Target target);
}

// 15. StaticReference -- dereferences a static pointer via native global or managed metadata.
[CdacType("TestStaticRef", "Test.StaticRef")]
internal sealed partial class TestStaticRef : IData<TestStaticRef>
{
    [StaticReference("s_cache")]
    public static partial TargetPointer? Cache(Target target);
}

// ---- Managed-to-Native migration types ----
// Declared with managed-style names only. After migration the runtime
// publishes a native descriptor with the SAME name and field names.

// 16. Fields -- managed name + managed field names.
[CdacType("Test.MigrateMN")]
internal sealed partial class MigrateMNFields : IData<MigrateMNFields>
{
    [Field] public uint _value { get; }
    [Field] public TargetPointer _ptr { get; }
}

// 17. Writable -- managed name + managed field name.
[CdacType("Test.MigrateMNWritable")]
internal sealed partial class MigrateMNWritable : IData<MigrateMNWritable>
{
    [Field(Writable = true)] public uint _flags { get; private set; }
}

// 18. Static -- managed name.
[CdacType("Test.MigrateMNStatic")]
internal sealed partial class MigrateMNStatic : IData<MigrateMNStatic>
{
    [StaticAddress("s_instance")]
    public static partial TargetPointer Instance(Target target);
}
