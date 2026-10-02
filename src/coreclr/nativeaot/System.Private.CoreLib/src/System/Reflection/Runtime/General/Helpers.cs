// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.LowLevelLinq;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;
using Internal.Runtime.Augments;

namespace System.Reflection.Runtime.General
{
    internal static partial class Helpers
    {
        // This helper helps reduce the temptation to write "h == default(RuntimeTypeHandle)" which causes boxing.
        public static bool IsNull(this RuntimeTypeHandle h)
        {
            return h.Equals(default(RuntimeTypeHandle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type[] GetGenericTypeParameters(this Type type)
        {
            Debug.Assert(type.IsGenericTypeDefinition);
            return type.GetGenericArguments();
        }

        public static Type[] ToTypeArray(this RuntimeTypeInfo[] typeInfos)
        {
            int count = typeInfos.Length;
            if (count == 0)
                return Array.Empty<Type>();

            Type[] types = new Type[count];
            for (int i = 0; i < count; i++)
            {
                types[i] = typeInfos[i].ToType();
            }
            return types;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RuntimeTypeInfo ToRuntimeTypeInfo(this Type type)
        {
            if (type is RuntimeType runtimeType)
            {
                return runtimeType.GetRuntimeTypeInfo();
            }
            Debug.Assert(false);
            return null;
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

        public static TypeLoadException CreateTypeLoadException(string typeName, string assemblyName)
        {
            string message = SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, typeName, assemblyName);
            return new TypeLoadException(message, typeName);
        }

        // Escape identifiers as described in "Specifying Fully Qualified Type Names" on msdn.
        // Current link is http://msdn.microsoft.com/en-us/library/yfsftwz6(v=vs.110).aspx
        public static string EscapeTypeNameIdentifier(this string identifier)
        {
            // Some characters in a type name need to be escaped

            // We're avoiding calling into MemoryExtensions here as it has paths that lead to reflection,
            // and that would lead to an infinite loop given that this is the implementation of reflection.
#pragma warning disable CA1870 // Use a cached 'SearchValues' instance
            if (identifier != null && identifier.IndexOfAny(s_charsToEscape) != -1)
#pragma warning restore CA1870
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Delegates always generate metadata for the Invoke method")]
        public static RuntimeMethodInfo GetInvokeMethod(this RuntimeTypeInfo delegateType)
        {
            Debug.Assert(delegateType.IsDelegate);

            MethodInfo? invokeMethod = delegateType.ToType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (invokeMethod == null)
            {
                // No Invoke method found. Since delegate types are compiler constructed, the most likely cause is missing metadata rather than
                // a missing Invoke method.
                throw ReflectionCoreExecution.ExecutionEnvironment.CreateMissingMetadataException(delegateType.ToType());
            }
            return (RuntimeMethodInfo)invokeMethod;
        }

        public static BinderBundle ToBinderBundle(this Binder binder, BindingFlags invokeAttr, CultureInfo cultureInfo)
        {
            if (binder == null || binder is DefaultBinder || ((invokeAttr & BindingFlags.ExactBinding) != 0))
                return null;
            return new BinderBundle(binder, cultureInfo);
        }

    }
}
