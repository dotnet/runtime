// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
