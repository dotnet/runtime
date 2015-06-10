// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib {}

.assembly 'test' {
	.permissionset reqrefuse =
	{
		[mscorlib]System.Security.Permissions.SecurityPermissionAttribute =
		{
			property bool 'SkipVerification' = bool(true)
		}
	}
}

.method public static int32 Main()
{
    .entrypoint
    ldc.i4 0
    box int8
    unbox.any int8
    pop

    ldstr "Test SUCCESS"
    call void [mscorlib]System.Console::WriteLine(string)

    ldc.i4 100 
    ret
}
