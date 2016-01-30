// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Internal.Reflection.Extensions.NonPortable;

namespace System.Reflection
{
    //==============================================================================================================
    // This api set retrieves the "effective" set of custom attributes associated with a given Reflection element.
    // The effective set not only includes attributes declared directly on the element but attributes inherited 
    // from the element's "parents."
    //
    // Api conventions:
    //
    //    - "T or "attributeType" arguments must be non-null, non-interface type that derives from System.Attribute.
    //
    //    - The default for the "inherited" parameter is "true". For Assemblies, Modules and FieldInfos, 
    //      the api ignores the value of "inherited."
    //
    //
    // Definition of "effective" set of custom attributes:
    //
    //   The following element types can inherit custom attributes from its "parents". Inheritance is transitive,
    //   so this also includes grandparents, etc.
    //
    //      - TypeInfos inherit from base classes (but not interfaces.)
    //
    //      - MethodInfos that override a virtual in a base class (but not an interface) inherit 
    //        from the method it overrode.
    //
    //      - PropertyInfos that override a virtual in a base class (but not an interface) inherit
    //        from the property it override.
    //
    //      - EventInfos that override a virtual in a base class (but not an interface) inherit
    //        from the event it override.
    //
    //      - ParameterInfos whose declaring method overrides a virtual in a base class (but not an interface)
    //        inherit from the matching parameter in the method that was overridden.
    //
    //   Custom attributes only flow down this chain if they are marked inheritable. Note that the 
    //   AttributeUsageAttribute attribute it itself inheritable, and custom attributes can derive from other custom attributes:
    //   if a custom attribute and its base class(s) both define AttributeUsages, the most derived AttributeUsage wins.
    //
    //   If an element and one of its parents both include a custom attribute of the *exact same type* (even 
    //   if calling different overloads of the constructor), and that attribute types does *not* declare AllowMultiple=true,
    //   these apis will only return the one attached to the most derived parent.)
    //==============================================================================================================
    public static class CustomAttributeExtensions
    {
        //==============================================================================================================
        // This group returns the single custom attribute whose type matches or is a subclass of "attributeType".
        //
        // Returns null if no matching custom attribute found.
        // 
        // Throws AmbiguousMatchException if multple matches found.
        //==============================================================================================================
        public static Attribute GetCustomAttribute(this Assembly element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.OneOrNull<Attribute>();
        }

        public static Attribute GetCustomAttribute(this Module element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.OneOrNull<Attribute>();
        }

        public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.OneOrNull<Attribute>();
        }

        public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.OneOrNull<Attribute>();
        }

        public static Attribute GetCustomAttribute(this ParameterInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.OneOrNull<Attribute>();
        }

        public static Attribute GetCustomAttribute(this ParameterInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.OneOrNull<Attribute>();
        }



        //==============================================================================================================
        // This group returns the single custom attribute whose type matches or is a subclass of "T".
        //
        // Returns null if no matching custom attribute found.
        // 
        // Throws AmbiguousMatchException if multple matches found.
        //==============================================================================================================
        public static T GetCustomAttribute<T>(this Assembly element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }

        public static T GetCustomAttribute<T>(this Module element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }

        public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit: true, skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }

        public static T GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit, skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }

        public static T GetCustomAttribute<T>(this ParameterInfo element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit: true, skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }

        public static T GetCustomAttribute<T>(this ParameterInfo element, bool inherit) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit, skipTypeValidation: true);
            return matches.OneOrNull<T>();
        }



        //==============================================================================================================
        // This group retrieves all custom attributes that applies to a given element.
        //==============================================================================================================
        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Module element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit: true, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit: true, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(null, inherit, skipTypeValidation: true);
            return matches.Select(m => m.Instantiate());
        }


        //==============================================================================================================
        // This group retrieves all custom attributes associated with a given element whose attribute type matches or
        // is a subclass of "attributeType".
        //==============================================================================================================
        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Instantiate(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Module element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Instantiate(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.Instantiate(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Instantiate(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.Instantiate(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Instantiate(attributeType);
        }

        //==============================================================================================================
        // This group retrieves all custom attributes associated with a given element whose attribute type matches or
        // is a subclass of "T".
        //==============================================================================================================
        public static IEnumerable<T> GetCustomAttributes<T>(this Assembly element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Module element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit: true, skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit, skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit: true, skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element, bool inherit) where T : Attribute
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit, skipTypeValidation: true);
            return matches.Select(m => (T)(m.Instantiate()));
        }

        //==============================================================================================================
        // This group determines whether the element has an associated custom attribute whose type matches or is a subclass
        // of "attributeType".
        //==============================================================================================================
        public static bool IsDefined(this Assembly element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Any();
        }

        public static bool IsDefined(this Module element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType);
            return matches.Any();
        }

        public static bool IsDefined(this MemberInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.Any();
        }

        public static bool IsDefined(this MemberInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Any();
        }

        public static bool IsDefined(this ParameterInfo element, Type attributeType)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit: true);
            return matches.Any();
        }

        public static bool IsDefined(this ParameterInfo element, Type attributeType, bool inherit)
        {
            IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(attributeType, inherit);
            return matches.Any();
        }


        //==============================================================================================================================
        // Helper for the GetCustomAttribute() family.
        //==============================================================================================================================
        private static T OneOrNull<T>(this IEnumerable<CustomAttributeData> results) where T : Attribute
        {
            IEnumerator<CustomAttributeData> enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            CustomAttributeData result = enumerator.Current;
            if (enumerator.MoveNext())
                throw new AmbiguousMatchException();
            return (T)(result.Instantiate());
        }


        //==============================================================================================================================
        // Helper for the GetCustomAttributes() methods that take a specific attribute type. For desktop compatibility, 
        // we return an array of the specific attribute type even though the api's return type promises only an IEnumerable<Attribute>.
        // There are known store apps that cast the results of apis and expect the cast to work.
        //==============================================================================================================================
        private static IEnumerable<Attribute> Instantiate(this IEnumerable<CustomAttributeData> cads, Type actualElementType)
        {
            LowLevelList<Attribute> attributes = new LowLevelList<Attribute>();
            foreach (CustomAttributeData cad in cads)
            {
                Attribute instantiatedAttribute = cad.Instantiate();
                attributes.Add(instantiatedAttribute);
            }
            int count = attributes.Count;
            Attribute[] result;
            result = (Attribute[])Array.CreateInstance(actualElementType, count);
            attributes.CopyTo(result, 0);
            return result;
        }
    }
}
