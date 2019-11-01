// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly stress2// as "stress2"
{
}
.module stress2.exe
// MVID: {5A1483AA-BABB-44FA-9E05-6A081C8BFE56}
.namespace JitTest
{
  .class private auto ansi StressTest
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32
            Main() il managed
    {
      .entrypoint
      // Code size       28 (0x1c)
      .maxstack  1
      .locals (typedref REF)
      /*
      ldloca.s   REF
      initobj [mscorlib]System.TypedReference

      ldloc.s   REF
      refanytype
      call      class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      callvirt   class [mscorlib]System.String [mscorlib]System.Object::ToString()
      call      void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      */
      
      ldloca.s   REF
      mkrefany   [mscorlib]System.TypedReference
      stloc	REF

      ldloc.s   REF
      refanytype
      call      class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      callvirt   instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
      call      void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

      ldc.i4 100
      ret
    } // end of method StressTest::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method StressTest::.ctor

  } // end of class StressTest

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
