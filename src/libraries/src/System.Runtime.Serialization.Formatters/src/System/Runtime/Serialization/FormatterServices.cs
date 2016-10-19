// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters;

namespace System.Runtime.Serialization
{
    public static class FormatterServices
    {
        private static readonly ConcurrentDictionary<MemberHolder, MemberInfo[]> s_memberInfoTable = new ConcurrentDictionary<MemberHolder, MemberInfo[]>();

        private static FieldInfo[] GetSerializableFields(Type type)
        {
            if (type.IsInterface)
            {
                return Array.Empty<FieldInfo>();
            }

            if (!type.IsSerializable)
            {
                throw new SerializationException(SR.Format(SR.Serialization_NonSerType, type.FullName, type.Assembly.FullName));
            }

            var results = new List<FieldInfo>();
            for (Type t = type; t != typeof(object); t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if ((field.Attributes & FieldAttributes.NotSerialized) != FieldAttributes.NotSerialized)
                    {
                        results.Add(field);
                    }
                }
            }
            return results.ToArray();
        }

        public static MemberInfo[] GetSerializableMembers(Type type)
        {
            return GetSerializableMembers(type, new StreamingContext(StreamingContextStates.All));
        }

        public static MemberInfo[] GetSerializableMembers(Type type, StreamingContext context)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // If we've already gathered the members for this type, just return them.
            // Otherwise, get them and add them.
            return s_memberInfoTable.GetOrAdd(
                new MemberHolder(type, context), 
                mh => GetSerializableFields(mh._memberType));
        }

        public static void CheckTypeSecurity(Type t, TypeFilterLevel securityLevel)
        {
            // nop
        }

        // TODO #8133: Fix this to avoid reflection
        private static readonly Func<Type, object> s_getUninitializedObjectDelegate = (Func<Type, object>)
            typeof(string).Assembly
            .GetType("System.Runtime.Serialization.FormatterServices")
            .GetMethod("GetUninitializedObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .CreateDelegate(typeof(Func<Type, object>));

        public static object GetUninitializedObject(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return s_getUninitializedObjectDelegate(type);
        }

        public static object GetSafeUninitializedObject(Type type) => GetUninitializedObject(type);

        internal static void SerializationSetValue(MemberInfo fi, object target, object value)
        {
            Debug.Assert(fi != null);

            var serField = fi as FieldInfo;
            if (serField != null)
            {
                serField.SetValue(target, value);
                return;
            }

            throw new ArgumentException(SR.Argument_InvalidFieldInfo);
        }

        public static object PopulateObjectMembers(object obj, MemberInfo[] members, object[] data)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (members.Length != data.Length)
            {
                throw new ArgumentException(SR.Argument_DataLengthDifferent);
            }

            for (int i = 0; i < members.Length; i++)
            {
                MemberInfo member = members[i];
                if (member == null)
                {
                    throw new ArgumentNullException(nameof(members), SR.Format(SR.ArgumentNull_NullMember, i));
                }

                // If we find an empty, it means that the value was never set during deserialization.
                // This is either a forward reference or a null.  In either case, this may break some of the
                // invariants mantained by the setter, so we'll do nothing with it for right now.
                object value = data[i];
                if (value == null)
                {
                    continue;
                }

                // If it's a field, set its value.
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    field.SetValue(obj, data[i]);
                    continue;
                }

                // Otherwise, it's not supported.
                throw new SerializationException(SR.Serialization_UnknownMemberInfo);
            }

            return obj;
        }

        public static object[] GetObjectData(object obj, MemberInfo[] members)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            object[] data = new object[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                MemberInfo member = members[i];
                if (member == null)
                {
                    throw new ArgumentNullException(nameof(members), SR.Format(SR.ArgumentNull_NullMember, i));
                }

                FieldInfo field = member as FieldInfo;
                if (field == null)
                {
                    throw new SerializationException(SR.Serialization_UnknownMemberInfo);
                }

                data[i] = field.GetValue(obj);
            }
            return data;
        }

        public static ISerializationSurrogate GetSurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            if (innerSurrogate == null)
            {
                throw new ArgumentNullException(nameof(innerSurrogate));
            }
            return new SurrogateForCyclicalReference(innerSurrogate);
        }

        public static Type GetTypeFromAssembly(Assembly assem, string name)
        {
            if (assem == null)
            {
                throw new ArgumentNullException(nameof(assem));
            }
            return assem.GetType(name, throwOnError: false, ignoreCase: false);
        }

        internal static Assembly LoadAssemblyFromString(string assemblyName)
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }

        internal static Assembly LoadAssemblyFromStringNoThrow(string assemblyName)
        {
            try
            {
                return LoadAssemblyFromString(assemblyName);
            }
            catch (Exception) { }
            return null;
        }

        internal static string GetClrAssemblyName(Type type, out bool hasTypeForwardedFrom)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (Attribute first in type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false))
            {
                hasTypeForwardedFrom = true;
                return ((TypeForwardedFromAttribute)first).AssemblyFullName;
            }

            hasTypeForwardedFrom = false;
            return type.Assembly.FullName;
        }

        internal static string GetClrTypeFullName(Type type)
        {
            return type.IsArray ?
                GetClrTypeFullNameForArray(type) :
                GetClrTypeFullNameForNonArrayTypes(type);
        }

        private static string GetClrTypeFullNameForArray(Type type)
        {
            int rank = type.GetArrayRank();
            Debug.Assert(rank >= 1);
            string typeName = GetClrTypeFullName(type.GetElementType());
            return rank == 1 ?
                typeName + "[]" :
                typeName + "[" + new string(',', rank - 1) + "]";
        }

        private static string GetClrTypeFullNameForNonArrayTypes(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var builder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append("[");

            bool hasTypeForwardedFrom;
            foreach (Type genericArgument in type.GetGenericArguments())
            {
                builder.Append("[").Append(GetClrTypeFullName(genericArgument)).Append(", ");
                builder.Append(GetClrAssemblyName(genericArgument, out hasTypeForwardedFrom)).Append("],");
            }

            //remove the last comma and close typename for generic with a close bracket
            return builder.Remove(builder.Length - 1, 1).Append("]").ToString();
        }
    }

    internal sealed class SurrogateForCyclicalReference : ISerializationSurrogate
    {
        private readonly ISerializationSurrogate _innerSurrogate;

        internal SurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            Debug.Assert(innerSurrogate != null);
            _innerSurrogate = innerSurrogate;
        }

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            _innerSurrogate.GetObjectData(obj, info, context);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return _innerSurrogate.SetObjectData(obj, info, context, selector);
        }
    }
}
