// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Used for data theory data sources that return enumerations of <see cref="ValueTuple"/>
    /// or <see cref="Tuple"/> instead of enumerations of <see cref="object"/>.
    /// </summary>
    [DataDiscoverer("Xunit.Sdk.MemberDataDiscoverer", "xunit.core")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class TupleMemberDataAttribute : MemberDataAttributeBase
    {
        public TupleMemberDataAttribute(string memberName, params object[] parameters)
            : base(memberName, parameters)
        {
        }

        protected override object[] ConvertDataItem(MethodInfo testMethod, object item)
        {
            if (item is null)
            {
                return null;
            }

            if (item is ITuple tuple)
            {
                Dictionary<string, object> tupleValues = GetTupleValuesDictionary(testMethod, tuple);
                ParameterInfo[] testMethodParameters = testMethod.GetParameters();

                object[] retVal = new object[testMethodParameters.Length];
                for (int i = 0; i < testMethodParameters.Length; i++)
                {
                    string parameterName = testMethodParameters[i].GetCustomAttribute<AliasAttribute>()?.ParameterName
                        ?? testMethodParameters[i].Name;

                    if (!tupleValues.TryGetValue(parameterName, out retVal[i]))
                    {
                        throw new ArgumentException(FormattableString.Invariant($"Member {MemberName} on {MemberType ?? testMethod.DeclaringType} has no element corresponding to test method parameter '{parameterName}'."));
                    }
                }
                return retVal;
            }

            throw new ArgumentException(FormattableString.Invariant($"Property {MemberName} on {MemberType ?? testMethod.DeclaringType} did not return a tuple."));
        }

        // from base implementation
        private FieldInfo GetFieldInfo(Type type)
        {
            FieldInfo fieldInfo = null;
            for (var reflectionType = type; reflectionType != null; reflectionType = reflectionType.GetTypeInfo().BaseType)
            {
                fieldInfo = reflectionType.GetRuntimeField(MemberName);
                if (fieldInfo != null)
                    break;
            }

            if (fieldInfo == null || !fieldInfo.IsStatic)
                return null;

            return fieldInfo;
        }

        // from base implementation
        private MethodInfo GetMethodInfo(Type type)
        {
            MethodInfo methodInfo = null;
            var parameterTypes = Parameters == null ? new Type[0] : Parameters.Select(p => p?.GetType()).ToArray();
            for (var reflectionType = type; reflectionType != null; reflectionType = reflectionType.GetTypeInfo().BaseType)
            {
                methodInfo = reflectionType.GetRuntimeMethods()
                                           .FirstOrDefault(m => m.Name == MemberName && ParameterTypesCompatible(m.GetParameters(), parameterTypes));
                if (methodInfo != null)
                    break;
            }

            if (methodInfo == null || !methodInfo.IsStatic)
                return null;

            return methodInfo;
        }

        // from base implementation
        private PropertyInfo GetPropertyInfo(Type type)
        {
            PropertyInfo propInfo = null;
            for (var reflectionType = type; reflectionType != null; reflectionType = reflectionType.GetTypeInfo().BaseType)
            {
                propInfo = reflectionType.GetRuntimeProperty(MemberName);
                if (propInfo != null)
                    break;
            }

            if (propInfo == null || propInfo.GetMethod == null || !propInfo.GetMethod.IsStatic)
                return null;

            return propInfo;
        }

        // from base implementation
        private static bool ParameterTypesCompatible(ParameterInfo[] parameters, Type[] parameterTypes)
        {
            if (parameters?.Length != parameterTypes.Length)
                return false;

            for (int idx = 0; idx < parameters.Length; ++idx)
                if (parameterTypes[idx] != null && !parameters[idx].ParameterType.GetTypeInfo().IsAssignableFrom(parameterTypes[idx].GetTypeInfo()))
                    return false;

            return true;
        }

        private Dictionary<string, object> GetTupleValuesDictionary(MethodInfo testMethod, ITuple item)
        {
            Type type = MemberType ?? testMethod.DeclaringType;
            TupleElementNamesAttribute attr = null;

            PropertyInfo propInfo = GetPropertyInfo(type);
            if (propInfo != null)
            {
                attr = propInfo.GetCustomAttribute<TupleElementNamesAttribute>();
                goto Return;
            }

            FieldInfo fieldInfo = GetFieldInfo(type);
            if (fieldInfo != null)
            {
                attr = fieldInfo.GetCustomAttribute<TupleElementNamesAttribute>();
                goto Return;
            }

            MethodInfo methodInfo = GetMethodInfo(type);
            if (methodInfo != null)
            {
                attr = methodInfo.ReturnParameter.GetCustomAttribute<TupleElementNamesAttribute>();
                goto Return;
            }

        Return:
            return TupleToDictionary(item, attr);
        }

        private static Dictionary<string, object> TupleToDictionary(ITuple tuple, TupleElementNamesAttribute attr)
        {
            // For now we don't support nested tuples or tuples whose element names differ
            // only in case. If we care about this scenario, we can add support for it in
            // the future.

            Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < tuple.Length; i++)
            {
                values[i.ToString(CultureInfo.InvariantCulture)] = tuple[i];
            }

            IList<string> friendlyNameList = attr?.TransformNames;
            if (friendlyNameList?.Count == tuple.Length)
            {
                for (int i = 0; i < tuple.Length; i++)
                {
                    string thisFriendlyName = friendlyNameList[i];
                    if (!string.IsNullOrEmpty(thisFriendlyName))
                    {
                        values[thisFriendlyName] = tuple[i];
                    }
                }
            }

            return values;
        }
    }
}
