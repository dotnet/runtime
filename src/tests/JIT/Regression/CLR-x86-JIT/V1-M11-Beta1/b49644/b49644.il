// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
}
.assembly 'b49644'
{
}


.field public static char g_char at label_char
.field public static bool g_bool at label_bool
.field public static int8 g_int8 at label_int8
.field public static unsigned int8 g_uint8 at label_uint8
.field public static int16 g_int16 at label_int16
.field public static unsigned int16 g_uint16 at label_uint16
.field public static int32 g_int32 at label_int32
.field public static unsigned int32 g_uint32 at label_uint32
.field public static int64 g_int64 at label_int64
.field public static unsigned int64 g_uint64 at label_uint64
.field public static float32 g_float32 at label_float32
.field public static float64 g_float64 at label_float64
.field public static void* g_pvoid at label_ptr


.data label_bool = int8( 0 )
.data label_int8 = int8( 0 )
.data label_uint8 = int8( -1 )
.data label_pad = int8( 0 )
.data label_char = int16( 32 ) 
.data label_int16 = int16( 0 )
.data label_uint16 = int16( -1 )
.data label_pad2 = int16( 0 )
.data label_int32 = int32( 0 )
.data label_uint32 = int32( -1 )
.data label_int64 = int64( 0 )
.data label_uint64 = int64( -1 )
.data label_float32 = int32( 0 )
.data label_pad3 = int32( 0 )
.data label_float64 = int64( 0 )
.data lable_str = char*("init string")
.data label_ptr = int32( 0 )


.method public static void glob_func1() il managed
{
	.maxstack  8

	ldc.i4.5
	stsfld int32 g_int32


	ldc.i4.5
	stsfld int32 g_int32


	ldc.i4.3
	stsfld int32 g_int32

	ret
    
} 


.class auto ansi glob2
{
  .method public static void func() il managed
  {
		.maxstack  8
	
		ret
    
  } 
  
} 


.class public auto ansi glob
{
  .field public static int32 s1
  .field public static int32 bob
  
  .method public static void func() il managed
  {
	.maxstack  8
	
	ret
    
  } 


  .method public static int32  main() il managed
  {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  8
	
		ldstr      "Started"
		call       void [System.Console]System.Console::WriteLine(class System.String)

		ldc.i4.3
		stsfld	   int32 glob::s1
	 	
	

		call       void glob_func1()
	
		ldstr      "Finished"
		call       void [System.Console]System.Console::WriteLine(class System.String)

		call 	   void glob2::func()

		ldc.i4 100
		ret
    
  } 


  .method public specialname rtspecialname instance void .ctor() il managed
  {
	    .maxstack  8
	
	  	ldarg.0
	  	call       instance void [mscorlib]System.Object::.ctor()
	  	ret
        
  } 

} 
