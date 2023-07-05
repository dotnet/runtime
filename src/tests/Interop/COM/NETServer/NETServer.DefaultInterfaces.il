// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Runtime.InteropServices { }

.assembly NETServer.DefaultInterfaces { }

// Interface used for defining a method on an interface
.class interface public abstract auto ansi Server.Contract.IDefaultInterfaceTesting
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) =
            ( 01 00 01 00 00 )
    // GUID: FB6DF997-4CEF-4DF7-ADBD-E7FA395A7E0C
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.GuidAttribute::.ctor(string) =
            ( 01 00 24 46 42 36 44 46 39 39 37 2D 34 43 45 46
              2D 34 44 46 37 2D 41 44 42 44 2D 45 37 46 41 33
              39 35 41 37 45 30 43 00 00 )
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [System.Runtime.InteropServices]System.Runtime.InteropServices.ComInterfaceType) =
            ( 01 00 01 00 00 00 00 00 )

    .method public virtual
        instance int32 DefOnInterfaceRet2() cil managed
    {
        // Default interface implementation
        ldc.i4 2
        ret
    }

    .method public hidebysig
        instance void DefOnInterfaceNonVirtual() cil managed
    {
        // Non-virtual interface instance method
        ret
    }

    .method public hidebysig
        static void DefOnInterfaceStatic() cil managed
    {
        // Static interface method
        ret
    }

    .method public abstract virtual
        instance int32 DefOnClassRet3() cil managed
    {
        // Simple interface method
    }

    .method public hidebysig
        static void DefOnInterfaceStatic2() cil managed
    {
        // Static interface method
        ret
    }

    .method public hidebysig
        instance void DefOnInterfaceNonVirtual2() cil managed
    {
        // Non-virtual interface instance method
        ret
    }

    .method public virtual
        instance int32 DefOnInterfaceRet5() cil managed
    {
        // Default interface implementation
        ldc.i4 5
        ret
    }
}

// Interface definition for overriding another interface's implementation
.class interface public abstract auto ansi Server.Contract.IDefaultInterfaceTesting2
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) =
            ( 01 00 01 00 00 )
    // GUID: 9B3CE792-F063-427D-B48E-4354094BF7A0
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.GuidAttribute::.ctor(string) =
            ( 01 00 24 39 42 33 43 45 37 39 32 2D 46 30 36 33
              2D 34 32 37 44 2D 42 34 38 45 2D 34 33 35 34 30
              39 34 42 46 37 41 30 00 00 )
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [System.Runtime.InteropServices]System.Runtime.InteropServices.ComInterfaceType) =
            ( 01 00 01 00 00 00 00 00 )
}

// COM server consuming interfaces with default methods
.class public auto ansi beforefieldinit DefaultInterfaceTesting
       extends [System.Runtime]System.Object
       implements Server.Contract.IDefaultInterfaceTesting,
                  Server.Contract.IDefaultInterfaceTesting2
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 01 00 00 )
    // GUID: FAEF42AE-C1A4-419F-A912-B768AC2679EA
    .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.GuidAttribute::.ctor(string) =
            ( 01 00 24 46 41 45 46 34 32 41 45 2D 43 31 41 34
              2D 34 31 39 46 2D 41 39 31 32 2D 42 37 36 38 41
              43 32 36 37 39 45 41 00 00 )

    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig newslot virtual final
          instance int32  DefOnClassRet3() cil managed
    {
        ldc.i4 3
        ret
    }
}
