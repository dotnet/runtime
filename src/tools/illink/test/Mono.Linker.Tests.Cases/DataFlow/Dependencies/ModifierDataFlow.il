// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module library.dll

.class public auto ansi sealed beforefieldinit Library.ModifierDataFlow
       extends [System.Runtime]System.Object
{
  .field public static class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) modReqType
  .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

  .field public static class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) modreq(Library.ModifierDataFlow/ModifierType) multipleModReqType
  .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

  .field public static class [System.Runtime]System.Type modopt(Library.ModifierDataFlow/ModifierType) modOptType
  .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

   .field public static class [System.Runtime]System.Type modopt(Library.ModifierDataFlow/ModifierType) modreq(Library.ModifierDataFlow/ModifierType) modReqModOptType
   .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

   .field public static class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) modopt(Library.ModifierDataFlow/ModifierType) modOptModReqType
   .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

   .field public static class [System.Runtime]System.Type[] modreq(Library.ModifierDataFlow/ModifierType) modReqArrayType
   .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

   .field public static class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType)[] arrayModReqType
   .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

   .field public static class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType)[] modreq(Library.ModifierDataFlow/ModifierType) modReqArrayModReqType
   .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
       01 00 20 00 00 00 00 00
   )

  .class nested public auto ansi beforefieldinit ModifierType
         extends [System.Runtime]System.Object
  {
  } // end of class ModifierType

  .method private hidebysig static 
        class [System.Runtime]System.Type GetUnknownType () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    } // end of method Library.ModifierDataFlow::GetUnknownType

    .method public hidebysig static 
        void WriteModReqType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()
        IL_0006: stsfld class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modReqType
        IL_000b: ret
    } // end of method C::WriteModReqType

    .method public hidebysig static 
        void WriteMultipleModReqType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()
        IL_0006: stsfld class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) modreq(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::multipleModReqType
        IL_000b: ret
    } // end of method Library.ModifierDataFlow::WriteMultipleModReqType

    .method public hidebysig static 
        void WriteModOptType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()
        IL_0006: stsfld class [System.Runtime]System.Type modopt(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modOptType
        IL_000b: ret
    } // end of method Library.ModifierDataFlow::WriteModOptType

    .method public hidebysig static 
        void WriteModReqModOptType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()
        IL_0006: stsfld class [System.Runtime]System.Type modopt(Library.ModifierDataFlow/ModifierType) modreq(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modReqModOptType
        IL_000b: ret
    } // end of method Library.ModifierDataFlow::WriteModReqModOptType

    .method public hidebysig static 
        void WriteModOptModReqType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()
        IL_0006: stsfld class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType) modopt(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modOptModReqType
        IL_000b: ret
    } // end of method Library.ModifierDataFlow::WriteModOptModReqType

    .method public hidebysig static 
        void WriteModReqArrayType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: ldc.i4.1
        IL_0002: newarr [System.Runtime]System.Type
        IL_0007: dup
        IL_0008: ldc.i4.0
        IL_0009: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()

        IL_000e: stelem.ref
        IL_000f: stsfld class [System.Runtime]System.Type[] modreq(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modReqArrayType
        IL_0014: ret
    } // end of method Library.ModifierDataFlow::WriteModReqArrayType

    .method public hidebysig static 
        void WriteArrayModReqType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: ldc.i4.1
        IL_0002: newarr [System.Runtime]System.Type
        IL_0007: dup
        IL_0008: ldc.i4.0
        IL_0009: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()

        IL_000e: stelem.ref
        IL_000f: stsfld class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType)[] Library.ModifierDataFlow::arrayModReqType
        IL_0014: ret
    } // end of method Library.ModifierDataFlow::WriteArrayModReqType

    .method public hidebysig static 
        void WriteModReqArrayModReqType () cil managed 
    {
        .maxstack 8

        IL_0000: nop
        IL_0001: ldc.i4.1
        IL_0002: newarr [System.Runtime]System.Type
        IL_0007: dup
        IL_0008: ldc.i4.0
        IL_0009: call class [System.Runtime]System.Type Library.ModifierDataFlow::GetUnknownType()

        IL_000e: stelem.ref
        IL_000f: stsfld class [System.Runtime]System.Type modreq(Library.ModifierDataFlow/ModifierType)[] modreq(Library.ModifierDataFlow/ModifierType) Library.ModifierDataFlow::modReqArrayModReqType
        IL_0014: ret
    } // end of method Library.ModifierDataFlow::WriteModReqArrayModReqType

} // end of class Library.ModifierDataFlow
