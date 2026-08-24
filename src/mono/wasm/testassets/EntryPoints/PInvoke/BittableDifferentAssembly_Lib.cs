[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

namespace WasmAppBuilderTests
{
    public struct __NonBlittableTypeForAutomatedTests__ { }
    public struct S {
        public int Value;
        public __NonBlittableTypeForAutomatedTests__ NonBlittable;
    }
}
