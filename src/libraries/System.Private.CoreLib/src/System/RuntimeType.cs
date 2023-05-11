// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed partial class RuntimeType : TypeInfo, ICloneable
    {
        public override Assembly Assembly => RuntimeTypeHandle.GetAssembly(this);
        public override Type? BaseType => GetBaseType();
        public override bool IsByRefLike => RuntimeTypeHandle.IsByRefLike(this);
        public override bool IsGenericParameter => RuntimeTypeHandle.IsGenericVariable(this);
        public override bool IsTypeDefinition => RuntimeTypeHandle.IsTypeDefinition(this);
        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;
        public override MemberTypes MemberType => (IsPublic || IsNotPublic) ? MemberTypes.TypeInfo : MemberTypes.NestedType;
        public override int MetadataToken => RuntimeTypeHandle.GetToken(this);
        public override Module Module => GetRuntimeModule();
        public override Type? ReflectedType => DeclaringType;
        public override RuntimeTypeHandle TypeHandle
        {
            [Intrinsic] // to avoid round-trip "handle -> RuntimeType -> handle" in JIT
            get => new RuntimeTypeHandle(this);
        }

        public override Type UnderlyingSystemType => this;

        public object Clone() => this;

        public override bool Equals(object? obj)
        {
            // ComObjects are identified by the instance of the Type object and not the TypeHandle.
            return obj == (object)this;
        }

        public override int GetArrayRank()
        {
            if (!IsArrayImpl())
                throw new ArgumentException(SR.Argument_HasToBeArrayClass);

            return RuntimeTypeHandle.GetArrayRank(this);
        }

        protected override TypeAttributes GetAttributeFlagsImpl() => RuntimeTypeHandle.GetAttributes(this);

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, ObjectType, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        // GetDefaultMembers
        // This will return a MemberInfo that has been marked with the [DefaultMemberAttribute]
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicEvents
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override MemberInfo[] GetDefaultMembers()
        {
            // See if we have cached the default member name
            MemberInfo[]? members = null;

            string? defaultMemberName = GetDefaultMemberName();
            if (defaultMemberName != null)
            {
                members = GetMember(defaultMemberName);
            }

            return members ?? Array.Empty<MemberInfo>();
        }

        public override Type GetElementType() => RuntimeTypeHandle.GetElementType(this);

        public override string? GetEnumName(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            RuntimeType valueType = (RuntimeType)value.GetType();

            if (!(valueType.IsActualEnum || IsIntegerType(valueType)))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            // Map the value to a ulong and then look up that value in the enum.
            // This supports numerical values of different types than the enum
            // or its underlying type.
            return Enum.GetName(this, Enum.ToUInt64(value));
        }

        private static void ThrowMustBeEnum() =>
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

        public override string[] GetEnumNames()
        {
            if (!IsActualEnum)
                ThrowMustBeEnum();

            string[] ret = Enum.GetNamesNoCopy(this);

            // Make a copy since we can't hand out the same array since users can modify them
            return new ReadOnlySpan<string>(ret).ToArray();
        }

        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetEnumValues<TEnum> overload or the GetEnumValuesAsUnderlyingType method instead.")]
        public override Array GetEnumValues()
        {
            if (!IsActualEnum)
                ThrowMustBeEnum();

            // Get all of the values as the underlying type and copy them to a new array of the enum type.
            Array values = Enum.GetValuesAsUnderlyingTypeNoCopy(this);
            Array ret = Array.CreateInstance(this, values.Length);
#if MONO
            // TODO https://github.com/dotnet/runtime/issues/79224:
            // Array.Copy can be used instead when bool[] is no longer supported, or if mono's Array.Copy is updated to support copying a bool[] to an EnumBackedByBool[].
            for (int i = 0; i < values.Length; i++)
            {
                ret.SetValue(Enum.ToObject(this, values.GetValue(i)!), i);
            }
#else
            Array.Copy(values, ret, values.Length);
#endif
            return ret;
        }

        /// <summary>
        /// Retrieves an array of the values of the underlying type constants in a specified enumeration type.
        /// </summary>
        /// <remarks>
        /// This method can be used to get enumeration values when creating an array of the enumeration type is challenging.
        /// For example, <see cref="T:System.Reflection.MetadataLoadContext" /> or on a platform where runtime codegen is not available.
        /// </remarks>
        /// <returns>An array that contains the values of the underlying type constants in enumType.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the type is not Enum
        /// </exception>
        public override Array GetEnumValuesAsUnderlyingType()
        {
            if (!IsActualEnum)
                ThrowMustBeEnum();

            return Enum.GetValuesAsUnderlyingType(this);
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsActualEnum)
                ThrowMustBeEnum();

            return Enum.InternalGetUnderlyingType(this);
        }

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        internal RuntimeModule GetRuntimeModule() => RuntimeTypeHandle.GetModule(this);

        protected override TypeCode GetTypeCodeImpl()
        {
            TypeCode typeCode = Cache.TypeCode;

            if (typeCode != TypeCode.Empty)
                return typeCode;

            typeCode = Type.GetRuntimeTypeCode(this);
            Cache.TypeCode = typeCode;

            return typeCode;
        }

        protected override bool HasElementTypeImpl() => RuntimeTypeHandle.HasElementType(this);

        protected override bool IsArrayImpl() => RuntimeTypeHandle.IsArray(this);

        protected override bool IsContextfulImpl() => false;

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override bool IsEnumDefined(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsActualEnum)
                ThrowMustBeEnum();

            // If the value is an Enum then we need to extract the underlying value from it
            RuntimeType valueType = (RuntimeType)value.GetType();
            if (valueType.IsActualEnum)
            {
                // The enum type must match this type.
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, this));

                valueType = (RuntimeType)valueType.GetEnumUnderlyingType();
            }

            // If a string is passed in, search the enum names with it.
            if (valueType == StringType)
                return Array.IndexOf(Enum.GetNamesNoCopy(this), (string)value) >= 0;

            // If an enum or integer value is passed in
            if (!IsIntegerType(valueType))
                throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);

            RuntimeType underlyingType = Enum.InternalGetUnderlyingType(this);
            if (underlyingType != valueType)
                throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, valueType, underlyingType));

            return GetTypeCode(underlyingType) switch
            {
                TypeCode.SByte => Enum.IsDefinedPrimitive(this, (byte)(sbyte)value),
                TypeCode.Byte => Enum.IsDefinedPrimitive(this, (byte)value),
                TypeCode.Int16 => Enum.IsDefinedPrimitive(this, (ushort)(short)value),
                TypeCode.UInt16 => Enum.IsDefinedPrimitive(this, (ushort)value),
                TypeCode.Int32 => Enum.IsDefinedPrimitive(this, (uint)(int)value),
                TypeCode.UInt32 => Enum.IsDefinedPrimitive(this, (uint)value),
                TypeCode.Int64 => Enum.IsDefinedPrimitive(this, (ulong)(long)value),
                TypeCode.UInt64 => Enum.IsDefinedPrimitive(this, (ulong)value),
                TypeCode.Single => Enum.IsDefinedPrimitive(this, (float)value),
                TypeCode.Double => Enum.IsDefinedPrimitive(this, (double)value),
                TypeCode.Char => Enum.IsDefinedPrimitive(this, (char)value),
                _ =>
                    underlyingType == typeof(nint) ? Enum.IsDefinedPrimitive(this, (nuint)(nint)value) :
                    underlyingType == typeof(nuint) ? Enum.IsDefinedPrimitive(this, (nuint)value) :
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        protected override bool IsByRefImpl() => RuntimeTypeHandle.IsByRef(this);

        protected override bool IsPrimitiveImpl() => RuntimeTypeHandle.IsPrimitive(this);

        protected override bool IsPointerImpl() => RuntimeTypeHandle.IsPointer(this);

        protected override bool IsCOMObjectImpl() => RuntimeTypeHandle.IsComObject(this, false);

        public override bool IsInstanceOfType([NotNullWhen(true)] object? o) => RuntimeTypeHandle.IsInstanceOfType(this, o);

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;

            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            if (c is null)
                return false;

            if (ReferenceEquals(c, this))
                return true;

            // For runtime type, let the VM decide.
            if (c.UnderlyingSystemType is RuntimeType fromType)
            {
                // both this and c (or their underlying system types) are runtime types
                return RuntimeTypeHandle.CanCastTo(fromType, this);
            }

            // Special case for TypeBuilder to be backward-compatible.
            if (c is System.Reflection.Emit.TypeBuilder)
            {
                // If c is a subclass of this class, then c can be cast to this type.
                if (c.IsSubclassOf(this))
                    return true;

                if (IsInterface)
                {
                    return c.ImplementInterface(this);
                }
                else if (IsGenericParameter)
                {
                    Type[] constraints = GetGenericParameterConstraints();
                    for (int i = 0; i < constraints.Length; i++)
                        if (!constraints[i].IsAssignableFrom(c))
                            return false;

                    return true;
                }
            }

            // For anything else we return false.
            return false;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(
            string name, BindingFlags bindingFlags, Binder? binder, object? target,
            object?[]? providedArgs, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParams)
        {
            const BindingFlags MemberBindingMask = (BindingFlags)0x000000FF;
            const BindingFlags InvocationMask = (BindingFlags)0x0000FF00;
            const BindingFlags BinderGetSetField = BindingFlags.GetField | BindingFlags.SetField;
            const BindingFlags BinderGetSetProperty = BindingFlags.GetProperty | BindingFlags.SetProperty;
            const BindingFlags BinderNonCreateInstance = BindingFlags.InvokeMethod | BinderGetSetField | BinderGetSetProperty;
            const BindingFlags BinderNonFieldGetSet = (BindingFlags)0x00FFF300;

            if (IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_GenericParameter);

            if ((bindingFlags & InvocationMask) == 0)
                // "Must specify binding flags describing the invoke operation required."
                throw new ArgumentException(SR.Arg_NoAccessSpec, nameof(bindingFlags));

            // Provide a default binding mask if none is provided
            if ((bindingFlags & MemberBindingMask) == 0)
            {
                bindingFlags |= BindingFlags.Instance | BindingFlags.Public;

                if ((bindingFlags & BindingFlags.CreateInstance) == 0)
                    bindingFlags |= BindingFlags.Static;
            }

            // There must not be more named parameters than provided arguments
            if (namedParams != null)
            {
                if (providedArgs != null)
                {
                    if (namedParams.Length > providedArgs.Length)
                        throw new ArgumentException(SR.Arg_NamedParamTooBig, nameof(namedParams));
                }
                else
                {
                    if (namedParams.Length != 0)
                        throw new ArgumentException(SR.Arg_NamedParamTooBig, nameof(namedParams));
                }
            }

#if FEATURE_COMINTEROP
            if (target != null && target.GetType().IsCOMObject)
            {
                const BindingFlags ClassicBindingMask =
                    BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty |
                    BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty;

                if ((bindingFlags & ClassicBindingMask) == 0)
                    throw new ArgumentException(SR.Arg_COMAccess, nameof(bindingFlags));

                if ((bindingFlags & BindingFlags.GetProperty) != 0 && (bindingFlags & ClassicBindingMask & ~(BindingFlags.GetProperty | BindingFlags.InvokeMethod)) != 0)
                    throw new ArgumentException(SR.Arg_PropSetGet, nameof(bindingFlags));

                if ((bindingFlags & BindingFlags.InvokeMethod) != 0 && (bindingFlags & ClassicBindingMask & ~(BindingFlags.GetProperty | BindingFlags.InvokeMethod)) != 0)
                    throw new ArgumentException(SR.Arg_PropSetInvoke, nameof(bindingFlags));

                if ((bindingFlags & BindingFlags.SetProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.SetProperty) != 0)
                    throw new ArgumentException(SR.Arg_COMPropSetPut, nameof(bindingFlags));

                if ((bindingFlags & BindingFlags.PutDispProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.PutDispProperty) != 0)
                    throw new ArgumentException(SR.Arg_COMPropSetPut, nameof(bindingFlags));

                if ((bindingFlags & BindingFlags.PutRefDispProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.PutRefDispProperty) != 0)
                    throw new ArgumentException(SR.Arg_COMPropSetPut, nameof(bindingFlags));

                ArgumentNullException.ThrowIfNull(name);

                bool[]? isByRef = modifiers?[0].IsByRefArray;

                // pass LCID_ENGLISH_US if no explicit culture is specified to match the behavior of VB
                int lcid = (culture == null ? 0x0409 : culture.LCID);

                // If a request to not wrap exceptions was made, we will unwrap
                // the TargetInvocationException since that is what will be thrown.
                bool unwrapExceptions = (bindingFlags & BindingFlags.DoNotWrapExceptions) != 0;
                try
                {
                    return InvokeDispMethod(name, bindingFlags, target, providedArgs, isByRef, lcid, namedParams);
                }
                catch (TargetInvocationException e) when (unwrapExceptions)
                {
                    // For target invocation exceptions, we need to unwrap the inner exception and
                    // re-throw it.
                    throw e.InnerException!;
                }
            }
#endif // FEATURE_COMINTEROP

            if (namedParams != null && Array.IndexOf(namedParams, null!) >= 0)
                throw new ArgumentException(SR.Arg_NamedParamNull, nameof(namedParams));

            int argCnt = (providedArgs != null) ? providedArgs.Length : 0;

            binder ??= DefaultBinder;

            // Delegate to Activator.CreateInstance
            if ((bindingFlags & BindingFlags.CreateInstance) != 0)
            {
                if ((bindingFlags & BindingFlags.CreateInstance) != 0 && (bindingFlags & BinderNonCreateInstance) != 0)
                    // "Can not specify both CreateInstance and another access type."
                    throw new ArgumentException(SR.Arg_CreatInstAccess, nameof(bindingFlags));

                return Activator.CreateInstance(this, bindingFlags, binder, providedArgs, culture);
            }

            // PutDispProperty and\or PutRefDispProperty ==> SetProperty.
            if ((bindingFlags & (BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) != 0)
                bindingFlags |= BindingFlags.SetProperty;

            ArgumentNullException.ThrowIfNull(name);
            if (name.Length == 0 || name.Equals("[DISPID=0]"))
            {
                // in InvokeMember we always pretend there is a default member if none is provided and we make it ToString
                name = GetDefaultMemberName() ?? "ToString";
            }

            // GetField or SetField
            bool IsGetField = (bindingFlags & BindingFlags.GetField) != 0;
            bool IsSetField = (bindingFlags & BindingFlags.SetField) != 0;

            if (IsGetField || IsSetField)
            {
                if (IsGetField)
                {
                    if (IsSetField)
                        throw new ArgumentException(SR.Arg_FldSetGet, nameof(bindingFlags));

                    if ((bindingFlags & BindingFlags.SetProperty) != 0)
                        throw new ArgumentException(SR.Arg_FldGetPropSet, nameof(bindingFlags));
                }
                else
                {
                    Debug.Assert(IsSetField);

                    ArgumentNullException.ThrowIfNull(providedArgs);

                    if ((bindingFlags & BindingFlags.GetProperty) != 0)
                        throw new ArgumentException(SR.Arg_FldSetPropGet, nameof(bindingFlags));

                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(SR.Arg_FldSetInvoke, nameof(bindingFlags));
                }

                // Lookup Field
                FieldInfo? selFld = null;
                FieldInfo[]? flds = GetMember(name, MemberTypes.Field, bindingFlags) as FieldInfo[];

                Debug.Assert(flds != null);

                if (flds.Length == 1)
                {
                    selFld = flds[0];
                }
                else if (flds.Length > 0)
                {
                    selFld = binder.BindToField(bindingFlags, flds, IsGetField ? Empty.Value : providedArgs![0]!, culture);
                }

                if (selFld != null)
                {
                    // Invocation on a field
                    if (selFld.FieldType.IsArray || ReferenceEquals(selFld.FieldType, typeof(Array)))
                    {
                        // Invocation of an array Field
                        int idxCnt;
                        if ((bindingFlags & BindingFlags.GetField) != 0)
                        {
                            idxCnt = argCnt;
                        }
                        else
                        {
                            idxCnt = argCnt - 1;
                        }

                        if (idxCnt > 0)
                        {
                            // Verify that all of the index values are ints
                            int[] idx = new int[idxCnt];
                            for (int i = 0; i < idxCnt; i++)
                            {
                                try
                                {
                                    idx[i] = ((IConvertible)providedArgs![i]!).ToInt32(null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new ArgumentException(SR.Arg_IndexMustBeInt);
                                }
                            }

                            // Set or get the value...
                            Array a = (Array)selFld.GetValue(target)!;

                            // Set or get the value in the array
                            if ((bindingFlags & BindingFlags.GetField) != 0)
                            {
                                return a.GetValue(idx);
                            }
                            else
                            {
                                a.SetValue(providedArgs![idxCnt], idx);
                                return null;
                            }
                        }
                    }

                    if (IsGetField)
                    {
                        if (argCnt != 0)
                            throw new ArgumentException(SR.Arg_FldGetArgErr, nameof(bindingFlags));

                        return selFld.GetValue(target);
                    }
                    else
                    {
                        if (argCnt != 1)
                            throw new ArgumentException(SR.Arg_FldSetArgErr, nameof(bindingFlags));

                        selFld.SetValue(target, providedArgs![0], bindingFlags, binder, culture);
                        return null;
                    }
                }

                if ((bindingFlags & BinderNonFieldGetSet) == 0)
                    throw new MissingFieldException(FullName, name);
            }

            // @Legacy - This is RTM behavior
            bool isGetProperty = (bindingFlags & BindingFlags.GetProperty) != 0;
            bool isSetProperty = (bindingFlags & BindingFlags.SetProperty) != 0;

            if (isGetProperty || isSetProperty)
            {
                if (isGetProperty)
                {
                    Debug.Assert(!IsSetField);

                    if (isSetProperty)
                        throw new ArgumentException(SR.Arg_PropSetGet, nameof(bindingFlags));
                }
                else
                {
                    Debug.Assert(isSetProperty);

                    Debug.Assert(!IsGetField);

                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(SR.Arg_PropSetInvoke, nameof(bindingFlags));
                }
            }

            MethodInfo[]? finalists = null;
            MethodInfo? finalist = null;

            if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
            {
                // Lookup Methods
                MethodInfo[] semiFinalists = (GetMember(name, MemberTypes.Method, bindingFlags) as MethodInfo[])!;
                List<MethodInfo>? results = null;

                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo semiFinalist = semiFinalists[i];
                    Debug.Assert(semiFinalist != null);

                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;

                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        results ??= new List<MethodInfo>(semiFinalists.Length) { finalist };
                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Debug.Assert(results.Count > 1);
                    finalists = results.ToArray();
                }
            }

            Debug.Assert(finalists == null || finalist != null);

            // BindingFlags.GetProperty or BindingFlags.SetProperty
            if (finalist == null && isGetProperty || isSetProperty)
            {
                // Lookup Property
                PropertyInfo[] semiFinalists = (GetMember(name, MemberTypes.Property, bindingFlags) as PropertyInfo[])!;
                List<MethodInfo>? results = null;

                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo? semiFinalist = null;

                    if (isSetProperty)
                    {
                        semiFinalist = semiFinalists[i].GetSetMethod(true);
                    }
                    else
                    {
                        semiFinalist = semiFinalists[i].GetGetMethod(true);
                    }

                    if (semiFinalist == null)
                        continue;

                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;

                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        results ??= new List<MethodInfo>(semiFinalists.Length) { finalist };
                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Debug.Assert(results.Count > 1);
                    finalists = results.ToArray();
                }
            }

            if (finalist != null)
            {
                // Invoke
                if (finalists == null &&
                    argCnt == 0 &&
                    finalist.GetParametersNoCopy().Length == 0 &&
                    (bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                {
                    return finalist.Invoke(target, bindingFlags, binder, providedArgs, culture);
                }

                finalists ??= new MethodInfo[] { finalist };
                providedArgs ??= Array.Empty<object>();
                object? state = null;
                MethodBase? invokeMethod = null;

                try { invokeMethod = binder.BindToMethod(bindingFlags, finalists, ref providedArgs!, modifiers, culture, namedParams, out state); }
                catch (MissingMethodException) { }

                if (invokeMethod == null)
                    throw new MissingMethodException(FullName, name);

                object? result = ((MethodInfo)invokeMethod).Invoke(target, bindingFlags, binder, providedArgs, culture);

                if (state != null)
                    binder.ReorderArgumentArray(ref providedArgs, state);

                return result;
            }

            throw new MissingMethodException(FullName, name);
        }

        private RuntimeType? GetBaseType()
        {
            if (IsInterface)
                return null;

            if (RuntimeTypeHandle.IsGenericVariable(this))
            {
                Type[] constraints = GetGenericParameterConstraints();

                RuntimeType baseType = ObjectType;

                for (int i = 0; i < constraints.Length; i++)
                {
                    RuntimeType constraint = (RuntimeType)constraints[i];

                    if (constraint.IsInterface)
                        continue;

                    if (constraint.IsGenericParameter)
                    {
                        GenericParameterAttributes special = constraint.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

                        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) == 0 &&
                            (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                            continue;
                    }

                    baseType = constraint;
                }

                if (baseType == ObjectType)
                {
                    GenericParameterAttributes special = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                    if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        baseType = ValueType;
                }

                return baseType;
            }

            return RuntimeTypeHandle.GetBaseType(this);
        }

        private static void ThrowIfTypeNeverValidGenericArgument(RuntimeType type)
        {
            if (type.IsPointer || type.IsByRef || type == typeof(void))
                throw new ArgumentException(
                    SR.Format(SR.Argument_NeverValidGenericArgument, type));
        }

        internal static void SanityCheckGenericArguments(RuntimeType[] genericArguments, RuntimeType[] genericParameters)
        {
            ArgumentNullException.ThrowIfNull(genericArguments);

            for (int i = 0; i < genericArguments.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(genericArguments[i], null);
                ThrowIfTypeNeverValidGenericArgument(genericArguments[i]);
            }

            if (genericArguments.Length != genericParameters.Length)
                throw new ArgumentException(
                    SR.Format(SR.Argument_NotEnoughGenArguments, genericArguments.Length, genericParameters.Length));
        }

        internal static CorElementType GetUnderlyingType(RuntimeType type)
        {
            if (type.IsActualEnum)
            {
                type = (RuntimeType)Enum.GetUnderlyingType(type);
            }

            return RuntimeTypeHandle.GetCorElementType(type);
        }

        internal static bool TryGetByRefElementType(RuntimeType type, [NotNullWhen(true)] out RuntimeType? elementType)
        {
            CorElementType corElemType = RuntimeTypeHandle.GetCorElementType(type);
            if (corElemType == CorElementType.ELEMENT_TYPE_BYREF)
            {
                elementType = RuntimeTypeHandle.GetElementType(type);
                return true;
            }

            elementType = null;
            return false;
        }

        private enum CheckValueStatus
        {
            Success = 0,
            ArgumentException,
            NotSupported_ByRefLike
        }

        /// <summary>
        /// Verify <paramref name="value"/> and optionally convert the value for special cases.
        /// </summary>
        /// <returns>True if <paramref name="value"/> is a value type, False otherwise</returns>
        internal bool CheckValue(
            ref object? value,
            ref ParameterCopyBackAction copyBack,
            Binder? binder,
            CultureInfo? culture,
            BindingFlags invokeAttr)
        {
            // Already fast-pathed by the caller.
            Debug.Assert(!ReferenceEquals(value?.GetType(), this));

            // Since this cannot be a generic parameter, we use RuntimeTypeHandle.IsValueType here
            // because it is faster than IsValueType
            Debug.Assert(!IsGenericParameter);

            // Fast path to whether a value can be assigned without conversion.
            if (IsInstanceOfType(value))
            {
                if (IsNullableOfT)
                {
                    // Pass as a true boxed Nullable<T>, not as a T or null.
                    value = RuntimeMethodHandle.ReboxToNullable(value, this);
                    return true;
                }

                // Other value types won't get here since Type equality was previous checked.
                Debug.Assert(!RuntimeTypeHandle.IsValueType(this));

                return false;
            }

            bool isValueType;
            CheckValueStatus result = TryChangeType(ref value, ref copyBack, out isValueType);
            if (result == CheckValueStatus.Success)
            {
                return isValueType;
            }

            if (result == CheckValueStatus.ArgumentException && (invokeAttr & BindingFlags.ExactBinding) == 0)
            {
                Debug.Assert(value != null);

                // Use the binder
                if (binder != null && binder != DefaultBinder)
                {
                    value = binder.ChangeType(value, this, culture);
                    if (IsInstanceOfType(value))
                    {
                        if (IsNullableOfT)
                        {
                            // Pass as a true boxed Nullable<T>, not as a T or null.
                            value = RuntimeMethodHandle.ReboxToNullable(value, this);
                            copyBack = ParameterCopyBackAction.CopyNullable;
                        }
                        else
                        {
                            copyBack = ParameterCopyBackAction.Copy;
                        }

                        return IsValueType; // Note the call to IsValueType, not the variable.
                    }

                    result = TryChangeType(ref value, ref copyBack, out isValueType);
                    if (result == CheckValueStatus.Success)
                    {
                        return isValueType;
                    }
                }
            }

            switch (result)
            {
                case CheckValueStatus.ArgumentException:
                    throw new ArgumentException(SR.Format(SR.Arg_ObjObjEx, value?.GetType(), this));
                case CheckValueStatus.NotSupported_ByRefLike:
                    throw new NotSupportedException(SR.NotSupported_ByRefLike);
            }

            Debug.Fail("Error result not expected");
            return false;
        }

        private CheckValueStatus TryChangeType(
            ref object? value,
            ref ParameterCopyBackAction copyBack,
            out bool isValueType)
        {
            RuntimeType? sigElementType;
            if (TryGetByRefElementType(this, out sigElementType))
            {
                copyBack = ParameterCopyBackAction.Copy;
                Debug.Assert(!sigElementType.IsGenericParameter);

                if (sigElementType.IsInstanceOfType(value))
                {
                    isValueType = RuntimeTypeHandle.IsValueType(sigElementType);
                    if (isValueType)
                    {
                        if (sigElementType.IsNullableOfT)
                        {
                            // Pass as a true boxed Nullable<T>, not as a T or null.
                            value = RuntimeMethodHandle.ReboxToNullable(value, sigElementType);
                            copyBack = ParameterCopyBackAction.CopyNullable;
                        }
                        else
                        {
                            // Make a copy to prevent the boxed instance from being directly modified by the method.
                            value = AllocateValueType(sigElementType, value);
                        }
                    }

                    return CheckValueStatus.Success;
                }

                if (value == null)
                {
                    isValueType = RuntimeTypeHandle.IsValueType(sigElementType);
                    if (!isValueType)
                    {
                        // Normally we don't get here since 'null' was previosuly checked, but due to binders we can.
                        return CheckValueStatus.Success;
                    }

                    if (sigElementType.IsByRefLike)
                    {
                        return CheckValueStatus.NotSupported_ByRefLike;
                    }

                    // Allocate default<T>.
                    value = AllocateValueType(sigElementType, value: null);
                    copyBack = sigElementType.IsNullableOfT ? ParameterCopyBackAction.CopyNullable : ParameterCopyBackAction.Copy;
                    return CheckValueStatus.Success;
                }

                isValueType = false;
                return CheckValueStatus.ArgumentException;
            }

            if (value == null)
            {
                isValueType = RuntimeTypeHandle.IsValueType(this);
                if (!isValueType)
                {
                    // Normally we don't get here since 'null' was previosuly checked, but due to binders we can.
                    return CheckValueStatus.Success;
                }

                if (IsByRefLike)
                {
                    return CheckValueStatus.NotSupported_ByRefLike;
                }

                // Allocate default<T>.
                value = AllocateValueType(this, value: null);
                return CheckValueStatus.Success;
            }

            // Check the strange ones courtesy of reflection:
            // - Implicit cast between primitives
            // - Enum treated as underlying type
            // - Pointer (*) types to IntPtr (if dest is IntPtr)
            // - System.Reflection.Pointer to appropriate pointer (*) type (if dest is pointer type)
            if (IsPointer || IsEnum || IsPrimitive)
                return TryChangeTypeSpecial(ref value, out isValueType);

            isValueType = false;
            return CheckValueStatus.ArgumentException;
        }

        internal bool TryByRefFastPath(ref object arg, ref bool isValueType)
        {
            if (TryGetByRefElementType(this, out RuntimeType? sigElementType) &&
                ReferenceEquals(sigElementType, arg.GetType()))
            {
                isValueType = sigElementType.IsValueType;
                if (isValueType)
                {
                    // Make a copy to prevent the boxed instance from being directly modified by the method.
                    arg = AllocateValueType(sigElementType, arg);
                }

                return true;
            }

            return false;
        }
    }
}
