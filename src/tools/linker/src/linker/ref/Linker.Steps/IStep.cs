// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker.Steps
{

	public interface IStep
	{
		void Process (LinkContext context);
	}
}
