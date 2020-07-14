// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// this test makes sure a generic type of form X<T> can't inherit from T

// class X_ofT<T> extends T
// class X_ofT_M<T,M> extends T
// class Y_ofT<T> implements T
// interface Z_ofT<T> implements T


.assembly extern mscorlib {}
.assembly Type_ofT_Inherit_FromT {}


// class X_ofT`1<T> extends T
.class public auto ansi beforefieldinit X_ofT`1<T>
       extends !T
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void A::.ctor()
    ret
  } // end of method X::.ctor

} // end of class X


// class X_ofT_M`1<T> extends T
.class public auto ansi beforefieldinit X_ofT_M`1<T,M>
       extends !T
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void A::.ctor()
    ret
  } // end of method X::.ctor

} // end of class X



// class Y_ofT<T> implements T
.class public auto ansi beforefieldinit Y_ofT`1<T>
       extends [mscorlib]System.Object
       implements !T
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method Y::.ctor

}

// interface Z_ofT<T> implements T
.class interface abstract public auto ansi Z_ofT`1<T>
       implements !T
{
 
} // end of interface Z_ofT


.class interface public abstract auto ansi I
{
} // end of class I

.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
       implements class Z_ofT`1<int32>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method A::.ctor

} // end of class A



.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  LoadX_ofT_ExtendsT() cil managed
  {
    .maxstack  1
    .locals init (class X_ofT`1<class A> V_0)
    
    newobj     instance void class X_ofT`1<class A>::.ctor()
    stloc.0
    ret
  } // end of method Test::LoadX_ofT_ExtendsT

  .method public hidebysig static void  LoadX_ofT_M_ExtendsT() cil managed
  {
    .maxstack  1
    .locals init (class X_ofT_M`1<class A, int32> V_0)
   
    newobj     instance void class X_ofT_M`1<class A, int32>::.ctor()
    stloc.0
    ret
  } // end of method Test::LoadX_ofT_ExtendsT



  .method public hidebysig static void  LoadY_ofT_ImplementsT() cil managed
  {
    .maxstack  1
    .locals init (class Y_ofT`1<class I> V_0)
    
    newobj     instance void class Y_ofT`1<class I>::.ctor()
    stloc.0
    ret
  } // end of method Test::LoadY_ofT_ImplementsT

  .method public hidebysig static void  LoadInterface_ofT_ImplementsT() cil managed
  {
    .maxstack  1
    .locals init (class Z_ofT`1<int32> V_0)
    
    newobj     instance void A::.ctor()
    stloc.0
    ret
  } // end of method Test::LoadInterface_ofT_ImplementsT

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       239 (0xef)
    .maxstack  2
    .locals init (bool V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop

      ldstr      "Test1: class X<T> extends T"
      call       void [System.Console]System.Console::WriteLine(string)

      IL_0004:  call       void Test::LoadX_ofT_ExtendsT()
      IL_0009:  nop
      IL_000a:  ldstr      "Did not catch expected TypeLoadException"
      IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0014:  nop
      IL_0015:  ldc.i4.0
      IL_0016:  stloc.0
      IL_0017:  nop
      IL_0018:  leave.s    IL_0042

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_001a:  pop
      IL_001b:  nop
      IL_001c:  ldstr      "Caught expected TypeLoadException"
      IL_0021:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0026:  nop
      IL_0027:  nop
      IL_0028:  leave.s    IL_0042

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_002a:  stloc.1
      IL_002b:  nop
      IL_002c:  ldstr      "Caught unexpected exception: "
      IL_0031:  ldloc.1
      IL_0032:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003c:  nop
      IL_003d:  ldc.i4.0
      IL_003e:  stloc.0
      IL_003f:  nop
      IL_0040:  leave.s    IL_0042

    }  // end handler
    IL_0042:  nop
    .try
    {
      IL_0043:  nop
   
      ldstr      "Test2: class Y<T> implements T"
      call       void [System.Console]System.Console::WriteLine(string)


      IL_0044:  call       void Test::LoadY_ofT_ImplementsT()
      IL_0049:  nop
      IL_004a:  ldstr      "Did not catch expected TypeLoadException"
      IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0054:  nop
      IL_0055:  ldc.i4.0
      IL_0056:  stloc.0
      IL_0057:  nop
      IL_0058:  leave.s    IL_0082

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_005a:  pop
      IL_005b:  nop
      IL_005c:  ldstr      "Caught expected TypeLoadException"
      IL_0061:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0066:  nop
      IL_0067:  nop
      IL_0068:  leave.s    IL_0082

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_006a:  stloc.1
      IL_006b:  nop
      IL_006c:  ldstr      "Caught unexpected exception: "
      IL_0071:  ldloc.1
      IL_0072:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0077:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007c:  nop
      IL_007d:  ldc.i4.0
      IL_007e:  stloc.0
      IL_007f:  nop
      IL_0080:  leave.s    IL_0082

    }  // end handler
    IL_0082:  nop
    .try
    {
      IL_0083:  nop

      ldstr      "Test3: interface Z<T> implements T"
      call       void [System.Console]System.Console::WriteLine(string)


      IL_0084:  call       void Test::LoadInterface_ofT_ImplementsT()
      IL_0089:  nop
      IL_008a:  ldstr      "Did not catch expected TypeLoadException"
      IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0094:  nop
      IL_0095:  ldc.i4.0
      IL_0096:  stloc.0
      IL_0097:  nop
      IL_0098:  leave.s    IL_00c2

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_009a:  pop
      IL_009b:  nop
      IL_009c:  ldstr      "Caught expected TypeLoadException"
      IL_00a1:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00a6:  nop
      IL_00a7:  nop
      IL_00a8:  leave.s    IL_00c2

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_00aa:  stloc.1
      IL_00ab:  nop
      IL_00ac:  ldstr      "Caught unexpected exception: "
      IL_00b1:  ldloc.1
      IL_00b2:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_00b7:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00bc:  nop
      IL_00bd:  ldc.i4.0
      IL_00be:  stloc.0
      IL_00bf:  nop
      IL_00c0:  leave.s    IL_00c2

    }  // end handler
    IL_00c2:  nop

    .try
    {
      
      ldstr      "Test1: class X<T,M> extends T"
      call       void [System.Console]System.Console::WriteLine(string)

      call       void Test::LoadX_ofT_M_ExtendsT()
      
      ldstr      "Did not catch expected TypeLoadException"
      call       void [System.Console]System.Console::WriteLine(string)
      
      ldc.i4.0
      stloc.0
      leave.s    END

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      pop
      nop
      ldstr      "Caught expected TypeLoadException"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s    END

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      stloc.1
      
      ldstr      "Caught unexpected exception: "
      ldloc.1
      call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      call       void [System.Console]System.Console::WriteLine(string)
     
      ldc.i4.0
      stloc.0
     
      leave.s    END

    }  // end handler
    END:  nop

    IL_00c3:  ldloc.0
    IL_00c4:  ldc.i4.0
    IL_00c5:  ceq
    IL_00c7:  stloc.3
    IL_00c8:  ldloc.3
    IL_00c9:  brtrue.s   IL_00dc

    IL_00cb:  nop
    IL_00cc:  ldstr      "PASS"
    IL_00d1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00d6:  nop
    IL_00d7:  ldc.i4.s   100
    IL_00d9:  stloc.2
    IL_00da:  br.s       IL_00ed

    IL_00dc:  nop
    IL_00dd:  ldstr      "FAIL"
    IL_00e2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e7:  nop
    IL_00e8:  ldc.i4.s   101
    IL_00ea:  stloc.2
    IL_00eb:  br.s       IL_00ed

    IL_00ed:  ldloc.2
    IL_00ee:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test

