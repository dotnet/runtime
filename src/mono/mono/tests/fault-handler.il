.assembly Test { }
.assembly extern mscorlib { }
 
.method public static int32 Main()
{
   .entrypoint
   .locals init (
   	int32 res
   )
    ldc.i4.1
    stloc res
   .try {
   .try
   {
      newobj instance void [mscorlib]System.Exception::.ctor()
      throw
      leave.s exitTry
   }
   fault
   {
      ldstr "Fault handler executed"
      call void [mscorlib]System.Console::WriteLine(string)
      ldc.i4.0
      stloc res
      endfault
   }
   } catch [mscorlib]System.Object {
   	pop
      leave.s exitTry
   }
   exitTry:
   ldloc res
   ret
}





