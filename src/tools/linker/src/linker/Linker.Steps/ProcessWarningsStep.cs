// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Steps
{
	public class ProcessWarningsStep : BaseStep
	{
		protected override void Process ()
		{
			// Flush all cached messages before the sweep and clean steps are run to be confident 
			// that we have all the information needed to gracefully generate the string.
			Context.FlushCachedWarnings ();
		}
	}
}
