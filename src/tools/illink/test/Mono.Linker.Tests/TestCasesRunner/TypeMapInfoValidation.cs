// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval;

namespace Mono.Linker.Tests.TestCasesRunner
{
	internal class TypeMapInfoValidation
	{
		public static IEnumerable<string> ValidateRuntimeInterfaces (TypeMapInfo typeMapInfo, TypeDefinition typeDef, string expectedInterfaceName, IEnumerable<string> expectedImplChain)
		{
			var runtimeInterfaces = typeMapInfo.GetRecursiveInterfaces (typeDef);
			if (!runtimeInterfaces.HasValue) {
				yield return ($"Expected type `{typeDef}` to have runtime interface `{expectedInterfaceName}`, but it has none");
				yield break;
			}
			var runtimeInterface = runtimeInterfaces.Value.SingleOrDefault (i => i.InflatedInterfaceType.FullName == expectedInterfaceName);
			if (runtimeInterface == default) {
				yield return ($"Expected type `{typeDef}` to have runtime interface `{expectedInterfaceName}`");
				yield break;
			}

			if (expectedImplChain.Any ()) {
				var matchingImplementationChainCount = runtimeInterface.InterfaceImplementationChains
					.Count (chain => ImplChainMatches (chain, expectedImplChain));
				if (matchingImplementationChainCount == 0) {
					yield return ($"Type {typeDef.FullName} does not have expected implementation chain for runtime interface `{expectedInterfaceName}`: {string.Join ("->", expectedImplChain.Select (i => $"`{i}`"))}");
					yield break;
				}
			}

			static bool ImplChainMatches (InterfaceImplementationChain chain, IEnumerable<string> expectedChain)
			{
				return chain.InterfaceImplementations.Select (i => i.InterfaceType.FullName).SequenceEqual (expectedChain);
			}
		}

		public static IEnumerable<string> ValidateMethodIsOverrideOf (TypeMapInfo typeMapInfo, MethodDefinition methodDef, string expectedOverriddenMethodName)
		{
			var overriddenMethods = typeMapInfo.GetBaseMethods (methodDef);
			if (overriddenMethods is null || !overriddenMethods.Select (o => o.Base.FullName).Contains (expectedOverriddenMethodName))
				yield return $"Expected method `{methodDef}` to be an override of `{expectedOverriddenMethodName}`";
		}
	}
}
