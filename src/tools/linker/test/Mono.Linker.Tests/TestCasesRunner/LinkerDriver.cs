﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		public virtual void Link (string [] args, ILogger logger)
		{
			new Driver (args).Run (logger);
		}
	}
}