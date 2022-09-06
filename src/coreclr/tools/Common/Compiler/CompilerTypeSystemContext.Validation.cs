// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    // Validates types to the extent that is required to make sure the compilation won't fail
    // in unpredictable spots.
    public partial class CompilerTypeSystemContext
    {
        /// <summary>
        /// Ensures that the type can be fully loaded. The method will throw one of the type system
        /// exceptions if the type is not loadable.
        /// </summary>
        public void EnsureLoadableType(TypeDesc type)
        {
            _validTypes.GetOrCreateValue(type);
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
            protected override TypeDesc CreateValueFromKey(TypeDesc key) => EnsureLoadableTypeUncached(key);
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(TypeDesc value) => value.GetHashCode();
        }
        private readonly ValidTypeHashTable _validTypes = new ValidTypeHashTable();

        private static TypeDesc EnsureLoadableTypeUncached(TypeDesc type)
        {
            if (type.IsParameterizedType)
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

                    // It might seem reasonable to disallow array of void, but the CLR doesn't prevent that too hard.
                    // E.g. "newarr void" will fail, but "newarr void[]" or "ldtoken void[]" will succeed.
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

                // Don't validate generic definitions
                if (type.IsGenericDefinition)
                {
                    return type;
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
                        || typeArg.IsVoid
                        || typeArg.IsByRefLike)
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                    }

                    // TODO: validate constraints
                }

                // Check the type doesn't have bogus MethodImpls or overrides and we can get the finalizer.
                defType.GetFinalizer();
            }

            return type;
        }
    }
}
