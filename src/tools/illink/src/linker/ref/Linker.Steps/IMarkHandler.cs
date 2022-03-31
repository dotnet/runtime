// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
