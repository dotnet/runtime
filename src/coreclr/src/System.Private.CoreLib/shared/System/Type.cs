// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        protected Type() { }

        public override MemberTypes MemberType => MemberTypes.TypeInfo;

        public new Type GetType() => base.GetType();

        public abstract string Namespace { get; }
        public abstract string AssemblyQualifiedName { get; }
        public abstract string FullName { get; }

        public abstract Assembly Assembly { get; }
        public abstract new Module Module { get; }

        public bool IsNested => DeclaringType != null;
        public override Type DeclaringType => null;
        public virtual MethodBase DeclaringMethod => null;

        public override Type ReflectedType => null;
        public abstract Type UnderlyingSystemType { get; }

        public virtual bool IsTypeDefinition { get { throw NotImplemented.ByDesign; } }
        public bool IsArray => IsArrayImpl();
        protected abstract bool IsArrayImpl();
        public bool IsByRef => IsByRefImpl();
        protected abstract bool IsByRefImpl();
        public bool IsPointer => IsPointerImpl();
        protected abstract bool IsPointerImpl();
        public virtual bool IsConstructedGenericType { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsGenericParameter => false;
        public virtual bool IsGenericTypeParameter => IsGenericParameter && DeclaringMethod == null;
        public virtual bool IsGenericMethodParameter => IsGenericParameter && DeclaringMethod != null;
        public virtual bool IsGenericType => false;
        public virtual bool IsGenericTypeDefinition => false;

        public virtual bool IsSZArray { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsVariableBoundArray => IsArray && !IsSZArray;

        public virtual bool IsByRefLike => throw new NotSupportedException(SR.NotSupported_SubclassOverride);

        public bool HasElementType => HasElementTypeImpl();
        protected abstract bool HasElementTypeImpl();
        public abstract Type GetElementType();

        public virtual int GetArrayRank() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public virtual Type GetGenericTypeDefinition() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual Type[] GenericTypeArguments => (IsGenericType && !IsGenericTypeDefinition) ? GetGenericArguments() : Array.Empty<Type>();
        public virtual Type[] GetGenericArguments() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public virtual int GenericParameterPosition { get { throw new InvalidOperationException(SR.Arg_NotGenericParameter); } }
        public virtual GenericParameterAttributes GenericParameterAttributes { get { throw new NotSupportedException(); } }
        public virtual Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);
            throw new InvalidOperationException();
        }

        public TypeAttributes Attributes => GetAttributeFlagsImpl();
        protected abstract TypeAttributes GetAttributeFlagsImpl();

        public bool IsAbstract => (GetAttributeFlagsImpl() & TypeAttributes.Abstract) != 0;
        public bool IsImport => (GetAttributeFlagsImpl() & TypeAttributes.Import) != 0;
        public bool IsSealed => (GetAttributeFlagsImpl() & TypeAttributes.Sealed) != 0;
        public bool IsSpecialName => (GetAttributeFlagsImpl() & TypeAttributes.SpecialName) != 0;

        public bool IsClass => (GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !IsValueType;

        public bool IsNestedAssembly => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
        public bool IsNestedFamANDAssem => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
        public bool IsNestedFamily => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
        public bool IsNestedFamORAssem => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem;
        public bool IsNestedPrivate => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
        public bool IsNestedPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
        public bool IsNotPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
        public bool IsPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.Public;

        public bool IsAutoLayout => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
        public bool IsExplicitLayout => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
        public bool IsLayoutSequential => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;

        public bool IsAnsiClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass;
        public bool IsAutoClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;
        public bool IsUnicodeClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;

        public bool IsCOMObject => IsCOMObjectImpl();
        protected abstract bool IsCOMObjectImpl();
        public bool IsContextful => IsContextfulImpl();
        protected virtual bool IsContextfulImpl() => false;

        public virtual bool IsEnum => IsSubclassOf(typeof(Enum));
        public bool IsMarshalByRef => IsMarshalByRefImpl();
        protected virtual bool IsMarshalByRefImpl() => false;
        public bool IsPrimitive => IsPrimitiveImpl();
        protected abstract bool IsPrimitiveImpl();
        public bool IsValueType => IsValueTypeImpl();
        protected virtual bool IsValueTypeImpl() => IsSubclassOf(typeof(ValueType));

        public virtual bool IsSignatureType => false;

        public virtual bool IsSecurityCritical { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsSecuritySafeCritical { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsSecurityTransparent { get { throw NotImplemented.ByDesign; } }

        public virtual StructLayoutAttribute StructLayoutAttribute { get { throw new NotSupportedException(); } }
        public ConstructorInfo TypeInitializer => GetConstructorImpl(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, Type.EmptyTypes, null);

        public ConstructorInfo GetConstructor(Type[] types) => GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, types, null);
        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => GetConstructor(bindingAttr, binder, CallingConventions.Any, types, modifiers);
        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }
            return GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);
        }
        protected abstract ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

        public ConstructorInfo[] GetConstructors() => GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        public abstract ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);

        public EventInfo GetEvent(string name) => GetEvent(name, Type.DefaultLookup);
        public abstract EventInfo GetEvent(string name, BindingFlags bindingAttr);

        public virtual EventInfo[] GetEvents() => GetEvents(Type.DefaultLookup);
        public abstract EventInfo[] GetEvents(BindingFlags bindingAttr);

        public FieldInfo GetField(string name) => GetField(name, Type.DefaultLookup);
        public abstract FieldInfo GetField(string name, BindingFlags bindingAttr);

        public FieldInfo[] GetFields() => GetFields(Type.DefaultLookup);
        public abstract FieldInfo[] GetFields(BindingFlags bindingAttr);

        public MemberInfo[] GetMember(string name) => GetMember(name, Type.DefaultLookup);
        public virtual MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => GetMember(name, MemberTypes.All, bindingAttr);
        public virtual MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public MemberInfo[] GetMembers() => GetMembers(Type.DefaultLookup);
        public abstract MemberInfo[] GetMembers(BindingFlags bindingAttr);

        public MethodInfo GetMethod(string name) => GetMethod(name, Type.DefaultLookup);
        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetMethodImpl(name, bindingAttr, null, CallingConventions.Any, null, null);
        }

        public MethodInfo GetMethod(string name, Type[] types) => GetMethod(name, types, null);
        public MethodInfo GetMethod(string name, Type[] types, ParameterModifier[] modifiers) => GetMethod(name, Type.DefaultLookup, null, types, modifiers);
        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => GetMethod(name, bindingAttr, binder, CallingConventions.Any, types, modifiers);
        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }
            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        protected abstract MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

        public MethodInfo GetMethod(string name, int genericParameterCount, Type[] types) => GetMethod(name, genericParameterCount, types, null);
        public MethodInfo GetMethod(string name, int genericParameterCount, Type[] types, ParameterModifier[] modifiers) => GetMethod(name, genericParameterCount, Type.DefaultLookup, null, types, modifiers);
        public MethodInfo GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => GetMethod(name, genericParameterCount, bindingAttr, binder, CallingConventions.Any, types, modifiers);
        public MethodInfo GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (genericParameterCount < 0)
                throw new ArgumentException(SR.ArgumentOutOfRange_NeedNonNegNum, nameof(genericParameterCount));
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }
            return GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
        }

        protected virtual MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotSupportedException();

        public MethodInfo[] GetMethods() => GetMethods(Type.DefaultLookup);
        public abstract MethodInfo[] GetMethods(BindingFlags bindingAttr);

        public Type GetNestedType(string name) => GetNestedType(name, Type.DefaultLookup);
        public abstract Type GetNestedType(string name, BindingFlags bindingAttr);

        public Type[] GetNestedTypes() => GetNestedTypes(Type.DefaultLookup);
        public abstract Type[] GetNestedTypes(BindingFlags bindingAttr);

        public PropertyInfo GetProperty(string name) => GetProperty(name, Type.DefaultLookup);
        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetPropertyImpl(name, bindingAttr, null, null, null, null);
        }

        public PropertyInfo GetProperty(string name, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));
            return GetPropertyImpl(name, Type.DefaultLookup, null, returnType, null, null);
        }

        public PropertyInfo GetProperty(string name, Type[] types) => GetProperty(name, null, types);
        public PropertyInfo GetProperty(string name, Type returnType, Type[] types) => GetProperty(name, returnType, types, null);
        public PropertyInfo GetProperty(string name, Type returnType, Type[] types, ParameterModifier[] modifiers) => GetProperty(name, Type.DefaultLookup, null, returnType, types, modifiers);
        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            return GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);
        }

        protected abstract PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

        public PropertyInfo[] GetProperties() => GetProperties(Type.DefaultLookup);
        public abstract PropertyInfo[] GetProperties(BindingFlags bindingAttr);

        public virtual MemberInfo[] GetDefaultMembers() { throw NotImplemented.ByDesign; }

        public virtual RuntimeTypeHandle TypeHandle { get { throw new NotSupportedException(); } }
        public static RuntimeTypeHandle GetTypeHandle(object o)
        {
            if (o == null)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            Type type = o.GetType();
            return type.TypeHandle;
        }

        public static Type[] GetTypeArray(object[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            Type[] cls = new Type[args.Length];
            for (int i = 0; i < cls.Length; i++)
            {
                if (args[i] == null)
                    throw new ArgumentNullException();
                cls[i] = args[i].GetType();
            }
            return cls;
        }

        public static TypeCode GetTypeCode(Type type)
        {
            if (type == null)
                return TypeCode.Empty;
            return type.GetTypeCodeImpl();
        }
        protected virtual TypeCode GetTypeCodeImpl()
        {
            Type systemType = UnderlyingSystemType;
            if (this != systemType && systemType != null)
                return Type.GetTypeCode(systemType);

            return TypeCode.Object;
        }

        public abstract Guid GUID { get; }

        public static Type GetTypeFromCLSID(Guid clsid) => GetTypeFromCLSID(clsid, null, throwOnError: false);
        public static Type GetTypeFromCLSID(Guid clsid, bool throwOnError) => GetTypeFromCLSID(clsid, null, throwOnError: throwOnError);
        public static Type GetTypeFromCLSID(Guid clsid, string server) => GetTypeFromCLSID(clsid, server, throwOnError: false);

        public static Type GetTypeFromProgID(string progID) => GetTypeFromProgID(progID, null, throwOnError: false);
        public static Type GetTypeFromProgID(string progID, bool throwOnError) => GetTypeFromProgID(progID, null, throwOnError: throwOnError);
        public static Type GetTypeFromProgID(string progID, string server) => GetTypeFromProgID(progID, server, throwOnError: false);

        public abstract Type BaseType { get; }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args) => InvokeMember(name, invokeAttr, binder, target, args, null, null, null);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, CultureInfo culture) => InvokeMember(name, invokeAttr, binder, target, args, null, culture, null);
        public abstract object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

        public Type GetInterface(string name) => GetInterface(name, ignoreCase: false);
        public abstract Type GetInterface(string name, bool ignoreCase);
        public abstract Type[] GetInterfaces();

        public virtual InterfaceMapping GetInterfaceMap(Type interfaceType) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public virtual bool IsInstanceOfType(object o) => o == null ? false : IsAssignableFrom(o.GetType());
        public virtual bool IsEquivalentTo(Type other) => this == other;

        public virtual Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            FieldInfo[] fields = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields == null || fields.Length != 1)
                throw new ArgumentException(SR.Argument_InvalidEnum, "enumType");

            return fields[0].FieldType;
        }
        public virtual Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            // We don't support GetEnumValues in the default implementation because we cannot create an array of
            // a non-runtime type. If there is strong need we can consider returning an object or int64 array.
            throw NotImplemented.ByDesign;
        }

        public virtual Type MakeArrayType() { throw new NotSupportedException(); }
        public virtual Type MakeArrayType(int rank) { throw new NotSupportedException(); }
        public virtual Type MakeByRefType() { throw new NotSupportedException(); }
        public virtual Type MakeGenericType(params Type[] typeArguments) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual Type MakePointerType() { throw new NotSupportedException(); }

        public static Type MakeGenericSignatureType(Type genericTypeDefinition, params Type[] typeArguments) => new SignatureConstructedGenericType(genericTypeDefinition, typeArguments);

        public static Type MakeGenericMethodParameter(int position)
        {
            if (position < 0)
                throw new ArgumentException(SR.ArgumentOutOfRange_NeedNonNegNum, nameof(position));
            return new SignatureGenericMethodParameterType(position);
        }

        public override string ToString() => "Type: " + Name;  // Why do we add the "Type: " prefix?

        public override bool Equals(object o) => o == null ? false : Equals(o as Type);
        public override int GetHashCode()
        {
            Type systemType = UnderlyingSystemType;
            if (!object.ReferenceEquals(systemType, this))
                return systemType.GetHashCode();
            return base.GetHashCode();
        }
        public virtual bool Equals(Type o) => o == null ? false : object.ReferenceEquals(this.UnderlyingSystemType, o.UnderlyingSystemType);

        public static Type ReflectionOnlyGetType(string typeName, bool throwIfNotFound, bool ignoreCase) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }

        public static Binder DefaultBinder
        {
            get
            {
                if (s_defaultBinder == null)
                {
                    DefaultBinder binder = new DefaultBinder();
                    Interlocked.CompareExchange<Binder>(ref s_defaultBinder, binder, null);
                }
                return s_defaultBinder;
            }
        }

        private static volatile Binder s_defaultBinder;

        public static readonly char Delimiter = '.';
        public static readonly Type[] EmptyTypes = Array.Empty<Type>();
        public static readonly object Missing = System.Reflection.Missing.Value;

        public static readonly MemberFilter FilterAttribute = FilterAttributeImpl;
        public static readonly MemberFilter FilterName = (m, c) => FilterNameImpl(m, c, StringComparison.Ordinal);
        public static readonly MemberFilter FilterNameIgnoreCase = (m, c) => FilterNameImpl(m, c, StringComparison.OrdinalIgnoreCase);

        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
    }
}
