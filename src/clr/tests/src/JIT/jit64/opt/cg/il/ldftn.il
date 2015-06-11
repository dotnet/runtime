// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib {}
.assembly Test{}

.method public static int32 Foo()
{
	ldc.i4 100
	ret
}

.class Test
{
.method public static int32 main()
{
	.entrypoint
	.locals init (int32 V_0)
	ldc.i4 101
	stloc.0
	ldftn int32 Foo()
	calli int32()
	stloc.0
	ldloc.0
	ret
}
}
