// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System
{
    public abstract partial class Attribute
    {
        #region Private Statics

        #region PropertyInfo

        private static bool InternalIsDefined(PropertyInfo element, Type attributeType, bool inherit)
        {
            // walk up the hierarchy chain
            if (element.IsDefined(attributeType, inherit))
                return true;

            if (inherit)
            {
                AttributeUsageAttribute usage = attributeType.GetAttributeUsageAttribute();

                if (!usage.Inherited)
                    return false;

                // if this is an index we need to get the parameter types to help disambiguate
                Type[] indexParamTypes = GetIndexParameterTypes(element);

                PropertyInfo? baseProp = CustomAttribute.GetParentDefinition(element, indexParamTypes);

                while (baseProp != null)
                {
                    if (baseProp.IsDefined(attributeType, false))
                        return true;

                    baseProp = CustomAttribute.GetParentDefinition(baseProp, indexParamTypes);
                }
            }

            return false;
        }


        #endregion

        #region EventInfo
        private static Attribute[] InternalGetCustomAttributes(EventInfo element, Type type, bool inherit)
        {
            Debug.Assert(element != null);
            Debug.Assert(type != null);
            Debug.Assert(type.IsSubclassOf(typeof(Attribute)) || type == typeof(Attribute));

            // walk up the hierarchy chain
            Attribute[] attributes = (Attribute[])element.GetCustomAttributes(type, inherit);
            if (inherit)
            {
                // create the hashtable that keeps track of inherited types
                Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
                // create an array list to collect all the requested attibutes
                List<Attribute> attributeList = new List<Attribute>();
                CopyToArrayList(attributeList, attributes, types);

                EventInfo? baseEvent = CustomAttribute.GetParentDefinition(element);
                while (baseEvent != null)
                {
                    attributes = GetCustomAttributes(baseEvent, type, false);
                    AddAttributesToList(attributeList, attributes, types);
                    baseEvent = CustomAttribute.GetParentDefinition(baseEvent);
                }
                Attribute[] array = CreateAttributeArrayHelper(type, attributeList.Count);
                attributeList.CopyTo(array, 0);
                return array;
            }
            else
                return attributes;
        }


        private static bool InternalIsDefined(EventInfo element, Type attributeType, bool inherit)
        {
            Debug.Assert(element != null);

            // walk up the hierarchy chain
            if (element.IsDefined(attributeType, inherit))
                return true;

            if (inherit)
            {
                AttributeUsageAttribute usage = attributeType.GetAttributeUsageAttribute();

                if (!usage.Inherited)
                    return false;

                EventInfo? baseEvent = CustomAttribute.GetParentDefinition(element);

                while (baseEvent != null)
                {
                    if (baseEvent.IsDefined(attributeType, false))
                        return true;
                    baseEvent = CustomAttribute.GetParentDefinition(baseEvent);
                }
            }

            return false;
        }

        #endregion

        #region ParameterInfo
        private static bool InternalParamIsDefined(ParameterInfo param, Type type, bool inherit)
        {
            Debug.Assert(param != null);
            Debug.Assert(type != null);

            // For ParameterInfo's we need to make sure that we chain through all the MethodInfo's in the inheritance chain.
            // We pick up all the CustomAttributes for the starting ParameterInfo. We need to pick up only attributes
            // that are marked inherited from the remainder of the ParameterInfo's in the inheritance chain.
            // For MethodInfo's on an interface we do not do an inheritance walk. For ParameterInfo's on a
            // Class we walk up the inheritance chain but do not look at the MethodInfo's on the interfaces that the class inherits from.

            if (param.IsDefined(type, false))
                return true;

            if (param.Member.DeclaringType is null || !inherit) // This is an interface so we are done.
                return false;

            ParameterInfo? baseParam = CustomAttribute.GetParentDefinition(param);

            while (baseParam != null)
            {
                object[] objAttr = baseParam.GetCustomAttributes(type, false);

                for (int i = 0; i < objAttr.Length; i++)
                {
                    Type objType = objAttr[i].GetType();
                    AttributeUsageAttribute attribUsage = objType.GetAttributeUsageAttribute();

                    if ((objAttr[i] is Attribute) && (attribUsage.Inherited))
                        return true;
                }

                baseParam = CustomAttribute.GetParentDefinition(baseParam);
            }

            return false;
        }

        #endregion

        #region Utility
        private static void CopyToArrayList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                attributeList.Add(attributes[i]);

                Type attrType = attributes[i].GetType();

                if (!types.ContainsKey(attrType))
                    types[attrType] = attrType.GetAttributeUsageAttribute();
            }
        }

        private static Type[] GetIndexParameterTypes(PropertyInfo element)
        {
            ParameterInfo[] indexParams = element.GetIndexParameters();

            if (indexParams.Length > 0)
            {
                Type[] indexParamTypes = new Type[indexParams.Length];
                for (int i = 0; i < indexParams.Length; i++)
                {
                    indexParamTypes[i] = indexParams[i].ParameterType;
                }
                return indexParamTypes;
            }

            return Type.EmptyTypes;
        }

        private static void AddAttributesToList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                Type attrType = attributes[i].GetType();
                types.TryGetValue(attrType, out AttributeUsageAttribute? usage);

                if (usage == null)
                {
                    // the type has never been seen before if it's inheritable add it to the list
                    usage = attrType.GetAttributeUsageAttribute();
                    types[attrType] = usage;

                    if (usage.Inherited)
                        attributeList.Add(attributes[i]);
                }
                else if (usage.Inherited && usage.AllowMultiple)
                {
                    // we saw this type already add it only if it is inheritable and it does allow multiple
                    attributeList.Add(attributes[i]);
                }
            }
        }

        private static Attribute[] CreateAttributeArrayHelper(Type elementType, int elementCount)
        {
            return (Attribute[])Array.CreateInstance(elementType, elementCount);
        }
        #endregion

        #endregion

        #region Public Statics

        #region MemberInfo
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.MemberType switch
            {
                MemberTypes.Event => InternalGetCustomAttributes((EventInfo)element, attributeType, inherit),
                _ => (element.GetCustomAttributes(attributeType, inherit) as Attribute[])!,
            };
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return element.MemberType switch
            {
                MemberTypes.Event => InternalGetCustomAttributes((EventInfo)element, typeof(Attribute), inherit),
                _ => (element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[])!,
            };
        }

        public static bool IsDefined(MemberInfo element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit)
        {
            // Returns true if a custom attribute subclass of attributeType class/interface with inheritance walk
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.MemberType switch
            {
                MemberTypes.Property => InternalIsDefined((PropertyInfo)element, attributeType, inherit),
                MemberTypes.Event => InternalIsDefined((EventInfo)element, attributeType, inherit),
                _ => element.IsDefined(attributeType, inherit),
            };
        }

        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, inherit: true);
        }

        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.GetCustomAttribute(attributeType, inherit);
        }

        #endregion

        #region ParameterInfo
        public static Attribute[] GetCustomAttributes(ParameterInfo element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            if (element.Member == null)
                throw new ArgumentException(SR.Argument_InvalidParameterInfo, nameof(element));

            return (element.GetCustomAttributes(attributeType, inherit) as Attribute[])!;
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (element.Member == null)
                throw new ArgumentException(SR.Argument_InvalidParameterInfo, nameof(element));

            return (element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[])!;
        }

        public static bool IsDefined(ParameterInfo element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit)
        {
            // Returns true is a custom attribute subclass of attributeType class/interface with inheritance walk
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            MemberInfo member = element.Member;

            switch (member.MemberType)
            {
                case MemberTypes.Method: // We need to climb up the member hierarchy
                    return InternalParamIsDefined(element, attributeType, inherit);

                case MemberTypes.Constructor:
                    return element.IsDefined(attributeType, false);

                case MemberTypes.Property:
                    return element.IsDefined(attributeType, false);

                default:
                    Debug.Fail("Invalid type for ParameterInfo member in Attribute class");
                    throw new ArgumentException(SR.Argument_InvalidParamInfo);
            }
        }

        public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.GetCustomAttribute(attributeType, inherit);
        }

        #endregion

        #region Module
        public static Attribute[] GetCustomAttributes(Module element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(Module element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(Module element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
        }

        public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
        }

        public static bool IsDefined(Module element, Type attributeType)
        {
            return IsDefined(element, attributeType, false);
        }

        public static bool IsDefined(Module element, Type attributeType, bool inherit)
        {
            // Returns true is a custom attribute subclass of attributeType class/interface with no inheritance walk
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.IsDefined(attributeType, false);
        }

        public static Attribute? GetCustomAttribute(Module element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute? GetCustomAttribute(Module element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.GetCustomAttribute(attributeType, inherit);
        }

        #endregion

        #region Assembly
        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
        }

        public static Attribute[] GetCustomAttributes(Assembly element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(Assembly element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
        }

        public static bool IsDefined(Assembly element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(Assembly element, Type attributeType, bool inherit)
        {
            // Returns true is a custom attribute subclass of attributeType class/interface with no inheritance walk
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.IsDefined(attributeType, false);
        }

        public static Attribute? GetCustomAttribute(Assembly element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute? GetCustomAttribute(Assembly element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            return element.GetCustomAttribute(attributeType, inherit);
        }

        #endregion

        #endregion
    }
}
