// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.InteropServices { }

.assembly ComImportTypes { }

// The C# compiler rejects a type that claims to implement an interface without providing an
// implementation for its methods, so the types below have to be defined in IL.

// A ComImport interface with a single method.
.class interface public abstract auto ansi import ComImportUnimplemented.IComImportInterface
{
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.GuidAttribute::.ctor(string) =
            {string('27E4C1B6-3F0A-4D5E-9C88-1A2B3C4D5E6F')}
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [System.Runtime.InteropServices]System.Runtime.InteropServices.ComInterfaceType) =
            ( 01 00 01 00 00 00 00 00 )

    .method public hidebysig newslot abstract virtual
        instance void Method() cil managed
    {
    }
}

// The same shape, without ComImport, to anchor the expected behavior.
.class interface public abstract auto ansi ComImportUnimplemented.IManagedInterface
{
    .method public hidebysig newslot abstract virtual
        instance void Method() cil managed
    {
    }
}

// An ordinary managed type - not a runtime callable wrapper - that claims to implement the
// ComImport interface without providing an implementation for its method. The CLR->COM call
// the interface method dispatches to only makes sense for an RCW, so this type must be
// rejected exactly like ClassMissingManagedMethod below.
.class public auto ansi beforefieldinit ComImportUnimplemented.ClassMissingComImportMethod
       extends [System.Runtime]System.Object
       implements ComImportUnimplemented.IComImportInterface
{
    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit ComImportUnimplemented.ClassMissingManagedMethod
       extends [System.Runtime]System.Object
       implements ComImportUnimplemented.IManagedInterface
{
    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
