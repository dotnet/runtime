// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}
.assembly extern System.Runtime.InteropServices
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly PInvokesIL { }

.class public auto ansi abstract sealed beforefieldinit PInvokesIL
    extends [System.Runtime]System.Object
{
    .class nested public auto ansi abstract sealed beforefieldinit UnsetPInvokeImpl
        extends [System.Runtime]System.Object
    {
        .class nested public auto ansi abstract sealed beforefieldinit Default
            extends [System.Runtime]System.Object
        {
            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Default")
                int32 Blittable_Double_DefaultUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 00 00
                )
            }

            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Default")
                bool NotBlittable_Double_DefaultUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 00 00
                )
            }
        }

        .class nested public auto ansi abstract sealed beforefieldinit Cdecl
            extends [System.Runtime]System.Object
        {
            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Cdecl")
                int32 Blittable_Double_CdeclUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 88 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 43
                    64 65 63 6c 2c 20 53 79 73 74 65 6d 2e 50 72 69
                    76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20 56 65
                    72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c 20 43
                    75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20
                    50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 37
                    63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }

            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Cdecl")
                bool NotBlittable_Double_CdeclUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 88 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 43
                    64 65 63 6c 2c 20 53 79 73 74 65 6d 2e 50 72 69
                    76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20 56 65
                    72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c 20 43
                    75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20
                    50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 37
                    63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }
        }

        .class nested public auto ansi abstract sealed beforefieldinit Stdcall
            extends [System.Runtime]System.Object
        {
            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Stdcall")
                int32 Blittable_Double_CdeclUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 88 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 43
                    64 65 63 6c 2c 20 53 79 73 74 65 6d 2e 50 72 69
                    76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20 56 65
                    72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c 20 43
                    75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20
                    50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 37
                    63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }

            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Stdcall")
                int32 Blittable_Double_StdcallUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 8a 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 53
                    74 64 63 61 6c 6c 2c 20 53 79 73 74 65 6d 2e 50
                    72 69 76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20
                    56 65 72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c
                    20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c
                    2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e
                    3d 37 63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }

            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Stdcall")
                bool NotBlittable_Double_CdeclUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 88 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 43
                    64 65 63 6c 2c 20 53 79 73 74 65 6d 2e 50 72 69
                    76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20 56 65
                    72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c 20 43
                    75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20
                    50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 37
                    63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }

            .method public hidebysig static pinvokeimpl("UnmanagedCallConvNative" as "Double_Stdcall")
                bool NotBlittable_Double_StdcallUnmanagedCallConv (int32 a, int32* b) cil managed preservesig
            {
                .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallConvAttribute::.ctor() = (
                    01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
                    73 01 00 00 00 80 8a 53 79 73 74 65 6d 2e 52 75
                    6e 74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65
                    72 76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 53
                    74 64 63 61 6c 6c 2c 20 53 79 73 74 65 6d 2e 50
                    72 69 76 61 74 65 2e 43 6f 72 65 4c 69 62 2c 20
                    56 65 72 73 69 6f 6e 3d 35 2e 30 2e 30 2e 30 2c
                    20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c
                    2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e
                    3d 37 63 65 63 38 35 64 37 62 65 61 37 37 39 38 65
                )
            }
        }
    }
}
