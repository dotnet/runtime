// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker.Steps
{
	/// <summary>
	/// This API supports the product infrastructure and is not intended to be used directly from your code.
	/// </summary>
	public interface IMarkHandler
	{
		void Initialize (LinkContext context, MarkContext markContext);
	}
}
