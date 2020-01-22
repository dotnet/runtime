.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly bug
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'bug-module'
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi 
beforefieldinit '<PrivateImplementationDetails$bug>'
       extends [mscorlib]System.Object
{
} // end of class '<PrivateImplementationDetails$bug>'

.class public auto ansi beforefieldinit Bug
       extends [mscorlib]System.Object
{
  .method public static char  get(string str,int32 i) cil managed
  {
    // Code size       61 (0x3d)
    .maxstack  4
    .locals init (char V_0,
             class [mscorlib]System.Exception V_1,
             class [mscorlib]System.Exception V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  
    
               call       instance char [mscorlib]System.String::get_Chars (int32)
              // Strangely this doesn't cause a crash
              //call       instance int32 [mscorlib]System.String::IndexOf (char)
              
              stloc.0

    IL_0008:  leave.s    IL_003b
    IL_000a:  pop 
              ldc.i4.1  
              endfilter
    
    
    IL_001e:  throw
    IL_003b:  ldloc.0
    .try IL_0000 to IL_000a filter IL_000a handler IL_001e to IL_003b
    IL_003c:  ret
  } // end of method Bug::get

  .method public static bool  capture(int32 i) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  4
    IL_0000:  ldstr      "Testing\n"
    IL_0005:  ldarg.0
    IL_0006:  call       char Bug::get(string,int32)
    IL_000b:  ldc.i4.s   37
    IL_000d:  ceq
    IL_000f:  ret
  } // end of method Bug::capture

} // end of class Bug

.class public auto ansi beforefieldinit '<StartupCode>'.Bug
       extends [mscorlib]System.Object
{
  .method public static void  _main() cil managed
  {
    .entrypoint
    // Code size       8 (0x8)
    .maxstack  3
    .locals init (bool V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  call       bool Bug::capture(int32)
    IL_0006:  stloc.0
    IL_0007:  ret
  } // end of method Bug::_main

} // end of class '<StartupCode>'.Bug


