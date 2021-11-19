// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly 'RMV-2-15-12'{}
.assembly extern mscorlib {}

.class Parent{
	.method virtual public int32 Fun1(int32){
		ldc.i4.1
		ret
	}

	.method public specialname void .ctor(){
		ldarg.0
		call	instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public int32 Invoke(){
		ldarg.0
		call	instance int32 Child::Fun1()
		ret
	}
}

.class Child extends Parent{
	.method virtual public int32 Fun2(){
		.override Parent::Fun1
		ldc.i4.2
		ret
	}

	.method public specialname void .ctor(){
		ldarg.0
		call	instance void Parent::.ctor()
		ret
	}
}

.class public CMain{
.method public static void DeferJitIndirect(){
    .maxstack 8
   	newobj instance	void Child::.ctor()
   	call	instance int32 Child::Invoke()
    pop
    ret
}
.method public static int32 Main(){
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
    .try{
        call void CMain::DeferJitIndirect()
        leave FAIL
    }
    catch [mscorlib]System.MissingMethodException{
        pop
        leave PASS
    }

FAIL:
    ldc.i4 42
    ldstr "FAIL - 2.15.12 - Was allowed to use a MethodImpl whose declaration and method have differing signatures"
    br END
PASS:
    ldc.i4 100
    ldstr "pass"

END:        
    call void [System.Console]System.Console::WriteLine(string)
	ret
}
}
