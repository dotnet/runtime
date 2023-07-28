// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Debug = global::System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    internal static partial class ConstraintValidator
    {
        //
        // We cannot do the constraint validation against real TypeInfo because of constraints need to be validated
        // before the type is built.
        //
        // InstantiatedType allows us to use TypeInfo for constraint validation without creating a real TypeInfo.
        // It implements just enough methods for constraint validation to work, and performs type variable substitution
        // as necessary.
        //

        private struct SigTypeContext
        {
            public readonly Type[] TypeInstantiation;
            public readonly Type[] MethodInstantiation;

            public SigTypeContext(Type[] typeInstantiation, Type[] methodInstantiation)
            {
                TypeInstantiation = typeInstantiation;
                MethodInstantiation = methodInstantiation;
            }
        }

        private sealed class InstantiatedTypeInfo : TypeInfo
        {
            private Type _underlyingType;
            private SigTypeContext _context;

            public InstantiatedTypeInfo(Type underlyingType, SigTypeContext context)
            {
                _underlyingType = underlyingType;
                _context = context;
            }

            public Type UnderlyingType
            {
                get
                {
                    return _underlyingType;
                }
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2080:UnrecognizedReflectionPattern",
                Justification = "We won're remove interfaces used in constraints")]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            public override Type[] GetInterfaces()
            {
                Type[] interfaces = _underlyingType.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                    interfaces[i] = interfaces[i].Instantiate(_context);
                return interfaces;
            }

            public override bool IsConstructedGenericType
            {
                get
                {
                    return _underlyingType.IsConstructedGenericType;
                }
            }

            public override bool IsGenericType
            {
                get
                {
                    return _underlyingType.IsGenericType;
                }
            }

            public override Type GetGenericTypeDefinition()
            {
                return _underlyingType.GetGenericTypeDefinition();
            }

            public override int GetArrayRank()
            {
                return _underlyingType.GetArrayRank();
            }

            public override Type GetElementType()
            {
                return _underlyingType.GetElementType().Instantiate(_context);
            }

            public override Type[] GetGenericArguments()
            {
                Type[] arguments = _underlyingType.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = arguments[i].Instantiate(_context);
                }
                return arguments;
            }

            public override Type BaseType
            {
                get
                {
                    return _underlyingType.BaseType.Instantiate(_context);
                }
            }

            protected override TypeAttributes GetAttributeFlagsImpl()
            {
                return _underlyingType.Attributes;
            }

            protected override bool IsValueTypeImpl()
            {
                return _underlyingType.IsValueType;
            }

            protected override bool IsArrayImpl()
            {
                return _underlyingType.IsArray;
            }

            protected override bool IsByRefImpl()
            {
                return _underlyingType.IsByRef;
            }

            protected override bool IsPointerImpl()
            {
                return _underlyingType.IsPointer;
            }

            public override Assembly Assembly { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            public override string AssemblyQualifiedName { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            public override string FullName { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            public override object[] GetCustomAttributes(bool inherit) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
            public override EventInfo GetEvent(string name, BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
            public override EventInfo[] GetEvents(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            public override FieldInfo GetField(string name, BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            public override FieldInfo[] GetFields(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern",
                Justification = "Linker doesn't recognize always throwing method. https://github.com/mono/linker/issues/2025")]
            public override Type GetInterface(string name, bool ignoreCase) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(GetAllMembers)]
            public override MemberInfo[] GetMembers(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            public override Type GetNestedType(string name, BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            public override Type[] GetNestedTypes(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            public override bool IsDefined(Type attributeType, bool inherit) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            public override Guid GUID { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            public override Module Module { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            public override string Namespace { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            public override string Name { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }
            public override Type UnderlyingSystemType { get { Debug.Assert(false); throw NotImplemented.ByDesign; } }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { Debug.Assert(false); throw NotImplemented.ByDesign; }
            protected override bool IsCOMObjectImpl() { Debug.Assert(false); throw NotImplemented.ByDesign; }
            protected override bool IsPrimitiveImpl() { Debug.Assert(false); throw NotImplemented.ByDesign; }
            protected override bool HasElementTypeImpl() { Debug.Assert(false); throw NotImplemented.ByDesign; }

            internal const DynamicallyAccessedMemberTypes GetAllMembers = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
                DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
                DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
                DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
                DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
                DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
        }

        private static Type Instantiate(this Type type, SigTypeContext context)
        {
            if (type.IsGenericParameter)
            {
                int position = type.GenericParameterPosition;
                if (type.DeclaringMethod != null)
                {
                    return context.MethodInstantiation[position];
                }
                else
                {
                    Debug.Assert(type.DeclaringType != null);
                    return context.TypeInstantiation[position];
                }
            }

            if (type.ContainsGenericParameters)
            {
                //
                // Note we can come here for both generic and non-generic types. Consider this example:
                //
                // interface IFoo<T> { }
                // class Foo<U> : IFoo<U[]> { }
                //
                // var foo = typeof(Foo<>);
                // var ifoo = foo.ImplementedInterfaces.First();
                // var arg = ifoo.GetGenericArguments()[0];
                //
                // arg.ContainsGenericParameters will be true, but arg.IsGenericType will be false.
                //
                return new InstantiatedTypeInfo(type, context);
            }

            return type;
        }

        private static bool IsInstantiatedTypeInfo(this Type type)
        {
            return type is InstantiatedTypeInfo;
        }

        //
        // Other helper methods to support constraint validation
        //

        private static bool IsNullable(this Type type)
        {
            return type.IsGenericType && typeof(Nullable<>) == type.GetGenericTypeDefinition();
        }

        private static Type GetNullableType(this Type type)
        {
            Debug.Assert(type.IsNullable());

            Type[] arguments = type.GetGenericArguments();
            Debug.Assert(arguments.Length == 1);

            return arguments[0];
        }

        private static bool IsSystemObject(this Type type)
        {
            return typeof(object) == type;
        }

        private static bool IsSystemValueType(this Type type)
        {
            return typeof(ValueType) == type;
        }

        private static bool IsSystemArray(this Type type)
        {
            return typeof(Array) == type;
        }

        private static bool IsSystemVoid(this Type type)
        {
            return typeof(void) == type;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Default constructors of types that could be arguments to MakeGenericType are preserved.")]
        private static bool HasExplicitOrImplicitPublicDefaultConstructor(this Type type)
        {
            // Strip InstantiatedTypeInfo - GetConstructors is not implemented on InstantiatedTypeInfo
            if (type is InstantiatedTypeInfo)
                type = ((InstantiatedTypeInfo)type).UnderlyingType;

            // valuetypes have public default ctors implicitly
            if (type.IsValueType)
                return true;

            foreach (var ctor in type.GetConstructors())
            {
                if (!ctor.IsStatic && ctor.IsPublic && ctor.GetParametersNoCopy().Length == 0)
                    return true;
            }
            return false;
        }

        private static unsafe int NormalizedPrimitiveTypeSizeForIntegerTypes(this Type type)
        {
            // Strip InstantiatedTypeInfo - IsEnum is not implemented on InstantiatedTypeInfo
            if (type is InstantiatedTypeInfo)
                type = ((InstantiatedTypeInfo)type).UnderlyingType;

            Type normalizedType;

            if (type.IsEnum)
            {
                // TODO: Enum.GetUnderlyingType does not work for generic type definitions
                return NormalizedPrimitiveTypeSizeForIntegerTypes(Enum.GetUnderlyingType(type));
            }
            else
            if (type.IsPrimitive)
            {
                normalizedType = type;
            }
            else
            {
                return 0;
            }

            if (typeof(byte) == normalizedType || typeof(sbyte) == normalizedType)
                return 1;

            if (typeof(ushort) == normalizedType || typeof(short) == normalizedType)
                return 2;

            if (typeof(uint) == normalizedType || typeof(int) == normalizedType)
                return 4;

            if (typeof(ulong) == normalizedType || typeof(long) == normalizedType)
                return 8;

            if (typeof(UIntPtr) == normalizedType || typeof(IntPtr) == normalizedType)
                return sizeof(IntPtr);

            return 0;
        }
    }
}
