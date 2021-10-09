// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;
using ILVerify;

namespace Internal.IL
{
    struct StackValue
    {
        [Flags]
        public enum StackValueFlags
        {
            None = 0,
            ReadOnly = 1 << 1,
            PermanentHome = 1 << 2,
            ThisPtr = 1 << 3,
        }
        private StackValueFlags Flags;

        public readonly StackValueKind Kind;
        public readonly TypeDesc Type;
        public readonly MethodDesc Method;

        private StackValue(StackValueKind kind, TypeDesc type = null, MethodDesc method = null, StackValueFlags flags = StackValueFlags.None)
        {
            this.Kind = kind;
            this.Type = type;
            this.Method = method;
            this.Flags = flags;
        }

        public void SetIsReadOnly()
        {
            Debug.Assert(Kind == StackValueKind.ByRef);
            Flags |= StackValueFlags.ReadOnly;
        }

        public void SetIsPermanentHome()
        {
            Debug.Assert(Kind == StackValueKind.ByRef);
            Flags |= StackValueFlags.PermanentHome;
        }

        public void SetIsThisPtr()
        {
            Flags |= StackValueFlags.ThisPtr;
        }

        public bool IsReadOnly
        {
            get { return (Flags & StackValueFlags.ReadOnly) == StackValueFlags.ReadOnly; }
        }

        public bool IsPermanentHome
        {
            get { return (Flags & StackValueFlags.PermanentHome) == StackValueFlags.PermanentHome; }
        }

        public bool IsThisPtr
        {
            get { return (Flags & StackValueFlags.ThisPtr) == StackValueFlags.ThisPtr; }
        }

        public bool IsNullReference
        {
            get { return Kind == StackValueKind.ObjRef && Type == null; }
        }

        public bool IsMethod
        {
            get { return Kind == StackValueKind.NativeInt && Method != null; }
        }

        public bool IsBoxedValueType
        {
            get { return Kind == StackValueKind.ObjRef && Type.IsValueType; }
        }

        public StackValue DereferenceByRef()
        {
            Debug.Assert(Kind == StackValueKind.ByRef && Type != null, "Cannot dereference");
            return CreateFromType(Type);
        }

        static public StackValue CreateUnknown()
        {
            return new StackValue(StackValueKind.Unknown);
        }

        static public StackValue CreatePrimitive(StackValueKind kind)
        {
            Debug.Assert(kind == StackValueKind.Int32 ||
                         kind == StackValueKind.Int64 ||
                         kind == StackValueKind.NativeInt ||
                         kind == StackValueKind.Float);

            return new StackValue(kind);
        }

        static public StackValue CreateObjRef(TypeDesc type)
        {
            return new StackValue(StackValueKind.ObjRef, type);
        }

        static public StackValue CreateValueType(TypeDesc type)
        {
            return new StackValue(StackValueKind.ValueType, type);
        }

        static public StackValue CreateByRef(TypeDesc type, bool readOnly = false, bool permanentHome = false)
        {
            return new StackValue(StackValueKind.ByRef, type, null,
                (readOnly ? StackValueFlags.ReadOnly : StackValueFlags.None) |
                (permanentHome ? StackValueFlags.PermanentHome : StackValueFlags.None));
        }

        static public StackValue CreateMethod(MethodDesc method)
        {
            return new StackValue(StackValueKind.NativeInt, null, method);
        }

        static public StackValue CreateFromType(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.SByte:
                case TypeFlags.Byte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                    return CreatePrimitive(StackValueKind.Int32);
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                    return CreatePrimitive(StackValueKind.Int64);
                case TypeFlags.Single:
                case TypeFlags.Double:
                    return CreatePrimitive(StackValueKind.Float);
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    return CreatePrimitive(StackValueKind.NativeInt);
                case TypeFlags.Enum:
                    return CreateFromType(type.UnderlyingType);
                case TypeFlags.ByRef:
                    return CreateByRef(((ByRefType)type).ParameterType);
                default:
                    if (type.IsValueType || type.IsGenericParameter)
                        return CreateValueType(type);
                    else
                        return CreateObjRef(type);
            }
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;

            if (!(obj is StackValue))
                return false;

            var value = (StackValue)obj;
            return this.Kind == value.Kind && this.Flags == value.Flags && this.Type == value.Type;
        }

        public static bool operator ==(StackValue left, StackValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StackValue left, StackValue right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
                const int prime = 17;
                int hash = 23;
                hash = (hash * prime) ^ Type.GetHashCode();
                hash = (hash * prime) ^ Kind.GetHashCode();
                hash = (hash * prime) ^ Flags.GetHashCode();
                return hash;
        }

        // For now, match PEVerify type formating to make it easy to compare with baseline
        static string TypeToStringForByRef(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Boolean: return "Boolean";
                case TypeFlags.Char:    return "Char";
                case TypeFlags.SByte:   return "SByte";
                case TypeFlags.Byte:    return "Byte";
                case TypeFlags.Int16:   return "Int16";
                case TypeFlags.UInt16:  return "UInt16";
                case TypeFlags.Int32:   return "Int32";
                case TypeFlags.UInt32:  return "UInt32";
                case TypeFlags.Int64:   return "Int64";
                case TypeFlags.UInt64:  return "UInt64";
                case TypeFlags.Single:  return "Single";
                case TypeFlags.Double:  return "Double";
                case TypeFlags.IntPtr:  return "IntPtr";
                case TypeFlags.UIntPtr: return "UIntPtr";
            }

            return "'" + type.ToString() + "'";
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case StackValueKind.Int32:
                    return "Int32";
                case StackValueKind.Int64:
                    return "Long";
                case StackValueKind.NativeInt:
                    return "Native Int";
                case StackValueKind.Float:
                    return "Double";
                case StackValueKind.ByRef:
                    return (IsReadOnly ? "readonly " : "") + "address of " + TypeToStringForByRef(Type);
                case StackValueKind.ObjRef:
                    return (Type != null) ? "ref '" + Type.ToString() + "'" : "Nullobjref 'NullReference'";
                case StackValueKind.ValueType:
                    return "value '" + Type.ToString() + "'";
                default:
                    return "unknown";
            }
        }
    }

    partial class ILImporter
    {
        /// <summary>
        /// Merges two stack values to a common stack value as defined in the ECMA-335
        /// standard III.1.8.1.3 (Merging stack states).
        /// </summary>
        /// <param name="valueA">The value to be merged with <paramref name="valueB"/>.</param>
        /// <param name="valueB">The value to be merged with <paramref name="valueA"/>.</param>
        /// <param name="merged">The resulting type of merging <paramref name="valueA"/> and <paramref name="valueB"/>.</param>
        /// <returns>True if merge operation was successful, false if the merge operation failed.</returns>
        public static bool TryMergeStackValues(StackValue valueA, StackValue valueB, out StackValue merged)
        {
            merged = valueA;

            if (valueB.IsReadOnly)
                merged.SetIsReadOnly();

            // Same type
            if (valueA.Kind == valueB.Kind && valueA.Type == valueB.Type)
                return true;

            if (valueA.IsNullReference)
            {
                //Null can be any reference type
                if (valueB.Kind == StackValueKind.ObjRef)
                {
                    merged = valueB;
                    return true;
                }
            }
            else if (valueA.Kind == StackValueKind.ObjRef)
            {
                if (valueB.Kind != StackValueKind.ObjRef)
                    return false;

                // Null can be any reference type
                if (valueB.IsNullReference)
                    return true;

                // Merging classes always succeeds since System.Object always works
                merged = StackValue.CreateFromType(MergeObjectReferences(valueA.Type, valueB.Type));
                return true;
            }

            return false;
        }

        // Used to merge stack states.
        static TypeDesc MergeObjectReferences(TypeDesc classA, TypeDesc classB)
        {
            if (classA == classB)
                return classA;

            // Array case
            if (classA.IsArray)
            {
                if (classB.IsArray)
                    return MergeArrayTypes((ArrayType)classA, (ArrayType)classB);
            }

            // Assumes generic parameters are boxed at this point.
            // Return supertype, if related, otherwhise object
            if (classA.IsGenericParameter || classB.IsGenericParameter)
            {
                if (classA.CanCastTo(classB))
                    return classB;
                if (classB.CanCastTo(classA))
                    return classA;

                return classA.Context.GetWellKnownType(WellKnownType.Object);
            }

            if (classB.IsInterface)
            {
                if (classA.IsInterface)
                    return MergeInterfaceWithInterface(classA, classB);
                else
                    return MergeClassWithInterface(classA, classB);
            }
            else if (classA.IsInterface)
                return MergeClassWithInterface(classB, classA);

            return MergeClassWithClass(classA, classB);
        }

        static TypeDesc MergeInterfaceWithInterface(TypeDesc interfA, TypeDesc interfB)
        {
            foreach (var interf in interfA.RuntimeInterfaces)
            {
                if (interf == interfB)
                    return interfB; // Interface A extends interface B
            }

            foreach (var interf in interfB.RuntimeInterfaces)
            {
                if (interf == interfA)
                    return interfA; // Interface B extends interface A
            }

            // Get common supertype
            foreach (var subInterfB in interfB.RuntimeInterfaces)
            {
                foreach (var subInterfA in interfA.RuntimeInterfaces)
                {
                    if (subInterfA == subInterfB)
                        return subInterfA;
                }
            }

            // No compatible interface found, return Object
            return interfA.Context.GetWellKnownType(WellKnownType.Object);
        }

        static TypeDesc MergeClassWithClass(TypeDesc classA, TypeDesc classB)
        {
            // Find class hierarchy depth for both classes
            int aDepth = 0;
            int bDepth = 0;
            TypeDesc curType;

            for (curType = classA; curType != null; curType = curType.BaseType)
                aDepth++;

            for (curType = classB; curType != null; curType = curType.BaseType)
                bDepth++;

            // Walk up superclass chain until both classes at same level
            while (aDepth > bDepth)
            {
                classA = classA.BaseType;
                aDepth--;
            }

            while (bDepth > aDepth)
            {
                classB = classB.BaseType;
                bDepth--;
            }

            while (classA != classB)
            {
                classA = classA.BaseType;
                classB = classB.BaseType;
            }

            // At this point we should either have found a common supertype or end up at System.Object
            Debug.Assert(classA != null);

            return classA;
        }

        static TypeDesc MergeClassWithInterface(TypeDesc classType, TypeDesc interfaceType)
        {
            // Check if class implements interface
            foreach (var interf in classType.RuntimeInterfaces)
            {
                if (interf == interfaceType)
                    return interfaceType;
            }

            // Check if class and interface implement common interface
            foreach (var iInterf in interfaceType.RuntimeInterfaces)
            {
                foreach (var cInterf in classType.RuntimeInterfaces)
                {
                    if (iInterf == cInterf)
                        return iInterf;
                }
            }

            // No compatible merge, return Object
            return classType.Context.GetWellKnownType(WellKnownType.Object);
        }

        static TypeDesc MergeArrayTypes(ArrayType arrayTypeA, ArrayType arrayTypeB)
        {
            if (arrayTypeA == arrayTypeB)
                return arrayTypeA;

            var basicArrayType = arrayTypeA.Context.GetWellKnownType(WellKnownType.Array);

            // If non matching rank, common ancestor = System.Array
            var rank = arrayTypeA.Rank;
            var mergeCategory = arrayTypeA.Category;
            if (rank != arrayTypeB.Rank)
                return basicArrayType;

            if (arrayTypeA.Category != arrayTypeB.Category)
            {
                if (rank == 1)
                    mergeCategory = TypeFlags.Array;
                else
                    return basicArrayType;
            }

            // Determine merged array element type
            TypeDesc mergedElementType;
            if (arrayTypeA.ElementType == arrayTypeB.ElementType)
                mergedElementType = arrayTypeA.ElementType;
            else if (arrayTypeA.ElementType.IsArray && arrayTypeB.ElementType.IsArray)
            {
                // Array of arrays -> find merged type
                mergedElementType = MergeArrayTypes(arrayTypeA, arrayTypeB);
            }
            //Both array element types are ObjRefs
            else if ((!arrayTypeA.ElementType.IsValueType && !arrayTypeA.ElementType.IsByRef) &&
                     (!arrayTypeB.ElementType.IsValueType && !arrayTypeB.ElementType.IsByRef))
            {
                // Find common ancestor of the element types
                mergedElementType = MergeObjectReferences(arrayTypeA.ElementType, arrayTypeB.ElementType);
            }
            else
            {
                // Array element types have nothing in common
                return basicArrayType;
            }

            Debug.Assert(mergeCategory == TypeFlags.Array || mergeCategory == TypeFlags.SzArray);

            if (mergeCategory == TypeFlags.SzArray)
                return arrayTypeA.Context.GetArrayType(mergedElementType);
            else
                return arrayTypeA.Context.GetArrayType(mergedElementType, rank);
        }

        static bool IsSameReducedType(TypeDesc src, TypeDesc dst)
        {
            return src.GetReducedType() == dst.GetReducedType();
        }

        bool IsAssignable(TypeDesc src, TypeDesc dst, bool allowSizeEquivalence = false)
        {
            if (src == dst)
                return true;

            if (src.IsValueType || dst.IsValueType)
            {
                if (allowSizeEquivalence && IsSameReducedType(src, dst))
                    return true;

                // TODO IsEquivalent
                return false;
            }

            return CastingHelper.CanCastTo(src, dst);
        }

        bool IsAssignable(StackValue src, StackValue dst)
        {
            if (src.Kind == dst.Kind && src.Type == dst.Type && src.IsReadOnly == dst.IsReadOnly)
                return true;

            if (dst.Type == null)
                return false;

            switch (src.Kind)
            {
            case StackValueKind.ObjRef:
                if (dst.Kind != StackValueKind.ObjRef)
                    return false;

                // null is always assignable
                if (src.Type == null)
                    return true;

                return CastingHelper.CanCastTo(src.Type, dst.Type);

            case StackValueKind.ValueType:

                // TODO: Other cases - variance, etc.

                return false;

            case StackValueKind.ByRef:
                if (dst.Kind == StackValueKind.ByRef && dst.IsReadOnly)
                    return src.Type == dst.Type;

                // TODO: Other cases - variance, etc.

                return false;

            case StackValueKind.Int32:
                return (dst.Kind == StackValueKind.Int64 || dst.Kind == StackValueKind.NativeInt);

            case StackValueKind.Int64:
                return false;

            case StackValueKind.NativeInt:
                return (dst.Kind == StackValueKind.Int64);

            case StackValueKind.Float:
                return false;

            default:
                // TODO:
                // return false;
                throw new NotImplementedException();
            }

#if false
    if (child == parent)
    {
        return(TRUE);
    }

    // Normally we just let the runtime sort it out but we wish to be more strict
    // than the runtime wants to be.  For backwards compatibility, the runtime considers
    // int32[] and nativeInt[] to be the same on 32-bit machines.  It also is OK with
    // int64[] and nativeInt[] on a 64-bit machine.

    if (child.IsType(TI_REF) && parent.IsType(TI_REF)
        && jitInfo->isSDArray(child.GetClassHandleForObjRef())
        && jitInfo->isSDArray(parent.GetClassHandleForObjRef()))
    {
        BOOL runtime_OK;

        // never be more lenient than the runtime
        runtime_OK = jitInfo->canCast(child.m_cls, parent.m_cls);
        if (!runtime_OK)
            return false;

        CORINFO_CLASS_HANDLE handle;
        CorInfoType pType = jitInfo->getChildType(child.GetClassHandleForObjRef(),  &handle);
        CorInfoType cType = jitInfo->getChildType(parent.GetClassHandleForObjRef(), &handle);

        // don't care whether it is signed
        if (cType == CORINFO_TYPE_NATIVEUINT)
            cType = CORINFO_TYPE_NATIVEINT;
        if (pType == CORINFO_TYPE_NATIVEUINT)
            pType = CORINFO_TYPE_NATIVEINT;

        if (cType == CORINFO_TYPE_NATIVEINT)
            return pType == CORINFO_TYPE_NATIVEINT;

        if (pType == CORINFO_TYPE_NATIVEINT)
            return cType == CORINFO_TYPE_NATIVEINT;

        return runtime_OK;
    }

    if (parent.IsUnboxedGenericTypeVar() || child.IsUnboxedGenericTypeVar())
    {
        return (FALSE);  // need to have had child == parent
    }
    else if (parent.IsType(TI_REF))
    {
        // An uninitialized objRef is not compatible to initialized.
        if (child.IsUninitialisedObjRef() && !parent.IsUninitialisedObjRef())
            return FALSE;

        if (child.IsNullObjRef())                   // NULL can be any reference type
            return TRUE;
        if (!child.IsType(TI_REF))
            return FALSE;

        return jitInfo->canCast(child.m_cls, parent.m_cls);

    }
    else if (parent.IsType(TI_METHOD))
    {
        if (!child.IsType(TI_METHOD))
            return FALSE;

        // Right now we don't bother merging method handles
        return FALSE;
    }
    else if (parent.IsType(TI_STRUCT))
    {
        if (!child.IsType(TI_STRUCT))
            return FALSE;

        // Structures are compatible if they are equivalent
        return jitInfo->areTypesEquivalent(child.m_cls, parent.m_cls);
    }
    else if (parent.IsByRef())
    {
        return tiCompatibleWithByRef(jitInfo, child, parent);
    }

    return FALSE;
#endif
        }


        bool IsBinaryComparable(StackValue src, StackValue dst, ILOpcode op)
        {
            if (src.Kind == dst.Kind && src.Type == dst.Type)
                return true;

            switch (src.Kind)
            {
                case StackValueKind.ObjRef:
                    switch (dst.Kind)
                    {
                        case StackValueKind.ObjRef:
                            // ECMA-335 III.1.5 Operand type table, P. 303:
                            // __cgt.un__ is allowed and verifiable on ObjectRefs (O). This is commonly used when
                            // comparing an ObjectRef with null(there is no "compare - not - equal" instruction, which
                            // would otherwise be a more obvious solution)
                            return op == ILOpcode.beq || op == ILOpcode.beq_s ||
                                   op == ILOpcode.bne_un || op == ILOpcode.bne_un_s ||
                                   op == ILOpcode.ceq || op == ILOpcode.cgt_un;
                        default:
                            return false;
                    }

                case StackValueKind.ValueType:
                    return false;

                case StackValueKind.ByRef:
                    switch (dst.Kind)
                    {
                        case StackValueKind.ByRef:
                            return true;
                        case StackValueKind.NativeInt:
                            return op == ILOpcode.beq || op == ILOpcode.beq_s ||
                                   op == ILOpcode.bne_un || op == ILOpcode.bne_un_s ||
                                   op == ILOpcode.ceq;
                        default:
                            return false;
                    }

                case StackValueKind.Int32:
                    return (dst.Kind == StackValueKind.Int64 || dst.Kind == StackValueKind.NativeInt);

                case StackValueKind.Int64:
                    return (dst.Kind == StackValueKind.Int64);

                case StackValueKind.NativeInt:
                    switch (dst.Kind)
                    {
                        case StackValueKind.Int32:
                        case StackValueKind.NativeInt:
                            return true;
                        case StackValueKind.ByRef:
                            return op == ILOpcode.beq || op == ILOpcode.beq_s ||
                                   op == ILOpcode.bne_un || op == ILOpcode.bne_un_s ||
                                   op == ILOpcode.ceq;
                        default:
                            return false;
                    }

                case StackValueKind.Float:
                    return dst.Kind == StackValueKind.Float;

                default:
                    throw new NotImplementedException();
            }
        }

        bool IsByRefLike(StackValue value)
        {
            if (value.Kind == StackValueKind.ByRef)
                return true;

            // TODO: Check for other by-ref like types Slice<T>, ArgIterator, TypedReference

            return false;
        }
    }
}
