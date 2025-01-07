// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class ByteArrayCom
	{
		public static void Main ()
		{
			TakesByteArray (new byte[] { });
		}

		[DllImport ("SampleText.dll")]
		static extern void TakesByteArray (byte[] x);
	}
}
