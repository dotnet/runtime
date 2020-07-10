// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

.assembly IllegalTypes {}
.assembly extern mscorlib {}

.data i1data = int32(43962)

// RVA + Context Static
.class public RVAContextStatic {

	.field public static int32 i1 at i1data
	.custom instance void [mscorlib]System.ContextStaticAttribute::.ctor() = ( 01 00 00 00 )	
	
}


// RVA + Thread Static (managed)
.class public RVAThreadStatic {

	
	.field public static int32 i1 at i1data
        .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 )	
	
}


// Thread Static (managed) + Context Static
.class public ThreadContextStatic {
	
	.field public static int32 i1
        .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 )	
	.custom instance void [mscorlib]System.ContextStaticAttribute::.ctor() = ( 01 00 00 00 )
	
}


