// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
	public enum AssemblyAction
	{
		Skip,
		Copy,
		CopyUsed,
		Link,
		Delete,
		Save,
		AddBypassNGen,
		AddBypassNGenUsed
	}
}
