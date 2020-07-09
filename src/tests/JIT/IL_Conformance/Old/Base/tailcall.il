// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern legacy library mscorlib { auto }


.assembly tailcall.exe{}

.method public static int32 main(string[]) {
.locals     (class [mscorlib]System.IO.TextWriter,class _tailcall)
.entrypoint
	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    stloc       0
    newobj   instance    void _tailcall::.ctor()
    stloc       1

    ldloc       1
    callvirt    instance int32 _tailcall::method1()

    ldc.i4      0x1234
    bne.un      fail

pass:
    ldloc       0
    ldstr       "TAILCALL Tests PASS."


    callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldc.i4 100
    br          end
fail:
    ldloc       0
    ldstr       "!!! FAILURE !!! - TAILCALL TESTS FAIL - !!! FAILURE !!!"




    callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldc.i4      0x0
    br          end
end:
    ret
}


.class public          _tailcall {
.method public void .ctor() {

.maxstack   10
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    ldstr       "TAILCALL test initialized."


    callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ret
}

.method public         int32 method1() {
.locals     (int32,string)

	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    ldstr       "Beginning Test."


    callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

    ldarg       0  //this ptr
    tail.
    callvirt    instance int32 _tailcall::method2()
    ret

}

.method public         int32 method2() {
.locals     ()

	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    ldstr       "Method2 has been called."


    callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldc.i4      0x1234
    ret //we should return from here to main.

}
}
