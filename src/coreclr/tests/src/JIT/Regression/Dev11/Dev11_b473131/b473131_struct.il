// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}



// ----- DOS Header:
// Magic:                      0x5a4d
// Bytes on last page:         0x0090
// Pages in file:              0x0003
// Relocations:                0x0000
// Size of header (paragraphs):0x0004
// Min extra paragraphs:       0x0000
// Max extra paragraphs:       0xffff
// Initial (relative) SS:      0x0000
// Initial SP:                 0x00b8
// Checksum:                   0x0000
// Initial IP:                 0x0000
// Initial (relative) CS:      0x0000
// File addr. of reloc table:  0x0040
// Overlay number:             0x0000
// OEM identifier:             0x0000
// OEM info:                   0x0000
// File addr. of COFF header:  0x0080
// ----- COFF/PE Headers:
// Signature:                  0x00004550
// ----- COFF Header:
// Machine:                    0x014c
// Number of sections:         0x0003
// Time-date stamp:            0x505125c2
// Ptr to symbol table:        0x00000000
// Number of symbols:          0x00000000
// Size of optional header:    0x00e0
// Characteristics:            0x0102
// ----- PE Optional Header (32 bit):
// Magic:                          0x010b
// Major linker version:           0x08
// Minor linker version:           0x00
// Size of code:                   0x00000600
// Size of init.data:              0x00000800
// Size of uninit.data:            0x00000000
// Addr. of entry point:           0x0000247e
// Base of code:                   0x00002000
// Base of data:                   0x00004000
// Image base:                     0x00400000
// Section alignment:              0x00002000
// File alignment:                 0x00000200
// Major OS version:               0x0004
// Minor OS version:               0x0000
// Major image version:            0x0000
// Minor image version:            0x0000
// Major subsystem version:        0x0004
// Minor subsystem version:        0x0000
// Size of image:                  0x00008000
// Size of headers:                0x00000200
// Checksum:                       0x00000000
// Subsystem:                      0x0003
// DLL characteristics:            0x8540
// Size of stack reserve:          0x00100000
// Size of stack commit:           0x00001000
// Size of heap reserve:           0x00100000
// Size of heap commit:            0x00001000
// Loader flags:                   0x00000000
// Directories:                    0x00000010
// 0x00000000 [0x00000000] address [size] of Export Directory:          
// 0x00002428 [0x00000053] address [size] of Import Directory:          
// 0x00004000 [0x000004f8] address [size] of Resource Directory:        
// 0x00000000 [0x00000000] address [size] of Exception Directory:       
// 0x00000000 [0x00000000] address [size] of Security Directory:        
// 0x00006000 [0x0000000c] address [size] of Base Relocation Table:     
// 0x00000000 [0x00000000] address [size] of Debug Directory:           
// 0x00000000 [0x00000000] address [size] of Architecture Specific:     
// 0x00000000 [0x00000000] address [size] of Global Pointer:            
// 0x00000000 [0x00000000] address [size] of TLS Directory:             
// 0x00000000 [0x00000000] address [size] of Load Config Directory:     
// 0x00000000 [0x00000000] address [size] of Bound Import Directory:    
// 0x00002000 [0x00000008] address [size] of Import Address Table:      
// 0x00000000 [0x00000000] address [size] of Delay Load IAT:            
// 0x00002008 [0x00000048] address [size] of CLR Header:                


// Image sections:
//              .text
//              0x00000484 Virtual Size
//              0x00002000 Virtual Address
//              0x00000600 Size of Raw Data
//              0x00000200 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x60000020 Characteristics
//                         CNT_CODE
//                         MEM_EXECUTE
//                         MEM_READ

//              .rsrc
//              0x000004f8 Virtual Size
//              0x00004000 Virtual Address
//              0x00000600 Size of Raw Data
//              0x00000800 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x40000040 Characteristics
//                         CNT_INITIALIZED_DATA
//                         MEM_READ

//              .reloc
//              0x0000000c Virtual Size
//              0x00006000 Virtual Address
//              0x00000200 Size of Raw Data
//              0x00000e00 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x42000040 Characteristics
//                         CNT_INITIALIZED_DATA
//                         MEM_DISCARDABLE
//                         MEM_READ

// Base Relocation Table
//              0x00002000 Page RVA
//              0x0000000c Block Size
//              0x00000002 Number of Entries
//              Entry 1: Type 0x3 Offset 0x00000480
//              Entry 2: Type 0x0 Offset 0x00000000

// Import Address Table
//     DLL : mscoree.dll
//              0x00002000 Import Address Table
//              0x0000246e Import Name Table
//              0          Time Date Stamp
//              0          Index of First Forwarder Reference
//
//              0x0000  _CorExeMain

// Delay Load Import Address Table

// Entry point code:
//FF 25 00 20 40 00 


// ----- CLR Header:
// Header size:                        0x00000048
// Major runtime version:              0x0002
// Minor runtime version:              0x0005
// 0x000020a0 [0x00000388] address [size] of Metadata Directory:        
// Flags:                              0x00000001
// Entry point token:                  0x06000002
// 0x00000000 [0x00000000] address [size] of Resources Directory:       
// 0x00000000 [0x00000000] address [size] of Strong Name Signature:     
// 0x00000000 [0x00000000] address [size] of CodeManager Table:         
// 0x00000000 [0x00000000] address [size] of VTableFixups Directory:    
// 0x00000000 [0x00000000] address [size] of Export Address Table:      
// 0x00000000 [0x00000000] address [size] of Precompile Header:         

// Metadata Header
//    Storage Signature:
//              0x424a5342 Signature
//                  0x0001 Major Version
//                  0x0001 Minor Version
//              0x00000000 Extra Data Offset
//              0x0000000c Version String Length
//              'v4.0.30319' Version String
//    Storage Header:
//                    0x00 Flags
//                  0x0005 Number of Streams
//    Stream 1:
//              0x0000006c Offset
//              0x00000148 Size
//              '#~' Name
//    Stream 2:
//              0x000001b4 Offset
//              0x00000154 Size
//              '#Strings' Name
//    Stream 3:
//              0x00000308 Offset
//              0x0000000c Size
//              '#US' Name
//    Stream 4:
//              0x00000314 Offset
//              0x00000010 Size
//              '#GUID' Name
//    Stream 5:
//              0x00000324 Offset
//              0x00000064 Size
//              '#Blob' Name

//    Metadata Stream Header:
//              0x00000000 Reserved
//                    0x02 Major
//                    0x00 Minor
//                    0x00 Heaps
//                    0x01 Rid
//      0x0000000900021557 MaskValid
//      0x000016003325fa00 Sorted

// Code Manager Table:
//  default


// Export Address Table Jumps:

// File size            : 4096
// PE header size       : 512 (496 used)    (12.50%)
// PE additional info   : 1447              (35.33%)
// Num.of PE sections   : 3
// CLR header size     : 72                 ( 1.76%)
// CLR meta-data size  : 904                (22.07%)
// CLR additional info : 0                  ( 0.00%)
// CLR method headers  : 14                 ( 0.34%)
// Managed code         : 60                ( 1.46%)
// Data                 : 2048              (50.00%)
// Unaccounted          : -961              (-23.46%)

// Num.of PE sections   : 3
//   .text    - 1536
//   .rsrc    - 1536
//   .reloc   - 512

// CLR meta-data size  : 904
//   Module        -    1 (10 bytes)
//   TypeDef       -    3 (42 bytes)      0 interfaces, 0 explicit layout
//   TypeRef       -    9 (54 bytes)
//   MethodDef     -    3 (42 bytes)      0 abstract, 0 native, 3 bodies
//   FieldDef      -    2 (12 bytes)      0 constant
//   MemberRef     -    6 (36 bytes)
//   ParamDef      -    1 (6 bytes)
//   CustomAttribute-    2 (12 bytes)
//   StandAloneSig -    1 (2 bytes)
//   Assembly      -    1 (22 bytes)
//   AssemblyRef   -    1 (20 bytes)
//   Strings       -   340 bytes
//   Blobs         -   100 bytes
//   UserStrings   -    12 bytes
//   Guids         -    16 bytes
//   Uncategorized -   178 bytes

// CLR method headers : 14
//   Num.of method bodies  - 3
//   Num.of fat headers    - 1
//   Num.of tiny headers   - 2

// Managed code : 60
//   Ave method size - 20
// Classes defined in this module:
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Class e_t                            (public) (sequential) (ansi) (sealed) 
// Class myclass                        (auto) (ansi) 
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


// Metadata version: v4.0.30319
.assembly extern /*23000001*/ mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly /*20000001*/ b473131_struct
{
  .custom /*0C000001:0A000001*/ instance void [mscorlib/*23000001*/]System.Runtime.CompilerServices.CompilationRelaxationsAttribute/*01000003*/::.ctor(int32) /* 0A000001 */ = ( 01 00 08 00 00 00 00 00 ) 
  .custom /*0C000002:0A000002*/ instance void [mscorlib/*23000001*/]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute/*01000004*/::.ctor() /* 0A000002 */ = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                                                                          63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module b473131_struct.exe
// MVID: {A0B36922-A9AF-463E-9F32-9FBF144F14C7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00270000


// =============== CLASS MEMBERS DECLARATION ===================

.class /*02000002*/ public sequential ansi sealed beforefieldinit mynamespace.e_t
       extends [mscorlib/*23000001*/]System.ValueType/*01000001*/
{
  .field /*04000001*/ public uint8 one
  .field /*04000002*/ public uint8 two
} // end of class mynamespace.e_t

.class /*02000003*/ private auto ansi beforefieldinit mynamespace.myclass
       extends [mscorlib/*23000001*/]System.Object/*01000002*/
{
  .method /*06000001*/ public hidebysig static 
          uint8/*02000002*/ 
          foo(uint8/*02000002*/& x) cil managed noinlining
  // SIG: 00 01 11 08 10 11 08
  {
    // Method begins at RVA 0x2050
    // Code size       28 (0x1c)
    .maxstack  8
    IL_0000:  /* 72   | (70)000001       */ ldstr      "{0}" /* 70000001 */
    IL_0005:  /* 02   |                  */ ldarg.0
//    IL_0006:  /* 71   | (02)000002       */ ldobj      mynamespace.e_t/*02000002*/
ldind.u1
    IL_000b:  /* 8C   | (02)000002       */ box        mynamespace.e_t/*02000002*/
    IL_0010:  /* 28   | (0A)000005       */ call       void [System.Console]System.Console::WriteLine(string,
                                                                                                                        object) /* 0A000005 */
    IL_0015:  /* 02   |                  */ ldarg.0
//    IL_0016:  /* 71   | (02)000002       */ ldobj      mynamespace.e_t/*02000002*/
ldind.u1
    IL_001b:  /* 2A   |                  */ ret
  } // end of method myclass::foo

  .method /*06000002*/ public hidebysig static 
          int32 Main() cil managed
  // SIG: 00 00 01
  {
    .entrypoint
    // Method begins at RVA 0x2070
    // Code size       25 (0x19)
    .maxstack  2
    .locals /*11000001*/ init (valuetype mynamespace.e_t/*02000002*/ V_0)
    IL_0000:  /* 12   | 00               */ ldloca.s   V_0
    IL_0002:  /* 18   |                  */ ldc.i4.2
    IL_0003:  /* 7D   | (04)000001       */ stfld      uint8 mynamespace.e_t/*02000002*/::one /* 04000001 */
    IL_0008:  /* 12   | 00               */ ldloca.s   V_0
    IL_000a:  /* 19   |                  */ ldc.i4.3
    IL_000b:  /* 7D   | (04)000002       */ stfld      uint8 mynamespace.e_t/*02000002*/::two /* 04000002 */
    IL_0010:  /* 12   | 00               */ ldloca.s   V_0
ldflda     uint8 mynamespace.e_t::two
    IL_0012:  /* 28   | (06)000001       */ call       uint8/*02000002*/ mynamespace.myclass/*02000003*/::foo(uint8/*02000002*/&) /* 06000001 */
    IL_0017:  /* 26   |                  */ pop
ldc.i4 100
    IL_0018:  /* 2A   |                  */ ret
  } // end of method myclass::Main

  .method /*06000003*/ public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  // SIG: 20 00 01
  {
    // Method begins at RVA 0x2095
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  /* 02   |                  */ ldarg.0
    IL_0001:  /* 28   | (0A)000006       */ call       instance void [mscorlib/*23000001*/]System.Object/*01000002*/::.ctor() /* 0A000006 */
    IL_0006:  /* 2A   |                  */ ret
  } // end of method myclass::.ctor

} // end of class mynamespace.myclass


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file e:\junk\b473131_struct.res
