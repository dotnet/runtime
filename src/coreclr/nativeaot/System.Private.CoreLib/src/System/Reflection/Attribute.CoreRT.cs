// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Internal.LowLevelLinq;
using Internal.Reflection.Extensions.NonPortable;

namespace System
{
    public abstract partial class Attribute
    {
        public static Attribute GetCustomAttribute(Assembly element, Type attributeType)
        {
            return OneOrNull(element.GetMatchingCustomAttributes(attributeType));
        }
        public static Attribute GetCustomAttribute(Assembly element, Type attributeType, bool inherit) => GetCustomAttribute(element, attributeType); // "inherit" is meaningless for assemblies

        public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType) => GetCustomAttribute(element, attributeType, inherit: true);
        public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
        {
            return OneOrNull(element.GetMatchingCustomAttributes(attributeType, inherit));
        }

        public static Attribute GetCustomAttribute(Module element, Type attributeType)
        {
            return OneOrNull(element.GetMatchingCustomAttributes(attributeType));
        }
        public static Attribute GetCustomAttribute(Module element, Type attributeType, bool inherit) => GetCustomAttribute(element, attributeType); // "inherit" is meaningless for modules

        public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType) => CustomAttributeExtensions.GetCustomAttribute(element, attributeType, inherit: true);
        public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit)
        {
            return OneOrNull(element.GetMatchingCustomAttributes(attributeType, inherit));
        }

        public static Attribute[] GetCustomAttributes(Assembly element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate()).ToArray();
        }
        public static Attribute[] GetCustomAttributes(Assembly element, bool inherit) => GetCustomAttributes(element); // "inherit" is meaningless for assemblies
        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType)
        {
            return Instantiate(element.GetMatchingCustomAttributes(attributeType), attributeType);
        }
        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit) => GetCustomAttributes(element, attributeType); // "inherit" is meaningless for modules

        public static Attribute[] GetCustomAttributes(MemberInfo element) => GetCustomAttributes(element, inherit: true);
        public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate()).ToArray();
        }
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType) => GetCustomAttributes(element, attributeType, inherit: true);
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit)
        {
            return Instantiate(element.GetMatchingCustomAttributes(attributeType, inherit), attributeType);
        }

        public static Attribute[] GetCustomAttributes(Module element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate()).ToArray();
        }
        public static Attribute[] GetCustomAttributes(Module element, bool inherit) => GetCustomAttributes(element); // "inherit" is meaningless for assemblies
        public static Attribute[] GetCustomAttributes(Module element, Type attributeType)
        {
            return Instantiate(element.GetMatchingCustomAttributes(attributeType), attributeType);
        }
        public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit) => GetCustomAttributes(element, attributeType); // "inherit" is meaningless for modules

        public static Attribute[] GetCustomAttributes(ParameterInfo element) => GetCustomAttributes(element, inherit: true);
        public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate()).ToArray();
        }
        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType) => GetCustomAttributes(element, attributeType, inherit: true);
        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit)
        {
            return Instantiate(element.GetMatchingCustomAttributes(attributeType, inherit), attributeType);
        }

        public static bool IsDefined(Assembly element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Any();
        }
        public static bool IsDefined(Assembly element, Type attributeType, bool inherit) => IsDefined(element, attributeType); // "inherit" is meaningless for assemblies

        public static bool IsDefined(MemberInfo element, Type attributeType) => IsDefined(element, attributeType, inherit: true);
        public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Any();
        }

        public static bool IsDefined(Module element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Any();
        }
        public static bool IsDefined(Module element, Type attributeType, bool inherit) => IsDefined(element, attributeType); // "inherit" is meaningless for modules

        public static bool IsDefined(ParameterInfo element, Type attributeType) => IsDefined(element, attributeType, inherit: true);
        public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Any();
        }

        //==============================================================================================================================
        // Helper for the GetCustomAttribute() family.
        //==============================================================================================================================
        private static Attribute OneOrNull(IEnumerable<CustomAttributeData> results)
        {
            IEnumerator<CustomAttributeData> enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            CustomAttributeData result = enumerator.Current;
            if (enumerator.MoveNext())
                throw new AmbiguousMatchException();
            return result.Instantiate();
        }

        //==============================================================================================================================
        // Helper for the GetCustomAttributes() methods that take a specific attribute type. For desktop compatibility,
        // we return a freshly allocated array of the specific attribute type even though the api's return type promises only an Attribute[].
        // There are known store apps that cast the results of apis and expect the cast to work.
        //==============================================================================================================================
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Arrays of reference types are safe to create.")]
        private static Attribute[] Instantiate(IEnumerable<CustomAttributeData> cads, Type actualElementType)
        {
            LowLevelList<Attribute> attributes = new LowLevelList<Attribute>();
            foreach (CustomAttributeData cad in cads)
            {
                Attribute instantiatedAttribute = cad.Instantiate();
                attributes.Add(instantiatedAttribute);
            }
            int count = attributes.Count;
            Attribute[] result = actualElementType.ContainsGenericParameters
                ? new Attribute[count]
                : (Attribute[])Array.CreateInstance(actualElementType, count);
            attributes.CopyTo(result, 0);
            return result;
        }
    }
}
