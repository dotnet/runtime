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

// 2. Managed-only reference type (Object.Size offset applied).
[CdacType(ManagedFullName = "Test.Managed.Ref")]
internal sealed partial class TestManagedRef : IData<TestManagedRef>
{
    [Field("_a")] public uint A { get; }
}

// 3. Managed-only value type (no Object.Size offset).
[CdacType(ManagedFullName = "Test.Managed.Val", IsValueType = true)]
internal sealed partial class TestManagedVal : IData<TestManagedVal>
{
    [Field] public uint A { get; }
}

// 4. Cross-source -- single unified name list per field is tried against
//    both sources (native first, then managed).
[CdacType("TestCross", ManagedFullName = "Test.Cross")]
internal sealed partial class TestCross : IData<TestCross>
{
    [Field("A", "_a")] public uint A { get; }
    [Field("B", "_b")] public TargetPointer B { get; }
}

// 5. Native renamed across runtime versions. The unified Names list
//    contains both candidates; the cascade tries each against the native
//    descriptor (managed is absent for a single-source class).
[CdacType("TestNativeAlias")]
internal sealed partial class TestNativeAlias : IData<TestNativeAlias>
{
    [Field("A", "A_old")] public uint A { get; }
}

// 6. Managed renamed across BCL versions.
[CdacType(ManagedFullName = "Test.ManagedAlias")]
internal sealed partial class TestManagedAlias : IData<TestManagedAlias>
{
    [Field("_a", "_a_old")] public uint A { get; }
}

// 7. Cross-source with multiple candidate names spanning both sides.
[CdacType("TestCrossAlias", ManagedFullName = "Test.CrossAlias")]
internal sealed partial class TestCrossAlias : IData<TestCrossAlias>
{
    [Field("A", "A_old", "_a", "_a_old")] public uint A { get; }
}

// 8. Writable, native-only.
[CdacType("TestWriteNative")]
internal sealed partial class TestWriteNative : IData<TestWriteNative>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
}

// 9. Writable, managed-only.
[CdacType(ManagedFullName = "Test.WriteManaged")]
internal sealed partial class TestWriteManaged : IData<TestWriteManaged>
{
    [Field("_flags", Writable = true)] public uint Flags { get; private set; }
}

// 10. Writable, cross-source (write must hit whichever side resolved).
[CdacType("TestWriteCross", ManagedFullName = "Test.WriteCross")]
internal sealed partial class TestWriteCross : IData<TestWriteCross>
{
    [Field("Flags", "_flags", Writable = true)] public uint Flags { get; private set; }
}

// 11. Optional nullable (T? on a value type).
[CdacType("TestOptional")]
internal sealed partial class TestOptional : IData<TestOptional>
{
    [Field] public uint Required { get; }
    [Field] public uint? Optional { get; }
}

// 12. FieldAddress under cross-source fallback.
[CdacType("TestFieldAddr", ManagedFullName = "Test.FieldAddr")]
internal sealed partial class TestFieldAddr : IData<TestFieldAddr>
{
    [Field("A", "_a")]      public uint A { get; }
    [FieldAddress("Anchor")] public TargetPointer AnchorAddress { get; }
}
