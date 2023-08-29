// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmingDriver
	{
		protected class TestDriver : Driver
		{
			readonly TrimmingCustomizations _customization;

			public TestDriver (Queue<string> args, TrimmingCustomizations customizations) : base (args)
			{
				_customization = customizations;
			}

			protected override LinkContext GetDefaultContext (Pipeline pipeline, ILogger logger)
			{
				LinkContext context = base.GetDefaultContext (pipeline, logger);
				_customization.CustomizeLinkContext (context);
				return context;
			}
		}

		public virtual TrimmingResults Trim (string[] args, TrimmingCustomizations customizations, ILogger logger)
		{
			Driver.ProcessResponseFile (args, out var queue);
			using (var driver = new TestDriver (queue, customizations)) {
				return new TrimmingResults (driver.Run (logger));
			}
		}
	}
}