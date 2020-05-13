using System;

namespace Mono.Linker
{
	public class LinkerFatalErrorException : Exception
	{
		public MessageContainer MessageContainer { get; }

		/// <summary>
		/// Represents a known error that occurred during link time which is solvable by the user.
		/// </summary>
		/// <param name="message">Error message with a description of what went wrong</param>
		public LinkerFatalErrorException (MessageContainer message)
			: base (message.ToString ())
		{
			if (message.Category != MessageCategory.Error)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' ought to be used for errors only");

			MessageContainer = message;
		}

		/// <summary>
		/// Represents a known error that occurred during link time which is solvable by the user.
		/// </summary>
		/// <param name="message">Error message with a description of what went wrong</param>
		/// <param name="innerException"></param>
		public LinkerFatalErrorException (MessageContainer message, Exception innerException)
			: base (message.ToString (), innerException)
		{
			if (message.Category != MessageCategory.Error)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' ought to be used for errors only");

			MessageContainer = message;
		}
	}
}
