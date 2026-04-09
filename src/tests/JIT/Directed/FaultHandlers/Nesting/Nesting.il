// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'Nesting'
{
}
.class public auto ansi X
{
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    .maxstack  1
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
  }
}

.class public TEST
{
	.method public static int32 Main() il managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
  //-------- BEGIN AUTO GENERATED CODE ----------//
try_0:
try_1:
  newobj     instance void X::.ctor()
  pop
try_2:
  newobj     instance void X::.ctor()
  pop
try_3:
  newobj     instance void X::.ctor()
  pop
try_4:
  newobj     instance void X::.ctor()
  pop
try_5:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
fault_5:
try_6:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_6:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_6
end_6:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_5:
fault_4:
try_7:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_7:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_7
end_7:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_4:
fault_3:
try_8:
try_9:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
fault_9:
try_10:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_10:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_10
end_10:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_9:
catch_8:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_8
end_8:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_3:
fault_2:
try_11:
try_12:
  newobj     instance void X::.ctor()
  pop
try_13:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
fault_13:
try_14:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_14:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_14
end_14:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_13:
fault_12:
try_15:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_15:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_15
end_15:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_12:
catch_11:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_11
end_11:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_2:
fault_1:
try_16:
try_17:
  newobj     instance void X::.ctor()
  pop
try_18:
  newobj     instance void X::.ctor()
  pop
try_19:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
fault_19:
try_20:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_20:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_20
end_20:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_19:
fault_18:
try_21:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_21:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_21
end_21:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_18:
fault_17:
try_22:
try_23:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
fault_23:
try_24:
  newobj     instance void X::.ctor()
  pop
  newobj     instance void [mscorlib]System.Exception::.ctor()
  throw
catch_24:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_24
end_24:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_23:
catch_22:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_22
end_22:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_17:
catch_16:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_16
end_16:
  call void [mscorlib]System.GC::Collect() 
  endfinally
end_1:
catch_0:
  pop
  newobj     instance void X::.ctor()
  pop
  call void [mscorlib]System.GC::Collect() 
  leave end_0
end_0:
  ldc.i4     0x64
  ret
  
.try try_24 to catch_24 catch [mscorlib]System.Exception handler catch_24 to end_24
.try try_23 to fault_23 fault handler fault_23 to end_23
.try try_22 to catch_22 catch [mscorlib]System.Exception handler catch_22 to end_22
.try try_21 to catch_21 catch [mscorlib]System.Exception handler catch_21 to end_21
.try try_20 to catch_20 catch [mscorlib]System.Exception handler catch_20 to end_20
.try try_19 to fault_19 fault handler fault_19 to end_19
.try try_18 to fault_18 fault handler fault_18 to end_18
.try try_17 to fault_17 fault handler fault_17 to end_17
.try try_16 to catch_16 catch [mscorlib]System.Exception handler catch_16 to end_16
.try try_15 to catch_15 catch [mscorlib]System.Exception handler catch_15 to end_15
.try try_14 to catch_14 catch [mscorlib]System.Exception handler catch_14 to end_14
.try try_13 to fault_13 fault handler fault_13 to end_13
.try try_12 to fault_12 fault handler fault_12 to end_12
.try try_11 to catch_11 catch [mscorlib]System.Exception handler catch_11 to end_11
.try try_10 to catch_10 catch [mscorlib]System.Exception handler catch_10 to end_10
.try try_9 to fault_9 fault handler fault_9 to end_9
.try try_8 to catch_8 catch [mscorlib]System.Exception handler catch_8 to end_8
.try try_7 to catch_7 catch [mscorlib]System.Exception handler catch_7 to end_7
.try try_6 to catch_6 catch [mscorlib]System.Exception handler catch_6 to end_6
.try try_5 to fault_5 fault handler fault_5 to end_5
.try try_4 to fault_4 fault handler fault_4 to end_4
.try try_3 to fault_3 fault handler fault_3 to end_3
.try try_2 to fault_2 fault handler fault_2 to end_2
.try try_1 to fault_1 fault handler fault_1 to end_1
.try try_0 to catch_0 catch [mscorlib]System.Exception handler catch_0 to end_0
  //-------- END AUTO GENERATED CODE ----------//
	}
}
