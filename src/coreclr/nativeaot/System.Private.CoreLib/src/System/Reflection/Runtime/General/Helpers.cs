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

        public static string LastResortString(this RuntimeTypeHandle typeHandle)
        {
            return ReflectionCoreExecution.ExecutionEnvironment.GetLastResortString(typeHandle);
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
            return new ReadOnlyCollection<T>(enumeration.ToArray());
        }

        public static MethodInfo FilterAccessor(this MethodInfo accessor, bool nonPublic)
        {
            if (nonPublic)
                return accessor;
            if (accessor.IsPublic)
                return accessor;
            return null;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Calling Assembly.GetType on a third-party Assembly class.")]
        public static Type GetTypeCore(this Assembly assembly, string name, bool ignoreCase)
        {
            if (assembly is RuntimeAssemblyInfo runtimeAssembly)
            {
                // Not a recursion - this one goes to the actual instance method on RuntimeAssembly.
                return runtimeAssembly.GetTypeCore(name, ignoreCase: ignoreCase);
            }
            else
            {
                // This is a third-party Assembly object. We can emulate GetTypeCore() by calling the public GetType()
                // method. This is wasteful because it'll probably reparse a type string that we've already parsed
                // but it can't be helped.
                string escapedName = name.EscapeTypeNameIdentifier();
                return assembly.GetType(escapedName, throwOnError: false, ignoreCase: ignoreCase);
            }
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

        public static bool GetCustomAttributeDefaultValueIfAny(IEnumerable<CustomAttributeData> customAttributes, bool raw, out object? defaultValue)
        {
            // Legacy: If there are multiple default value attribute, the desktop picks one at random (and so do we...)
            foreach (CustomAttributeData cad in customAttributes)
            {
                Type attributeType = cad.AttributeType;
                if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    if (raw)
                    {
                        foreach (CustomAttributeNamedArgument namedArgument in cad.NamedArguments)
                        {
                            if (namedArgument.MemberName.Equals("Value"))
                            {
                                defaultValue = namedArgument.TypedValue.Value;
                                return true;
                            }
                        }
                        defaultValue = null;
                        return false;
                    }
                    else
                    {
                        CustomConstantAttribute customConstantAttribute = (CustomConstantAttribute)(cad.Instantiate());
                        defaultValue = customConstantAttribute.Value;
                        return true;
                    }
                }
                if (attributeType.Equals(typeof(DecimalConstantAttribute)))
                {
                    // We should really do a non-instanting check if "raw == false" but given that we don't support
                    // reflection-only loads, there isn't an observable difference.
                    DecimalConstantAttribute decimalConstantAttribute = (DecimalConstantAttribute)(cad.Instantiate());
                    defaultValue = decimalConstantAttribute.Value;
                    return true;
                }
            }

            defaultValue = null;
            return false;
        }
    }
}
