.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}


.assembly 'fault' {}
	
.class auto ansi Fault
{
	.method public static int32 Main(class [mscorlib]System.String[] args) il managed
	{
	.entrypoint
	.locals init (int32 V_0)
		ldc.i4	3
		stloc.0

	try_begin:
		ldstr	 "message"
		newobj	instance void [mscorlib]System.Exception::.ctor(class [mscorlib]System.String)
		throw
		leave.s try_end

	fault_begin:
		ldstr	"in fault block."
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
		ldloc.0
		ldc.i4	1
		sub
		stloc.0
		endfinally

	filter_begin:
		pop
		ldstr	"in filter."
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
		ldloc.0
		ldc.i4	1
		sub
		stloc.0
		ldc.i4	 1
		endfilter
				
	except_begin:
		call	 void [mscorlib]System.GC::Collect()
                castclass  [mscorlib]System.Exception
		callvirt instance class [mscorlib]System.String [mscorlib]System.Exception::get_Message()
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)

		ldstr	"in except block."
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
		ldloc.0
		ldc.i4	1
		sub
		stloc.0
		leave.s	try_end
	
	try_end:
		ldloc.0
		ldc.i4.0
		bne.un.s FAIL
			
		ldstr	"test passed."
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4	 0x64
		ret

	FAIL:
		ldstr	"test failed."
		call	 void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)

		ldloc.0
		ret

	.try try_begin to fault_begin fault handler fault_begin to filter_begin
	.try try_begin to filter_begin filter filter_begin handler except_begin to try_end
	}

	.method public specialname rtspecialname instance void .ctor() il managed
	{
		ldarg.0
		call	 instance void [mscorlib]System.Object::.ctor()
		ret
	}

}

