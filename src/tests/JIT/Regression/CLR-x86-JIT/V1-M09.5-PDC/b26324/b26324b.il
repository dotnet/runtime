// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'b26324b'
{
  
  
}
.assembly extern xunit.core {}
.namespace Test
{
  .class public auto ansi FormatSpecException extends [mscorlib]System.Exception
  {
    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldstr      "Invalid format spec"
      IL_0006:  call       instance void [mscorlib]System.Exception::.ctor(class [mscorlib]System.String)
      IL_000b:  ret
    } 

  } 

  .class public auto ansi Complex extends [mscorlib]System.Object
  {
    .field private float64 m_dReal
    .field private float64 m_dImg
    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  4
      .locals ([0] float64 V_0)
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.0
      IL_0008:  ldc.r8     0.
      IL_0011:  dup
      IL_0012:  stloc.0
      IL_0013:  stfld      float64 Test.Complex::m_dImg
      IL_0018:  ldloc.0
      IL_0019:  stfld      float64 Test.Complex::m_dReal
      IL_001e:  ret
    } 

    .method public specialname rtspecialname instance void  .ctor(float64 real,
                                                                  float64 img) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      float64 Test.Complex::m_dReal
      IL_000d:  ldarg.0
      IL_000e:  ldarg.2
      IL_000f:  stfld      float64 Test.Complex::m_dImg
      IL_0014:  ret
    } 

    .method public virtual instance class [mscorlib]System.String ToString() il managed
    {
      .maxstack  3
      .locals ([0] class [mscorlib]System.String[] V_0)
      IL_0000:  ldc.i4.5
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "C("
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldarg.0
      IL_0012:  ldflda     float64 Test.Complex::m_dReal
      IL_0017:  call       instance class [mscorlib]System.String [mscorlib]System.Double::ToString()
      IL_001c:  stelem.ref
      IL_001d:  ldloc.0
      IL_001e:  ldc.i4.2
      IL_001f:  ldstr      ", "
      IL_0024:  stelem.ref
      IL_0025:  ldloc.0
      IL_0026:  ldc.i4.3
      IL_0027:  ldarg.0
      IL_0028:  ldflda     float64 Test.Complex::m_dImg
      IL_002d:  call       instance class [mscorlib]System.String [mscorlib]System.Double::ToString()
      IL_0032:  stelem.ref
      IL_0033:  ldloc.0
      IL_0034:  ldc.i4.4
      IL_0035:  ldstr      ")"
      IL_003a:  stelem.ref
      IL_003b:  ldloc.0
      IL_003c:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String[])
      IL_0041:  ret
    } 

  } 

  .class auto ansi GCClass extends [mscorlib]System.Object
  {
    .method public instance vararg void VargFunc(class [mscorlib]System.String format) il managed
    {
      .maxstack  4
      ret
    } 

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

  .class auto ansi App extends [mscorlib]System.Object
  {
    .field private static initonly class [mscorlib]System.String formatSpec
    .field private static initonly class [mscorlib]System.String expResult
    .method private static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  12
      .locals ([0] class Test.GCClass inst)
               
      	newobj     instance void Test.GCClass::.ctor()
		ldstr "In vararg function."
		ldftn    instance vararg void Test.GCClass::VargFunc(class [mscorlib]System.String)
		calli    instance vararg void(class [mscorlib]System.String, ...)
		
		ldstr      "**** TEST PASSED ****"
		call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
		ldc.i4 100
		ret
    } 

    .method public specialname rtspecialname static void .cctor() il managed
    {
      .maxstack  8
      IL_0000:  ldstr      "%d %u %ld %lu %s %x %o %o %b"
      IL_0005:  stsfld     class [mscorlib]System.String Test.App::formatSpec
      IL_000a:  ldstr      "-11 324 -11 324 string C(1.5, 2) -11 -11 true"
      IL_000f:  stsfld     class [mscorlib]System.String Test.App::expResult
      IL_0014:  ret
    } 

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
