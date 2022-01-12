// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


//---- CLASS ----------------
.class          _initblk {

//---- GLOBAL DATA ----------

.field public static int32 DATA

//---- METHODS --------------


//---- CONSTRUCTOR ----------
.method public         void _initblk() {
.maxstack       0
        ret
}

//---- MAIN -----------------
.method public static  int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack               5
//====== begin testing ======

        // -- init a 4 byte block of memory to 0xAAAAAAAA
        ldsflda		int32 _initblk::DATA
        ldc.i4          0xAA
        ldc.i4          4
        initblk
        // -- load the 4 bytes of memory _and be sure it is 0xAAAAAAAA
        ldsflda		int32 _initblk::DATA
        ldind.i4
        ldc.i4          0xAAAAAAAA
        ceq
        brfalse         FAIL

//====== end testing ========

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


.assembly ldc_c_initblk {}


