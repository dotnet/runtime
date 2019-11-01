// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// valuetype non-blittable by objref field, large offset, small field size, overlapped objref
.assembly test21{}
.assembly extern mscorlib{}

.class public explicit sealed Class1 extends [mscorlib]System.ValueType{
	.field [0] public int32 i1
	.field [0] public object o1
	.field [0x200004] public int32 i2

}

.method public static int32 Main(){
	.entrypoint

    .locals init (class [mscorlib]System.Exception e2)

    .try{
        call void Go()
        ldstr "FAIL: expected TypeLoadException, but no exception was caught"
        call void [System.Console]System.Console::WriteLine(string)
        leave NOT_CAUGHT
    }    
    catch [mscorlib]System.TypeLoadException{
        pop // throw away the exception object
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)            
        leave CAUGHT_EXPECTED
    }
    catch [mscorlib]System.Exception{
        stloc e2
        ldstr "FAIL: expected TypeLoadException, but instead caught the following:\n{0}"
        ldloc e2
        call void [System.Console]System.Console::WriteLine(string,object)            
        leave CAUGHT_UNEXPECTED
    }

    NOT_CAUGHT:
    ldc.i4 101
    br END

    CAUGHT_EXPECTED:
    ldc.i4 100
    br END

    CAUGHT_UNEXPECTED:
    ldc.i4 102
    br END

    END:
    ret
}

.method public static void Go(){
	.locals init(valuetype Class1 c1)

	ldloca c1
	initobj Class1


	ldstr "hello"
	call void [System.Console]System.Console::WriteLine(string)

	ldloca c1
	ldc.i4.1
	stfld int32 Class1::i1


	ldloca c1
	ldc.i4.2
	stfld int32 Class1::i2


	ldstr "field i1: {0}, i2: {1}\n\nPASS"
	ldloca c1
	ldfld int32 Class1::i1
	box int32
	ldloca c1
	ldfld int32 Class1::i2
	box int32
	call void [System.Console]System.Console::WriteLine(string,object,object)

	ret
}
