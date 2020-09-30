// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern CoreTestAssembly
{
}

.assembly ILTestAssembly
{
}

// Mark this as core library so that ILASM doesn't take the code paths that
// 'fix' all the references to e.g. System.Object to be mscorlib.
.mscorlib


