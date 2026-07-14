// Reference a dedicated first-party library ("Library") that is not part of
// the WasmBasicTestApp's default assembly closure. This guarantees the rebuild
// pulls in a genuinely new AOT module, regardless of which BCL assemblies the
// base app happens to root.
using NativeRebuildReferencedLibrary;

public class Test
{
    public static int Main() => NewlyReferencedType.GetValue();
}
