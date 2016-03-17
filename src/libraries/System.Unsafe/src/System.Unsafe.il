// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}
.assembly System.Unsafe
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 34 2E 30 2E 30 2E 30 00 00 )             // ...4.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 07 34 2E 30 2E 30 2E 30 00 00 )             // ...4.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0D 53 79 73 74 65 6D 2E 55 6E 73 61 66 65   // ...System.Unsafe
                                                                                              00 00 )
  .custom instance void [System.Runtime]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 0D 53 79 73 74 65 6D 2E 55 6E 73 61 66 65   // ...System.Unsafe
                                                                                                    00 00 )
  .custom instance void [System.Runtime]System.Reflection.AssemblyMetadataAttribute::.ctor(string, string) = (
    01 00 15 2e 4e 45 54 46 72 61 6d 65 77 6f 72 6b
    41 73 73 65 6d 62 6c 79 00 00 00
  ) // ".NETFrameworkAssembly", ""
  .custom instance void [System.Runtime]System.Reflection.AssemblyMetadataAttribute::.ctor(string, string) = (
    01 00 0b 53 65 72 76 69 63 65 61 62 6c 65 04 54
    72 75 65 00 00
  ) // "Serviceable", "True"
  .custom instance void [System.Runtime]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 2F C2 A9 20 4D 69 63 72 6F 73 6F 66 74 20   // ../.. Microsoft 
                                                                                        43 6F 72 70 6F 72 61 74 69 6F 6E 2E 20 20 41 6C   // Corporation.  Al
                                                                                        6C 20 72 69 67 68 74 73 20 72 65 73 65 72 76 65   // l rights reserve
                                                                                        64 2E 00 00 )                                     // d...
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 15 4D 69 63 72 6F 73 6F 66 74 20 43 6F 72   // ...Microsoft Cor
                                                                                      70 6F 72 61 74 69 6F 6E 00 00 )                   // poration..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 1A 4D 69 63 72 6F 73 6F 66 74 C2 AE 20 2E   // ...Microsoft.. .
                                                                                      4E 45 54 20 46 72 61 6D 65 77 6F 72 6B 00 00 )    // NET Framework..
  .custom instance void [System.Runtime]System.CLSCompliantAttribute::.ctor(bool) = (
    01 00 00 00 00
  ) // false
  .hash algorithm 0x00008004
  .ver 4:0:0:0
}
.module System.Unsafe.dll
// MVID: {1E97D84A-565B-49C5-B60A-F31A1A4ACE13}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02ED0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit System.Unsafe
       extends [System.Runtime]System.Object
{
  .method public hidebysig static !!T  Read<T>(void* p) cil managed aggressiveinlining
  {
         .maxstack  8
          ldarg.0
          ldobj !!T
          ret
  } // end of method Unsafe::Read

  .method public hidebysig static void  Write<T>(void* p,
                                                 !!T 'value') cil managed aggressiveinlining
  {
         .maxstack  8
          ldarg.0
          ldarg.1
          stobj !!T
          ret
  } // end of method Unsafe::Write

  .method public hidebysig static void*  AddressOf<T>(!!T& 'value') cil managed aggressiveinlining
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0002:  ret
  } // end of method Unsafe::AddressOf

  .method public hidebysig static int32  SizeOf<T>() cil managed aggressiveinlining
  {
    .maxstack  8
    sizeof !!T
    ret
  } // end of method Unsafe::SizeOf

} // end of class System.Unsafe
