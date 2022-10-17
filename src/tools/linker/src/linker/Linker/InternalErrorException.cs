// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker
{
	public class InternalErrorException : Exception
	{
		/// <summary>
		/// Represents an internal fatal error. Should be used in places that are not expected to be hit by the linker.
		/// </summary>
		/// <param name="message">Message description of what went wrong and why</param>
		public InternalErrorException (string message)
			: base (message)
		{
		}

		/// <summary>
		/// Represents an internal fatal error. Should be used in places that are not expected to be hit by the linker.
		/// </summary>
		/// <param name="message">Message description of what went wrong and why</param>
		/// <param name="innerException"></param>
		public InternalErrorException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
