// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract partial class TypeBuilder
    {
        #region Public Static Methods
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            if (type is not TypeBuilder && type is not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            // The following checks establishes invariants that more simply put require type to be generic and
            // method to be a generic method definition declared on the generic type definition of type.
            // To create generic method G<Foo>.M<Bar> these invariants require that G<Foo>.M<S> be created by calling
            // this function followed by MakeGenericMethod on the resulting MethodInfo to finally get G<Foo>.M<Bar>.
            // We could also allow G<T>.M<Bar> to be created before G<Foo>.M<Bar> (BindGenParm followed by this method)
            // if we wanted to but that just complicates things so these checks are designed to prevent that scenario.

            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                throw new ArgumentException(SR.Argument_NeedGenericMethodDefinition, nameof(method));

            if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_MethodNeedGenericDeclaringType, nameof(method));

            if (type.GetGenericTypeDefinition() != method.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidMethodDeclaringType, nameof(type));

            // The following converts from Type or TypeBuilder of G<T> to TypeBuilderInstantiation G<T>. These types
            // both logically represent the same thing. The runtime displays a similar convention by having
            // G<M>.M() be encoded by a typeSpec whose parent is the typeDef for G<M> and whose instantiation is also G<M>.
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return MethodOnTypeBuilderInstantiation.GetMethod(method, typeBuilderInstantiation);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            if (type is not TypeBuilder && type is not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            if (!constructor.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_ConstructorNeedGenericDeclaringType, nameof(constructor));

            if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidConstructorDeclaringType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, typeBuilderInstantiation);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (type is not TypeBuilder and not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            if (!field.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_FieldNeedGenericDeclaringType, nameof(field));

            if (type.GetGenericTypeDefinition() != field.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidFieldDeclaringType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return FieldOnTypeBuilderInstantiation.GetField(field, typeBuilderInstantiation);
        }
        #endregion
    }
}
