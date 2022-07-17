// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public sealed partial class TypeBuilder : TypeInfo
    {
        internal TypeBuilder()
        {
            // Prevent generating a default constructor
        }

        public override Assembly Assembly
        {
            get
            {
                return default;
            }
        }

        public override string AssemblyQualifiedName
        {
            get
            {
                return default;
            }
        }

        public override Type BaseType
        {
            get
            {
                return default;
            }
        }

        public override MethodBase DeclaringMethod
        {
            get
            {
                return default;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return default;
            }
        }

        public override string FullName
        {
            get
            {
                return default;
            }
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                return default;
            }
        }

        public override int GenericParameterPosition
        {
            get
            {
                return default;
            }
        }

        public override Guid GUID
        {
            get
            {
                return default;
            }
        }

        public override bool IsByRefLike
        {
            get
            {
                return default;
            }
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return default;
            }
        }

        public override bool IsGenericParameter
        {
            get
            {
                return default;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return default;
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                return default;
            }
        }

        public override bool IsSecurityCritical
        {
            get
            {
                return default;
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                return default;
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                return default;
            }
        }

        public override Module Module
        {
            get
            {
                return default;
            }
        }

        public override string Name
        {
            get
            {
                return default;
            }
        }

        public override string Namespace
        {
            get
            {
                return default;
            }
        }

        public PackingSize PackingSize
        {
            get
            {
                return default;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return default;
            }
        }

        public int Size
        {
            get
            {
                return default;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                return default;
            }
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return default;
            }
        }

        public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType) { }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? CreateType()
        {
            return default;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo? CreateTypeInfo()
        {
            return default;
        }

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            return default;
        }

        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        {
            return default;
        }

        public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
        {
            return default;
        }

        public FieldBuilder DefineField(string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
        {
            return default;
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            return default;
        }

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            return default;
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            return default;
        }

        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, PackingSize packSize, int typeSize)
        {
            return default;
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces)
        {
            return default;
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            return default;
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            return default;
        }

        public ConstructorBuilder DefineTypeInitializer()
        {
            return default;
        }

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        {
            return default;
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return default;
        }

        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return default;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return default;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return default;
        }

        public override Type GetElementType()
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents()
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return default;
        }

        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return default;
        }

        public override Type[] GetGenericArguments()
        {
            return default;
        }

        public override Type GetGenericTypeDefinition()
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type GetInterface(string name, bool ignoreCase)
        {
            return default;
        }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            return default;
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return default;
        }

        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return default;
        }

        protected override bool HasElementTypeImpl()
        {
            return default;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, Globalization.CultureInfo? culture, string[]? namedParameters)
        {
            return default;
        }

        protected override bool IsArrayImpl()
        {
            return default;
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            return default;
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            return default;
        }

        protected override bool IsByRefImpl()
        {
            return default;
        }

        protected override bool IsCOMObjectImpl()
        {
            return default;
        }

        public bool IsCreated()
        {
            return default;
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return default;
        }

        protected override bool IsPointerImpl()
        {
            return default;
        }

        protected override bool IsPrimitiveImpl()
        {
            return default;
        }

        public override bool IsSubclassOf(Type c)
        {
            return default;
        }

        public override bool IsTypeDefinition
        {
            get
            {
                return default;
            }
        }

        public override bool IsSZArray
        {
            get
            {
                return default;
            }
        }

        public override bool IsVariableBoundArray
        {
            get
            {
                return default;
            }
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return default;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            return default;
        }

        public override Type MakeByRefType()
        {
            return default;
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments)
        {
            return default;
        }

        public override Type MakePointerType()
        {
            return default;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }

        public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent)
        {
        }

        public override string ToString()
        {
            return default;
        }
    }
}
