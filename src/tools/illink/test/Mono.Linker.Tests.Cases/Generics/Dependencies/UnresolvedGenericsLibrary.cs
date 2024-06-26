// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Generics.Dependencies
{
	public class UnresolvedGenericsLibrary
	{
		public class GenericClass<T> { }

		public static void GenericMethod<T> () { }
	}
}
