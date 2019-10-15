// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly test5{}
.assembly extern test5{}
.assembly extern mscorlib{}

// In this module, we define types Foo and Bar.  We'll use PEHacker to postprocess
// the executable that results from this, changing the name of Bar to Foo.  Then,
// we have two entirely different types that happen to have the same name in the
// same namespace.
//
// In Main, we have three tests.
// The first two effective load types Foo and (what originally was:) Bar.
// TypeDefs are used here, so the IL code will actually contain tokens to the 
// respective types.  This works fine.
// But in the third test, we try to use Foo through a TypeRef whose resolution scope
// is 'this' assembly, thereby trying to load type Foo by name.  But this is ambiguous,
// since both types (after our postprocessing) have the name Foo.  Originally, the runtime
// just took the first one it found, giving us no way to load the second one by name.
// Thus, VSWhidbey 181424 was filed to disallow this scenario.
// We expected a TypeLoadException now when trying to load two types that have the same
// name in the same namespace. 

.class public Bar extends [mscorlib]System.Object{
    .field public int32 i
    .method public specialname instance void .ctor(){
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ldarg.0
        ldc.i4.s 2
        stfld int32 Bar::i
        ret
    }
}

.class public Foo extends [mscorlib]System.Object{
    .field public int32 i
    .method public specialname instance void .ctor(){
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ldarg.0
        ldc.i4.s 3
        stfld int32 Foo::i
        ret
    }
}

.method public static int32 Main(){
    .entrypoint
    .locals init(int32 retVal)
    // there's no way to get to Foo through TypeRef
    ldc.i4 100 // default pass
    stloc retVal
    newobj instance void Foo::.ctor()
    ldfld int32 Foo::i
    ldc.i4.s 3
    beq NEXT1
    ldc.i4 101
    stloc retVal
    ldstr "FAIL: (Type 0x02000003) i != 3"
    call void [System.Console]System.Console::WriteLine(string)

    NEXT1:
    newobj instance void Bar::.ctor()
    ldfld int32 Bar::i
    ldc.i4.s 2
    beq NEXT2
    ldc.i4 101
    stloc retVal
    ldstr "FAIL: (Type 0x02000002) i != 2"
    call void [System.Console]System.Console::WriteLine(string)

    NEXT2:
    newobj instance void [test5]Foo::.ctor()
    ldfld int32 [test5]Foo::i
    ldc.i4.s 3
    beq NEXT3
    ldc.i4 101
    stloc retVal
    ldstr "FAIL: (TypeRef to Foo) i != 3"
    call void [System.Console]System.Console::WriteLine(string)
    
    NEXT3:
    ldloc retVal
    ldc.i4 100
    bne.un END
    ldstr "PASS"
    call void [System.Console]System.Console::WriteLine(string)    

    END:
    ldloc retVal
    ret
}
