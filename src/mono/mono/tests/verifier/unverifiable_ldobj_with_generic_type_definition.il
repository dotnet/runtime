.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'bug-new'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'bug-new.exe'
// MVID: {6822252F-C77F-4A9B-9396-C53036CCD72E}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02E80000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit G`1<T>
       extends [mscorlib]System.Object
{
  .class sequential ansi sealed nested public beforefieldinit S<T>
         extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 1
    .method public hidebysig instance void 
            Test() cil managed
    {
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldobj      G`1/S
      IL_0006:  box        G`1/S
      IL_000b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0010:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0015:  ret
    } // end of method S::Test

  } // end of class S

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method G`1::.ctor

} // end of class G`1

.class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

  .method public hidebysig static void  Main() cil managed
  {
    .entrypoint
    // Code size       16 (0x10)
    .maxstack  2
    .locals init (valuetype G`1/S<int32> V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    valuetype G`1/S<int32>
    IL_0008:  ldloca.s   V_0
    IL_000a:  call       instance void valuetype G`1/S<int32>::Test()
    IL_000f:  ret
  } // end of method C::Main

} // end of class C


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
