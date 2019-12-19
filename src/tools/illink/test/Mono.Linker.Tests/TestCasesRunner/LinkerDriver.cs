﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		protected class TestDriver : Driver
		{
			LinkerCustomizations _customization;

			public TestDriver(string[] args, LinkerCustomizations customizations) : base(args)
			{
				_customization = customizations;
			}

			protected override LinkContext GetDefaultContext (Pipeline pipeline)
			{
				LinkContext context = base.GetDefaultContext (pipeline);
				_customization.CustomizeLinkContext (context);
				return context;
			}
		}

		public virtual void Link (string [] args, LinkerCustomizations customizations, ILogger logger)
		{
			new TestDriver (args, customizations).Run (logger);
		}
	}
}