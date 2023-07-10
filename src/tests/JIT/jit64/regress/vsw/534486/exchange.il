// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'exchange'
{
}

.method public static void Assert(bool)
{
    ret
}

.class public auto ansi exchange {
.method public static int32 Main()
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
		.locals init
		(
			native unsigned int,
                        native int
		)
        ldc.i4 10
        stloc.0
    ldc.i4.0
    stloc.1
  label:
    ldloc.1
    ldc.i4.5
    beq.s end
    ldc.i4.1
    ldloc.1
    add
    stloc.1
    newobj instance void [mscorlib]System.Object::.ctor()
    callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    ldtoken    [mscorlib]System.String
    call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ceq
    call       void Assert(bool)
    ldloc.0
    ldc.i4 0
    bne.un.s label
  end:
    ldc.i4 100
    ret
}
}


