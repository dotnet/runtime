// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Logging
{
    // Currently this is implemented using heuristics
    public class CompilerGeneratedState
    {
        private static bool HasRoslynCompilerGeneratedName(DefType type) =>
            type.Name.Contains('<') || (type.ContainingType != null && HasRoslynCompilerGeneratedName(type.ContainingType));

        public static MethodDesc GetUserDefinedMethodForCompilerGeneratedMember(MethodDesc sourceMember)
        {
            var compilerGeneratedType = sourceMember.OwningType.GetTypeDefinition() as EcmaType;
            if (compilerGeneratedType == null)
                return null;

            // Only handle async or iterator state machine
            // So go to the declaring type and check if it's compiler generated (as a perf optimization)
            if (!HasRoslynCompilerGeneratedName(compilerGeneratedType) || compilerGeneratedType.ContainingType == null)
                return null;

            // Now go to its declaring type and search all methods to find the one which points to the type as its
            // state machine implementation.
            foreach (EcmaMethod method in compilerGeneratedType.ContainingType.GetMethods())
            {
                var decodedAttribute = method.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "AsyncIteratorStateMachineAttribute")
                    ?? method.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute")
                    ?? method.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute");

                if (!decodedAttribute.HasValue)
                    continue;

                if (decodedAttribute.Value.FixedArguments.Length != 1
                    || decodedAttribute.Value.FixedArguments[0].Value is not TypeDesc stateMachineType)
                    continue;

                if (stateMachineType == compilerGeneratedType)
                    return method;
            }

            return null;
        }
    }
}
