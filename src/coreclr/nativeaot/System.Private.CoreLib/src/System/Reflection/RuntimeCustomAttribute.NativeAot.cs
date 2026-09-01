// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;

using Internal.LowLevelLinq;
using Internal.Reflection.Augments;
using Internal.Reflection.Extensions.NonPortable;

namespace System.Reflection
{
    internal static class RuntimeCustomAttribute
    {
        internal static object[] GetCustomAttributes(Assembly element, Type attributeType) =>
            Instantiate(GetMatchingCustomAttributes(element, attributeType, inherit: false), attributeType);

        internal static object[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit) =>
            Instantiate(GetMatchingCustomAttributes(element, attributeType, inherit), attributeType);

        internal static object[] GetCustomAttributes(Module element, Type attributeType) =>
            Instantiate(GetMatchingCustomAttributes(element, attributeType, inherit: false), attributeType);

        internal static object[] GetCustomAttributes(ParameterInfo element, Type attributeType) =>
            Instantiate(GetMatchingCustomAttributes(element, attributeType, inherit: false), attributeType);

        internal static bool IsDefined(Assembly element, Type attributeType) =>
            Any(GetMatchingCustomAttributes(element, attributeType, inherit: false));

        internal static bool IsDefined(MemberInfo element, Type attributeType, bool inherit) =>
            Any(GetMatchingCustomAttributes(element, attributeType, inherit));

        internal static bool IsDefined(Module element, Type attributeType) =>
            Any(GetMatchingCustomAttributes(element, attributeType, inherit: false));

        internal static bool IsDefined(ParameterInfo element, Type attributeType) =>
            Any(GetMatchingCustomAttributes(element, attributeType, inherit: false));

        private static bool Any(IEnumerable<CustomAttributeData> attributes)
        {
            using IEnumerator<CustomAttributeData> enumerator = attributes.GetEnumerator();
            return enumerator.MoveNext();
        }

        private static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(object element, Type attributeType, bool inherit)
        {
            Func<Type, bool> passesFilter = CreateFilter(attributeType);
            ListBuilder<CustomAttributeData> immediateResults = default;

            foreach (CustomAttributeData attribute in GetDeclaredCustomAttributes(element))
            {
                if (passesFilter(attribute.AttributeType))
                {
                    yield return attribute;

                    if (inherit)
                        immediateResults.Add(attribute);
                }
            }

            if (!inherit)
                yield break;

            object? parent = GetParent(element);
            if (parent is null)
                yield break;

            LowLevelDictionary<TypeUnificationKey, AttributeUsageAttribute> encounteredTypes =
                new LowLevelDictionary<TypeUnificationKey, AttributeUsageAttribute>(11);

            for (int i = 0; i < immediateResults.Count; i++)
            {
                TypeUnificationKey attributeTypeKey = new TypeUnificationKey(immediateResults[i].AttributeType);
                if (!encounteredTypes.TryGetValue(attributeTypeKey, out _))
                    encounteredTypes.Add(attributeTypeKey, null);
            }

            do
            {
                foreach (CustomAttributeData attribute in GetDeclaredCustomAttributes(parent))
                {
                    Type actualAttributeType = attribute.AttributeType;
                    if (!passesFilter(actualAttributeType))
                        continue;

                    TypeUnificationKey attributeTypeKey = new TypeUnificationKey(actualAttributeType);
                    if (!encounteredTypes.TryGetValue(attributeTypeKey, out AttributeUsageAttribute? usage))
                    {
                        usage = Attribute.InternalGetAttributeUsage(actualAttributeType);
                        encounteredTypes.Add(attributeTypeKey, usage);
                        if (usage.Inherited)
                            yield return attribute;
                    }
                    else
                    {
                        usage ??= Attribute.InternalGetAttributeUsage(actualAttributeType);
                        encounteredTypes[attributeTypeKey] = usage;
                        if (usage.Inherited && usage.AllowMultiple)
                            yield return attribute;
                    }
                }
            }
            while ((parent = GetParent(parent)) is not null);
        }

        private static Func<Type, bool> CreateFilter(Type attributeType)
        {
            bool attributeTypeIsSealed = attributeType.IsSealed;

            if (attributeType.IsGenericTypeDefinition)
            {
                return actualType =>
                {
                    if (actualType.IsConstructedGenericType && actualType.GetGenericTypeDefinition() == attributeType)
                        return true;

                    if (!attributeTypeIsSealed)
                    {
                        for (Type? type = actualType.BaseType; type is not null; type = type.BaseType)
                        {
                            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == attributeType)
                                return true;
                        }
                    }

                    return false;
                };
            }

            return actualType =>
                attributeType.Equals(actualType) ||
                (!attributeTypeIsSealed && attributeType.IsAssignableFrom(actualType));
        }

        private static IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(object element)
        {
            return element switch
            {
                Assembly assembly => assembly.CustomAttributes,
                MemberInfo member => member.CustomAttributes,
                Module module => module.CustomAttributes,
                ParameterInfo parameter => parameter.CustomAttributes,
                _ => throw new NotSupportedException()
            };
        }

        private static object? GetParent(object element)
        {
            if (element is Type type)
            {
                Type? baseType = type.BaseType;
                return baseType == typeof(object) || baseType == typeof(ValueType) ? null : baseType;
            }

            if (element is RuntimeMethodInfo method)
                return method.GetParentDefinition();

            return null;
        }

        private static object[] Instantiate(IEnumerable<CustomAttributeData> customAttributes, Type actualElementType)
        {
            ArrayBuilder<object> attributes = default;
            foreach (CustomAttributeData customAttribute in customAttributes)
            {
                attributes.Add(customAttribute.Instantiate());
            }

            object[] result = CreateAttributeArrayHelper(actualElementType, attributes.Count);
            attributes.CopyTo(result);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Array.CreateInstance is only used with reference types here and is therefore safe.")]
        private static object[] CreateAttributeArrayHelper(Type attributeType, int elementCount)
        {
            if (attributeType == typeof(Attribute) ||
                (attributeType.ContainsGenericParameters && attributeType.IsSubclassOf(typeof(Attribute))))
            {
                return new Attribute[elementCount];
            }

            if (attributeType.IsValueType || attributeType.ContainsGenericParameters)
                return new object[elementCount];

            return (object[])Array.CreateInstance(attributeType, elementCount);
        }
    }
}
