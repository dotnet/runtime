// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b'// as "b"
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.module 'b.exe'
// MVID: {0573B9C2-3980-4A83-9B01-889CCC1D6FBC}
.namespace Test
{
  .class private auto ansi App
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32
            Main() il managed
    {
      .entrypoint
      // Code size       31 (0x1f)
      .maxstack  6
      		//init counter with -1
			ldc.i4.m1
      	again:
      		//increment counter
      		ldc.i4.1
      		add
      		//print out counter
      		ldstr    "value is "
      		call      void [System.Console]System.Console::Write(class [mscorlib]System.String)
      		dup
      		call      void [System.Console]System.Console::WriteLine(int32)
      		//make a switch
			dup
			switch     (again, again, again, stop)
			//switch worked incorrectly
			//throw an exception
			newobj    instance void [mscorlib]System.Exception::.ctor()
			throw
      	stop:
      		//check counter
      		//we should reach here when counter=3
      		ldc.i4.3
      		ceq
      		brfalse wrong
      		ldstr    "passed"
      		call      void [System.Console]System.Console::Write(class [mscorlib]System.String)
      		br done
      	wrong:
      		ldstr    "failed"
      		call      void [System.Console]System.Console::Write(class [mscorlib]System.String)
      	done:
      		ldc.i4 100
		ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'App::.ctor'

  } // end of class 'App'

} // end of namespace 'Test'

//*********** DISASSEMBLY COMPLETE ***********************
