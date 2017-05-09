﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		public virtual void Link (string [] args)
		{
			Driver.Main (args);
		}
	}
}