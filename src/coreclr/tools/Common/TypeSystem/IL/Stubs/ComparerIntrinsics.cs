// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Intrinsic support arround EqualityComparer&lt;T&gt; and Comparer&lt;T&gt;.
    /// </summary>
    public static class ComparerIntrinsics
    {
        /// <summary>
        /// Generates a specialized method body for Comparer`1.Create or returns null if no specialized body can be generated.
        /// </summary>
        public static MethodIL EmitComparerCreate(MethodDesc target)
        {
            return EmitComparerAndEqualityComparerCreateCommon(target, "Comparer"u8, "IComparable`1"u8);
        }

        /// <summary>
        /// Generates a specialized method body for EqualityComparer`1.Create or returns null if no specialized body can be generated.
        /// </summary>
        public static MethodIL EmitEqualityComparerCreate(MethodDesc target)
        {
            return EmitComparerAndEqualityComparerCreateCommon(target, "EqualityComparer"u8, "IEquatable`1"u8);
        }

        /// <summary>
        /// Gets the concrete type Comparer`1.Create returns or null if it's not known at compile time.
        /// </summary>
        public static TypeDesc GetComparerForType(TypeDesc comparand)
        {
            return GetComparerForType(comparand, "Comparer"u8, "IComparable`1"u8);
        }

        /// <summary>
        /// Gets the concrete type EqualityComparer`1.Create returns or null if it's not known at compile time.
        /// </summary>
        public static TypeDesc GetEqualityComparerForType(TypeDesc comparand)
        {
            return GetComparerForType(comparand, "EqualityComparer"u8, "IEquatable`1"u8);
        }

        private static MethodIL EmitComparerAndEqualityComparerCreateCommon(MethodDesc methodBeingGenerated, ReadOnlySpan<byte> flavor, ReadOnlySpan<byte> interfaceName)
        {
            // We expect the method to be fully instantiated
            Debug.Assert(!methodBeingGenerated.IsTypicalMethodDefinition);

            TypeDesc owningType = methodBeingGenerated.OwningType;
            TypeDesc comparedType = owningType.Instantiation[0];

            // If the type is canonical, we use the default implementation provided by the class library.
            // This will rely on the type loader to load the proper type at runtime.
            if (comparedType.IsCanonicalSubtype(CanonicalFormKind.Any))
                return null;

            TypeDesc comparerType = GetComparerForType(comparedType, flavor, interfaceName);
            Debug.Assert(comparerType != null);

            ILEmitter emitter = new ILEmitter();
            var codeStream = emitter.NewCodeStream();

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(comparerType.GetParameterlessConstructor()));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(methodBeingGenerated);
        }

        /// <summary>
        /// Gets the comparer type that is suitable to compare instances of <paramref name="type"/>
        /// or null if such comparer cannot be determined at compile time.
        /// </summary>
        private static TypeDesc GetComparerForType(TypeDesc type, ReadOnlySpan<byte> flavor, ReadOnlySpan<byte> interfaceName)
        {
            TypeSystemContext context = type.Context;

            if (context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any) ||
                (type.IsRuntimeDeterminedSubtype && !type.HasInstantiation))
            {
                // The comparer will be determined at runtime. We can't tell the exact type at compile time.
                return null;
            }

            if (type.IsNullable)
            {
                return context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Nullable"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type.Instantiation[0]);
            }

            if (type.IsString && flavor.SequenceEqual("EqualityComparer"u8))
            {
                return context.SystemModule.GetKnownType("System.Collections.Generic"u8, "StringEqualityComparer"u8);
            }

            if (type.IsEnum)
            {
                // Enums have a specialized comparer that avoids boxing
                return context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Enum"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type);
            }

            bool? implementsInterfaceOfSelf = ImplementsInterfaceOfSelf(type, interfaceName);
            if (!implementsInterfaceOfSelf.HasValue)
            {
                return null;
            }

            return context.SystemModule.GetKnownType("System.Collections.Generic"u8,
                implementsInterfaceOfSelf.Value ? "Generic"u8.Append(flavor, "`1"u8) : "Object"u8.Append(flavor, "`1"u8))
                .MakeInstantiatedType(type);
        }

        public static TypeDesc[] GetPotentialComparersForType(TypeDesc type)
        {
            return GetPotentialComparersForTypeCommon(type, "Comparer"u8, "IComparable`1"u8);
        }

        public static TypeDesc[] GetPotentialEqualityComparersForType(TypeDesc type)
        {
            return GetPotentialComparersForTypeCommon(type, "EqualityComparer"u8, "IEquatable`1"u8);
        }

        /// <summary>
        /// Gets the set of template types needed to support loading comparers for the give canonical type at runtime.
        /// </summary>
        private static TypeDesc[] GetPotentialComparersForTypeCommon(TypeDesc type, ReadOnlySpan<byte> flavor, ReadOnlySpan<byte> interfaceName)
        {
            Debug.Assert(type.IsCanonicalSubtype(CanonicalFormKind.Any));

            TypeDesc exactComparer = GetComparerForType(type, flavor, interfaceName);

            if (exactComparer != null)
            {
                // If we have a comparer that is exactly known at runtime, we're done.
                // This will typically be if type is a generic struct, generic enum, or a nullable.
                return new TypeDesc[] { exactComparer };
            }

            TypeSystemContext context = type.Context;

            if (context.IsCanonicalDefinitionType(type, CanonicalFormKind.Universal))
            {
                // This can be any of the comparers we have.

                ArrayBuilder<TypeDesc> universalComparers = default(ArrayBuilder<TypeDesc>);

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Nullable"u8.Append(flavor, "`1"u8))
                        .MakeInstantiatedType(type));

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Enum"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type));

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Generic"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type));

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Object"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type));

                return universalComparers.ToArray();
            }

            // This mirrors exactly what GetUnknownEquatableComparer and GetUnknownComparer (in the class library)
            // will need at runtime. This is the general purpose code path that can be used to compare
            // anything.

            if (type.IsNullable)
            {
                TypeDesc nullableType = type.Instantiation[0];

                // This should only be reachable for universal canon code.
                // For specific canon, this should have been an exact match above.
                Debug.Assert(context.IsCanonicalDefinitionType(nullableType, CanonicalFormKind.Universal));

                return new TypeDesc[]
                {
                    context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Nullable"u8.Append(flavor, "`1"u8))
                        .MakeInstantiatedType(nullableType),
                    context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Object"u8.Append(flavor, "`1"u8))
                        .MakeInstantiatedType(type),
                };
            }

            return new TypeDesc[]
            {
                context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Generic"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type),
                context.SystemModule.GetKnownType("System.Collections.Generic"u8, "Object"u8.Append(flavor, "`1"u8))
                    .MakeInstantiatedType(type),
            };
        }

        public static bool? ImplementsIEquatable(TypeDesc type)
            => ImplementsInterfaceOfSelf(type, "IEquatable`1"u8);

        private static bool? ImplementsInterfaceOfSelf(TypeDesc type, ReadOnlySpan<byte> interfaceName)
        {
            MetadataType interfaceType = null;

            foreach (TypeDesc implementedInterface in type.RuntimeInterfaces)
            {
                Instantiation interfaceInstantiation = implementedInterface.Instantiation;
                if (interfaceInstantiation.Length == 1)
                {
                    interfaceType ??= interfaceType = type.Context.SystemModule.GetKnownType("System"u8, interfaceName);

                    if (implementedInterface.GetTypeDefinition() == interfaceType)
                    {
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                        {
                            // Ignore interface instantiations that cannot possibly be the interface of self
                            if (implementedInterface.ConvertToCanonForm(CanonicalFormKind.Specific) !=
                                interfaceType.MakeInstantiatedType(type).ConvertToCanonForm(CanonicalFormKind.Specific))
                            {
                                continue;
                            }
                            // Try to prove that the interface of self is always implemented using the type definition.
                            TypeDesc typeDefinition = type.GetTypeDefinition();
                            return typeDefinition.CanCastTo(interfaceType.MakeInstantiatedType(typeDefinition)) ? true : null;
                        }
                        else
                        {
                            // Shortcut for exact match
                            if (interfaceInstantiation[0] == type)
                            {
                                Debug.Assert(type.CanCastTo(interfaceType.MakeInstantiatedType(type)));
                                return true;
                            }
                            return type.CanCastTo(interfaceType.MakeInstantiatedType(type));
                        }
                    }
                }
            }

            return false;
        }

        public static bool CanCompareValueTypeBits(MetadataType type, MethodDesc objectEqualsMethod)
        {
            return CanCompareValueTypeBitsUntilOffset(type, objectEqualsMethod, out int lastFieldOffset)
                && lastFieldOffset == type.InstanceFieldSize.AsInt;
        }

        public static bool CanCompareValueTypeBitsUntilOffset(MetadataType type, MethodDesc objectEqualsMethod, out int lastFieldEndOffset)
        {
            Debug.Assert(type.IsValueType);

            lastFieldEndOffset = 0;

            if (type.ContainsGCPointers)
                return false;

            if (type.IsInlineArray)
                return false;

            if (type.IsGenericDefinition)
                return false;

            OverlappingFieldTracker overlappingFieldTracker = new OverlappingFieldTracker(type);

            bool result = true;
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                lastFieldEndOffset = Math.Max(lastFieldEndOffset, field.Offset.AsInt + field.FieldType.GetElementSize().AsInt);

                if (!overlappingFieldTracker.TrackField(field))
                {
                    // This field overlaps with another field - can't compare memory
                    result = false;
                    break;
                }

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType.IsFunctionPointer)
                {
                    TypeFlags category = fieldType.UnderlyingType.Category;
                    if (category == TypeFlags.Single || category == TypeFlags.Double)
                    {
                        // Double/Single have weird behaviors around negative/positive zero
                        result = false;
                        break;
                    }
                }
                else
                {
                    // Would be a suprise if this wasn't a valuetype. We checked ContainsGCPointers above.
                    Debug.Assert(fieldType.IsValueType);

                    // If the field overrides Equals, we can't use the fast helper because we need to call the method.
                    if (fieldType.FindVirtualFunctionTargetMethodOnObjectType(objectEqualsMethod).OwningType == fieldType)
                    {
                        result = false;
                        break;
                    }

                    if (!CanCompareValueTypeBits((MetadataType)fieldType, objectEqualsMethod))
                    {
                        result = false;
                        break;
                    }
                }
            }

            // If there are gaps, we can't memcompare
            if (result && overlappingFieldTracker.HasGapsBeforeOffset(lastFieldEndOffset))
                result = false;

            return result;
        }

        /// <summary>
        /// Determines whether a value type's <see cref="System.IEquatable{T}"/> implementation of self is a
        /// plain field-wise comparison that is equivalent to a bitwise (memcmp) comparison. This lets a type
        /// that implements IEquatable&lt;T&gt; still be reported as bitwise-equatable when its Equals does
        /// nothing more than compare every field with ==.
        /// </summary>
        public static bool IsIEquatableEqualsFieldwise(MetadataType type)
        {
            // The type's layout must be tightly packed (no padding gaps and no overlapping fields) so
            // that a byte-wise compare never inspects bytes the field-wise Equals ignores. This is
            // checked at every level of the recursion, matching the CoreCLR VM.
            if (!IsTightlyPacked(type))
                return false;

            MethodDesc equalsImpl = GetIEquatableEqualsImplementation(type);
            if (equalsImpl is not EcmaMethod ecmaImpl)
                return false;

            MethodIL methodIL = EcmaMethodIL.Create(ecmaImpl);

            // A common pattern forwards `bool Equals(T other) => this == other;` to a user-defined
            // `op_Equality`. Follow that single forward before scanning the field-wise comparison.
            if (TryGetOpEqualityForward(methodIL, type) is EcmaMethod forwarded)
                methodIL = EcmaMethodIL.Create(forwarded);

            return ScanFieldwiseEqualsBody(methodIL, type);
        }

        private static bool IsTightlyPacked(MetadataType type)
        {
            // Mirrors the CoreCLR VM's MethodTable::IsNotTightlyPacked (negated): a byte-wise compare
            // equals comparing every field only if there is no padding anywhere. That needs the declared
            // fields to exactly cover the instance size (no gaps, no overlap) and every nested value-type
            // field to itself be tightly packed. The nested check makes this transitive, like the VM flag.
            if (type.ContainsGCPointers)
                return false;

            if (type.IsInlineArray)
                return false;

            if (type.IsGenericDefinition)
                return false;

            OverlappingFieldTracker overlappingFieldTracker = new OverlappingFieldTracker(type);
            int lastFieldEndOffset = 0;

            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                lastFieldEndOffset = Math.Max(lastFieldEndOffset, field.Offset.AsInt + field.FieldType.GetElementSize().AsInt);

                if (!overlappingFieldTracker.TrackField(field))
                    return false;

                TypeDesc fieldType = field.FieldType;
                if (!fieldType.IsPrimitive && !fieldType.IsEnum && !fieldType.IsPointer && !fieldType.IsFunctionPointer)
                {
                    // Not a leaf field, so (having excluded GC pointers above) it is a nested value type.
                    if (fieldType is not MetadataType nestedType || !IsTightlyPacked(nestedType))
                        return false;
                }
            }

            if (overlappingFieldTracker.HasGapsBeforeOffset(lastFieldEndOffset))
                return false;

            return lastFieldEndOffset == type.InstanceFieldSize.AsInt;
        }

        private static MethodDesc GetIEquatableEqualsImplementation(MetadataType type)
        {
            // Keep token resolution simple by only handling non-generic value types, matching the VM.
            if (type.HasInstantiation)
                return null;

            MetadataType iequatableType = type.Context.SystemModule.GetKnownType("System"u8, "IEquatable`1"u8);
            MethodDesc equalsInterfaceMethod = iequatableType.MakeInstantiatedType(type).GetMethod("Equals"u8, null);
            if (equalsInterfaceMethod == null)
                return null;

            return type.ResolveInterfaceMethodToVirtualMethodOnType(equalsInterfaceMethod);
        }

        private static MethodDesc TryGetOpEqualityForward(MethodIL methodIL, MetadataType type)
        {
            // ldarg.0; ldobj T; ldarg.1; call op_Equality; ret
            ILReader reader = new ILReader(methodIL.GetILBytes());

            if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ldarg_0)
                return null;
            if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ldobj)
                return null;
            if (methodIL.GetObject(reader.ReadILToken()) as TypeDesc != type)
                return null;
            if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ldarg_1)
                return null;
            if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.call)
                return null;
            MethodDesc callee = methodIL.GetObject(reader.ReadILToken()) as MethodDesc;
            if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ret)
                return null;
            if (reader.HasNext)
                return null;

            if (callee == null || !callee.Signature.IsStatic || callee.OwningType != type || callee.Name != "op_Equality"u8)
                return null;

            return callee;
        }

        private static bool ScanFieldwiseEqualsBody(MethodIL methodIL, MetadataType type)
        {
            try
            {
                return ScanFieldwiseEqualsBodyCore(methodIL, type);
            }
            catch (TypeSystemException.InvalidProgramException)
            {
                // Malformed or truncated IL: stay conservative and treat it as not field-wise.
                return false;
            }
        }

        private static bool ScanFieldwiseEqualsBodyCore(MethodIL methodIL, MetadataType type)
        {
            // Verifies the body is a plain field-wise equality: every instance field is compared exactly once
            // (via `==`, its own `Equals`, or `EqualityComparer<F>.Default.Equals` for records) and the results
            // are ANDed together, which is equivalent to a bitwise (memcmp) comparison.
            int instanceFieldCount = 0;
            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic)
                    instanceFieldCount++;
            }

            if (instanceFieldCount == 0)
                return false;

            HashSet<FieldDesc> comparedFields = new HashSet<FieldDesc>();
            ILReader reader = new ILReader(methodIL.GetILBytes());

            int falseTarget = -1;
            bool sawFinalCompare = false;

            while (!sawFinalCompare)
            {
                if (!reader.HasNext)
                    return false;

                // Optional records lead-in: `call EqualityComparer<F>::get_Default` before the operands.
                MethodDesc getDefault = null;
                bool records = false;
                if (reader.PeekILOpcode() == ILOpcode.call)
                {
                    reader.ReadILOpcode();
                    getDefault = methodIL.GetObject(reader.ReadILToken()) as MethodDesc;
                    records = true;
                }

                // Left operand: `ldarg.0; ldfld/ldflda F`. Records and inline `==` load by value; the
                // `.Equals` call form loads the left side by address.
                if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ldarg_0)
                    return false;

                ILOpcode leftLoad = reader.ReadILOpcode();
                if (leftLoad != ILOpcode.ldfld && leftLoad != ILOpcode.ldflda)
                    return false;
                if (records && leftLoad != ILOpcode.ldfld)
                    return false;
                FieldDesc leftField = methodIL.GetObject(reader.ReadILToken()) as FieldDesc;

                if (reader.ReadILOpcode() != ILOpcode.ldarg_1)
                    return false;
                if (reader.ReadILOpcode() != ILOpcode.ldfld)
                    return false;
                FieldDesc rightField = methodIL.GetObject(reader.ReadILToken()) as FieldDesc;

                if (leftField == null || leftField != rightField || leftField.IsStatic || leftField.OwningType != type)
                    return false;

                // Each field must be compared exactly once.
                if (!comparedFields.Add(leftField))
                    return false;

                if (!records && leftLoad == ILOpcode.ldfld)
                {
                    // Inline `==`: only integer-like primitives are memcmp-equivalent.
                    if (!IsBitwiseComparablePrimitive(leftField.FieldType))
                        return false;

                    ILOpcode compareOpcode = reader.ReadILOpcode();
                    if (compareOpcode == ILOpcode.bne_un_s)
                    {
                        // Non-final field: `bne.un.s FALSE` jumps to the shared `return false` tail.
                        int target = reader.ReadBranchDestination(compareOpcode);
                        if (falseTarget == -1)
                            falseTarget = target;
                        else if (falseTarget != target)
                            return false;
                    }
                    else if (compareOpcode == ILOpcode.ceq)
                    {
                        // Final field: `ceq; ret` produces the result directly.
                        if (reader.ReadILOpcode() != ILOpcode.ret)
                            return false;
                        sawFinalCompare = true;
                    }
                    else
                    {
                        return false;
                    }

                    continue;
                }

                if (records)
                {
                    // `callvirt EqualityComparer<F>::Equals(!0, !0)`.
                    if (reader.ReadILOpcode() != ILOpcode.callvirt)
                        return false;
                    MethodDesc equals = methodIL.GetObject(reader.ReadILToken()) as MethodDesc;
                    if (!IsEqualityComparerDefaultEquals(getDefault, equals, leftField.FieldType))
                        return false;
                }
                else
                {
                    // `.Equals` call form: a primitive's own Equals, or a nested type's field-wise Equals.
                    if (reader.ReadILOpcode() != ILOpcode.call)
                        return false;
                    MethodDesc callee = methodIL.GetObject(reader.ReadILToken()) as MethodDesc;
                    if (!IsPrimitiveEqualsCall(callee, leftField.FieldType) && !IsNestedFieldwiseEquatable(callee, leftField.FieldType))
                        return false;
                }

                // The Equals call already yields a bool: `brfalse.s` to the shared tail, or `ret` if final.
                ILOpcode terminator = reader.ReadILOpcode();
                if (terminator == ILOpcode.brfalse_s)
                {
                    int target = reader.ReadBranchDestination(terminator);
                    if (falseTarget == -1)
                        falseTarget = target;
                    else if (falseTarget != target)
                        return false;
                }
                else if (terminator == ILOpcode.ret)
                {
                    sawFinalCompare = true;
                }
                else
                {
                    return false;
                }
            }

            if (falseTarget != -1)
            {
                // Shared tail for a mismatch: `ldc.i4.0; ret`.
                if (reader.Offset != falseTarget)
                    return false;
                if (!reader.HasNext || reader.ReadILOpcode() != ILOpcode.ldc_i4_0)
                    return false;
                if (reader.ReadILOpcode() != ILOpcode.ret)
                    return false;
            }

            return !reader.HasNext && comparedFields.Count == instanceFieldCount;
        }

        private static bool IsNestedFieldwiseEquatable(MethodDesc callee, TypeDesc fieldType)
        {
            // The nested field must be compared through the nested type's own IEquatable<F>.Equals, and
            // that Equals must itself be field-wise (its layout is validated by IsIEquatableEqualsFieldwise).
            if (callee == null || fieldType is not MetadataType nestedType || !nestedType.IsValueType)
                return false;

            if (callee != GetIEquatableEqualsImplementation(nestedType))
                return false;

            return IsIEquatableEqualsFieldwise(nestedType);
        }

        private static bool IsPrimitiveEqualsCall(MethodDesc callee, TypeDesc fieldType)
        {
            // A primitive field compared via 'x.Equals(y)' instead of 'x == y'; for these integer-like
            // types both lower to the same bit-for-bit compare. Confirm the callee is its IEquatable<F>.Equals.
            if (callee == null || !IsBitwiseComparablePrimitive(fieldType))
                return false;

            return fieldType is MetadataType primitiveType
                && callee == GetIEquatableEqualsImplementation(primitiveType);
        }

        private static bool IsEqualityComparerDefaultEquals(MethodDesc getDefault, MethodDesc equals, TypeDesc fieldType)
        {
            // Records compare each field with EqualityComparer<F>.Default.Equals(this.F, other.F). That is
            // a memcmp only when F is itself bitwise-equatable: a bit-comparable primitive, or a nested
            // value type whose own IEquatable<F>.Equals is field-wise.
            if (!IsEqualityComparerMethod(getDefault, fieldType, "get_Default"u8, isStatic: true) ||
                !IsEqualityComparerMethod(equals, fieldType, "Equals"u8, isStatic: false))
            {
                return false;
            }

            if (IsBitwiseComparablePrimitive(fieldType))
                return true;

            return fieldType is MetadataType nestedType && nestedType.IsValueType
                && GetIEquatableEqualsImplementation(nestedType) != null
                && IsIEquatableEqualsFieldwise(nestedType);
        }

        private static bool IsEqualityComparerMethod(MethodDesc method, TypeDesc fieldType, ReadOnlySpan<byte> name, bool isStatic)
        {
            if (method == null || method.Signature.IsStatic != isStatic || method.Name != name)
                return false;

            MetadataType equalityComparer = fieldType.Context.SystemModule.GetType("System.Collections.Generic"u8, "EqualityComparer`1"u8, throwIfNotFound: false);
            TypeDesc owningType = method.OwningType;
            return equalityComparer != null
                && owningType.GetTypeDefinition() == equalityComparer
                && owningType.Instantiation.Length == 1
                && owningType.Instantiation[0] == fieldType;
        }

        private static bool IsBitwiseComparablePrimitive(TypeDesc fieldType)
        {
            if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType.IsFunctionPointer)
            {
                TypeFlags category = fieldType.UnderlyingType.Category;
                return category != TypeFlags.Single && category != TypeFlags.Double;
            }

            return false;
        }

        private struct OverlappingFieldTracker
        {
            private BitArray _usedBytes;

            public OverlappingFieldTracker(MetadataType type)
            {
                _usedBytes = new BitArray(type.InstanceFieldSize.AsInt);
            }

            public bool TrackField(FieldDesc field)
            {
                int fieldBegin = field.Offset.AsInt;

                TypeDesc fieldType = field.FieldType;

                int fieldEnd;
                if (fieldType.IsPointer || fieldType.IsFunctionPointer)
                {
                    fieldEnd = fieldBegin + field.Context.Target.PointerSize;
                }
                else
                {
                    Debug.Assert(fieldType.IsValueType);
                    fieldEnd = fieldBegin + ((DefType)fieldType).InstanceFieldSize.AsInt;
                }

                for (int i = fieldBegin; i < fieldEnd; i++)
                {
                    if (_usedBytes[i])
                        return false;
                    _usedBytes[i] = true;
                }

                return true;
            }

            public bool HasGapsBeforeOffset(int offset)
            {
                for (int i = 0; i < offset; i++)
                    if (!_usedBytes[i])
                        return true;

                return false;
            }
        }
    }
}
