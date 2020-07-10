// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// non-blittable by objref field, small offset, large field size, overlapping objref field
.assembly test16{}
.assembly extern mscorlib{}

.class public explicit sealed LargeClass extends [mscorlib]System.ValueType{
	.size 0x20000000 // REALLY large, >5 zeros
}

.class public explicit Class1 extends [mscorlib]System.Object{
	.field [0] public int32 i1
	.field [0] public object o1
	.field [8] public int32 i2
	.field [16] public valuetype LargeClass lc


	.method public specialname rtspecialname void .ctor(){
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

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

    // on some machines with low memory we will get OutOfMemoryException
    catch [mscorlib]System.OutOfMemoryException{
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
	.locals init(class Class1 c1)

	newobj instance void Class1::.ctor()
	stloc.0



	ldstr "hello"
	call void [System.Console]System.Console::WriteLine(string)

	ldloc.0
	ldc.i4.1
	stfld int32 Class1::i1


	ldloc.0
	ldc.i4.2
	stfld int32 Class1::i2


	ldstr "field i1: {0}, i2: {1}\n\nPASS"
	ldloc.0
	ldfld int32 Class1::i1
	box int32
	ldloc.0
	ldfld int32 Class1::i2
	box int32
	call void [System.Console]System.Console::WriteLine(string,object,object)

	ret
}
