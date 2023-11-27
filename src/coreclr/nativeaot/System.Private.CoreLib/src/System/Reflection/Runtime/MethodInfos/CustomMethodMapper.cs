// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal static partial class CustomMethodMapper
    {
        //
        // Certain types and methods are edge-cases that require special handling.
        //
        public static MethodBaseInvoker GetCustomMethodInvokerIfNeeded(this MethodBase methodBase)
        {
            Type declaringType = methodBase.DeclaringType!;
            bool isNullable = declaringType.IsConstructedGenericType && declaringType.GetGenericTypeDefinition() == typeof(Nullable<>);

            Dictionary<MethodBase, CustomMethodInvokerAction> map;
            if (isNullable)
                map = NullableActions.Map;
            else if (declaringType == typeof(string))
                map = StringActions.Map;
            else
                return null;

            if (!map.TryGetValue(methodBase.MetadataDefinitionMethod, out CustomMethodInvokerAction? action))
                return null;

            ReadOnlySpan<ParameterInfo> parameterInfos = methodBase.GetParametersAsSpan();
            Type[] parameterTypes = new Type[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            InvokerOptions options = (methodBase.IsStatic || methodBase is ConstructorInfo || isNullable) ? InvokerOptions.AllowNullThis : InvokerOptions.None;
            return new CustomMethodInvoker(declaringType, parameterTypes, options, action);
        }

        private static void AddConstructor(this Dictionary<MethodBase, CustomMethodInvokerAction> map, Type declaringType, Type[] parameterTypes, CustomMethodInvokerAction action)
        {
            map.AddMethod(declaringType, ConstructorInfo.ConstructorName, parameterTypes, action);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "GetConstructor/GetMethod being null is handled and expected.")]
        private static void AddMethod(this Dictionary<MethodBase, CustomMethodInvokerAction> map, Type declaringType, string name, Type[] parameterTypes, CustomMethodInvokerAction action)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;

            MethodBase? methodBase;
            if (name == ConstructorInfo.ConstructorName)
            {
                methodBase = declaringType.GetConstructor(bf, null, parameterTypes, null);
            }
            else
            {
                methodBase = declaringType.GetMethod(name, 0, bf, parameterTypes);
            }

            if (methodBase == null)
                return; // If we got here, this specific member was not included in the metadata.

            Debug.Assert(methodBase == methodBase.MetadataDefinitionMethod);
            map.Add(methodBase, action);
        }
    }
}
