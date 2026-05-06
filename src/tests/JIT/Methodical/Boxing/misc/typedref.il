// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib { }
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace BoxTest_typedref_il
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method family hidebysig instance class [mscorlib]System.Object 
            Fibonacci(typedref num, typedref flag) il managed
    {
      .maxstack  8
      			ldarg.1
		       	refanytype
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		       	ldtoken    [mscorlib]System.Object
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		       	bne.un bad
		       	
      			ldarg.2
		       	refanytype
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		       	ldtoken    [mscorlib]System.Object
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		       	bne.un bad

				br equal_types
						       	
	  bad:
      			newobj     instance void [mscorlib]System.Exception::.ctor()
      			throw
      
equal_types:
      			ldarg.0
      			ldarg.1
				refanyval	[mscorlib]System.Object
				ldind.ref
      			ldarg.2
				refanyval	[mscorlib]System.Object
				ldind.ref
      			tail. call       instance class System.Object BoxTest_typedref_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
      			ret
    } // end of method 'Test::Fibonacci'

    .method family hidebysig instance class [mscorlib]System.Object 
            Fibonacci2(class [mscorlib]System.Object num,
                       class [mscorlib]System.Object flag) il managed
    {
      .maxstack  4
      .locals ([0] class [mscorlib]System.Object N,
               [1] int32 V_1,
               [2] bool V_2,
               [3] class [mscorlib]System.Object buf1,
               [4] class [mscorlib]System.Object buf2)
      IL_0000:  ldarg.1
      IL_0001:  unbox      [mscorlib]System.Int32
      IL_0006:  ldind.i4
      IL_0007:  ldc.i4.1
      IL_0008:  bgt.s      IL_000e

      IL_000a:  ldarg.1
      IL_000b:  stloc.0
      IL_000c:  br.s       IL_005c

      IL_000e:  ldarg.0
      IL_000f:  ldarg.1
      IL_0010:  unbox      [mscorlib]System.Int32
      IL_0015:  ldind.i4
      IL_0016:  ldc.i4.2
      IL_0017:  sub
      IL_0018:  stloc.1
      IL_0019:  ldloc.s   V_1
      IL_001b:  box        [mscorlib]System.Int32
      			stloc buf1
      			ldloca buf1
      			mkrefany	[mscorlib]System.Object
      IL_0020:  ldc.i4.0
      IL_0021:  stloc.2
      IL_0022:  ldloc.s   V_2
      IL_0024:  box[mscorlib]System.Boolean
      			stloc buf2
      			ldloca buf2
      			mkrefany	[mscorlib]System.Object
      IL_0029:  call       instance class [mscorlib]System.Object BoxTest_typedref_il.Test::Fibonacci(typedref, typedref)
      IL_002e:  unbox      [mscorlib]System.Int32
      IL_0033:  ldind.i4
      IL_0034:  ldarg.0
      IL_0035:  ldarg.1
      IL_0036:  unbox      [mscorlib]System.Int32
      IL_003b:  ldind.i4
      IL_003c:  ldc.i4.1
      IL_003d:  sub
      IL_003e:  stloc.1
      IL_003f:  ldloc.s   V_1
      IL_0041:  box[mscorlib]System.Int32
      			stloc buf1
      			ldloca buf1
      			mkrefany	[mscorlib]System.Object
      IL_0046:  ldarga flag
      			mkrefany	[mscorlib]System.Object
      IL_0047:  call       instance class [mscorlib]System.Object BoxTest_typedref_il.Test::Fibonacci(typedref, typedref)
      IL_004c:  unbox      [mscorlib]System.Int32
      IL_0051:  ldind.i4
      IL_0052:  add
      IL_0053:  stloc.1
      IL_0054:  ldloc.s   V_1
      IL_0056:  box[mscorlib]System.Int32
      IL_005b:  stloc.0
      IL_005c:  ldarg.2
      IL_005d:  unbox      [mscorlib]System.Boolean
      IL_0062:  ldind.i1
      IL_0063:  brfalse.s  IL_009d

      IL_0065:  ldarg.1
      IL_0066:  unbox      [mscorlib]System.Int32
      IL_006b:  ldind.i4
      IL_006c:  ldc.i4.2
      IL_006d:  rem
      IL_006e:  ldc.i4.0
      IL_006f:  bne.un.s   IL_0088

      IL_0071:  ldloc.0
      IL_0072:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_0077:  ldstr      " "
      IL_007c:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0081:  call       void [System.Console]System.Console::Write(class System.String)
      IL_0086:  br.s       IL_009d

      IL_0088:  ldloc.0
      IL_0089:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_008e:  ldstr      " "
      IL_0093:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0098:  call       void [System.Console]System.Console::Write(class System.String)
      IL_009d:  ldloc.0
      IL_009e:  ret
    } // end of method 'Test::Fibonacci2'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals ([0] int32 V_0,
               [1] bool V_1,
               [2] class [mscorlib]System.Object buf1,
               [3] class [mscorlib]System.Object buf2)
      IL_0000:  newobj     instance void BoxTest_typedref_il.Test::.ctor()
      IL_0005:  ldc.i4.s   20
      IL_0007:  stloc.0
      
      IL_0008:  ldloc.s   V_0
      IL_000a:  box[mscorlib]System.Int32
      			stloc.2
      			ldloca buf1
      			mkrefany	[mscorlib]System.Object
      
      IL_000f:  ldc.i4.1
      IL_0010:  stloc.1
      IL_0011:  ldloc.s   V_1
      IL_0013:  box[mscorlib]System.Boolean
      			stloc.3
      			ldloca buf2
      			mkrefany	[mscorlib]System.Object
      
      IL_0018:  call       instance class [mscorlib]System.Object BoxTest_typedref_il.Test::Fibonacci(typedref, typedref)
      IL_001d:  pop
      IL_001e:  call       void [System.Console]System.Console::WriteLine()
      IL_0023:  ldstr      "*** PASSED ***"
      IL_0028:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002d:  ldc.i4    0x64
      IL_002e:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'BoxTest_typedref_il'

