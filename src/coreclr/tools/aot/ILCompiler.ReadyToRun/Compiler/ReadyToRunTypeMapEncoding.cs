// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.ReadyToRun
{
    internal static class ReadyToRunTypeMapEncoding
    {
        // Keep in sync with TypeMapState in readytoruninfo.cpp.
        public const uint RuntimeAttributeFallback = 0;
        public const uint PrecomputedFixups = 1;
        public const uint PrecomputedFixupsAndTypeNames = 2;

        public static bool IsTypeDescEncodable(NodeFactory factory, ModuleDesc sourceModule, TypeDesc type)
        {
            if (factory.CompilationModuleGroup.VersionsWithTypeReference(type))
            {
                return true;
            }

            if (type is EcmaType ecmaType)
            {
                return MutableModule.CanCreateReferenceToType(
                    sourceModule,
                    ecmaType,
                    (ReadyToRunCompilationModuleGroupBase)factory.CompilationModuleGroup);
            }

            if (type.IsParameterizedType)
            {
                return IsTypeDescEncodable(factory, sourceModule, ((ParameterizedType)type).ParameterType);
            }

            if (type.IsFunctionPointer)
            {
                MethodSignature signature = ((FunctionPointerType)type).Signature;
                if (!IsTypeDescEncodable(factory, sourceModule, signature.ReturnType))
                {
                    return false;
                }

                for (int i = 0; i < signature.Length; i++)
                {
                    if (!IsTypeDescEncodable(factory, sourceModule, signature[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (type.HasInstantiation)
            {
                if (!IsTypeDescEncodable(factory, sourceModule, type.GetTypeDefinition()))
                {
                    return false;
                }

                foreach (TypeDesc instantiationArgument in type.Instantiation)
                {
                    if (!IsTypeDescEncodable(factory, sourceModule, instantiationArgument))
                    {
                        return false;
                    }
                }

                return true;
            }

            return type.IsSignatureVariable;
        }
    }
}
