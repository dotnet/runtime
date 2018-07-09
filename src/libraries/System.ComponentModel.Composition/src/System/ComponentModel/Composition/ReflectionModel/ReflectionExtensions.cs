// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

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
                    if(accessors.Length != 1)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    return ((FieldInfo)accessors[0]).ToReflectionField();

                case MemberTypes.Property:
                    if(accessors.Length != 2)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    return ReflectionExtensions.CreateReflectionProperty((MethodInfo)accessors[0], (MethodInfo)accessors[1]);

                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    return ((Type)accessors[0]).ToReflectionType();

                default:
                    if(memberType != MemberTypes.Method)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    return ((MethodInfo)accessors[0]).ToReflectionMethod();
            }
        }

        public static LazyMemberInfo ToLazyMember(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo property = member as PropertyInfo;
                if (property == null)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }

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
            if((lazyMember.MemberType != MemberTypes.Field) && (lazyMember.MemberType != MemberTypes.Property))
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            ReflectionWritableMember reflectionMember = lazyMember.ToReflectionMember() as ReflectionWritableMember;
            if (reflectionMember == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            return reflectionMember;
        }

        public static ReflectionProperty ToReflectionProperty(this PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return CreateReflectionProperty(property.GetGetMethod(true), property.GetSetMethod(true));
        }

        public static ReflectionProperty CreateReflectionProperty(MethodInfo getMethod, MethodInfo setMethod)
        {
            if(getMethod == null && setMethod == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            return new ReflectionProperty(getMethod, setMethod);
        }

        public static ReflectionParameter ToReflectionParameter(this ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new ReflectionParameter(parameter);
        }

        public static ReflectionMethod ToReflectionMethod(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            return new ReflectionMethod(method);
        }

        public static ReflectionField ToReflectionField(this FieldInfo field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return new ReflectionField(field);
        }

        public static ReflectionType ToReflectionType(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new ReflectionType(type);
        }

        public static ReflectionWritableMember ToReflectionWritableMember(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member.MemberType == MemberTypes.Property)
            {
                return ((PropertyInfo)member).ToReflectionProperty();
            }

            return ((FieldInfo)member).ToReflectionField();
        }
    }
}
