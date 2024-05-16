// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit
{
    /*
     * TypeBuilderInstantiation represents an instantiation of a generic TypeBuilder.
     */
    internal sealed partial class TypeBuilderInstantiation : TypeInfo
    {
        #region Fields need to be kept in order
        // For Mono runtime its important to keep this declaration order in sync with MonoReflectionGenericClass struct in object-internals.h
        private Type _genericType;
        private Type[] _typeArguments;
        #endregion
        private string? _strFullQualName;
        internal Hashtable _hashtable;


        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Static Members
        internal static Type MakeGenericType(Type type, Type[] typeArguments)
        {
            Debug.Assert(type != null, "this is only called from RuntimeType.MakeGenericType and TypeBuilder.MakeGenericType so 'type' cannot be null");

            if (!type.IsGenericTypeDefinition)
                throw new InvalidOperationException();

            ArgumentNullException.ThrowIfNull(typeArguments);

            foreach (Type t in typeArguments)
            {
                ArgumentNullException.ThrowIfNull(t, nameof(typeArguments));
            }

            return new TypeBuilderInstantiation(type, typeArguments);
        }
        #endregion

        #region Constructor
        internal TypeBuilderInstantiation(Type type, Type[] inst)
        {
            _genericType = type;
            _typeArguments = inst;
            _hashtable = new Hashtable();
        }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString)!;
        }
        #endregion

        #region MemberInfo Overrides
        public override Type? DeclaringType => _genericType.DeclaringType;

        public override Type? ReflectedType => _genericType.ReflectedType;

        public override string Name => _genericType.Name;

        public override Module Module => _genericType.Module;
        #endregion

        #region Type Overrides
        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*", this, 0)!;
        }
        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }
        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }
        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();

            string s = rank == 1 ?
                "[]" :
                "[" + new string(',', rank - 1) + "]";

            return SymbolType.FormCompoundType(s, this, 0)!;
        }
        public override Guid GUID => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) { throw new NotSupportedException(); }

        public override Assembly Assembly => _genericType.Assembly;
        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException();
        public override string? FullName => _strFullQualName ??= TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
        public override string? Namespace => _genericType.Namespace;
        public override string? AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "The entire TypeBuilderInstantiation is serving the MakeGenericType implementation. " +
                            "Currently this is not supported by linker. Once it is supported the outercall (Type.MakeGenericType)" +
                            "will validate that the types fulfill the necessary requirements of annotations on type parameters." +
                            "As such the actual internals of the implementation are not interesting.")]
        private Type Substitute(Type[] substitutes)
        {
            Type[] inst = GetGenericArguments();
            Type[] instSubstituted = new Type[inst.Length];

            for (int i = 0; i < instSubstituted.Length; i++)
            {
                Type t = inst[i];

                if (t is TypeBuilderInstantiation tbi)
                {
                    instSubstituted[i] = tbi.Substitute(substitutes);
                }
                else if (t is GenericTypeParameterBuilder)
                {
                    // Substitute
                    instSubstituted[i] = substitutes[t.GenericParameterPosition];
                }
                else
                {
                    instSubstituted[i] = t;
                }
            }

            return GetGenericTypeDefinition().MakeGenericType(instSubstituted);
        }
        public override Type? BaseType
        {
            // B<A,B,C>
            // D<T,S> : B<S,List<T>,char>

            // D<string,int> : B<int,List<string>,char>
            // D<S,T> : B<T,List<S>,char>
            // D<S,string> : B<string,List<S>,char>
            get
            {
                Type? typeBldrBase = _genericType.BaseType;

                if (typeBldrBase == null)
                    return null;

                TypeBuilderInstantiation? typeBldrBaseAs = typeBldrBase as TypeBuilderInstantiation;

                if (typeBldrBaseAs == null)
                    return typeBldrBase;

                return typeBldrBaseAs.Substitute(GetGenericArguments());
            }
        }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type GetInterface(string name, bool ignoreCase) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        protected override TypeAttributes GetAttributeFlagsImpl() { return _genericType.Attributes; }

        public override bool IsTypeDefinition => false;
        public override bool IsSZArray => false;

        protected override bool IsArrayImpl() { return false; }
        protected override bool IsByRefImpl() { return false; }
        protected override bool IsPointerImpl() { return false; }
        protected override bool IsPrimitiveImpl() { return false; }
        protected override bool IsCOMObjectImpl() { return false; }
        public override Type GetElementType() { throw new NotSupportedException(); }
        protected override bool HasElementTypeImpl() { return false; }
        public override Type UnderlyingSystemType => this;
        public override Type[] GetGenericArguments() { return _typeArguments; }
        public override bool IsGenericTypeDefinition => false;
        public override bool IsGenericType => true;
        public override bool IsConstructedGenericType => true;
        public override bool IsGenericParameter => false;
        public override int GenericParameterPosition => throw new InvalidOperationException();
        protected override bool IsValueTypeImpl() { return _genericType.IsValueType; }
        public override bool ContainsGenericParameters
        {
            get
            {
                for (int i = 0; i < _typeArguments.Length; i++)
                {
                    if (_typeArguments[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }
        public override MethodBase? DeclaringMethod => null;
        public override Type GetGenericTypeDefinition() { return _genericType; }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] inst) { throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this)); }
        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c) { throw new NotSupportedException(); }

        public override bool IsSubclassOf(Type c)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) { throw new NotSupportedException(); }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { throw new NotSupportedException(); }

        public override bool IsDefined(Type attributeType, bool inherit) { throw new NotSupportedException(); }
        #endregion
    }
}
