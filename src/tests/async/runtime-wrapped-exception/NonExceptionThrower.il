// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper that throws an object which does not derive from System.Exception.
// This is only expressible in IL, so it lives in its own IL assembly. The thrown
// object (a string) is what a catch handler in a WrapNonExceptionThrows=true
// assembly observes as RuntimeWrappedException.WrappedException.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 4:0:0:0 }

.assembly NonExceptionThrower
{
}

.class public auto ansi abstract sealed beforefieldinit NonExceptionThrower
       extends [System.Runtime]System.Object
{
    .method public hidebysig static void ThrowNonException() cil managed noinlining
    {
        .maxstack 8
        ldstr "A non-Exception object thrown from IL"
        throw
    }
}
