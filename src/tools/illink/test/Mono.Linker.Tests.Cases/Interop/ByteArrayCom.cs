// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
