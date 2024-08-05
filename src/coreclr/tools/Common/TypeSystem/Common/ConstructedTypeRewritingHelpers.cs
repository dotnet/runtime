// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public static class ConstructedTypeRewritingHelpers
    {
        /// <summary>
        /// Determine if the construction of a type contains one of a given set of types. This is a deep
        /// scan. For instance, given type MyType&lt;SomeGeneric&lt;int[]&gt;&gt;, and a set of typesToFind
        /// that includes int, this function will return true. Does not detect the open generics that may be
        /// instantiated over in this type. IsConstructedOverType would return false if only passed MyType,
        /// or SomeGeneric for the above examplt.
        /// </summary>
        /// <param name="type">type to examine</param>
        /// <param name="typesToFind">types to search for in the construction of type</param>
        /// <returns>true if a type in typesToFind is found</returns>
        public static bool IsConstructedOverType(this TypeDesc type, TypeDesc[] typesToFind)
        {
            int directDiscoveryIndex = Array.IndexOf(typesToFind, type);

            if (directDiscoveryIndex != -1)
                return true;

            if (type.HasInstantiation)
            {
                for (int instantiationIndex = 0; instantiationIndex < type.Instantiation.Length; instantiationIndex++)
                {
                    if (type.Instantiation[instantiationIndex].IsConstructedOverType(typesToFind))
                    {
                        return true;
                    }
                }
            }
            else if (type.IsParameterizedType)
            {
                ParameterizedType parameterizedType = (ParameterizedType)type;
                return parameterizedType.ParameterType.IsConstructedOverType(typesToFind);
            }
            else if (type.IsFunctionPointer)
            {
                MethodSignature functionPointerSignature = ((FunctionPointerType)type).Signature;
                if (functionPointerSignature.ReturnType.IsConstructedOverType(typesToFind))
                    return true;

                for (int paramIndex = 0; paramIndex < functionPointerSignature.Length; paramIndex++)
                {
                    if (functionPointerSignature[paramIndex].IsConstructedOverType(typesToFind))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replace some of the types in a type's construction with a new set of types. This function does not
        /// support any situation where there is an instantiated generic that is not represented by an
        /// InstantiatedType. Does not replace the open generics that may be instantiated over in this type.
        ///
        /// For instance, Given MyType&lt;object, int[]&gt;,
        ///  an array of types to replace such as {int,object}, and
        ///  an array of replacement types such as {string,__Canon}.
        ///  The result shall be MyType&lt;__Canon, string[]&gt;
        ///
        /// This function cannot be used to replace MyType in the above example.
        /// </summary>
        public static TypeDesc ReplaceTypesInConstructionOfType(this TypeDesc type, TypeDesc[] typesToReplace, TypeDesc[] replacementTypes)
        {
            int directReplacementIndex = Array.IndexOf(typesToReplace, type);

            if (directReplacementIndex != -1)
                return replacementTypes[directReplacementIndex];

            if (type.HasInstantiation)
            {
                TypeDesc[] newInstantiation = null;
                int instantiationIndex = 0;
                for (; instantiationIndex < type.Instantiation.Length; instantiationIndex++)
                {
                    TypeDesc oldType = type.Instantiation[instantiationIndex];
                    TypeDesc newType = oldType.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                    if ((oldType != newType) || (newInstantiation != null))
                    {
                        if (newInstantiation == null)
                        {
                            newInstantiation = new TypeDesc[type.Instantiation.Length];
                            for (int i = 0; i < instantiationIndex; i++)
                                newInstantiation[i] = type.Instantiation[i];
                        }
                        newInstantiation[instantiationIndex] = newType;
                    }
                }
                if (newInstantiation != null)
                    return type.Context.GetInstantiatedType((MetadataType)type.GetTypeDefinition(), new Instantiation(newInstantiation));
            }
            else if (type.IsParameterizedType)
            {
                ParameterizedType parameterizedType = (ParameterizedType)type;
                TypeDesc oldParameter = parameterizedType.ParameterType;
                TypeDesc newParameter = oldParameter.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                if (oldParameter != newParameter)
                {
                    if (type.IsArray)
                    {
                        ArrayType arrayType = (ArrayType)type;
                        if (arrayType.IsSzArray)
                            return type.Context.GetArrayType(newParameter);
                        else
                            return type.Context.GetArrayType(newParameter, arrayType.Rank);
                    }
                    else if (type.IsPointer)
                    {
                        return type.Context.GetPointerType(newParameter);
                    }
                    else if (type.IsByRef)
                    {
                        return type.Context.GetByRefType(newParameter);
                    }
                    Debug.Fail("Unknown form of type");
                }
            }
            else if (type.IsFunctionPointer)
            {
                MethodSignature oldSig = ((FunctionPointerType)type).Signature;
                MethodSignatureBuilder sigBuilder = new MethodSignatureBuilder(oldSig);
                sigBuilder.ReturnType = oldSig.ReturnType.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                for (int paramIndex = 0; paramIndex < oldSig.Length; paramIndex++)
                    sigBuilder[paramIndex] = oldSig[paramIndex].ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);

                MethodSignature newSig = sigBuilder.ToSignature();
                if (newSig != oldSig)
                    return type.Context.GetFunctionPointerType(newSig);
            }

            return type;
        }

        /// <summary>
        /// Replace some of the types in a method's construction with a new set of types.
        /// Does not replace the open generics that may be instantiated over in this type.
        ///
        /// For instance, Given MyType&lt;object, int[]&gt;.Function&lt;short&gt;(),
        ///  an array of types to replace such as {int,short}, and
        ///  an array of replacement types such as {string,char}.
        ///  The result shall be MyType&lt;object, string[]&gt;.Function&lt;char&gt;
        ///
        /// This function cannot be used to replace MyType in the above example.
        /// </summary>
        public static MethodDesc ReplaceTypesInConstructionOfMethod(this MethodDesc method, TypeDesc[] typesToReplace, TypeDesc[] replacementTypes)
        {
            TypeDesc newOwningType = method.OwningType.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
            MethodDesc methodOnOwningType;
            bool owningTypeChanged = false;
            if (newOwningType == method.OwningType)
            {
                methodOnOwningType = method.GetMethodDefinition();
            }
            else
            {
                methodOnOwningType = TypeSystemHelpers.FindMethodOnExactTypeWithMatchingTypicalMethod(newOwningType, method);
                owningTypeChanged = true;
            }

            MethodDesc result;
            if (!method.HasInstantiation)
            {
                result = methodOnOwningType;
            }
            else
            {
                Debug.Assert(method is InstantiatedMethod);

                TypeDesc[] newInstantiation = null;
                int instantiationIndex = 0;
                for (; instantiationIndex < method.Instantiation.Length; instantiationIndex++)
                {
                    TypeDesc oldType = method.Instantiation[instantiationIndex];
                    TypeDesc newType = oldType.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                    if ((oldType != newType) || (newInstantiation != null))
                    {
                        if (newInstantiation == null)
                        {
                            newInstantiation = new TypeDesc[method.Instantiation.Length];
                            for (int i = 0; i < instantiationIndex; i++)
                                newInstantiation[i] = method.Instantiation[i];
                        }
                        newInstantiation[instantiationIndex] = newType;
                    }
                }

                if (newInstantiation != null)
                    result = method.Context.GetInstantiatedMethod(methodOnOwningType, new Instantiation(newInstantiation));
                else if (owningTypeChanged)
                    result = method.Context.GetInstantiatedMethod(methodOnOwningType, method.Instantiation);
                else
                    result = method;
            }

            return result;
        }
    }
}
