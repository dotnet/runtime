// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace System
{
    public abstract partial class Attribute
    {
        #region Private Statics

        #region PropertyInfo
        private static Attribute[] InternalGetCustomAttributes(PropertyInfo element, Type type, bool inherit)
        {
            Debug.Assert(element != null);
            Debug.Assert(type != null);
            Debug.Assert(type.IsSubclassOf(typeof(Attribute)) || type == typeof(Attribute));

            // walk up the hierarchy chain
            Attribute[] attributes = (Attribute[])element.GetCustomAttributes(type, inherit);

            if (!inherit)
                return attributes;

            // create the hashtable that keeps track of inherited types
            Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);

            // create an array list to collect all the requested attibutes
            List<Attribute> attributeList = new List<Attribute>();
            CopyToArrayList(attributeList, attributes, types);

            //if this is an index we need to get the parameter types to help disambiguate
            Type[] indexParamTypes = GetIndexParameterTypes(element);


            PropertyInfo? baseProp = GetParentDefinition(element, indexParamTypes);
            while (baseProp != null)
            {
                attributes = GetCustomAttributes(baseProp, type, false);
                AddAttributesToList(attributeList, attributes, types);
                baseProp = GetParentDefinition(baseProp, indexParamTypes);
            }
            Attribute[] array = CreateAttributeArrayHelper(type, attributeList.Count);
            attributeList.CopyTo(array, 0);
            return array;
        }

        private static bool InternalIsDefined(PropertyInfo element, Type attributeType, bool inherit)
        {
            // walk up the hierarchy chain
            if (element.IsDefined(attributeType, inherit))
                return true;

            if (inherit)
            {
                AttributeUsageAttribute usage = InternalGetAttributeUsage(attributeType);

                if (!usage.Inherited)
                    return false;

                //if this is an index we need to get the parameter types to help disambiguate
                Type[] indexParamTypes = GetIndexParameterTypes(element);

                PropertyInfo? baseProp = GetParentDefinition(element, indexParamTypes);

                while (baseProp != null)
                {
                    if (baseProp.IsDefined(attributeType, false))
                        return true;

                    baseProp = GetParentDefinition(baseProp, indexParamTypes);
                }
            }

            return false;
        }

        private static PropertyInfo? GetParentDefinition(PropertyInfo property, Type[] propertyParameters)
        {
            Debug.Assert(property != null);

            // for the current property get the base class of the getter and the setter, they might be different
            // note that this only works for RuntimeMethodInfo
            MethodInfo? propAccessor = property.GetGetMethod(true);

            if (propAccessor == null)
                propAccessor = property.GetSetMethod(true);

            RuntimeMethodInfo? rtPropAccessor = propAccessor as RuntimeMethodInfo;

            if (rtPropAccessor != null)
            {
                rtPropAccessor = rtPropAccessor.GetParentDefinition();

                if (rtPropAccessor != null)
                {
                    // There is a public overload of Type.GetProperty that takes both a BingingFlags enum and a return type.
                    // However, we cannot use that because it doesn't accept null for "types".
                    return rtPropAccessor.DeclaringType!.GetProperty(
                        property.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                        null, //will use default binder
                        property.PropertyType,
                        propertyParameters, //used for index properties
                        null);
                }
            }

            return null;
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

                EventInfo? baseEvent = GetParentDefinition(element);
                while (baseEvent != null)
                {
                    attributes = GetCustomAttributes(baseEvent, type, false);
                    AddAttributesToList(attributeList, attributes, types);
                    baseEvent = GetParentDefinition(baseEvent);
                }
                Attribute[] array = CreateAttributeArrayHelper(type, attributeList.Count);
                attributeList.CopyTo(array, 0);
                return array;
            }
            else
                return attributes;
        }

        private static EventInfo? GetParentDefinition(EventInfo ev)
        {
            Debug.Assert(ev != null);

            // note that this only works for RuntimeMethodInfo
            MethodInfo? add = ev.GetAddMethod(true);

            RuntimeMethodInfo? rtAdd = add as RuntimeMethodInfo;

            if (rtAdd != null)
            {
                rtAdd = rtAdd.GetParentDefinition();
                if (rtAdd != null)
                    return rtAdd.DeclaringType!.GetEvent(ev.Name!);
            }
            return null;
        }

        private static bool InternalIsDefined(EventInfo element, Type attributeType, bool inherit)
        {
            Debug.Assert(element != null);

            // walk up the hierarchy chain
            if (element.IsDefined(attributeType, inherit))
                return true;

            if (inherit)
            {
                AttributeUsageAttribute usage = InternalGetAttributeUsage(attributeType);

                if (!usage.Inherited)
                    return false;

                EventInfo? baseEvent = GetParentDefinition(element);

                while (baseEvent != null)
                {
                    if (baseEvent.IsDefined(attributeType, false))
                        return true;
                    baseEvent = GetParentDefinition(baseEvent);
                }
            }

            return false;
        }

        #endregion

        #region ParameterInfo
        private static ParameterInfo? GetParentDefinition(ParameterInfo param)
        {
            Debug.Assert(param != null);

            // note that this only works for RuntimeMethodInfo
            RuntimeMethodInfo? rtMethod = param.Member as RuntimeMethodInfo;

            if (rtMethod != null)
            {
                rtMethod = rtMethod.GetParentDefinition();

                if (rtMethod != null)
                {
                    // Find the ParameterInfo on this method
                    int position = param.Position;
                    if (position == -1)
                    {
                        return rtMethod.ReturnParameter;
                    }
                    else
                    {
                        ParameterInfo[] parameters = rtMethod.GetParameters();
                        return parameters[position]; // Point to the correct ParameterInfo of the method
                    }
                }
            }
            return null;
        }

        private static Attribute[] InternalParamGetCustomAttributes(ParameterInfo param, Type? type, bool inherit)
        {
            Debug.Assert(param != null);

            // For ParameterInfo's we need to make sure that we chain through all the MethodInfo's in the inheritance chain that
            // have this ParameterInfo defined. .We pick up all the CustomAttributes for the starting ParameterInfo. We need to pick up only attributes 
            // that are marked inherited from the remainder of the MethodInfo's in the inheritance chain.
            // For MethodInfo's on an interface we do not do an inheritance walk so the default ParameterInfo attributes are returned.
            // For MethodInfo's on a class we walk up the inheritance chain but do not look at the MethodInfo's on the interfaces that the
            // class inherits from and return the respective ParameterInfo attributes

            List<Type> disAllowMultiple = new List<Type>();
            object?[] objAttr;

            if (type == null)
                type = typeof(Attribute);

            objAttr = param.GetCustomAttributes(type, false);

            for (int i = 0; i < objAttr.Length; i++)
            {
                Type objType = objAttr[i]!.GetType();
                AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);
                if (attribUsage.AllowMultiple == false)
                    disAllowMultiple.Add(objType);
            }

            // Get all the attributes that have Attribute as the base class
            Attribute[] ret;
            if (objAttr.Length == 0)
                ret = CreateAttributeArrayHelper(type, 0);
            else
                ret = (Attribute[])objAttr;

            if (param.Member.DeclaringType is null) // This is an interface so we are done.
                return ret;

            if (!inherit)
                return ret;

            ParameterInfo? baseParam = GetParentDefinition(param);

            while (baseParam != null)
            {
                objAttr = baseParam.GetCustomAttributes(type, false);

                int count = 0;
                for (int i = 0; i < objAttr.Length; i++)
                {
                    Type objType = objAttr[i]!.GetType();
                    AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);

                    if ((attribUsage.Inherited) && (disAllowMultiple.Contains(objType) == false))
                    {
                        if (attribUsage.AllowMultiple == false)
                            disAllowMultiple.Add(objType);
                        count++;
                    }
                    else
                        objAttr[i] = null;
                }

                // Get all the attributes that have Attribute as the base class
                Attribute[] attributes = CreateAttributeArrayHelper(type, count);

                count = 0;
                for (int i = 0; i < objAttr.Length; i++)
                {
                    if (objAttr[i] != null)
                    {
                        attributes[count] = (Attribute)objAttr[i]!; // TODO-NULLABLE https://github.com/dotnet/roslyn/issues/34644
                        count++;
                    }
                }

                Attribute[] temp = ret;
                ret = CreateAttributeArrayHelper(type, temp.Length + count);
                Array.Copy(temp, 0, ret, 0, temp.Length);

                int offset = temp.Length;

                for (int i = 0; i < attributes.Length; i++)
                    ret[offset + i] = attributes[i];

                baseParam = GetParentDefinition(baseParam);
            }

            return ret;
        }

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

            ParameterInfo? baseParam = GetParentDefinition(param);

            while (baseParam != null)
            {
                object[] objAttr = baseParam.GetCustomAttributes(type, false);

                for (int i = 0; i < objAttr.Length; i++)
                {
                    Type objType = objAttr[i].GetType();
                    AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);

                    if ((objAttr[i] is Attribute) && (attribUsage.Inherited))
                        return true;
                }

                baseParam = GetParentDefinition(baseParam);
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
                    types[attrType] = InternalGetAttributeUsage(attrType);
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

            return Array.Empty<Type>();
        }

        private static void AddAttributesToList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                Type attrType = attributes[i].GetType();
                AttributeUsageAttribute usage;
                types.TryGetValue(attrType, out usage);

                if (usage == null)
                {
                    // the type has never been seen before if it's inheritable add it to the list
                    usage = InternalGetAttributeUsage(attrType);
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

        private static AttributeUsageAttribute InternalGetAttributeUsage(Type type)
        {
            // Check if the custom attributes is Inheritable
            object[] obj = type.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

            if (obj.Length == 1)
                return (AttributeUsageAttribute)obj[0];

            if (obj.Length == 0)
                return AttributeUsageAttribute.Default;

            throw new FormatException(
                SR.Format(SR.Format_AttributeUsage, type));
        }

        private static Attribute[] CreateAttributeArrayHelper(Type elementType, int elementCount)
        {
            return (Attribute[])Array.CreateInstance(elementType, elementCount);
        }
        #endregion

        #endregion

        #region Public Statics

        #region MemberInfo
        public static Attribute[] GetCustomAttributes(MemberInfo element, Type type)
        {
            return GetCustomAttributes(element, type, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, Type type, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsSubclassOf(typeof(Attribute)) && type != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

            switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalGetCustomAttributes((PropertyInfo)element, type, inherit);

                case MemberTypes.Event:
                    return InternalGetCustomAttributes((EventInfo)element, type, inherit);

                default:
                    return (element.GetCustomAttributes(type, inherit) as Attribute[])!;
            }
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalGetCustomAttributes((PropertyInfo)element, typeof(Attribute), inherit);

                case MemberTypes.Event:
                    return InternalGetCustomAttributes((EventInfo)element, typeof(Attribute), inherit);

                default:
                    return (element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[])!;
            }
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

            switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalIsDefined((PropertyInfo)element, attributeType, inherit);

                case MemberTypes.Event:
                    return InternalIsDefined((EventInfo)element, attributeType, inherit);

                default:
                    return element.IsDefined(attributeType, inherit);
            }
        }

        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
        {
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);

            if (attrib == null || attrib.Length == 0)
                return null;

            if (attrib.Length == 1)
                return attrib[0];

            throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
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


            MemberInfo member = element.Member;
            if (member.MemberType == MemberTypes.Method && inherit)
                return InternalParamGetCustomAttributes(element, attributeType, inherit) as Attribute[];

            return (element.GetCustomAttributes(attributeType, inherit) as Attribute[])!;
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (element.Member == null)
                throw new ArgumentException(SR.Argument_InvalidParameterInfo, nameof(element));


            MemberInfo member = element.Member;
            if (member.MemberType == MemberTypes.Method && inherit)
                return InternalParamGetCustomAttributes(element, null, inherit) as Attribute[];

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
            // Returns an Attribute of base class/inteface attributeType on the ParameterInfo or null if none exists.
            // throws an AmbiguousMatchException if there are more than one defined.
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);

            if (attrib == null || attrib.Length == 0)
                return null;

            if (attrib.Length == 0)
                return null;

            if (attrib.Length == 1)
                return attrib[0];

            throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
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
            // Returns an Attribute of base class/inteface attributeType on the Module or null if none exists.
            // throws an AmbiguousMatchException if there are more than one defined.
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);

            if (attrib == null || attrib.Length == 0)
                return null;

            if (attrib.Length == 1)
                return attrib[0];

            throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
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
            // Returns an Attribute of base class/inteface attributeType on the Assembly or null if none exists.
            // throws an AmbiguousMatchException if there are more than one defined.
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);

            if (attrib == null || attrib.Length == 0)
                return null;

            if (attrib.Length == 1)
                return attrib[0];

            throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
        }

        #endregion

        #endregion
    }
}
