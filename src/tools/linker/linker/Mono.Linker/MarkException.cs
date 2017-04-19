using System;

using Mono.Cecil;

namespace Mono.Linker
{
	public class MarkException : Exception
	{
		public MethodDefinition Method { get; private set; }

		public MarkException (string message)
			: base (message)
		{
		}

		public MarkException (string message, Exception innerException)
			: base (message, innerException)
		{
		}

		public MarkException (string message, Exception innerException, MethodDefinition method)
			: base (message, innerException)
		{
			Method = method;
		}
	}
}
