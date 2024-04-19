// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// (c) 2002,2003 Ximian, Inc. (http://www.ximian.com)
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2013 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    internal static class CustomAttribute
    {
        private static Assembly? corlib;
        [ThreadStatic]
        private static Dictionary<Type, AttributeUsageAttribute>? usage_cache;

        /* Treat as user types all corlib types extending System.Type that are not RuntimeType and TypeBuilder */
        private static bool IsUserCattrProvider(object obj)
        {
            Type? type = obj as Type;
            if ((type is RuntimeType) || (RuntimeFeature.IsDynamicCodeSupported && type?.IsTypeBuilder() == true))
                return false;
            if ((obj is Type))
                return true;

            corlib ??= typeof(int).Assembly;
            return obj.GetType().Assembly != corlib;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Attribute[] GetCustomAttributesInternal(ICustomAttributeProvider obj, Type attributeType, bool pseudoAttrs);

        internal static object[]? GetPseudoCustomAttributes(ICustomAttributeProvider obj, Type attributeType)
        {
            object[]? pseudoAttrs = null;
            /* FIXME: Add other types */
            if (obj is RuntimeMethodInfo monoMethod)
                pseudoAttrs = monoMethod.GetPseudoCustomAttributes();
            else if (obj is RuntimeFieldInfo fieldInfo)
                pseudoAttrs = fieldInfo.GetPseudoCustomAttributes();
            else if (obj is RuntimeParameterInfo monoParamInfo)
                pseudoAttrs = monoParamInfo.GetPseudoCustomAttributes();
            else if (obj is Type t)
                pseudoAttrs = GetPseudoCustomAttributes(t);

            if ((attributeType != null) && (pseudoAttrs != null))
            {
                for (int i = 0; i < pseudoAttrs.Length; ++i)
                    if (attributeType.IsAssignableFrom(pseudoAttrs[i].GetType()))
                        if (pseudoAttrs.Length == 1)
                            return pseudoAttrs;
                        else
                            return new object[] { pseudoAttrs[i] };
                return Array.Empty<object>();
            }

            return pseudoAttrs;
        }

        private static object[]? GetPseudoCustomAttributes(Type type)
        {
            int count = 0;
            TypeAttributes Attributes = type.Attributes;

#pragma warning disable SYSLIB0050 // TypeAttributes.Serializable is obsolete
            /* IsSerializable returns true for delegates/enums as well */
            if ((Attributes & TypeAttributes.Serializable) != 0)
                count++;
#pragma warning restore SYSLIB0050
            if ((Attributes & TypeAttributes.Import) != 0)
                count++;

            if (count == 0)
                return null;
            object[] attrs = new object[count];
            count = 0;

#pragma warning disable SYSLIB0050 // TypeAttributes.Serializable is obsolete
            if ((Attributes & TypeAttributes.Serializable) != 0)
                attrs[count++] = new SerializableAttribute();
#pragma warning restore SYSLIB0050
            if ((Attributes & TypeAttributes.Import) != 0)
                attrs[count++] = new ComImportAttribute();

            return attrs;
        }

        // FIXME: Callers are explicitly passing in null for attributeType, but GetCustomAttributes prohibits null attributeType arguments
        internal static object[] GetCustomAttributesBase(ICustomAttributeProvider obj, Type? attributeType, bool inheritedOnly)
        {
            object[] attrs = GetCustomAttributesInternal(obj, attributeType!, pseudoAttrs: false);
            //
            // All pseudo custom attributes are Inherited = false hence we can avoid
            // building attributes array which would be discarded by inherited checks
            //
            if (!inheritedOnly)
            {
                object[]? pseudoAttrs = GetPseudoCustomAttributes(obj, attributeType!);
                if (pseudoAttrs != null)
                {
                    object[] res = new Attribute[attrs.Length + pseudoAttrs.Length];
                    Array.Copy(attrs, res, attrs.Length);
                    Array.Copy(pseudoAttrs, 0, res, attrs.Length, pseudoAttrs.Length);
                    return res;
                }
            }

            return attrs;
        }

        private static bool AttrTypeMatches(Type? attributeType, Type attrType)
        {
            if (attributeType == null)
                return true;
            return attributeType.IsAssignableFrom(attrType) ||
                (attributeType.IsGenericTypeDefinition && attrType.IsGenericType && attributeType.IsAssignableFrom(attrType.GetGenericTypeDefinition()));
        }

        internal static object[] GetCustomAttributes(ICustomAttributeProvider obj, Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(attributeType);
            if (!attributeType.IsSubclassOf(typeof(Attribute)) && !attributeType.IsInterface
                && attributeType != typeof(Attribute) && attributeType != typeof(CustomAttribute) && attributeType != typeof(object))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass + " " + attributeType.FullName);

            if (IsUserCattrProvider(obj))
                return obj.GetCustomAttributes(attributeType, inherit);

            // FIXME: GetCustomAttributesBase doesn't like being passed a null attributeType
            if (attributeType == typeof(CustomAttribute))
                attributeType = null!;
            if (attributeType == typeof(Attribute))
                attributeType = null!;
            if (attributeType == typeof(object))
                attributeType = null!;

            object[] r;
            object[] res = GetCustomAttributesBase(obj, attributeType!, false);
            // shortcut
            if (!inherit && res.Length == 1)
            {
                if (res[0] == null)
                    throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);

                if (attributeType != null && !attributeType.IsGenericTypeDefinition)
                {
                    if (attributeType.IsAssignableFrom(res[0].GetType()))
                    {
                        r = (object[])Array.CreateInstance(attributeType, 1);
                        r[0] = res[0];
                    }
                    else
                    {
                        r = (object[])Array.CreateInstance(attributeType, 0);
                    }
                }
                else
                {
                    r = (object[])Array.CreateInstance(res[0].GetType(), 1);
                    r[0] = res[0];
                }
                return r;
            }

            if (inherit && GetBase(obj) == null)
                inherit = false;

            // if AttributeType is sealed, and Inherited is set to false, then
            // there's no use in scanning base types
            if ((attributeType != null && attributeType.IsSealed) && inherit)
            {
                AttributeUsageAttribute usageAttribute = RetrieveAttributeUsage(
                    attributeType);
                if (!usageAttribute.Inherited)
                    inherit = false;
            }

            int initialSize = Math.Max(res.Length, 16);
            List<object>? a;
            ICustomAttributeProvider? btype = obj;
            object[] array;

            /* Non-inherit case */
            if (!inherit)
            {
                if (attributeType == null)
                {
                    foreach (object attr in res)
                    {
                        if (attr == null)
                            throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);
                    }
                    var result = new Attribute[res.Length];
                    res.CopyTo(result, 0);
                    return result;
                }

                a = new List<object>(initialSize);
                foreach (object attr in res)
                {
                    if (attr == null)
                        throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);

                    if (AttrTypeMatches(attributeType, attr.GetType()))
                        a.Add(attr);
                }

                if (attributeType == null || attributeType.IsValueType || attributeType.IsGenericTypeDefinition)
                    array = new Attribute[a.Count];
                else
                    array = (Array.CreateInstance(attributeType, a.Count) as object[])!;
                a.CopyTo(array, 0);
                return array;
            }

            /* Inherit case */
            var attributeInfos = new Dictionary<Type, AttributeInfo>(initialSize);
            int inheritanceLevel = 0;
            a = new List<object>(initialSize);

            do
            {
                foreach (object attr in res)
                {
                    AttributeUsageAttribute usage;
                    if (attr == null)
                        throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);

                    Type attrType = attr.GetType();
                    if (!AttrTypeMatches(attributeType, attr.GetType()))
                        continue;

                    AttributeInfo? firstAttribute;
                    if (attributeInfos.TryGetValue(attrType, out firstAttribute))
                        usage = firstAttribute.Usage;
                    else
                        usage = RetrieveAttributeUsage(attrType);

                    // only add attribute to the list of attributes if
                    // - we are on the first inheritance level, or the attribute can be inherited anyway
                    // and (
                    // - multiple attributes of the type are allowed
                    // or (
                    // - this is the first attribute we've discovered
                    // or
                    // - the attribute is on same inheritance level than the first
                    //   attribute that was discovered for this attribute type ))
                    if ((inheritanceLevel == 0 || usage.Inherited) && (usage.AllowMultiple ||
                        (firstAttribute == null || (firstAttribute != null
                            && firstAttribute.InheritanceLevel == inheritanceLevel))))
                        a.Add(attr);

                    if (firstAttribute == null)
                        attributeInfos.Add(attrType, new AttributeInfo(usage, inheritanceLevel));
                }

                if ((btype = GetBase(btype)) != null)
                {
                    inheritanceLevel++;
                    res = GetCustomAttributesBase(btype, attributeType, true);
                }
            } while (btype != null);

            if (attributeType == null || attributeType.IsValueType || attributeType.IsGenericTypeDefinition)
                array = new Attribute[a.Count];
            else
                array = (Array.CreateInstance(attributeType, a.Count) as object[])!;

            // copy attributes to array
            a.CopyTo(array, 0);

            return array;
        }

        internal static object[] GetCustomAttributes(ICustomAttributeProvider obj, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (IsUserCattrProvider(obj))
                return obj.GetCustomAttributes(typeof(Attribute), inherit);

            if (!inherit)
                return (object[])GetCustomAttributesBase(obj, null, false).Clone();

            return GetCustomAttributes(obj, typeof(CustomAttribute), inherit);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [DynamicDependency("#ctor(System.Reflection.ConstructorInfo,System.Reflection.Assembly,System.IntPtr,System.UInt32)", typeof(RuntimeCustomAttributeData))]
        [DynamicDependency("#ctor(System.Reflection.MemberInfo,System.Object)", typeof(CustomAttributeNamedArgument))]
        [DynamicDependency("#ctor(System.Type,System.Object)", typeof(CustomAttributeTypedArgument))]
        private static extern CustomAttributeData[] GetCustomAttributesDataInternal(ICustomAttributeProvider obj);

        internal static IList<CustomAttributeData> GetCustomAttributesData(ICustomAttributeProvider obj, bool inherit = false)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (!inherit)
                return GetCustomAttributesDataBase(obj, null, false);

            return GetCustomAttributesData(obj, typeof(CustomAttribute), inherit);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesData(ICustomAttributeProvider obj, Type? attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType == typeof(CustomAttribute))
                attributeType = null;

            IList<CustomAttributeData> r;
            IList<CustomAttributeData> res = GetCustomAttributesDataBase(obj, attributeType, false);
            // shortcut
            if (!inherit && res.Count == 1)
            {
                if (res[0] == null)
                    throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);
                if (attributeType != null)
                {
                    if (attributeType.IsAssignableFrom(res[0].AttributeType))
                        r = new CustomAttributeData[] { res[0] };
                    else
                        r = Array.Empty<CustomAttributeData>();
                }
                else
                {
                    r = new CustomAttributeData[] { res[0] };
                }

                return r;
            }

            if (inherit && GetBase(obj) == null)
                inherit = false;

            // if AttributeType is sealed, and Inherited is set to false, then
            // there's no use in scanning base types
            if ((attributeType != null && attributeType.IsSealed) && inherit)
            {
                AttributeUsageAttribute? usageAttribute = RetrieveAttributeUsage(attributeType);
                if (!usageAttribute.Inherited)
                    inherit = false;
            }

            int initialSize = Math.Max(res.Count, 16);
            List<CustomAttributeData>? a;
            ICustomAttributeProvider? btype = obj;

            /* Non-inherit case */
            if (!inherit)
            {
                if (attributeType == null)
                {
                    foreach (CustomAttributeData attrData in res)
                    {
                        if (attrData == null)
                            throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);
                    }

                    var result = new CustomAttributeData[res.Count];
                    res.CopyTo(result, 0);
                    return result;
                }
                else
                {
                    a = new List<CustomAttributeData>(initialSize);
                    foreach (CustomAttributeData attrData in res)
                    {
                        if (attrData == null)
                            throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);
                        if (!attributeType.IsAssignableFrom(attrData.AttributeType))
                            continue;
                        a.Add(attrData);
                    }

                    return a.ToArray();
                }
            }

            /* Inherit case */
            var attributeInfos = new Dictionary<Type, AttributeInfo>(initialSize);
            int inheritanceLevel = 0;
            a = new List<CustomAttributeData>(initialSize);

            do
            {
                foreach (CustomAttributeData attrData in res)
                {
                    AttributeUsageAttribute usage;
                    if (attrData == null)
                        throw new CustomAttributeFormatException(SR.Arg_CustomAttributeFormatException);

                    Type attrType = attrData.AttributeType;
                    if (attributeType != null)
                    {
                        if (!attributeType.IsAssignableFrom(attrType))
                            continue;
                    }

                    AttributeInfo? firstAttribute;
                    if (attributeInfos.TryGetValue(attrType, out firstAttribute))
                        usage = firstAttribute.Usage;
                    else
                        usage = RetrieveAttributeUsage(attrType);

                    // The same as for CustomAttributes.
                    //
                    // Only add attribute to the list of attributes if
                    // - we are on the first inheritance level, or the attribute can be inherited anyway
                    // and (
                    // - multiple attributes of the type are allowed
                    // or (
                    // - this is the first attribute we've discovered
                    // or
                    // - the attribute is on same inheritance level than the first
                    //   attribute that was discovered for this attribute type ))
                    if ((inheritanceLevel == 0 || usage.Inherited) && (usage.AllowMultiple ||
                        (firstAttribute == null || (firstAttribute != null
                            && firstAttribute.InheritanceLevel == inheritanceLevel))))
                        a.Add(attrData);

                    if (firstAttribute == null)
                        attributeInfos.Add(attrType, new AttributeInfo(usage, inheritanceLevel));
                }

                if ((btype = GetBase(btype)) != null)
                {
                    inheritanceLevel++;
                    res = GetCustomAttributesDataBase(btype, attributeType, true);
                }
            } while (btype != null);

            return a.ToArray();
        }

        internal static IList<CustomAttributeData> GetCustomAttributesDataBase(ICustomAttributeProvider obj, Type? attributeType, bool inheritedOnly)
        {
            CustomAttributeData[] attrsData;
            if (IsUserCattrProvider(obj))
            {
                //FIXME resolve this case if it makes sense. Assign empty array for now.
                //attrsData = obj.GetCustomAttributesData(attributeType, true);
                attrsData = Array.Empty<CustomAttributeData>();
            }
            else
                attrsData = GetCustomAttributesDataInternal(obj);

            //
            // All pseudo custom attributes are Inherited = false hence we can avoid
            // building attributes data array which would be discarded by inherited checks
            //
            if (!inheritedOnly)
            {
                CustomAttributeData[]? pseudoAttrsData = GetPseudoCustomAttributesData(obj, attributeType);
                if (pseudoAttrsData != null)
                {
                    if (attrsData.Length == 0)
                        return Array.AsReadOnly(pseudoAttrsData);
                    CustomAttributeData[] res = new CustomAttributeData[attrsData.Length + pseudoAttrsData.Length];
                    Array.Copy(attrsData, res, attrsData.Length);
                    Array.Copy(pseudoAttrsData, 0, res, attrsData.Length, pseudoAttrsData.Length);
                    return Array.AsReadOnly(res);
                }
            }

            return Array.AsReadOnly(attrsData);
        }

        internal static CustomAttributeData[]? GetPseudoCustomAttributesData(ICustomAttributeProvider obj, Type? attributeType)
        {
            CustomAttributeData[]? pseudoAttrsData = null;

            /* FIXME: Add other types */
            if (obj is RuntimeMethodInfo monoMethod)
                pseudoAttrsData = monoMethod.GetPseudoCustomAttributesData();
            else if (obj is RuntimeFieldInfo fieldInfo)
                pseudoAttrsData = fieldInfo.GetPseudoCustomAttributesData();
            else if (obj is RuntimeParameterInfo monoParamInfo)
                pseudoAttrsData = monoParamInfo.GetPseudoCustomAttributesData();
            else if (obj is Type t)
                pseudoAttrsData = GetPseudoCustomAttributesData(t);

            if ((attributeType != null) && (pseudoAttrsData != null))
            {
                for (int i = 0; i < pseudoAttrsData.Length; ++i)
                {
                    if (attributeType.IsAssignableFrom(pseudoAttrsData[i].AttributeType))
                    {
                        if (pseudoAttrsData.Length == 1)
                            return pseudoAttrsData;
                        else
                            return new CustomAttributeData[] { pseudoAttrsData[i] };
                    }
                }

                return Array.Empty<CustomAttributeData>();
            }

            return pseudoAttrsData;
        }

        private static CustomAttributeData[]? GetPseudoCustomAttributesData(Type type)
        {
            int count = 0;
            TypeAttributes Attributes = type.Attributes;

#pragma warning disable SYSLIB0050 // TypeAttributes.Serializable is obsolete
            /* IsSerializable returns true for delegates/enums as well */
            if ((Attributes & TypeAttributes.Serializable) != 0)
                count++;
#pragma warning restore SYSLIB0050
            if ((Attributes & TypeAttributes.Import) != 0)
                count++;

            if (count == 0)
                return null;
            CustomAttributeData[] attrsData = new CustomAttributeData[count];
            count = 0;

#pragma warning disable SYSLIB0050 // TypeAttributes.Serializable is obsolete
            if ((Attributes & TypeAttributes.Serializable) != 0)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(SerializableAttribute)).GetConstructor(Type.EmptyTypes)!);
#pragma warning restore SYSLIB0050
            if ((Attributes & TypeAttributes.Import) != 0)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(ComImportAttribute)).GetConstructor(Type.EmptyTypes)!);

            return attrsData;
        }

        internal static bool IsDefined(ICustomAttributeProvider obj, Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            if (!attributeType.IsSubclassOf(typeof(Attribute)) && !attributeType.IsInterface && attributeType != typeof(Attribute))
                throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass + " " + attributeType.FullName);

            AttributeUsageAttribute? usage = null;
            do
            {
                if (IsUserCattrProvider(obj))
                    return obj.IsDefined(attributeType, inherit);

                if (IsDefinedInternal(obj, attributeType))
                    return true;

                object[]? pseudoAttrs = GetPseudoCustomAttributes(obj, attributeType);
                if (pseudoAttrs != null)
                {
                    for (int i = 0; i < pseudoAttrs.Length; ++i)
                        if (attributeType.IsAssignableFrom(pseudoAttrs[i].GetType()))
                            return true;
                }

                if (usage == null)
                {
                    if (!inherit)
                        return false;

                    usage = RetrieveAttributeUsage(attributeType);
                    if (!usage.Inherited)
                        return false;
                }

                obj = GetBase(obj)!;
            } while (obj != null);

            return false;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsDefinedInternal(ICustomAttributeProvider obj, Type AttributeType);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Linker analyzes base properties and marks them")]
        private static PropertyInfo? GetBasePropertyDefinition(RuntimePropertyInfo property)
        {
            MethodInfo? method = property.GetGetMethod(true);
            if (method == null || !method.IsVirtual)
                method = property.GetSetMethod(true);
            if (method == null || !method.IsVirtual)
                return null;

            MethodInfo baseMethod = ((RuntimeMethodInfo)method).GetBaseMethod();
            if (baseMethod != null && baseMethod != method)
            {
                ParameterInfo[] parameters = property.GetIndexParameters();
                if (parameters != null && parameters.Length > 0)
                {
                    Type[] paramTypes = new Type[parameters.Length];
                    for (int i = 0; i < paramTypes.Length; i++)
                        paramTypes[i] = parameters[i].ParameterType;
                    return baseMethod.DeclaringType!.GetProperty(property.Name, property.PropertyType,
                                             paramTypes);
                }
                else
                {
                    return baseMethod.DeclaringType!.GetProperty(property.Name, property.PropertyType);
                }
            }
            return null;

        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Linker analyzes base events and marks them")]
        private static EventInfo? GetBaseEventDefinition(RuntimeEventInfo evt)
        {
            MethodInfo? method = evt.GetAddMethod(true);
            if (method == null || !method.IsVirtual)
                method = evt.GetRaiseMethod(true);
            if (method == null || !method.IsVirtual)
                method = evt.GetRemoveMethod(true);
            if (method == null || !method.IsVirtual)
                return null;

            MethodInfo baseMethod = ((RuntimeMethodInfo)method).GetBaseMethod();
            if (baseMethod != null && baseMethod != method)
            {
                BindingFlags flags = method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
                flags |= method.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

                return baseMethod.DeclaringType!.GetEvent(evt.Name, flags);
            }
            return null;
        }

        // Handles Type, RuntimePropertyInfo and RuntimeMethodInfo.
        // The runtime has also cases for RuntimeEventInfo, RuntimeFieldInfo, Assembly and ParameterInfo,
        // but for those we return null here.
        private static ICustomAttributeProvider? GetBase(ICustomAttributeProvider obj)
        {
            if (obj == null)
                return null;

            if (obj is Type)
                return ((Type)obj).BaseType;

            MethodInfo? method = null;
            if (obj is RuntimePropertyInfo)
                return GetBasePropertyDefinition((RuntimePropertyInfo)obj);
            else if (obj is RuntimeEventInfo)
                return GetBaseEventDefinition((RuntimeEventInfo)obj);
            else if (obj is RuntimeMethodInfo)
                method = (MethodInfo)obj;
            if (obj is RuntimeParameterInfo parinfo)
            {
                MemberInfo? member = parinfo.Member;
                if (member is MethodInfo)
                {
                    method = (MethodInfo)member;
                    MethodInfo bmethod = ((RuntimeMethodInfo)method).GetBaseMethod();
                    if (bmethod == method)
                        return null;
                    int position = parinfo.Position;
                    if (position == -1)
                        return bmethod.ReturnParameter;
                    return bmethod.GetParametersAsSpan()[position];
                }
            }
            /*
             * ParameterInfo -> null
             * Assembly -> null
             * RuntimeEventInfo -> null
             * RuntimeFieldInfo -> null
             */
            if (method == null || !method.IsVirtual)
                return null;

            MethodInfo baseMethod = ((RuntimeMethodInfo)method).GetBaseMethod();
            if (baseMethod == method)
                return null;

            return baseMethod;
        }

        private static AttributeUsageAttribute RetrieveAttributeUsageNoCache(Type attributeType)
        {
            if (attributeType == typeof(AttributeUsageAttribute))
                /* Avoid endless recursion */
                return new AttributeUsageAttribute(AttributeTargets.Class);

            AttributeUsageAttribute? usageAttribute = null;
            object[] attribs = GetCustomAttributes(attributeType, typeof(AttributeUsageAttribute), false);
            if (attribs.Length == 0)
            {
                // if no AttributeUsage was defined on the attribute level, then
                // try to retrieve if from its base type
                if (attributeType.BaseType != null)
                {
                    usageAttribute = RetrieveAttributeUsage(attributeType.BaseType);

                }
                if (usageAttribute != null)
                {
                    // return AttributeUsage of base class
                    return usageAttribute;

                }
                // return default AttributeUsageAttribute if no AttributeUsage
                // was defined on attribute, or its base class
                return DefaultAttributeUsage;
            }
            // check if more than one AttributeUsageAttribute has been specified
            // on the type
            // NOTE: compilers should prevent this, but that doesn't prevent
            // anyone from using IL ofcourse
            if (attribs.Length > 1)
            {
                throw new FormatException(SR.Format(SR.Format_AttributeUsage, attributeType.GetType().FullName));
            }

            return ((AttributeUsageAttribute)attribs[0]);
        }

        private static AttributeUsageAttribute RetrieveAttributeUsage(Type attributeType)
        {
            AttributeUsageAttribute? usageAttribute;
            /* Usage a thread-local cache to speed this up, since it is called a lot from GetCustomAttributes () */
            usage_cache ??= new Dictionary<Type, AttributeUsageAttribute>();
            if (usage_cache.TryGetValue(attributeType, out usageAttribute))
                return usageAttribute;
            usageAttribute = RetrieveAttributeUsageNoCache(attributeType);
            usage_cache[attributeType] = usageAttribute;
            return usageAttribute;
        }

        internal static object[] CreateAttributeArrayHelper(RuntimeType caType, int elementCount)
        {
            bool useAttributeArray = false;
            bool useObjectArray = false;

            if (caType == typeof(Attribute))
            {
                useAttributeArray = true;
            }
            else if (caType.IsValueType)
            {
                useObjectArray = true;
            }
            else if (caType.ContainsGenericParameters)
            {
                if (caType.IsSubclassOf(typeof(Attribute)))
                {
                    useAttributeArray = true;
                }
                else
                {
                    useObjectArray = true;
                }
            }

            if (useAttributeArray)
            {
                return elementCount == 0 ? Array.Empty<Attribute>() : new Attribute[elementCount];
            }
            if (useObjectArray)
            {
                return elementCount == 0 ? Array.Empty<object>() : new object[elementCount];
            }
            return /*elementCount == 0 ? caType.GetEmptyArray() :*/ (object[])Array.CreateInstance(caType, elementCount);
        }

        private static readonly AttributeUsageAttribute DefaultAttributeUsage =
            new AttributeUsageAttribute(AttributeTargets.All);

        private sealed class AttributeInfo
        {
            private readonly AttributeUsageAttribute _usage;
            private readonly int _inheritanceLevel;

            public AttributeInfo(AttributeUsageAttribute usage, int inheritanceLevel)
            {
                _usage = usage;
                _inheritanceLevel = inheritanceLevel;
            }

            public AttributeUsageAttribute Usage
            {
                get
                {
                    return _usage;
                }
            }

            public int InheritanceLevel
            {
                get
                {
                    return _inheritanceLevel;
                }
            }
        }
    }
}
