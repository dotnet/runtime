// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { auto }
.assembly DataTls
{
}

.data tls FieldData = int32(51966)

.class public ClassWithTlsStatic extends [System.Runtime]System.Object
{
	.field public static int32 TLSFieldData at FieldData
	.method public static int32 GetTlsStaticValue() 
	{
		ldsfld int32 ClassWithTlsStatic::TLSFieldData
        ldc.i4 100
		ret
	}
}
