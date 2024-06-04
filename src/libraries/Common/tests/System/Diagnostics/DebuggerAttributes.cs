// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Diagnostics
{
    internal class DebuggerAttributeInfo
    {
        public object Instance { get; set; }
        public IEnumerable<PropertyInfo> Properties { get; set; }
    }

    internal class DebuggerDisplayResult
    {
        public string Value { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
    }

    internal static class DebuggerAttributes
    {
        internal static object GetFieldValue(object obj, string fieldName)
        {
            return GetField(obj, fieldName).GetValue(obj);
        }

        internal static void InvokeDebuggerTypeProxyProperties(object obj)
        {
            DebuggerAttributeInfo info = ValidateDebuggerTypeProxyProperties(obj);
            foreach (PropertyInfo pi in info.Properties)
            {
                pi.GetValue(info.Instance, null);
            }
        }

        internal static DebuggerAttributeInfo ValidateDebuggerTypeProxyProperties(object obj)
        {
            Type proxyType = GetProxyType(obj);

            // Create an instance of the proxy type, and make sure we can access all of the instance properties
            // on the type without exception
            object proxyInstance = Activator.CreateInstance(proxyType, obj);
            IEnumerable<PropertyInfo> properties = GetDebuggerVisibleProperties(proxyType);
            return new DebuggerAttributeInfo
            {
                Instance = proxyInstance,
                Properties = properties
            };
        }

        internal static void CreateDebuggerTypeProxyWithNullArgument(Type type)
        {
            Type proxyType = GetProxyType(type);
            Activator.CreateInstance(proxyType, [null]);
        }

        internal static DebuggerBrowsableState? GetDebuggerBrowsableState(MemberInfo info)
        {
            CustomAttributeData debuggerBrowsableAttribute = info.CustomAttributes
                .SingleOrDefault(a => a.AttributeType == typeof(DebuggerBrowsableAttribute));
            // Enums in attribute constructors are boxed as ints, so cast to int? first.
            return (DebuggerBrowsableState?)(int?)debuggerBrowsableAttribute?.ConstructorArguments.Single().Value;
        }

        internal static IEnumerable<FieldInfo> GetDebuggerVisibleFields(Type debuggerAttributeType)
        {
            // The debugger doesn't evaluate non-public members of type proxies.
            IEnumerable<FieldInfo> visibleFields = debuggerAttributeType.GetFields()
                .Where(fi => fi.IsPublic && GetDebuggerBrowsableState(fi) != DebuggerBrowsableState.Never);
            return visibleFields;
        }

        internal static IEnumerable<PropertyInfo> GetDebuggerVisibleProperties(Type debuggerAttributeType)
        {
            // The debugger doesn't evaluate non-public members of type proxies. GetGetMethod returns null if the getter is non-public.
            IEnumerable<PropertyInfo> visibleProperties = debuggerAttributeType.GetProperties()
                .Where(pi => pi.GetGetMethod() != null && GetDebuggerBrowsableState(pi) != DebuggerBrowsableState.Never);
            return visibleProperties;
        }

        internal static object GetProxyObject(object obj) => Activator.CreateInstance(GetProxyType(obj), obj);

        internal static Type GetProxyType(object obj) => GetProxyType(obj.GetType());

        internal static Type GetProxyType(Type type)
        {
            CustomAttributeData cad = FindAttribute(type, attributeType: typeof(DebuggerTypeProxyAttribute));

            Type proxyType = cad.ConstructorArguments[0].ArgumentType == typeof(Type) ?
                (Type)cad.ConstructorArguments[0].Value :
                Type.GetType((string)cad.ConstructorArguments[0].Value);
            if (type.GenericTypeArguments.Length > 0)
            {
                proxyType = proxyType.MakeGenericType(type.GenericTypeArguments);
            }

            return proxyType;
        }

        internal static DebuggerDisplayResult ValidateFullyDebuggerDisplayReferences(object obj)
        {
            CustomAttributeData cad = FindAttribute(obj.GetType(), attributeType: typeof(DebuggerDisplayAttribute));

            // Get the text of the DebuggerDisplayAttribute
            string attrText = (string)cad.ConstructorArguments[0].Value;
            string formattedValue = EvaluateDisplayString(attrText, obj);

            string formattedKey = FormatDebuggerDisplayNamedArgument(nameof(DebuggerDisplayAttribute.Name), cad, obj);
            string formattedType = FormatDebuggerDisplayNamedArgument(nameof(DebuggerDisplayAttribute.Type), cad, obj);

            return new DebuggerDisplayResult { Value = formattedValue, Key = formattedKey, Type = formattedType };
        }

        internal static string ValidateDebuggerDisplayReferences(object obj)
        {
            CustomAttributeData cad = FindAttribute(obj.GetType(), attributeType: typeof(DebuggerDisplayAttribute));

            // Get the text of the DebuggerDisplayAttribute
            string attrText = (string)cad.ConstructorArguments[0].Value;

            return EvaluateDisplayString(attrText, obj);
        }

        private static CustomAttributeData FindAttribute(Type type, Type attributeType)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                CustomAttributeData[] attributes = t.GetTypeInfo().CustomAttributes
                    .Where(a => a.AttributeType == attributeType)
                    .ToArray();
                if (attributes.Length != 0)
                {
                    if (attributes.Length > 1)
                    {
                        throw new InvalidOperationException($"Expected one {attributeType.Name} on {type} but found more.");
                    }
                    return attributes[0];
                }
            }
            throw new InvalidOperationException($"Expected one {attributeType.Name} on {type}.");
        }

        private static string FormatDebuggerDisplayNamedArgument(string argumentName, CustomAttributeData debuggerDisplayAttributeData, object obj)
        {
            CustomAttributeNamedArgument namedAttribute = debuggerDisplayAttributeData.NamedArguments.FirstOrDefault(na => na.MemberName == argumentName);
            if (namedAttribute != default)
            {
                string? value = (string?)namedAttribute.TypedValue.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    return EvaluateDisplayString(value, obj);
                }
            }
            return "";
        }

        private static string EvaluateDisplayString(string displayString, object obj)
        {
            Type objType = obj.GetType();
            string[] segments = displayString.Split(['{', '}']);

            if (segments.Length % 2 == 0)
            {
                throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} lacks a closing brace.");
            }

            if (segments.Length == 1)
            {
                throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} doesn't reference any expressions.");
            }

            var sb = new StringBuilder();

            for (int i = 0; i < segments.Length; i += 2)
            {
                string literal = segments[i];
                sb.Append(literal);

                if (i + 1 < segments.Length)
                {
                    string reference = segments[i + 1];
                    bool noQuotes = reference.EndsWith(",nq");

                    reference = reference.Replace(",nq", string.Empty);

                    // Evaluate the reference.
                    object member;
                    if (!TryEvaluateReference(obj, reference, out member))
                    {
                        throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} contains the expression \"{reference}\".");
                    }

                    string memberString = GetDebuggerMemberString(member, noQuotes);

                    sb.Append(memberString);
                }
            }

            return sb.ToString();
        }

        private static string GetDebuggerMemberString(object member, bool noQuotes)
        {
            string memberString = "null";
            if (member != null)
            {
                memberString = member.ToString();
                if (member is string)
                {
                    if (!noQuotes)
                    {
                        memberString = '"' + memberString + '"';
                    }
                }
                else if (!IsPrimitiveType(member))
                {
                    memberString = '{' + memberString + '}';
                }
            }

            return memberString;
        }

        private static bool IsPrimitiveType(object obj) =>
            obj is byte || obj is sbyte ||
            obj is short || obj is ushort ||
            obj is int || obj is uint ||
            obj is long || obj is ulong ||
            obj is float || obj is double;

        private static bool TryEvaluateReference(object obj, string reference, out object member)
        {
            PropertyInfo pi = GetProperty(obj, reference);
            if (pi != null)
            {
                member = pi.GetValue(obj);
                return true;
            }

            FieldInfo fi = GetField(obj, reference);
            if (fi != null)
            {
                member = fi.GetValue(obj);
                return true;
            }

            member = null;
            return false;
        }

        private static FieldInfo GetField(object obj, string fieldName)
        {
            for (Type t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                FieldInfo fi = t.GetTypeInfo().GetDeclaredField(fieldName);
                if (fi != null)
                {
                    return fi;
                }
            }
            return null;
        }

        private static PropertyInfo GetProperty(object obj, string propertyName)
        {
            for (Type t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                PropertyInfo pi = t.GetTypeInfo().GetDeclaredProperty(propertyName);
                if (pi != null)
                {
                    return pi;
                }
            }
            return null;
        }
    }
}
