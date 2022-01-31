// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Testing inliner on a small func that contains ldvirtftn and calli
.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b64026'
{
}
.assembly extern xunit.core {}
.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32 f1() 
  {
    ldc.i4.1
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig virtual instance int32
          f1() 
  {
    ldc.i4.s   100
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class private auto ansi beforefieldinit Test_b64026
       extends [mscorlib]System.Object
{
  .field public static class B b1
  .method public hidebysig static int32 f1a() 
  {
    ldsfld     class B Test_b64026::b1
    dup
    ldvirtftn   instance int32 A::f1()
    calli instance int32 ()
    ret
  }

  .method public hidebysig static int32 Main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       279 (0x117)
    .maxstack  2
    	call       int32 Test_b64026::f1a()
    	ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void .cctor() 
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  newobj     instance void B::.ctor()
    IL_0005:  stsfld     class B Test_b64026::b1
    IL_000a:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

}
