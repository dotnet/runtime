// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.Runtime
{
    internal static class UniversalGenericParameterLayout
    {
        private enum HasVarsInvestigationLevel
        {
            Parameter,
            NotParameter
        }

        /// <summary>
        /// IF THESE SEMANTICS EVER CHANGE UPDATE THE LOGIC WHICH DEFINES THIS BEHAVIOR IN
        /// THE DYNAMIC TYPE LOADER AS WELL AS THE COMPILER.
        /// (There is a version of this in TypeLoaderEnvironment.SignatureParsing.cs that must be kept in sync with this.)
        ///
        /// Parameter's are considered to have type layout dependent on their generic instantiation
        /// if the type of the parameter in its signature is a type variable, or if the type is a generic
        /// structure which meets 2 characteristics:
        /// 1. Structure size/layout is affected by the size/layout of one or more of its generic parameters
        /// 2. One or more of the generic parameters is a type variable, or a generic structure which also recursively
        ///    would satisfy constraint 2. (Note, that in the recursion case, whether or not the structure is affected
        ///    by the size/layout of its generic parameters is not investigated.)
        ///
        /// Examples parameter types, and behavior.
        ///
        /// T = true
        /// List[T] = false
        /// StructNotDependentOnArgsForSize[T] = false
        /// GenStructDependencyOnArgsForSize[T] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[T]] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[List[T]]]] = false
        ///
        /// Example non-parameter type behavior
        /// T = true
        /// List[T] = false
        /// StructNotDependentOnArgsForSize[T] = *true*
        /// GenStructDependencyOnArgsForSize[T] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[T]] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[List[T]]]] = false
        /// </summary>
        public static bool IsLayoutDependentOnGenericInstantiation(TypeDesc type)
        {
            return IsLayoutDependentOnGenericInstantiation(type, HasVarsInvestigationLevel.Parameter);
        }

        private static bool IsLayoutDependentOnGenericInstantiation(TypeDesc type, HasVarsInvestigationLevel investigationLevel)
        {
            if (type.IsSignatureVariable)
            {
                return true;
            }
            else if (type.HasInstantiation && type.IsValueType)
            {
                foreach (TypeDesc valueTypeInstantiationParam in type.Instantiation)
                {
                    if (IsLayoutDependentOnGenericInstantiation(valueTypeInstantiationParam, HasVarsInvestigationLevel.NotParameter))
                    {
                        if (investigationLevel == HasVarsInvestigationLevel.Parameter)
                        {
                            DefType universalCanonForm = (DefType)type.ConvertToCanonForm(CanonicalFormKind.Universal);
                            return universalCanonForm.InstanceFieldSize.IsIndeterminate;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                // All other forms of type do not change their shape dependent on signature variables.
                return false;
            }
        }

        public static bool MethodSignatureHasVarsNeedingCallingConventionConverter(TypeSystem.MethodSignature methodSignature)
        {
            if (IsLayoutDependentOnGenericInstantiation(methodSignature.ReturnType, HasVarsInvestigationLevel.Parameter))
                return true;

            for (int i = 0; i < methodSignature.Length; i++)
            {
                if (IsLayoutDependentOnGenericInstantiation(methodSignature[i], HasVarsInvestigationLevel.Parameter))
                    return true;
            }

            return false;
        }

        public static bool VTableMethodRequiresCallingConventionConverter(MethodDesc method)
        {
            if (!MethodSignatureHasVarsNeedingCallingConventionConverter(method.GetTypicalMethodDefinition().Signature))
                return false;

            MethodDesc slotDecl = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method).GetCanonMethodTarget(CanonicalFormKind.Specific);
            return slotDecl.IsCanonicalMethod(CanonicalFormKind.Universal);
        }
    }
}
