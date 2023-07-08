// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


//---- CLASS ----------------
.namespace ldc_c_cpblk
{
.class public          _cpblk {

//---- GLOBAL DATA ----------

.field public static int32 DATA
.field public static int32 DATA2

//---- METHODS --------------

.method public static int32 initialize_0() {
        // -- init first 4 bytes to 0xAAAAAAAA
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          0xAA
        ldc.i4          4
        initblk
        // -- init next 4 bytes to 0xFFFFFFFF
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          4
        add
        ldc.i4          0xFF
        ldc.i4          4
        initblk
        //-- be sure memory is properly initialized --
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldind.i4
        ldc.i4          0xAAAAAAAA
        ceq
        brfalse         FAIL
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          4
        add
        ldind.i4
        ldc.i4          0xFFFFFFFF
        ceq
        brfalse         FAIL
        ldc.i4  0x00000001
        ret
FAIL:
        ldc.i4  0x00000000
        ret

}
//---- CONSTRUCTOR ----------
.method public         void _cpblk() {
.maxstack       0
        ret
}

//---- MAIN -----------------
.method public static  int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack               5
//----------------
// initialize    -
//----------------
        call    int32 ldc_c_cpblk._cpblk::initialize_0()
        brfalse         FAIL

        // -- copy first chunk into second chunk
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          4
        add
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          4
        cpblk
        // -- Second chunk should now be 0xAAAAAAAA
        ldsflda		int32 ldc_c_cpblk._cpblk::DATA
        ldc.i4          4
        add
        ldind.i4
        ldc.i4          0xAAAAAAAA
        ceq
        brfalse         FAIL

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


.assembly ldc_c_cpblk {}


}
