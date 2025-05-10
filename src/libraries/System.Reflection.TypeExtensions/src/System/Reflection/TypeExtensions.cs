// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TypeExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ConstructorInfo? GetConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type,
            Type[] types)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetConstructor(types);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ConstructorInfo[] GetConstructors(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetConstructors();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ConstructorInfo[] GetConstructors(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetConstructors(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MemberInfo[] GetDefaultMembers(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.PublicMethods
                | DynamicallyAccessedMemberTypes.PublicEvents
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.PublicConstructors
                | DynamicallyAccessedMemberTypes.PublicNestedTypes)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetDefaultMembers();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EventInfo? GetEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] this Type type,
            string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvent(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EventInfo? GetEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvent(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EventInfo[] GetEvents(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvents();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EventInfo[] GetEvents(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvents(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static FieldInfo? GetField(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type,
            string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetField(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static FieldInfo? GetField(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetField(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static FieldInfo[] GetFields(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetFields();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static FieldInfo[] GetFields(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetFields(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Type[] GetGenericArguments(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetGenericArguments();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Type[] GetInterfaces(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetInterfaces();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MemberInfo[] GetMember(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.PublicMethods
                | DynamicallyAccessedMemberTypes.PublicEvents
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.PublicConstructors
                | DynamicallyAccessedMemberTypes.PublicNestedTypes)] this Type type,
            string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMember(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MemberInfo[] GetMember(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMember(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MemberInfo[] GetMembers(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.PublicMethods
                | DynamicallyAccessedMemberTypes.PublicEvents
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.PublicConstructors
                | DynamicallyAccessedMemberTypes.PublicNestedTypes)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMembers();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MemberInfo[] GetMembers(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMembers(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type,
            string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethod(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethod(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type,
            string name,
            Type[] types)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethod(name, types);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo[] GetMethods(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethods();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo[] GetMethods(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethods(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Type? GetNestedType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetNestedType(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Type[] GetNestedTypes(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetNestedTypes(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo[] GetProperties(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperties();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo[] GetProperties(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperties(bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo? GetProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type,
            string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperty(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo? GetProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type,
            string name,
            BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperty(name, bindingAttr);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo? GetProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type,
            string name,
            Type? returnType)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperty(name, returnType);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo? GetProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type,
            string name,
            Type? returnType,
            Type[] types)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperty(name, returnType, types);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsAssignableFrom(this Type type, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] Type? c)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.IsAssignableFrom(c);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsInstanceOfType(this Type type, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? o)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.IsInstanceOfType(o);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AssemblyExtensions
    {
        [RequiresUnreferencedCode("Types might be removed"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Type[] GetExportedTypes(this Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return assembly.GetExportedTypes();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Module[] GetModules(this Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return assembly.GetModules();
        }

        [RequiresUnreferencedCode("Types might be removed"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Type[] GetTypes(this Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return assembly.GetTypes();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EventInfoExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetAddMethod(this EventInfo eventInfo)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetAddMethod();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetAddMethod(this EventInfo eventInfo, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetAddMethod(nonPublic);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRaiseMethod(this EventInfo eventInfo)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetRaiseMethod();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRaiseMethod(this EventInfo eventInfo, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetRaiseMethod(nonPublic);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRemoveMethod(this EventInfo eventInfo)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetRemoveMethod();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRemoveMethod(this EventInfo eventInfo, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            return eventInfo.GetRemoveMethod(nonPublic);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MemberInfoExtensions
    {

        /// <summary>
        /// Determines if there is a metadata token available for the given member.
        /// <see cref="GetMetadataToken(MemberInfo)"/> throws <see cref="InvalidOperationException"/> otherwise.
        /// </summary>
        /// <remarks>This maybe</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool HasMetadataToken(this MemberInfo member)
        {
            ArgumentNullException.ThrowIfNull(member);

            try
            {
                return GetMetadataTokenOrZeroOrThrow(member) != 0;
            }
            catch (InvalidOperationException)
            {
                // Thrown for unbaked ref-emit members/types.
                // Other cases such as typeof(byte[]).MetadataToken will be handled by comparison to zero above.
                return false;
            }
        }

        /// <summary>
        /// Gets a metadata token for the given member if available. The returned token is never nil.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// There is no metadata token available. <see cref="HasMetadataToken(MemberInfo)"/> returns false in this case.
        /// </exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int GetMetadataToken(this MemberInfo member)
        {
            ArgumentNullException.ThrowIfNull(member);

            int token = GetMetadataTokenOrZeroOrThrow(member);

            if (token == 0)
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }

            return token;
        }

        private static int GetMetadataTokenOrZeroOrThrow(this MemberInfo member)
        {
            int token = member.MetadataToken;

            // Tokens have MSB = table index, 3 LSBs = row index
            // row index of 0 is a nil token
            const int rowMask = 0x00FFFFFF;
            if ((token & rowMask) == 0)
            {
                // Nil token is returned for edge cases like typeof(byte[]).MetadataToken.
                return 0;
            }

            return token;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MethodInfoExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo GetBaseDefinition(this MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);

            return method.GetBaseDefinition();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ModuleExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool HasModuleVersionId(this Module module)
        {
            ArgumentNullException.ThrowIfNull(module);

            return true; // not expected to fail on platforms with Module.ModuleVersionId built-in.
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Guid GetModuleVersionId(this Module module)
        {
            ArgumentNullException.ThrowIfNull(module);

            return module.ModuleVersionId;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PropertyInfoExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo[] GetAccessors(this PropertyInfo property)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetAccessors();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo[] GetAccessors(this PropertyInfo property, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetAccessors(nonPublic);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetGetMethod(this PropertyInfo property)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetGetMethod();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetGetMethod(this PropertyInfo property, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetGetMethod(nonPublic);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetSetMethod(this PropertyInfo property)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetSetMethod();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetSetMethod(this PropertyInfo property, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(property);

            return property.GetSetMethod(nonPublic);
        }
    }
}
