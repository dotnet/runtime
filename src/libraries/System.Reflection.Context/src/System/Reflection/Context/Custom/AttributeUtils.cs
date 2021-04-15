// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Context.Custom
{
    internal static class AttributeUtils
    {
        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomType type, Type attributeFilterType, bool inherit)
        {
            IEnumerable<object> attributes = GetFilteredAttributes(context, type.UnderlyingType, attributeFilterType);

            if (!inherit)
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            CustomType baseMember = type.BaseType as CustomType;

            if (baseMember == null)
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            // GetAttributeUsage is expensive and should be put off as much as possible.
            bool isSealed = attributeFilterType.IsSealed;
            bool inherited;
            bool allowMultiple;
            GetAttributeUsage(attributeFilterType, out inherited, out allowMultiple);

            if (isSealed && !inherited)
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            // declaredAttributes should have already been filtered by attributeFilterType
            List<object> results = new List<object>(attributes);

            do
            {
                if (isSealed && results.Count > 0 && !allowMultiple)
                    break;

                type = baseMember;

                IEnumerable<object> inheritedAttributes = GetFilteredAttributes(context, type.UnderlyingType, attributeFilterType);
                CombineCustomAttributes(results, inheritedAttributes, attributeFilterType, inherited, allowMultiple);

                baseMember = type.BaseType as CustomType;
            } while (baseMember != null);

            return CollectionServices.ConvertListToArray(results, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomMethodInfo method, Type attributeFilterType, bool inherit)
        {
            IEnumerable<object> attributes = GetFilteredAttributes(context, method.UnderlyingMethod, attributeFilterType);

            if (!inherit)
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            CustomMethodInfo baseMember = method.GetBaseDefinition() as CustomMethodInfo;

            if (baseMember == null || baseMember.Equals(method))
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            // GetAttributeUsage is expensive and should be put off as much as possible.
            bool isSealed = attributeFilterType.IsSealed;
            bool inherited;
            bool allowMultiple;
            GetAttributeUsage(attributeFilterType, out inherited, out allowMultiple);

            if (isSealed && !inherited)
                return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);

            // declaredAttributes should have already been filtered by attributeFilterType
            List<object> results = new List<object>(attributes);

            do
            {
                if (isSealed && results.Count > 0 && !allowMultiple)
                    break;

                method = baseMember;

                IEnumerable<object> inheritedAttributes = GetFilteredAttributes(context, method.UnderlyingMethod, attributeFilterType);
                CombineCustomAttributes(results, inheritedAttributes, attributeFilterType, inherited, allowMultiple);

                baseMember = method.GetBaseDefinition() as CustomMethodInfo;
            } while (baseMember != null && !baseMember.Equals(method));

            return CollectionServices.ConvertListToArray(results, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomConstructorInfo constructor, Type attributeFilterType, bool inherit)
        {
            ConstructorInfo provider = constructor.UnderlyingConstructor;
            IEnumerable<object> attributes = GetFilteredAttributes(context, provider, attributeFilterType);

            return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomPropertyInfo property, Type attributeFilterType, bool inherit)
        {
            PropertyInfo provider = property.UnderlyingProperty;
            IEnumerable<object> attributes = GetFilteredAttributes(context, provider, attributeFilterType);

            return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomEventInfo evnt, Type attributeFilterType, bool inherit)
        {
            EventInfo provider = evnt.UnderlyingEvent;
            IEnumerable<object> attributes = GetFilteredAttributes(context, provider, attributeFilterType);

            return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomFieldInfo field, Type attributeFilterType, bool inherit)
        {
            FieldInfo provider = field.UnderlyingField;
            IEnumerable<object> attributes = GetFilteredAttributes(context, provider, attributeFilterType);

            return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);
        }

        public static object[] GetCustomAttributes(CustomReflectionContext context, CustomParameterInfo parameter, Type attributeFilterType, bool inherit)
        {
            ParameterInfo provider = parameter.UnderlyingParameter;
            IEnumerable<object> attributes = GetFilteredAttributes(context, provider, attributeFilterType);

            return CollectionServices.IEnumerableToArray(attributes, attributeFilterType);
        }

        public static bool IsDefined(ICustomAttributeProvider provider, Type attributeType, bool inherit)
        {
            object[] attributes = provider.GetCustomAttributes(attributeType, inherit);
            return attributes != null && attributes.Length > 0;
        }

        private static IEnumerable<object> GetFilteredAttributes(CustomReflectionContext context, MemberInfo member, Type attributeFilterType)
        {
            object[] objects = member.GetCustomAttributes(attributeFilterType, false);

            return context.GetCustomAttributesOnMember(member, objects, attributeFilterType);
        }

        private static IEnumerable<object> GetFilteredAttributes(CustomReflectionContext context, ParameterInfo parameter, Type attributeFilterType)
        {
            object[] objects = parameter.GetCustomAttributes(attributeFilterType, false);

            return context.GetCustomAttributesOnParameter(parameter, objects, attributeFilterType);
        }

        private static void CombineCustomAttributes(List<object> declaredAttributes, IEnumerable<object> inheritedAttributes, Type attributeFilterType, bool inherited, bool allowMultiple)
        {
            foreach (object newAttribute in inheritedAttributes)
            {
                // derived attributes should have already been filtered
                Debug.Assert(attributeFilterType.IsInstanceOfType(newAttribute));

                Type attributeType = newAttribute.GetType();

                if (attributeType != attributeFilterType)
                {
                    Debug.Assert(attributeFilterType.IsAssignableFrom(attributeType));
                    GetAttributeUsage(attributeType, out inherited, out allowMultiple);
                }

                // Don't add duplicate attributes whose AllowMultiple is false.
                // Note that duplicates declared on the same type won't be filtered out.
                if (inherited &&
                    (allowMultiple ||
                     declaredAttributes.FindIndex((obj) => obj.GetType() == attributeType) < 0))
                {
                    declaredAttributes.Add(newAttribute);
                }
            }
        }

        private static void GetAttributeUsage(Type attributeFilterType, out bool inherited, out bool allowMultiple)
        {
            AttributeUsageAttribute[] usageAttributes = (AttributeUsageAttribute[])attributeFilterType.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

            if (usageAttributes == null || usageAttributes.Length == 0)
            {
                // The default AttributeUsageAttribute.
                inherited = true;
                allowMultiple = false;
            }
            else if (usageAttributes.Length == 1)
            {
                AttributeUsageAttribute usage = usageAttributes[0];
                inherited = usage.Inherited;
                allowMultiple = usage.AllowMultiple;
            }
            else
                throw new FormatException(SR.Format(SR.Format_AttributeUsage, attributeFilterType));
        }

        internal static IEnumerable<object> FilterCustomAttributes(IEnumerable<object> attributes, Type attributeFilterType)
        {
            foreach (object attr in attributes)
            {
                if (attr == null)
                    throw new InvalidOperationException(SR.InvalidOperation_NullAttribute);

                if (attributeFilterType.IsInstanceOfType(attr))
                    yield return attr;
            }
        }
    }
}
