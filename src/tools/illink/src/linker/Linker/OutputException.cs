using System;

namespace Mono.Linker
{
	public class OutputException : Exception
	{
		public OutputException (string message)
			: base (message)
		{
		}

		public OutputException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}