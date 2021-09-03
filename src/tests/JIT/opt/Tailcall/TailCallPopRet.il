.assembly extern System.Private.CoreLib {}
.assembly TailCallPopRet {}

.class TailCallPopRet extends [System.Private.CoreLib]System.Object
{
    .field static int32 retCode

    .method hidebysig static int32 NonVoidMethod () cil managed noinlining 
    {
        .maxstack 8
        ldc.i4.s 100
        stsfld int32 TailCallPopRet::retCode
        ldc.i4.s 42
        ret
    }

    .method hidebysig static void VoidMethod () cil managed noinlining 
    {
        .maxstack 8
        tail. // Jit64 tolerates pop between tailcall and ret
        call int32 TailCallPopRet::NonVoidMethod()
        pop
        ret
    }

    .method hidebysig static int32 Main () cil managed 
    {
        .entrypoint
        .maxstack 8
        call void TailCallPopRet::VoidMethod()
        ldsfld int32 TailCallPopRet::retCode
        ret
    }
}