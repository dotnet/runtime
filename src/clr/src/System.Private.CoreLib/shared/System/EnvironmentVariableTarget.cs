// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System
{
#if PROJECTN
	[Internal.Runtime.CompilerServices.RelocatedType("System.Runtime.Extensions")]
#endif
	public enum EnvironmentVariableTarget
	{
		Process = 0,
		User = 1,
		Machine = 2,
	}
}
