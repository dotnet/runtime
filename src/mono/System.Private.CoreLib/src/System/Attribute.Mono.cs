// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System
{
    public partial class Attribute
    {
        private static Attribute? GetAttr(ICustomAttributeProvider element, Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            if (!attributeType.IsSubclassOf(typeof(Attribute)) && !attributeType.IsInterface
                && attributeType != typeof(Attribute) && attributeType != typeof(RuntimeCustomAttribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass + " " + attributeType.FullName);

            object[] attrs = RuntimeCustomAttribute.GetCustomAttributes(element, attributeType, inherit);
            if (attrs == null || attrs.Length == 0)
                return null;
            Attribute match = (Attribute)attrs[0];
            if (attrs.Length != 1)
                throw ThrowHelper.GetAmbiguousMatchException(match);
            return match;
        }

        public static Attribute? GetCustomAttribute(Assembly element, Type attributeType) => GetAttr(element, attributeType, true);
        public static Attribute? GetCustomAttribute(Assembly element, Type attributeType, bool inherit) => GetAttr(element, attributeType, inherit);
        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType) => GetAttr(element, attributeType, true);
        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit) => GetAttr(element, attributeType, inherit);
        public static Attribute? GetCustomAttribute(Module element, Type attributeType) => GetAttr(element, attributeType, true);
        public static Attribute? GetCustomAttribute(Module element, Type attributeType, bool inherit) => GetAttr(element, attributeType, inherit);
        public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType) => GetAttr(element, attributeType, true);
        public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit) => GetAttr(element, attributeType, inherit);

        public static Attribute[] GetCustomAttributes(Assembly element) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, true);
        public static Attribute[] GetCustomAttributes(Assembly element, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, inherit);
        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, true);
        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, inherit);
        public static Attribute[] GetCustomAttributes(MemberInfo element) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, true);
        public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, inherit);
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, true);
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, inherit);
        public static Attribute[] GetCustomAttributes(Module element) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, true);
        public static Attribute[] GetCustomAttributes(Module element, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, inherit);
        public static Attribute[] GetCustomAttributes(Module element, Type attributeType) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, true);
        public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, inherit);
        public static Attribute[] GetCustomAttributes(ParameterInfo element) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, true);
        public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes(element, inherit);
        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, true);
        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit) => (Attribute[])RuntimeCustomAttribute.GetCustomAttributes((ICustomAttributeProvider)element, attributeType, inherit);

        public static bool IsDefined(Assembly element, Type attributeType) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, true);
        public static bool IsDefined(Assembly element, Type attributeType, bool inherit) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, inherit);
        public static bool IsDefined(MemberInfo element, Type attributeType) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, true);
        public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, inherit);
        public static bool IsDefined(Module element, Type attributeType) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, true);
        public static bool IsDefined(Module element, Type attributeType, bool inherit) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, inherit);
        public static bool IsDefined(ParameterInfo element, Type attributeType) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, true);
        public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit) => RuntimeCustomAttribute.IsDefined((ICustomAttributeProvider)element, attributeType, inherit);
    }
}
