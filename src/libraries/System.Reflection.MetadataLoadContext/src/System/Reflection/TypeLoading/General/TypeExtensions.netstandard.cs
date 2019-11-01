// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file makes NetStandard Reflection's "subclassing" surface area look as much like NetCore as possible so the rest of the code can be written without #if's.

namespace System.Reflection.TypeLoading
{
    // For code that have to interact with "Type" rather than "RoType", some handy extension methods that "add" the NetCore reflection apis to NetStandard.
    internal static class NetCoreApiEmulators
    {
        // On NetStandard, have to do with slower emulations.

        public static bool IsSignatureType(this Type type) => false;
        public static bool IsSZArray(this Type type) => type.IsArray && type.GetArrayRank() == 1 && type.Name.EndsWith("[]");
        public static bool IsVariableBoundArray(this Type type) => type.IsArray && !type.IsSZArray();
        public static bool IsGenericMethodParameter(this Type type) => type.IsGenericParameter && type.DeclaringMethod != null;

        // Signature Types do not exist on NetStandard 2.0 but it's possible we could reach this if a NetCore app uses the NetStandard build of this library.
        public static Type MakeSignatureGenericType(this Type genericTypeDefinition, Type[] typeArguments) => throw new NotSupportedException(SR.NotSupported_MakeGenericType_SignatureTypes);
    }

    /// <summary>
    /// Another layer of base types. For NetCore, these base types are all but empty. For NetStandard, these base types add the NetCore apis to NetStandard
    /// so code interacting with "RoTypes" and friends can happily code to the full NetCore surface area.
    ///
    /// On NetStandard (and pre-2.2 NetCore), the TypeInfo constructor is not exposed so we cannot derive directly from TypeInfo.
    /// But we *can* derive from TypeDelegator which derives from TypeInfo. Since we're overriding (almost) every method,
    /// none of TypeDelegator's own methods get called (and the instance field it has for holding the "underlying Type" goes
    /// to waste.)
    ///
    /// For future platforms, RoTypeBase's base type should be changed back to TypeInfo. Deriving from TypeDelegator is a hack and
    /// causes us to waste an extra pointer-sized field per Type instance. It is also fragile as TypeDelegator could break us in the future
    /// by overriding more methods.
    /// </summary>
    internal abstract class LeveledTypeInfo : TypeDelegator
    {
        protected LeveledTypeInfo() : base() { }

        // This is an api that TypeDelegator overrides that it needn't have. Since RoType expects to fall through to System.Type's method, we have to reimplement
        // System.Type's behavior here to avoid getting TypeDelegator's method.
        //
        // This is an annoying and fragile requirement as we have to do this for any api that (1) RoType declines to override and (2) TypeDelegator does override.
        // This could be policed by an analyzer that searches RoType's method bodies for non-virtual calls to apis declared on TypeDelegator.
        public override EventInfo[] GetEvents() => GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

        public abstract bool IsGenericTypeParameter { get; }
        public abstract bool IsGenericMethodParameter { get; }
        public abstract bool IsSZArray { get; }
        public abstract bool IsVariableBoundArray { get; }
        public abstract bool IsTypeDefinition { get; }
        public abstract bool IsByRefLike { get; }
        public virtual bool IsSignatureType => false;
        protected abstract MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledAssembly : Assembly
    {
        public abstract Type[] GetForwardedTypes();
    }

    internal abstract class LeveledConstructorInfo : ConstructorInfo
    {
        public abstract bool IsConstructedGenericMethod { get; }
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledMethodInfo : MethodInfo
    {
        public abstract bool IsConstructedGenericMethod { get; }
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledEventInfo : EventInfo
    {
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledFieldInfo : FieldInfo
    {
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledPropertyInfo : PropertyInfo
    {
        public abstract bool HasSameMetadataDefinitionAs(MemberInfo other);
    }

    internal abstract class LeveledCustomAttributeData : CustomAttributeData
    {
        // On NetStandard, AttributeType is declared non-virtually so apps are stuck calling the slow version that builds a constructor.
        public new abstract Type AttributeType { get; }
    }
}
