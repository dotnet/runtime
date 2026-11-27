[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

namespace WasmAppBuilderTests
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    public struct S {
        public int Value;
        public float Value2;
    }
}
