// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Dataflow
{
	sealed class CompilerGeneratedNames
	{
		internal static bool IsGeneratedMemberName (string memberName)
		{
			return memberName.Length > 0 && memberName[0] == '<';
		}

		internal static bool IsLambdaDisplayClass (string className)
		{
			if (!IsGeneratedMemberName (className))
				return false;

			// This is true for static lambdas (which are emitted into a class like <>c)
			// and for instance lambdas (which are emitted into a class like <>c__DisplayClass1_0)
			return className.StartsWith ("<>c");
		}

		internal static bool IsStateMachineType (string typeName)
		{
			if (!IsGeneratedMemberName (typeName))
				return false;

			// State machines are generated into types with names like <OwnerMethodName>d__0
			// Or if its nested in a local function the name will look like <<OwnerMethodName>g__Local>d and so on
			int i = typeName.LastIndexOf ('>');
			if (i == -1)
				return false;

			return typeName.Length > i + 1 && typeName[i + 1] == 'd';
		}

		internal static bool IsGeneratedType (string name) => IsStateMachineType (name) || IsLambdaDisplayClass (name);

		internal static bool IsLambdaOrLocalFunction (string methodName) => IsLambdaMethod (methodName) || IsLocalFunction (methodName);

		// Lambda methods have generated names like "<UserMethod>b__0_1" where "UserMethod" is the name
		// of the original user code that contains the lambda method declaration.
		internal static bool IsLambdaMethod (string methodName)
		{
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;

			// Ignore the method ordinal/generation and lambda ordinal/generation.
			return methodName.Length > i + 1 && methodName[i + 1] == 'b';
		}

		// Local functions have generated names like "<UserMethod>g__LocalFunction|0_1" where "UserMethod" is the name
		// of the original user code that contains the lambda method declaration, and "LocalFunction" is the name of
		// the local function.
		internal static bool IsLocalFunction (string methodName)
		{
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;

			// Ignore the method ordinal/generation and local function ordinal/generation.
			return methodName.Length > i + 1 && methodName[i + 1] == 'g';
		}
	}
}
