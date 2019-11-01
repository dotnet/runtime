// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly b47392 { }
.namespace Test
{
  .class private auto ansi AA extends [mscorlib]System.Object
  {
	.field public int32 m_i
	.field public int32[0...] m_ai

    .method private hidebysig static int32 Main() il managed
    {
		.entrypoint
		.maxstack  8
		.locals (class Test.AA, int32 i)
		newobj instance void Test.AA::.ctor()
		stloc.0

		ldc.i4.0
		stloc i

	again:
		ldloc i
		switch (case1,case2,case3,stop)
		newobj instance void [mscorlib]System.Exception::.ctor()
		throw
	case1:
		ldloca i
		br merge
	case2:
		ldloc.0
		ldflda int32 Test.AA::m_i
		br merge
	case3:
		ldloc.0
		ldfld int32[0...] Test.AA::m_ai
		ldc.i4.1
		ldelema [mscorlib]System.Int32
	merge:
		pop
		ldloc i
		ldc.i4.1
		add.ovf.un
		stloc i
		br again
	stop:
		ldc.i4 100
		ret
    } // end of method 'AA::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ldarg.0
      ldc.i4.0
      stfld		int32 Test.AA::m_i
      ldarg.0
      ldc.i4.8
      newobj   instance void int32[0...]::.ctor(int32)
      stfld		int32[0...] Test.AA::m_ai
      ret
    } // end of method 'AA::.ctor'

  } // end of class 'AA'

} // end of namespace 'Test'

