// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b03995
{
}
//Simple program that demonstrates wrong results from JitEnable
.class _simple
{
	.method static int32 main(class System.String[])
	{
		.entrypoint
		.maxstack	5
			
		 ldc.i4 2
		 ldc.i4 2
		 ldc.i4 2 
		 sub         //Result is 0
		 ldc.i4 2
		 ldc.i4 2
		 mul         //Result is 4
		 add         //Result is 4
		 ldc.i4 2
		 neg         //Result is -2
		 add         //Result is 2
		 sub         //JitEnable==1 Says 4, JitEnable==0 says 0!
ldc.i4 100
add
		 ret
	}
}
