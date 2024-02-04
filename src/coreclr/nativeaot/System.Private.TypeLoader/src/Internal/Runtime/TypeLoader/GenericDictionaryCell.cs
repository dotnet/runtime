// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;
using Internal.TypeSystem.NoMetadata;

namespace Internal.Runtime.TypeLoader
{
    public abstract class GenericDictionaryCell
    {
        internal abstract void Prepare(TypeBuilder builder);
        internal abstract IntPtr Create(TypeBuilder builder);
        internal virtual unsafe void WriteCellIntoDictionary(TypeBuilder typeBuilder, IntPtr* pDictionary, int slotIndex)
        {
            pDictionary[slotIndex] = Create(typeBuilder);
        }

        internal virtual IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
        {
            auxResult = IntPtr.Zero;
            return Create(builder);
        }

        // Helper method for nullable transform. Ideally, we would do the nullable transform upfront before
        // the types is build. Unfortunately, there does not seem to be easy way to test for Nullable<> type definition
        // without introducing type builder recursion
        private static RuntimeTypeHandle GetRuntimeTypeHandleWithNullableTransform(TypeBuilder builder, TypeDesc type)
        {
            RuntimeTypeHandle th = builder.GetRuntimeTypeHandle(type);
            if (RuntimeAugments.IsNullable(th))
                th = builder.GetRuntimeTypeHandle(((DefType)type).Instantiation[0]);
            return th;
        }

        private class TypeHandleCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return builder.GetRuntimeTypeHandle(Type).ToIntPtr();
            }
        }

        private class UnwrapNullableTypeCell : GenericDictionaryCell
        {
            internal DefType Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                if (Type.IsNullable)
                {
                    Debug.Assert(Type.Instantiation.Length == 1);
                    builder.RegisterForPreparation(Type.Instantiation[0]);
                }
                else
                    builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                if (Type.IsNullable)
                    return builder.GetRuntimeTypeHandle(Type.Instantiation[0]).ToIntPtr();
                else
                    return builder.GetRuntimeTypeHandle(Type).ToIntPtr();
            }
        }

        private class InterfaceCallCell : GenericDictionaryCell
        {
            internal TypeDesc InterfaceType;
            internal int Slot;

            internal override void Prepare(TypeBuilder builder)
            {
                if (InterfaceType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical interface");

                builder.RegisterForPreparation(InterfaceType);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return RuntimeAugments.NewInterfaceDispatchCell(builder.GetRuntimeTypeHandle(InterfaceType), Slot);
            }
        }

        /// <summary>
        /// Used for non-generic static constrained Methods
        /// </summary>
        private class NonGenericStaticConstrainedMethodCell : GenericDictionaryCell
        {
            internal TypeDesc ConstraintType;
            internal TypeDesc ConstrainedMethodType;
            internal int ConstrainedMethodSlot;

            internal override void Prepare(TypeBuilder builder)
            {
                if (ConstraintType.IsCanonicalSubtype(CanonicalFormKind.Any) || ConstrainedMethodType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical type/method.");

                builder.RegisterForPreparation(ConstraintType);
                builder.RegisterForPreparation(ConstrainedMethodType);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                IntPtr result = RuntimeAugments.ResolveStaticDispatchOnType(
                    builder.GetRuntimeTypeHandle(ConstraintType),
                    builder.GetRuntimeTypeHandle(ConstrainedMethodType),
                    ConstrainedMethodSlot,
                    out RuntimeTypeHandle genericContext);

                Debug.Assert(result != IntPtr.Zero);

                if (!genericContext.IsNull())
                {
                    result = FunctionPointerOps.GetGenericMethodFunctionPointer(result, genericContext.ToIntPtr());
                }

                return result;
            }
        }

        /// <summary>
        /// Used for generic static constrained Methods
        /// </summary>
        private class GenericStaticConstrainedMethodCell : GenericDictionaryCell
        {
            internal DefType ConstraintType;
            internal InstantiatedMethod ConstrainedMethod;
            private InstantiatedMethod _resolvedMethod;

            internal override void Prepare(TypeBuilder builder)
            {
                _resolvedMethod = TypeLoaderEnvironment.GVMLookupForSlotWorker(ConstraintType, ConstrainedMethod);
                builder.PrepareMethod(_resolvedMethod);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                IntPtr methodDictionary = _resolvedMethod.RuntimeMethodDictionary;
                return FunctionPointerOps.GetGenericMethodFunctionPointer(_resolvedMethod.FunctionPointer, methodDictionary);
            }
        }

        private class StaticDataCell : GenericDictionaryCell
        {
            internal StaticDataKind DataKind;
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute static field locations for a canonical type.");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle typeHandle = builder.GetRuntimeTypeHandle(Type);
                switch (DataKind)
                {
                    case StaticDataKind.NonGc:
                        return TypeLoaderEnvironment.Instance.TryGetNonGcStaticFieldData(typeHandle);

                    case StaticDataKind.Gc:
                        return TypeLoaderEnvironment.Instance.TryGetGcStaticFieldData(typeHandle);

                    default:
                        Debug.Assert(false);
                        return IntPtr.Zero;
                }
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                auxResult = IntPtr.Zero;
                return *(IntPtr*)Create(builder);
            }
        }

        private class ThreadStaticIndexCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute static field locations for a canonical type.");

                builder.RegisterForPreparation(Type);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                return TypeLoaderEnvironment.Instance.TryGetThreadStaticFieldData(builder.GetRuntimeTypeHandle(Type));
            }
        }

        private class MethodDictionaryCell : GenericDictionaryCell
        {
            internal InstantiatedMethod GenericMethod;

            internal override unsafe void Prepare(TypeBuilder builder)
            {
                if (GenericMethod.IsCanonicalMethod(CanonicalFormKind.Any))
                    Environment.FailFast("Method dictionaries of canonical methods do not exist");

                builder.PrepareMethod(GenericMethod);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                // TODO (USG): What if this method's instantiation is a non-shareable one (from a normal canonical
                // perspective) and there's an exact method pointer for the method in question, do we still
                // construct a method dictionary to be used with the universal canonical method implementation?
                Debug.Assert(GenericMethod.RuntimeMethodDictionary != IntPtr.Zero);
                return GenericMethod.RuntimeMethodDictionary;
            }
        }

        private class FieldLdTokenCell : GenericDictionaryCell
        {
            internal TypeDesc ContainingType;
            internal IntPtr FieldName;

            internal override unsafe void Prepare(TypeBuilder builder)
            {
                if (ContainingType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Ldtoken is not permitted for a canonical field");

                builder.RegisterForPreparation(ContainingType);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                RuntimeFieldHandle handle = TypeLoaderEnvironment.Instance.GetRuntimeFieldHandleForComponents(
                    builder.GetRuntimeTypeHandle(ContainingType),
                    FieldName);

                return *(IntPtr*)&handle;
            }
        }

        private class MethodLdTokenCell : GenericDictionaryCell
        {
            internal MethodDesc Method;
            internal IntPtr MethodName;
            internal RuntimeSignature MethodSignature;

            internal override unsafe void Prepare(TypeBuilder builder)
            {
                if (Method.IsCanonicalMethod(CanonicalFormKind.Any))
                    Environment.FailFast("Ldtoken is not permitted for a canonical method");

                // Do not use builder.PrepareMethod here. That
                // would prepare the dictionary for the method,
                // and if the method is abstract, there is no
                // dictionary. Also, the dictionary is not necessary
                // to create the ldtoken.
                builder.RegisterForPreparation(Method.OwningType);
                foreach (var type in Method.Instantiation)
                    builder.RegisterForPreparation(type);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle[] genericArgHandles = Method.HasInstantiation && !Method.IsMethodDefinition ?
                    builder.GetRuntimeTypeHandles(Method.Instantiation) : null;

                RuntimeMethodHandle handle = TypeLoaderEnvironment.Instance.GetRuntimeMethodHandleForComponents(
                    builder.GetRuntimeTypeHandle(Method.OwningType),
                    MethodName,
                    MethodSignature,
                    genericArgHandles);

                return *(IntPtr*)&handle;
            }
        }

        private class AllocateObjectCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types cannot be allocated");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                return RuntimeAugments.GetAllocateObjectHelperForType(th);
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                auxResult = th.ToIntPtr();
                return RuntimeAugments.GetAllocateObjectHelperForType(th);
            }
        }

        private class DefaultConstructorCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                IntPtr result = TypeLoaderEnvironment.TryGetDefaultConstructorForType(Type);


                if (result == IntPtr.Zero)
                    result = RuntimeAugments.GetFallbackDefaultConstructor();
                return result;
            }
        }

        private class MethodCell : GenericDictionaryCell
        {
            internal MethodDesc Method;

            internal override unsafe void Prepare(TypeBuilder builder)
            {
                builder.PrepareMethod(Method);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                IntPtr methodDictionary = Method.Instantiation.Length > 0 ?
                    ((InstantiatedMethod)Method).RuntimeMethodDictionary :
                    builder.GetRuntimeTypeHandle(Method.OwningType).ToIntPtr();
                return FunctionPointerOps.GetGenericMethodFunctionPointer(Method.FunctionPointer, methodDictionary);
            }
        }

        internal static unsafe GenericDictionaryCell[] BuildDictionary(TypeBuilder typeBuilder, NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser parser)
        {
            uint parserStartOffset = parser.Offset;

            uint count = parser.GetSequenceCount();
            Debug.Assert(count > 0);

            TypeLoaderLogger.WriteLine("Parsing dictionary layout @ " + parserStartOffset.LowLevelToString() + " (" + count.LowLevelToString() + " entries)");

            GenericDictionaryCell[] dictionary = new GenericDictionaryCell[count];

            for (uint i = 0; i < count; i++)
            {
                TypeLoaderLogger.WriteLine("  -> DictionaryCell[" + i.LowLevelToString() + "] = ");

                dictionary[i] = ParseAndCreateCell(nativeLayoutInfoLoadContext, ref parser);
            }

            for (uint i = 0; i < count; i++)
                dictionary[i].Prepare(typeBuilder);

            return dictionary;
        }

        internal static GenericDictionaryCell ParseAndCreateCell(NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, ref NativeParser parser)
        {
            GenericDictionaryCell cell;

            var kind = parser.GetFixupSignatureKind();
            switch (kind)
            {
                case FixupSignatureKind.TypeHandle:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("TypeHandle: " + type.ToString());

                        cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case FixupSignatureKind.InterfaceCall:
                    {
                        var interfaceType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var slot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("InterfaceCall: " + interfaceType.ToString() + ", slot #" + slot.LowLevelToString());

                        cell = new InterfaceCallCell() { InterfaceType = interfaceType, Slot = (int)slot };
                    }
                    break;

                case FixupSignatureKind.MethodDictionary:
                    {
                        var genericMethod = nativeLayoutInfoLoadContext.GetMethod(ref parser);
                        Debug.Assert(genericMethod.Instantiation.Length > 0);
                        TypeLoaderLogger.WriteLine("MethodDictionary: " + genericMethod.ToString());

                        cell = new MethodDictionaryCell { GenericMethod = (InstantiatedMethod)genericMethod };
                    }
                    break;

                case FixupSignatureKind.StaticData:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        StaticDataKind staticDataKind = (StaticDataKind)parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case FixupSignatureKind.UnwrapNullableType:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("UnwrapNullableType of: " + type.ToString());

                        if (type is DefType)
                            cell = new UnwrapNullableTypeCell() { Type = (DefType)type };
                        else
                            cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case FixupSignatureKind.FieldLdToken:
                    {
                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();

                        var type = nativeLayoutInfoLoadContext.GetType(ref ldtokenSigParser);
                        IntPtr fieldNameSig = ldtokenSigParser.Reader.OffsetToAddress(ldtokenSigParser.Offset);
                        TypeLoaderLogger.WriteLine("LdToken on: " + type.ToString() + "." + ldtokenSigParser.GetString());

                        cell = new FieldLdTokenCell() { FieldName = fieldNameSig, ContainingType = type };
                    }
                    break;

                case FixupSignatureKind.MethodLdToken:
                    {
                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();

                        RuntimeSignature methodNameSig;
                        RuntimeSignature methodSig;
                        var method = nativeLayoutInfoLoadContext.GetMethod(ref ldtokenSigParser, out methodNameSig, out methodSig);
                        TypeLoaderLogger.WriteLine("LdToken on: " + method.OwningType.ToString() + "::" + method.NameAndSignature.Name);

                        cell = new MethodLdTokenCell
                        {
                            Method = method,
                            MethodName = methodNameSig.NativeLayoutSignature(),
                            MethodSignature = methodSig
                        };
                    }
                    break;

                case FixupSignatureKind.AllocateObject:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("AllocateObject on: " + type.ToString());

                        cell = new AllocateObjectCell { Type = type };
                    }
                    break;

                case FixupSignatureKind.DefaultConstructor:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("DefaultConstructor on: " + type.ToString());

                        cell = new DefaultConstructorCell { Type = type };
                    }
                    break;

                case FixupSignatureKind.Method:
                    {
                        var method = nativeLayoutInfoLoadContext.GetMethod(ref parser, out _, out _);
                        TypeLoaderLogger.WriteLine("Method: " + method.ToString());

                        cell = new MethodCell
                        {
                            Method = method,
                        };
                    }
                    break;

                case FixupSignatureKind.NonGenericStaticConstrainedMethod:
                    {
                        var constraintType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodSlot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("NonGenericStaticConstrainedMethod: " + constraintType.ToString() + " Method " + constrainedMethodType.ToString() + ", slot #" + constrainedMethodSlot.LowLevelToString());

                        cell = new NonGenericStaticConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethodType = constrainedMethodType,
                            ConstrainedMethodSlot = (int)constrainedMethodSlot
                        };
                    }
                    break;

                case FixupSignatureKind.GenericStaticConstrainedMethod:
                    {
                        TypeDesc constraintType = nativeLayoutInfoLoadContext.GetType(ref parser);

                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();
                        MethodDesc constrainedMethod = nativeLayoutInfoLoadContext.GetMethod(ref ldtokenSigParser);

                        TypeLoaderLogger.WriteLine("GenericStaticConstrainedMethod: " + constraintType.ToString() + " Method " + constrainedMethod.ToString());

                        cell = new GenericStaticConstrainedMethodCell()
                        {
                            ConstraintType = (DefType)constraintType,
                            ConstrainedMethod = (InstantiatedMethod)constrainedMethod,
                        };
                    }
                    break;

                case FixupSignatureKind.ThreadStaticIndex:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("ThreadStaticIndex on: " + type.ToString());

                        cell = new ThreadStaticIndexCell { Type = type };
                    }
                    break;

                case FixupSignatureKind.NotYetSupported:
                    TypeLoaderLogger.WriteLine("Valid dictionary entry, but not yet supported by the TypeLoader!");
                    throw new TypeBuilder.MissingTemplateException();

                default:
                    NativeParser.ThrowBadImageFormatException();
                    cell = null;
                    break;
            }

            return cell;
        }
    }
}
