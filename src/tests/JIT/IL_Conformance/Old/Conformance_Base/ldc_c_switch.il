// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


//
//======================================

//---- CLASS ----------------
.class          _switch {

//---- GLOBAL DATA ----------


//---- METHODS --------------


//---- CONSTRUCTOR ----------
.method public         void _switch() {
.maxstack       0
        ret
}

//---- MAIN -----------------
.method public static  int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack               5
.locals (int32)
//====== begin testing ======
        // -- Setup
        ldc.i4  0x0
        stloc   0
        ldc.i4  0x0

        TAG:
        switch (TC1,TC2,TC3)
        // -- After TC3, we should fall through the
        // -- switch. The value in loc 0 should be 3
        // -- indicating that all cases were hit
        ldloc   0
        ldc.i4          0x3
        ceq
        brfalse         FAIL
        br                      PASS

                TC1:
                        // -- inc loc 0
                        // -- we inc to prove we
                        // -- made it here.
                        ldloc   0
                        ldc.i4          0x1
                        add
                        stloc   0

                        // -- set index for _switch
                        ldc.i4          0x1
                        // -- go back to _switch
                        br                      TAG

                TC2:
                        // -- inc loc 0
                        // -- we inc to prove we
                        // -- made it here.
                        ldloc   0
                        ldc.i4          0x1
                        add
                        stloc   0

                        // -- set index for _switch
                        ldc.i4          0x2
                        // -- go back to _switch
                        br                      TAG

                TC3:
                        // -- inc loc 0
                        // -- we inc to prove we
                        // -- made it here.
                        ldloc   0
                        ldc.i4          0x1
                        add
                        stloc   0

                        // -- set index for _switch
                        ldc.i4          0x3
                        // -- go back to _switch
                        br                      TAG

        // -- We should never get here
        br      FAIL

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


.assembly ldc_c_switch.exe {}


