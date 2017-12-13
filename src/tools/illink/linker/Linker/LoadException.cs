using System;

namespace Mono.Linker
{
	public class LoadException : Exception
	{
		public LoadException (string message)
			: base (message)
		{
		}

		public LoadException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
