// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

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
            return EmitComparerAndEqualityComparerCreateCommon(target, "Comparer", "IComparable`1");
        }

        /// <summary>
        /// Generates a specialized method body for EqualityComparer`1.Create or returns null if no specialized body can be generated.
        /// </summary>
        public static MethodIL EmitEqualityComparerCreate(MethodDesc target)
        {
            return EmitComparerAndEqualityComparerCreateCommon(target, "EqualityComparer", "IEquatable`1");
        }

        /// <summary>
        /// Gets the concrete type Comparer`1.Create returns or null if it's not known at compile time.
        /// </summary>
        public static TypeDesc GetComparerForType(TypeDesc comparand)
        {
            return GetComparerForType(comparand, "Comparer", "IComparable`1");
        }

        /// <summary>
        /// Gets the concrete type EqualityComparer`1.Create returns or null if it's not known at compile time.
        /// </summary>
        public static TypeDesc GetEqualityComparerForType(TypeDesc comparand)
        {
            return GetComparerForType(comparand, "EqualityComparer", "IEquatable`1");
        }

        private static MethodIL EmitComparerAndEqualityComparerCreateCommon(MethodDesc methodBeingGenerated, string flavor, string interfaceName)
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

            FieldDesc defaultField = owningType.GetKnownField("s_default");

            TypeSystemContext  context = comparerType.Context;
            TypeDesc objectType = context.GetWellKnownType(WellKnownType.Object);
            MethodDesc compareExchangeObject = context.SystemModule.
                GetKnownType("System.Threading", "Interlocked").
                    GetKnownMethod("CompareExchange",
                        new MethodSignature(
                            MethodSignatureFlags.Static,
                            genericParameterCount: 0,
                            returnType: objectType,
                            parameters: new TypeDesc[] { objectType.MakeByRefType(), objectType, objectType }));

            codeStream.Emit(ILOpcode.ldsflda, emitter.NewToken(defaultField));
            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(comparerType.GetParameterlessConstructor()));
            codeStream.Emit(ILOpcode.ldnull);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(compareExchangeObject));
            codeStream.Emit(ILOpcode.pop);
            codeStream.Emit(ILOpcode.ldsfld, emitter.NewToken(defaultField));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(methodBeingGenerated);
        }

        /// <summary>
        /// Gets the comparer type that is suitable to compare instances of <paramref name="type"/>
        /// or null if such comparer cannot be determined at compile time.
        /// </summary>
        private static TypeDesc GetComparerForType(TypeDesc type, string flavor, string interfaceName)
        {
            TypeSystemContext context = type.Context;

            if (context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any) ||
                (type.IsRuntimeDeterminedSubtype && !type.HasInstantiation))
            {
                // The comparer will be determined at runtime. We can't tell the exact type at compile time.
                return null;
            }
            else if (type.IsNullable)
            {
                TypeDesc nullableType = type.Instantiation[0];

                if (context.IsCanonicalDefinitionType(nullableType, CanonicalFormKind.Universal))
                {
                    // We can't tell at compile time either.
                    return null;
                }

                return context.SystemModule.GetKnownType("System.Collections.Generic", $"Nullable{flavor}`1")
                    .MakeInstantiatedType(nullableType);
            }
            else if (type.IsEnum)
            {
                // Enums have a specialized comparer that avoids boxing
                return context.SystemModule.GetKnownType("System.Collections.Generic", $"Enum{flavor}`1")
                    .MakeInstantiatedType(type);
            }
            else if (ImplementsInterfaceOfSelf(type, interfaceName))
            {
                return context.SystemModule.GetKnownType("System.Collections.Generic", $"Generic{flavor}`1")
                    .MakeInstantiatedType(type);
            }

            return context.SystemModule.GetKnownType("System.Collections.Generic", $"Object{flavor}`1")
                    .MakeInstantiatedType(type);
        }

        public static TypeDesc[] GetPotentialComparersForType(TypeDesc type)
        {
            return GetPotentialComparersForTypeCommon(type, "Comparer", "IComparable`1");
        }

        public static TypeDesc[] GetPotentialEqualityComparersForType(TypeDesc type)
        {
            return GetPotentialComparersForTypeCommon(type, "EqualityComparer", "IEquatable`1");
        }

        /// <summary>
        /// Gets the set of template types needed to support loading comparers for the give canonical type at runtime.
        /// </summary>
        private static TypeDesc[] GetPotentialComparersForTypeCommon(TypeDesc type, string flavor, string interfaceName)
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

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic", $"Nullable{flavor}`1")
                        .MakeInstantiatedType(type));

                if (flavor == "EqualityComparer")
                    universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic", $"Enum{flavor}`1")
                        .MakeInstantiatedType(type));

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic", $"Generic{flavor}`1")
                    .MakeInstantiatedType(type));

                universalComparers.Add(context.SystemModule.GetKnownType("System.Collections.Generic", $"Object{flavor}`1")
                    .MakeInstantiatedType(type));

                return universalComparers.ToArray();
            }

            // This mirrors exactly what GetUnknownEquatableComparer and GetUnknownComparer (in the class library)
            // will need at runtime. This is the general purpose code path that can be used to compare
            // anything.

            if (type.IsNullable)
            {
                TypeDesc nullableType = type.Instantiation[0];

                // This should only be reachabe for universal canon code.
                // For specific canon, this should have been an exact match above.
                Debug.Assert(context.IsCanonicalDefinitionType(nullableType, CanonicalFormKind.Universal));

                return new TypeDesc[]
                {
                    context.SystemModule.GetKnownType("System.Collections.Generic", $"Nullable{flavor}`1")
                        .MakeInstantiatedType(nullableType),
                    context.SystemModule.GetKnownType("System.Collections.Generic", $"Object{flavor}`1")
                        .MakeInstantiatedType(type),
                };
            }

            return new TypeDesc[]
            {
                context.SystemModule.GetKnownType("System.Collections.Generic", $"Generic{flavor}`1")
                    .MakeInstantiatedType(type),
                context.SystemModule.GetKnownType("System.Collections.Generic", $"Object{flavor}`1")
                    .MakeInstantiatedType(type),
            };
        }

        public static bool ImplementsIEquatable(TypeDesc type)
            => ImplementsInterfaceOfSelf(type, "IEquatable`1");

        private static bool ImplementsInterfaceOfSelf(TypeDesc type, string interfaceName)
        {
            MetadataType interfaceType = null;

            foreach (TypeDesc implementedInterface in type.RuntimeInterfaces)
            {
                Instantiation interfaceInstantiation = implementedInterface.Instantiation;
                if (interfaceInstantiation.Length == 1 &&
                    interfaceInstantiation[0] == type)
                {
                    interfaceType ??= interfaceType = type.Context.SystemModule.GetKnownType("System", interfaceName);

                    if (implementedInterface.GetTypeDefinition() == interfaceType)
                        return true;
                }
            }

            return false;
        }

        public static bool CanCompareValueTypeBits(MetadataType type, MethodDesc objectEqualsMethod)
        {
            Debug.Assert(type.IsValueType);

            if (type.ContainsGCPointers)
                return false;

            if (type.IsGenericDefinition)
                return false;

            OverlappingFieldTracker overlappingFieldTracker = new OverlappingFieldTracker(type);

            bool result = true;
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

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
            if (result && overlappingFieldTracker.HasGaps)
                result = false;

            return result;
        }

        private struct OverlappingFieldTracker
        {
            private bool[] _usedBytes;

            public OverlappingFieldTracker(MetadataType type)
            {
                _usedBytes = new bool[type.InstanceFieldSize.AsInt];
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

            public bool HasGaps
            {
                get
                {
                    for (int i = 0; i < _usedBytes.Length; i++)
                        if (!_usedBytes[i])
                            return true;

                    return false;
                }
            }
        }

    }
}
