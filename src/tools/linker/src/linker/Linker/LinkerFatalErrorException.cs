using System;

namespace Mono.Linker
{
	public class LinkerFatalErrorException : Exception
	{
		public MessageContainer MessageContainer { get; }

		/// <summary>
		/// Represents an internal error that occured during link time which is not solvable by the user.
		/// </summary>
		/// <param name="internalErrorMessage">The additional message to attach to the error.
		/// The main error message will be about internal error and make it clear this is not a user error.</param>
		public LinkerFatalErrorException (string internalErrorMessage)
			: this (MessageContainer.CreateErrorMessage (
				"IL Linker has encountered an unexpected error. Please report the issue at https://github.com/mono/linker/issues \n" + internalErrorMessage,
				1012))
		{
		}

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
