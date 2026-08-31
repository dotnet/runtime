// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly Test6_forwarder{}
.assembly extern mscorlib{}
.assembly extern Test6_supplier{}


.class extern public forwarder A{
	.assembly extern Test6_supplier
}


.class extern public forwarder B{
	.assembly extern Test6_supplier
}
