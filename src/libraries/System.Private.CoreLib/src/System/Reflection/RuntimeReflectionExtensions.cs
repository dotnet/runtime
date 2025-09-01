// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class RuntimeReflectionExtensions
    {
        private const BindingFlags Everything = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<FieldInfo> GetRuntimeFields(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetFields(Everything);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<MethodInfo> GetRuntimeMethods(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethods(Everything);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<PropertyInfo> GetRuntimeProperties(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperties(Everything);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<EventInfo> GetRuntimeEvents(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
            this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvents(Everything);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static FieldInfo? GetRuntimeField(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            this Type type, string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetField(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRuntimeMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            this Type type, string name, Type[] parameters)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetMethod(name, parameters);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PropertyInfo? GetRuntimeProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            this Type type, string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetProperty(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EventInfo? GetRuntimeEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
            this Type type, string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetEvent(name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo? GetRuntimeBaseDefinition(this MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);

            return method.GetBaseDefinition();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);

            return typeInfo.GetInterfaceMap(interfaceType);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            ArgumentNullException.ThrowIfNull(del);

            return del.Method;
        }
    }
}
