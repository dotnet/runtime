// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'typedref'// as "typedref"
{
}
.namespace BoxTest
{
  .class auto ansi Test extends [mscorlib]System.Object 
{
    .method family hidebysig instance vararg class [mscorlib]System.Object Fibonacci(typedref num, typedref flag) il managed 
{
      // Code size       60 (0x3c)
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  refanytype
      			pop

      IL_000d:  ldarg.2
      IL_000e:  refanytype
      			pop

      IL_0025:  ldarg.0
      IL_0026:  ldarg.1
      IL_0027:  refanyval  [mscorlib]System.Object
      IL_002c:  ldind.ref
      IL_002d:  ldarg.2
      IL_002e:  refanyval  [mscorlib]System.Object
      IL_0033:  ldind.ref
      IL_0034:  tail.
      IL_0036:  call instance vararg class [mscorlib]System.Object BoxTest.Test::Fibonacci2(class [mscorlib]System.Object, class [mscorlib]System.Object)

      IL_003b:  ret
    } // end of method 'Test::Fibonacci'

    .method family hidebysig instance vararg class [mscorlib]System.Object Fibonacci2(class [mscorlib]System.Object num, class [mscorlib]System.Object flag) il managed 
{
      // Code size       206 (0xce)
      .maxstack  4
      .locals (class [mscorlib]System.Object V_0,
               int32 V_1,
               bool V_2,
               class [mscorlib]System.Object V_3,
               class [mscorlib]System.Object V_4)
      IL_0000:  ldarg.1
      IL_0001:  unbox      [mscorlib]System.Int32
      IL_0006:  ldind.i4
      IL_0007:  ldc.i4.1
      IL_0008:  bgt.s      IL_000e

      IL_000a:  ldarg.1
      IL_000b:  stloc.0
      IL_000c:  br.s       IL_008b

      IL_000e:  ldarg.0
      IL_000f:  ldarg.1
      IL_0010:  unbox      [mscorlib]System.Int32
      IL_0015:  ldind.i4
      IL_0016:  ldc.i4.2
      IL_0017:  sub
      IL_0018:  stloc.1
      IL_0019:  ldloc.s   V_1
      IL_001b:  box        [mscorlib]System.Int32
      IL_0020:  stloc      V_3
      IL_0024:  ldloca     V_3
      IL_0028:  mkrefany   [mscorlib]System.Object
      IL_002d:  ldc.i4.0
      IL_002e:  stloc.2
      IL_002f:  ldloc.s   V_2
      IL_0031:  box        [mscorlib]System.Boolean
      IL_0036:  stloc      V_4
      IL_003a:  ldloca     V_4
      IL_003e:  mkrefany   [mscorlib]System.Object
      IL_0043:  call instance vararg class [mscorlib]System.Object BoxTest.Test::Fibonacci(typedref, typedref)

      IL_0048:  unbox      [mscorlib]System.Int32
      IL_004d:  ldind.i4
      IL_004e:  ldarg.0
      IL_004f:  ldarg.1
      IL_0050:  unbox      [mscorlib]System.Int32
      IL_0055:  ldind.i4
      IL_0056:  ldc.i4.1
      IL_0057:  sub
      IL_0058:  stloc.1
      IL_0059:  ldloc.s   V_1
      IL_005b:  box       [mscorlib]System.Int32
      IL_0060:  stloc      V_3
      IL_0064:  ldloca     V_3
      IL_0068:  mkrefany   [mscorlib]System.Object
      IL_006d:  ldarga     flag
      IL_0071:  mkrefany   [mscorlib]System.Object
      IL_0076:  call instance vararg class [mscorlib]System.Object BoxTest.Test::Fibonacci(typedref, typedref)

      IL_007b:  unbox      [mscorlib]System.Int32
      IL_0080:  ldind.i4
      IL_0081:  add
      IL_0082:  stloc.1
      IL_0083:  ldloc.s   V_1
      IL_0085:  box       [mscorlib]System.Int32
      IL_008a:  stloc.0
      IL_008b:  ldarg.2
      IL_008c:  unbox      [mscorlib]System.Boolean
      IL_0091:  ldind.i1
      IL_0092:  brfalse.s  IL_00cc

      IL_0094:  ldarg.1
      IL_0095:  unbox      [mscorlib]System.Int32
      IL_009a:  ldind.i4
      IL_009b:  ldc.i4.2
      IL_009c:  rem
      IL_009d:  ldc.i4.0
      IL_009e:  bne.un.s   IL_00b7

      IL_00a0:  ldloc.0
      IL_00a1:  callvirt instance class [mscorlib]System.String [mscorlib]System.Object::ToString()

      IL_00a6:  ldstr      " "
      IL_00ab:  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)

      IL_00b0:  call void [System.Console]System.Console::Write(class [mscorlib]System.String)

      IL_00b5:  br.s       IL_00cc

      IL_00b7:  ldloc.0
      IL_00b8:  callvirt instance class [mscorlib]System.String [mscorlib]System.Object::ToString()

      IL_00bd:  ldstr      " "
      IL_00c2:  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)

      IL_00c7:  call void [System.Console]System.Console::Write(class [mscorlib]System.String)

      IL_00cc:  ldloc.0
      IL_00cd:  ret
    } // end of method 'Test::Fibonacci2'

    .method private hidebysig static int32 Main() il managed 
{
      .entrypoint
      // Code size       67 (0x43)
      .maxstack  3
      .locals (int32 V_0,
               bool V_1,
               class [mscorlib]System.Object V_2,
               class [mscorlib]System.Object V_3)
      IL_0000:  newobj     instance void BoxTest.Test::.ctor()
      IL_0005:  ldc.i4.s   20
      IL_0007:  stloc.0
      IL_0008:  ldloc.s   V_0
      IL_000a:  box       [mscorlib]System.Int32
      IL_000f:  stloc.2
      IL_0010:  ldloca     V_2
      IL_0014:  mkrefany   [mscorlib]System.Object
      IL_0019:  ldc.i4.1
      IL_001a:  stloc.1
      IL_001b:  ldloc.s   V_1
      IL_001d:  box       [mscorlib]System.Boolean
      IL_0022:  stloc.3
      IL_0023:  ldloca     V_3
      IL_0027:  mkrefany   [mscorlib]System.Object
      IL_002c:  call instance vararg class [mscorlib]System.Object BoxTest.Test::Fibonacci(typedref, typedref)

      IL_0031:  pop
      IL_0032:  call void [System.Console]System.Console::WriteLine()

      IL_0037:  ldstr      "*** PASSED ***"
      IL_003c:  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

      IL_0041:  ldc.i4 100
      IL_0042:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname instance void .ctor() il managed 
{
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call instance void [mscorlib]System.Object::.ctor()

      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'BoxTest'

//*********** DISASSEMBLY COMPLETE ***********************
