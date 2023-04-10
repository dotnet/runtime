// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: InternalsVisibleTo ("Microsoft.AspNetCore.Http.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]

	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresInLibraryAssembly
	{
		public static void Main ()
		{
		}

		[RequiresDynamicCode ("--MethodWhichRequires--")]
		public static void MethodWhichRequires () { }
	}

	[RequiresUnreferencedCode ("--ClassWithRequires--")]
	internal sealed class ClassWithRequires
	{
		public static int Field;

		internal static readonly ParameterExpression HttpContextExpr = Expression.Parameter (typeof (ParameterExpression), "httpContext");

		public static void Method () { }
	}
}
