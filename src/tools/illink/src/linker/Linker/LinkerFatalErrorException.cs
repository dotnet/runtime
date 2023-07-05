// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker
{
	/// <summary>
	/// Represents a known error that occurred during link time which is not solvable by the user.
	/// This is used when we want to present the non-recoverable error with a specific error code.
	/// </summary>
	public class LinkerFatalErrorException : Exception
	{
		public MessageContainer MessageContainer { get; }

		/// <param name="message">Error message with a description of what went wrong</param>
		public LinkerFatalErrorException (MessageContainer message)
			: base (message.ToString ())
		{
			if (message.Category != MessageCategory.Error)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' ought to be used for errors only");

			if (message.Code == null || message.Code.Value == 0)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' must have a code that indicates a failure");

			MessageContainer = message;
		}

		/// <param name="message">Error message with a description of what went wrong</param>
		/// <param name="innerException"></param>
		public LinkerFatalErrorException (MessageContainer message, Exception innerException)
			: base (message.ToString (), innerException)
		{
			if (message.Category != MessageCategory.Error)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' ought to be used for errors only");

			if (message.Code == null || message.Code.Value == 0)
				throw new ArgumentException ($"'{nameof (LinkerFatalErrorException)}' must have a code that indicates failure");

			MessageContainer = message;
		}
	}
}
