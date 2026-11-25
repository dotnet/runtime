// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern xunit.core {}
.assembly InheritedGenericOwner {}
.module InheritedGenericOwner.dll
.class public OwnerTestBase`2<A,B> extends [System.Runtime]System.Object {
    .method public static class [System.Runtime]System.Type Get<X>() cil managed aggressiveinlining {
        .maxstack 1
        ldtoken !0
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        ret
    }
}
.class public OwnerTestDerived`2<P,Q> extends class OwnerTestBase`2<!1,!0> {}
.class public InheritedOwnerTest extends [System.Runtime]System.Object {
    .method public static class [System.Runtime]System.Type Indirect<P,Q>() cil managed noinlining {
        .maxstack 1
        // The MemberRef names Derived<P,Q>, but the inherited body's !0 is Q.
        call class [System.Runtime]System.Type class OwnerTestDerived`2<!!0,!!1>::Get<!!0>()
        ret
    }
    .method public static int32 Main() cil managed {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 2
        call class [System.Runtime]System.Type InheritedOwnerTest::Indirect<string,object>()
        ldtoken [System.Runtime]System.Object
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        bne.un.s FAIL
        ldc.i4.s 100
        ret
    FAIL:
        ldc.i4.m1
        ret
    }
}
