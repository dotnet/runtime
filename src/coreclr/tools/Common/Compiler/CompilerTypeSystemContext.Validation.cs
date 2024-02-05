// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    // Validates types to the extent that is required to make sure the compilation won't fail
    // in unpredictable spots.
    public partial class CompilerTypeSystemContext
    {
        [ThreadStatic]
        private static List<TypeLoadabilityCheckInProgress> t_typeLoadCheckInProgressStack;
        private static List<TypeDesc> EmptyList = new List<TypeDesc>();
        private readonly ValidTypeHashTable _validTypes = new ValidTypeHashTable();

        /// <summary>
        /// Once the type check stack is this deep, we declare the type being scanned as
        /// recursive. In practice, for recursive types, stack overflow happens when the type
        /// load stack is approximately twice as deep (1800~1900).
        /// </summary>
        private const int MaximumTypeLoadCheckStackDepth = 1024;

        /// <summary>
        /// Ensures that the type can be fully loaded. The method will throw one of the type system
        /// exceptions if the type is not loadable.
        /// </summary>
        public void EnsureLoadableType(TypeDesc type)
        {
            if (type == null)
                return;

            if (_validTypes.Contains(type))
                return;

            // Use a scheme where we push a stack of types in the process of loading
            // When the stack pops, without throwing an exception, the type will be marked as being detected as successfully loadable.
            // We need this complex scheme, as types can have circular dependencies. In addition, due to circular references, we can
            // be forced to move when a type is successfully marked as loaded up the stack to an earlier call to EnsureLoadableType
            //
            // For example, consider the following case:
            // interface IInterface<T> {}
            // interface IPassThruInterface<T> : IInterface<T> {}
            // interface ISimpleInterface {}
            // class A<T> : IInterface<A<T>>, IPassThruInterface<A<T>>, ISimpleInterface {}
            // class B : A<B> {}
            //
            // We call EnsureLoadableType on B
            //
            // This will generate the following interesting stacks of calls to EnsureLoadableType
            //
            // B -> A<B> -> B
            //  This stack indicates that A<B> can only be considered loadable if B is considered loadable, so we must defer marking
            //  A<B> as loadable until we finish processing B.
            //
            // B -> A<B> -> ISimpleInterface
            //  Since examining ISimpleInterface does not have any dependency on B or A<B>, it can be marked as loadable as soon
            //  as we finish processing it.
            //
            // B -> A<B> -> IInterface<A<B>> -> A<B>
            //  This stack indicates that IInterface<A<B>> can be considered loadable if A<B> is considered loadable. We must defer
            //  marking IInterface<A<B>> as loadable until we are able to mark A<B> as loadable. Based on the stack above, that can
            //  only happen once B is considered loadable.
            //
            // B -> A<B> -> IPassthruInterface<A<B>> -> IInterface<A<B>>
            //  This stack indicates that IPassthruInterface<A<B>> can be considered loadable if IInterface<A<B>> is considered
            //  loadable. If this happens after the IInterface<A<B>> is marked as being loadable once B is considered loadable
            //  then we will push the loadibility marking to the B level at this point. OR we will continue to recurse and the logic
            //  will note that IInterface<A<B>> needs A<B> needs B which will move the marking up at that point.

            if (PushTypeLoadInProgress(type))
                return;

            bool threwException = true;
            try
            {
                EnsureLoadableTypeUncached(type);
                threwException = false;
            }
            finally
            {
                PopTypeLoadabilityCheckInProgress(threwException);
            }
        }

        private sealed class TypeLoadabilityCheckInProgress
        {
            public TypeDesc TypeInLoadabilityCheck;
            public bool MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown;
            public List<TypeDesc> OtherTypesToMarkAsSuccessfullyLoaded;

            public void AddToOtherTypesToMarkAsSuccessfullyLoaded(TypeDesc type)
            {
                if (OtherTypesToMarkAsSuccessfullyLoaded == EmptyList)
                {
                    OtherTypesToMarkAsSuccessfullyLoaded = new List<TypeDesc>();
                }

                Debug.Assert(!OtherTypesToMarkAsSuccessfullyLoaded.Contains(type));
                OtherTypesToMarkAsSuccessfullyLoaded.Add(type);
            }
        }

        // Returns true to indicate the type should be considered to be loadable (although it might not be, actually safety may require more code to execute)
        private static bool PushTypeLoadInProgress(TypeDesc type)
        {
            t_typeLoadCheckInProgressStack ??= new List<TypeLoadabilityCheckInProgress>();

            if (t_typeLoadCheckInProgressStack.Count >= MaximumTypeLoadCheckStackDepth)
            {
                // Extreme stack depth typically indicates infinite recursion in recursive descent into the type
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
            }

            // Walk stack to see if the specified type is already in the process of being type checked.
            int typeLoadCheckInProgressStackOffset = -1;
            bool checkingMode = false; // Checking for match on TypeLoadabilityCheck field or in OtherTypesToMarkAsSuccessfullyLoaded. (true for OtherTypesToMarkAsSuccessfullyLoaded)
            for (int typeCheckDepth = t_typeLoadCheckInProgressStack.Count - 1; typeCheckDepth >= 0; typeCheckDepth--)
            {
                if (t_typeLoadCheckInProgressStack[typeCheckDepth].TypeInLoadabilityCheck == type)
                {
                    // The stack contains the interesting type.
                    if (t_typeLoadCheckInProgressStack[typeCheckDepth].MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown)
                    {
                        // And this is the level where the type is known to be successfully loaded.
                        typeLoadCheckInProgressStackOffset = typeCheckDepth;
                        break;
                    }
                }
                else if (t_typeLoadCheckInProgressStack[typeCheckDepth].OtherTypesToMarkAsSuccessfullyLoaded.Contains(type))
                {
                    // We've found where the type will be marked as successfully loaded.
                    typeLoadCheckInProgressStackOffset = typeCheckDepth;
                    break;
                }
            }

            if (checkingMode)
            {
                // If we enabled checkingMode we should always have found the type
                Debug.Assert(typeLoadCheckInProgressStackOffset != -1);
            }

            if (typeLoadCheckInProgressStackOffset == -1)
            {
                // The type is not already in the process of being checked for loadability, so return false to indicate that normal load checking should begin
                TypeLoadabilityCheckInProgress typeCheckInProgress = new TypeLoadabilityCheckInProgress();
                typeCheckInProgress.TypeInLoadabilityCheck = type;
                typeCheckInProgress.OtherTypesToMarkAsSuccessfullyLoaded = EmptyList;
                typeCheckInProgress.MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown = true;
                t_typeLoadCheckInProgressStack.Add(typeCheckInProgress);
                return false;
            }

            // Move timing of when types are considered loaded back to the point at which we mark this type as loaded
            var typeLoadCheckToAddTo = t_typeLoadCheckInProgressStack[typeLoadCheckInProgressStackOffset];
            for (int typeCheckDepth = t_typeLoadCheckInProgressStack.Count - 1; typeCheckDepth > typeLoadCheckInProgressStackOffset; typeCheckDepth--)
            {
                if (t_typeLoadCheckInProgressStack[typeCheckDepth].MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown)
                {
                    typeLoadCheckToAddTo.AddToOtherTypesToMarkAsSuccessfullyLoaded(t_typeLoadCheckInProgressStack[typeCheckDepth].TypeInLoadabilityCheck);
                    t_typeLoadCheckInProgressStack[typeCheckDepth].MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown = false;
                }

                foreach (var typeToMove in t_typeLoadCheckInProgressStack[typeCheckDepth].OtherTypesToMarkAsSuccessfullyLoaded)
                {
                    typeLoadCheckToAddTo.AddToOtherTypesToMarkAsSuccessfullyLoaded(typeToMove);
                }

                t_typeLoadCheckInProgressStack[typeCheckDepth].OtherTypesToMarkAsSuccessfullyLoaded = EmptyList;
            }

            // We are going to report that the type should be considered to be loadable at this stage
            return true;
        }

        private void PopTypeLoadabilityCheckInProgress(bool exceptionThrown)
        {
            Debug.Assert(EmptyList.Count == 0);
            var typeLoadabilityCheck = t_typeLoadCheckInProgressStack[t_typeLoadCheckInProgressStack.Count - 1];
            t_typeLoadCheckInProgressStack.RemoveAt(t_typeLoadCheckInProgressStack.Count - 1);

            if (!exceptionThrown)
            {
                if (!typeLoadabilityCheck.MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown)
                {
                    Debug.Assert(typeLoadabilityCheck.OtherTypesToMarkAsSuccessfullyLoaded.Count == 0);
                }

                if (typeLoadabilityCheck.MarkTypeAsSuccessfullyLoadedIfNoExceptionThrown)
                {
                    _validTypes.GetOrCreateValue(typeLoadabilityCheck.TypeInLoadabilityCheck);
                    foreach (var type in typeLoadabilityCheck.OtherTypesToMarkAsSuccessfullyLoaded)
                    {
                        _validTypes.GetOrCreateValue(type);
                    }
                }
            }
        }

        public void EnsureLoadableMethod(MethodDesc method)
        {
            EnsureLoadableType(method.OwningType);

            // If this is an instantiated generic method, check the instantiation.
            MethodDesc methodDef = method.GetMethodDefinition();
            if (methodDef != method)
            {
                foreach (var instType in method.Instantiation)
                    EnsureLoadableType(instType);
            }
        }

        private sealed class ValidTypeHashTable : LockFreeReaderHashtable<TypeDesc, TypeDesc>
        {
            protected override bool CompareKeyToValue(TypeDesc key, TypeDesc value) => key == value;
            protected override bool CompareValueToValue(TypeDesc value1, TypeDesc value2) => value1 == value2;
            protected override TypeDesc CreateValueFromKey(TypeDesc key) => key;
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(TypeDesc value) => value.GetHashCode();
        }

        private static TypeDesc EnsureLoadableTypeUncached(TypeDesc type)
        {
            if (type.TypeIdentifierData != null)
            {
                if (!type.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }
            }

            if (type.IsGenericParameter)
            {
                // Generic parameters don't need validation
            }
            else if (type.IsParameterizedType)
            {
                // Validate parameterized types
                var parameterizedType = (ParameterizedType)type;

                TypeDesc parameterType = parameterizedType.ParameterType;

                // Make sure type of the parameter is loadable.
                ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(parameterType);

                // Validate we're not constructing a type over a ByRef
                if (parameterType.IsByRef)
                {
                    // CLR compat note: "ldtoken int32&&" will actually fail with a message about int32&; "ldtoken int32&[]"
                    // will fail with a message about being unable to create an array of int32&. This is a middle ground.
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                // Validate the parameter is not an uninstantiated generic.
                if (parameterType.IsGenericDefinition)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                if (parameterizedType.IsArray)
                {
                    LayoutInt elementSize = parameterType.GetElementSize();
                    if (!elementSize.IsIndeterminate && elementSize.AsInt >= ushort.MaxValue)
                    {
                        // Element size over 64k can't be encoded in the GCDesc
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadValueClassTooLarge, parameterType);
                    }

                    if (((ArrayType)parameterizedType).Rank > 32)
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadRankTooLarge, type);
                    }

                    if (parameterType.IsByRefLike)
                    {
                        // Arrays of byref-like types are not allowed
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                    }

                    if (parameterType.IsVoid)
                    {
                        // Arrays of System.Void are not allowed
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                    }
                }
            }
            else if (type.IsFunctionPointer)
            {
                var functionPointer = ((FunctionPointerType)type).Signature;
                ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(functionPointer.ReturnType);

                foreach (TypeDesc param in functionPointer)
                {
                    ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(param);
                }
            }
            else
            {
                // Validate classes, structs, enums, interfaces, and delegates
                Debug.Assert(type.IsDefType);

                // Don't validate generic definitions much other than by checking for illegal recursion.
                if (type.IsGenericDefinition)
                {
                    // Check for illegal recursion
                    if (type is EcmaType ecmaType && ILCompiler.LazyGenericsSupport.CheckForECMAIllegalGenericRecursion(ecmaType))
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                    }
                    return type;
                }
                else if (type.HasInstantiation)
                {
                    ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(type.GetTypeDefinition());
                }

                // System.__Canon or System.__UniversalCanon
                if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                {
                    return type;
                }

                // We need to be able to load interfaces
                foreach (var intf in type.RuntimeInterfaces)
                {
                    ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(intf.NormalizeInstantiation());
                }

                if (type.BaseType != null)
                {
                    ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(type.BaseType);
                }

                var defType = (DefType)type;

                // Ensure we can compute the type layout
                defType.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields);
                defType.ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizesAndFields);

                // Make sure instantiation length matches the expectation
                if (defType.Instantiation.Length != defType.GetTypeDefinition().Instantiation.Length)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                foreach (TypeDesc typeArg in defType.Instantiation)
                {
                    // ByRefs, pointers, function pointers, and System.Void are never valid instantiation arguments
                    if (typeArg.IsByRef
                        || typeArg.IsPointer
                        || typeArg.IsFunctionPointer
                        || typeArg.IsVoid)
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                    }

                    ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(typeArg);
                }

                if (!defType.IsCanonicalSubtype(CanonicalFormKind.Any) && !defType.CheckConstraints())
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                // Validate fields meet the constraints of the enclosing type.
                foreach (FieldDesc field in defType.GetFields())
                {
                    if (!field.FieldType.IsCanonicalSubtype(CanonicalFormKind.Any)
                        && !field.FieldType.CheckConstraints())
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, field.FieldType);
                    }
                }

                // Check the type doesn't have bogus MethodImpls or overrides and we can get the finalizer.
                defType.GetFinalizer();
            }

            return type;
        }
    }
}
