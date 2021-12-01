// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    public abstract partial class TypeInfo : Type, IReflectableType
    {
        protected TypeInfo() { }

        TypeInfo IReflectableType.GetTypeInfo() => this;
        public virtual Type AsType() => this;

        public virtual Type[] GenericTypeParameters => IsGenericTypeDefinition ? GetGenericArguments() : Type.EmptyTypes;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public virtual EventInfo? GetDeclaredEvent(string name) => GetEvent(name, TypeInfo.DeclaredOnlyLookup);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public virtual FieldInfo? GetDeclaredField(string name) => GetField(name, TypeInfo.DeclaredOnlyLookup);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public virtual MethodInfo? GetDeclaredMethod(string name) => GetMethod(name, TypeInfo.DeclaredOnlyLookup);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public virtual TypeInfo? GetDeclaredNestedType(string name) => GetNestedType(name, TypeInfo.DeclaredOnlyLookup)?.GetTypeInfo();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public virtual PropertyInfo? GetDeclaredProperty(string name) => GetProperty(name, TypeInfo.DeclaredOnlyLookup);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public virtual IEnumerable<MethodInfo> GetDeclaredMethods(string name)
        {
            foreach (MethodInfo method in GetDeclaredOnlyMethods(this))
            {
                if (method.Name == name)
                    yield return method;
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
                Justification = "The yield return state machine doesn't propagate annotations")]
            static MethodInfo[] GetDeclaredOnlyMethods(
                Type type) => type.GetMethods(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            get => GetConstructors(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<EventInfo> DeclaredEvents
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
            get => GetEvents(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<FieldInfo> DeclaredFields
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            get => GetFields(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<MemberInfo> DeclaredMembers
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            get => GetMembers(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<MethodInfo> DeclaredMethods
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            get => GetMethods(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<System.Reflection.TypeInfo> DeclaredNestedTypes
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            get
            {
                foreach (Type t in GetDeclaredOnlyNestedTypes(this))
                {
                    yield return t.GetTypeInfo();
                }

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
                    Justification = "The yield return state machine doesn't propagate annotations")]
                static Type[] GetDeclaredOnlyNestedTypes(
                    Type type) => type.GetNestedTypes(TypeInfo.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<PropertyInfo> DeclaredProperties
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            get => GetProperties(TypeInfo.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<Type> ImplementedInterfaces
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            get => GetInterfaces();
        }

        // a re-implementation of ISAF from Type, skipping the use of UnderlyingType
        public virtual bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;

            if (this == typeInfo)
                return true;

            // If c is a subclass of this class, then c can be cast to this type.
            if (typeInfo.IsSubclassOf(this))
                return true;

            if (this.IsInterface)
            {
                return typeInfo.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(typeInfo))
                        return false;

                return true;
            }

            return false;
        }

        internal static string GetRankString(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();

            return rank == 1 ?
                "[*]" :
                "[" + new string(',', rank - 1) + "]";
        }

        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    }
}
