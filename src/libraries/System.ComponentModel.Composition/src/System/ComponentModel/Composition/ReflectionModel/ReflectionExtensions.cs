// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal static class ReflectionExtensions
    {
        public static ReflectionMember ToReflectionMember(this LazyMemberInfo lazyMember)
        {
            MemberInfo[] accessors = lazyMember.GetAccessors();
            MemberTypes memberType = lazyMember.MemberType;

            switch (memberType)
            {
                case MemberTypes.Field:
                    Assumes.IsTrue(accessors.Length == 1);
                    return ((FieldInfo)accessors[0]).ToReflectionField();

                case MemberTypes.Property:
                    Assumes.IsTrue(accessors.Length == 2);
                    return ReflectionExtensions.CreateReflectionProperty((MethodInfo)accessors[0], (MethodInfo)accessors[1]);

                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    return ((Type)accessors[0]).ToReflectionType();

                default:
                    Assumes.IsTrue(memberType == MemberTypes.Method);
                    return ((MethodInfo)accessors[0]).ToReflectionMethod();
            }
        }

        public static LazyMemberInfo ToLazyMember(this MemberInfo member)
        {
            Assumes.NotNull(member);

            if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo property = member as PropertyInfo;
                Assumes.NotNull(property);

                MemberInfo[] accessors = new MemberInfo[] { property.GetGetMethod(true), property.GetSetMethod(true) };
                return new LazyMemberInfo(MemberTypes.Property, accessors);
            }
            else
            {
                return new LazyMemberInfo(member);
            }
        }

        public static ReflectionWritableMember ToReflectionWriteableMember(this LazyMemberInfo lazyMember)
        {
            Assumes.IsTrue((lazyMember.MemberType == MemberTypes.Field) || (lazyMember.MemberType == MemberTypes.Property));

            ReflectionWritableMember reflectionMember = lazyMember.ToReflectionMember() as ReflectionWritableMember;
            Assumes.NotNull(reflectionMember);

            return reflectionMember;
        }

        public static ReflectionProperty ToReflectionProperty(this PropertyInfo property)
        {
            Assumes.NotNull(property);
            return CreateReflectionProperty(property.GetGetMethod(true), property.GetSetMethod(true));
        }

        public static ReflectionProperty CreateReflectionProperty(MethodInfo getMethod, MethodInfo setMethod)
        {
            Assumes.IsTrue(getMethod != null || setMethod != null);

            return new ReflectionProperty(getMethod, setMethod);
        }

        public static ReflectionParameter ToReflectionParameter(this ParameterInfo parameter)
        {
            Assumes.NotNull(parameter);
            return new ReflectionParameter(parameter);
        }

        public static ReflectionMethod ToReflectionMethod(this MethodInfo method)
        {
            Assumes.NotNull(method);
            return new ReflectionMethod(method);
        }

        public static ReflectionField ToReflectionField(this FieldInfo field)
        {
            Assumes.NotNull(field);
            return new ReflectionField(field);
        }

        public static ReflectionType ToReflectionType(this Type type)
        {
            Assumes.NotNull(type);
            return new ReflectionType(type);
        }

        public static ReflectionWritableMember ToReflectionWritableMember(this MemberInfo member)
        {
            Assumes.NotNull(member);
            if (member.MemberType == MemberTypes.Property)
            {
                return ((PropertyInfo)member).ToReflectionProperty();
            }

            return ((FieldInfo)member).ToReflectionField();
        }
    }
}
