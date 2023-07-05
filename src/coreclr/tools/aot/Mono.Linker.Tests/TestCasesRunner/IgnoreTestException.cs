﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.TestCasesRunner
{
	internal sealed class IgnoreTestException : Exception
	{
		public IgnoreTestException (string message) : base (message) { }
	}
}
