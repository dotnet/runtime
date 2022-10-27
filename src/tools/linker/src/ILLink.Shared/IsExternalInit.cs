// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

#if NETSTANDARD
// Allow use of init setters on downlevel frameworks.
namespace System.Runtime.CompilerServices
{
	public sealed class IsExternalInit
	{
	}
}
#endif
