// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
//---- CLASS ----------------
.class          _nop {

//---- GLOBAL DATA ----------


//---- METHODS --------------

.method public static int32 return_0(int32) {
.maxstack       2
        ldarg   0
        ret

}
.method public static int32 return_1(int32) {
.maxstack       2
        ldarg   0
        ret

}
//---- CONSTRUCTOR ----------
.method public         void _nop() {
.maxstack       0
        ret
}

//---- MAIN -----------------
.method public static  int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack               5

        ldc.i4  0xFF
        call    int32 _nop::return_0(int32)
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        ldc.i4  0xFF
        call    int32 _nop::return_1(int32)
        ceq
        brfalse FAIL

//---- branch here on pass --
PASS:
	ldc.i4 100
        br              END

//---- branch here on fail --
FAIL:
	ldc.i4 101

//---- return the result ----
END:
        ret
//---- END OF METHOD --------
}
//---- EOF ------------------
}


.assembly ldc_c_nop {}


