﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		public virtual void Link (string [] args)
		{
			new Driver (args).Run ();
		}
	}
}