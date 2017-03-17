using System;

namespace Mono.Linker
{
	public class MarkException : Exception
	{
		public MarkException (string message)
			: base (message)
		{
		}

		public MarkException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
