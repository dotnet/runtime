// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.MethodInfos;

using Internal.LowLevelLinq;
using Internal.Runtime.Augments;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;
using Internal.Reflection.Extensions.NonPortable;

namespace System.Reflection.Runtime.General
{
    internal static partial class Helpers
    {
        // This helper helps reduce the temptation to write "h == default(RuntimeTypeHandle)" which causes boxing.
        public static bool IsNull(this RuntimeTypeHandle h)
        {
            return h.Equals(default(RuntimeTypeHandle));
        }

        // Clones a Type[] array for the purpose of returning it from an api.
        public static Type[] CloneTypeArray(this Type[] types)
        {
            int count = types.Length;
            if (count == 0)
                return Array.Empty<Type>();  // Ok not to clone empty arrays - those are immutable.

            Type[] clonedTypes = new Type[count];
            for (int i = 0; i < count; i++)
            {
                clonedTypes[i] = types[i];
            }
            return clonedTypes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type[] GetGenericTypeParameters(this Type type)
        {
            Debug.Assert(type.IsGenericTypeDefinition);
            return type.GetGenericArguments();
        }

        public static RuntimeTypeInfo[] ToRuntimeTypeInfoArray(this Type[] types)
        {
            int count = types.Length;
            RuntimeTypeInfo[] typeInfos = new RuntimeTypeInfo[count];
            for (int i = 0; i < count; i++)
            {
                typeInfos[i] = types[i].CastToRuntimeTypeInfo();
            }
            return typeInfos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RuntimeNamedTypeInfo CastToRuntimeNamedTypeInfo(this Type type)
        {
            Debug.Assert(type is RuntimeNamedTypeInfo);
            return (RuntimeNamedTypeInfo)type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RuntimeTypeInfo CastToRuntimeTypeInfo(this Type type)
        {
            Debug.Assert(type == null || type is RuntimeTypeInfo);
            return (RuntimeTypeInfo)type!;
        }

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumeration)
        {
            return Array.AsReadOnly(enumeration.ToArray());
        }

        public static MethodInfo FilterAccessor(this MethodInfo accessor, bool nonPublic)
        {
            if (nonPublic)
                return accessor;
            if (accessor.IsPublic)
                return accessor;
            return null;
        }

        public static TypeLoadException CreateTypeLoadException(string typeName, Assembly assemblyIfAny)
        {
            if (assemblyIfAny == null)
                throw new TypeLoadException(SR.Format(SR.TypeLoad_TypeNotFound, typeName));
            else
                throw Helpers.CreateTypeLoadException(typeName, assemblyIfAny.FullName);
        }

        public static TypeLoadException CreateTypeLoadException(string typeName, string assemblyName)
        {
            string message = SR.Format(SR.TypeLoad_TypeNotFoundInAssembly, typeName, assemblyName);
            return ReflectionAugments.CreateTypeLoadException(message, typeName);
        }

        // Escape identifiers as described in "Specifying Fully Qualified Type Names" on msdn.
        // Current link is http://msdn.microsoft.com/en-us/library/yfsftwz6(v=vs.110).aspx
        public static string EscapeTypeNameIdentifier(this string identifier)
        {
            // Some characters in a type name need to be escaped
            if (identifier != null && identifier.IndexOfAny(s_charsToEscape) != -1)
            {
                StringBuilder sbEscapedName = new StringBuilder(identifier.Length);
                foreach (char c in identifier)
                {
                    if (c.NeedsEscapingInTypeName())
                        sbEscapedName.Append('\\');

                    sbEscapedName.Append(c);
                }
                identifier = sbEscapedName.ToString();
            }
            return identifier;
        }

        public static bool NeedsEscapingInTypeName(this char c)
        {
            return Array.IndexOf(s_charsToEscape, c) >= 0;
        }

        private static readonly char[] s_charsToEscape = new char[] { '\\', '[', ']', '+', '*', '&', ',' };

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Delegates always generate metadata for the Invoke method")]
        public static RuntimeMethodInfo GetInvokeMethod(this RuntimeTypeInfo delegateType)
        {
            Debug.Assert(delegateType.IsDelegate);

            MethodInfo? invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (invokeMethod == null)
            {
                // No Invoke method found. Since delegate types are compiler constructed, the most likely cause is missing metadata rather than
                // a missing Invoke method.
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(delegateType);
            }
            return (RuntimeMethodInfo)invokeMethod;
        }

        public static BinderBundle ToBinderBundle(this Binder binder, BindingFlags invokeAttr, CultureInfo cultureInfo)
        {
            if (binder == null || binder is DefaultBinder || ((invokeAttr & BindingFlags.ExactBinding) != 0))
                return null;
            return new BinderBundle(binder, cultureInfo);
        }

        // Helper for ICustomAttributeProvider.GetCustomAttributes(). The result of this helper is returned directly to apps
        // so it must always return a newly allocated array. Unlike most of the newer custom attribute apis, the attribute type
        // need not derive from System.Attribute. (In particular, it can be an interface or System.Object.)
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Array.CreateInstance is only used with reference types here and is therefore safe.")]
        public static object[] InstantiateAsArray(this IEnumerable<CustomAttributeData> cads, Type actualElementType)
        {
            LowLevelList<object> attributes = new LowLevelList<object>();
            foreach (CustomAttributeData cad in cads)
            {
                object instantiatedAttribute = cad.Instantiate();
                attributes.Add(instantiatedAttribute);
            }

            // This is here for desktop compatibility. ICustomAttribute.GetCustomAttributes() normally returns an array of the
            // exact attribute type requested except in two cases: when the passed in type is an open type and when
            // it is a value type. In these two cases, it returns an array of type Object[].
            bool useObjectArray = actualElementType.ContainsGenericParameters || actualElementType.IsValueType;
            int count = attributes.Count;
            object[] result = useObjectArray ? new object[count] : (object[])Array.CreateInstance(actualElementType, count);

            attributes.CopyTo(result, 0);
            return result;
        }

        private static object? GetRawDefaultValue(IEnumerable<CustomAttributeData> customAttributes)
        {
            foreach (CustomAttributeData attributeData in customAttributes)
            {
                Type attributeType = attributeData.AttributeType;
                if (attributeType == typeof(DecimalConstantAttribute))
                {
                    return GetRawDecimalConstant(attributeData);
                }
                else if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    if (attributeType == typeof(DateTimeConstantAttribute))
                    {
                        return GetRawDateTimeConstant(attributeData);
                    }
                    return GetRawConstant(attributeData);
                }
            }
            return DBNull.Value;
        }

        private static decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
            System.Collections.Generic.IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;

            return new decimal(
                lo: GetConstructorArgument(args, 4),
                mid: GetConstructorArgument(args, 3),
                hi: GetConstructorArgument(args, 2),
                isNegative: ((byte)args[1].Value!) != 0,
                scale: (byte)args[0].Value!);

            static int GetConstructorArgument(IList<CustomAttributeTypedArgument> args, int index)
            {
                // The constructor is overloaded to accept both signed and unsigned arguments
                object obj = args[index].Value!;
                return (obj is int value) ? value : (int)(uint)obj;
            }
        }

        private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
            return new DateTime((long)attr.ConstructorArguments[0].Value!);
        }

        // We are relying only on named arguments for historical reasons
        private static object? GetRawConstant(CustomAttributeData attr)
        {
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }
            return DBNull.Value;
        }

        private static object? GetDefaultValue(IEnumerable<CustomAttributeData> customAttributes)
        {
            // we first look for a CustomConstantAttribute, but we will save the first occurrence of DecimalConstantAttribute
            // so we don't go through all custom attributes again
            CustomAttributeData? firstDecimalConstantAttributeData = null;
            foreach (CustomAttributeData attributeData in customAttributes)
            {
                Type attributeType = attributeData.AttributeType;
                if (firstDecimalConstantAttributeData == null && attributeType == typeof(DecimalConstantAttribute))
                {
                    firstDecimalConstantAttributeData = attributeData;
                }
                else if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    CustomConstantAttribute customConstantAttribute = (CustomConstantAttribute)(attributeData.Instantiate());
                    return customConstantAttribute.Value;
                }
            }

            if (firstDecimalConstantAttributeData != null)
            {
                DecimalConstantAttribute decimalConstantAttribute = (DecimalConstantAttribute)(firstDecimalConstantAttributeData.Instantiate());
                return decimalConstantAttribute.Value;
            }
            else
            {
                return DBNull.Value;
            }
        }

        public static bool GetCustomAttributeDefaultValueIfAny(IEnumerable<CustomAttributeData> customAttributes, bool raw, out object? defaultValue)
        {
            // The resolution of default value is done by following these rules:
            // 1. For RawDefaultValue, we pick the first custom attribute holding the constant value
            //  in the following order: DecimalConstantAttribute, DateTimeConstantAttribute, CustomConstantAttribute
            // 2. For DefaultValue, we first look for CustomConstantAttribute and pick the first occurrence.
            //  If none is found, then we repeat the same process searching for DecimalConstantAttribute.
            // IMPORTANT: Please note that there is a subtle difference in order custom attributes are inspected for
            //  RawDefaultValue and DefaultValue.
            object? resolvedValue = raw ? GetRawDefaultValue(customAttributes) : GetDefaultValue(customAttributes);
            if (resolvedValue != DBNull.Value)
            {
                defaultValue = resolvedValue;
                return true;
            }
            else
            {
                defaultValue = null;
                return false;
            }
        }
    }
}
