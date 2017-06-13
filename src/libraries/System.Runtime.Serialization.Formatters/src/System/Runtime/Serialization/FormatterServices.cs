// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace System.Runtime.Serialization
{
    public static class FormatterServices
    {
        private static readonly ConcurrentDictionary<MemberHolder, MemberInfo[]> s_memberInfoTable = new ConcurrentDictionary<MemberHolder, MemberInfo[]>();

        private static FieldInfo[] InternalGetSerializableMembers(Type type)
        {
            Debug.Assert(type != null);

            if (type.IsInterface)
            {
                return Array.Empty<FieldInfo>();
            }

            if (!type.IsSerializable)
            {
                throw new SerializationException(SR.Format(SR.Serialization_NonSerType, type.FullName, type.Assembly.FullName));
            }

            // Get all of the serializable members in the class to be serialized.
            FieldInfo[] typeMembers = GetSerializableFields(type);

            // If this class doesn't extend directly from object, walk its hierarchy and 
            // get all of the private and assembly-access fields (e.g. all fields that aren't
            // virtual) and include them in the list of things to be serialized.  
            Type parentType = type.BaseType;
            if (parentType != null && parentType != typeof(object))
            {
                Type[] parentTypes;
                int parentTypeCount;
                bool classNamesUnique = GetParentTypes(parentType, out parentTypes, out parentTypeCount);

                if (parentTypeCount > 0)
                {
                    var allMembers = new List<FieldInfo>();
                    for (int i = 0; i < parentTypeCount; i++)
                    {
                        parentType = parentTypes[i];
                        if (!parentType.IsSerializable)
                        {
                            throw new SerializationException(SR.Format(SR.Serialization_NonSerType, parentType.FullName, parentType.Module.Assembly.FullName));
                        }

                        FieldInfo[] typeFields = parentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        string typeName = classNamesUnique ? parentType.Name : parentType.FullName;
                        foreach (FieldInfo field in typeFields)
                        {
                            // Family and Assembly fields will be gathered by the type itself.
                            if (!field.IsNotSerialized)
                            {
                                allMembers.Add(new SerializationFieldInfo(field, typeName));
                            }
                        }
                    }

                    // If we actually found any new MemberInfo's, we need to create a new MemberInfo array and
                    // copy all of the members which we've found so far into that.
                    if (allMembers != null && allMembers.Count > 0)
                    {
                        var membersTemp = new FieldInfo[allMembers.Count + typeMembers.Length];
                        Array.Copy(typeMembers, membersTemp, typeMembers.Length);
                        allMembers.CopyTo(membersTemp, typeMembers.Length);
                        typeMembers = membersTemp;
                    }
                }
            }

            return typeMembers;
        }

        private static FieldInfo[] GetSerializableFields(Type type)
        {
            // Get the list of all fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            int countProper = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                if ((fields[i].Attributes & FieldAttributes.NotSerialized) == FieldAttributes.NotSerialized)
                {
                    continue;
                }

                countProper++;
            }

            if (countProper != fields.Length)
            {
                var properFields = new FieldInfo[countProper];
                countProper = 0;
                for (int i = 0; i < fields.Length; i++)
                {
                    if ((fields[i].Attributes & FieldAttributes.NotSerialized) == FieldAttributes.NotSerialized)
                    {
                        continue;
                    }

                    properFields[countProper] = fields[i];
                    countProper++;
                }
                return properFields;
            }
            else
            {
                return fields;
            }
        }

        private static bool GetParentTypes(Type parentType, out Type[] parentTypes, out int parentTypeCount)
        {
            parentTypes = null;
            parentTypeCount = 0;

            // Check if there are any dup class names. Then we need to include as part of
            // typeName to prefix the Field names in SerializationFieldInfo
            bool unique = true;
            Type objectType = typeof(object);
            for (Type t1 = parentType; t1 != objectType; t1 = t1.BaseType)
            {
                if (t1.IsInterface)
                {
                    continue;
                }

                string t1Name = t1.Name;
                for (int i = 0; unique && i < parentTypeCount; i++)
                {
                    string t2Name = parentTypes[i].Name;
                    if (t2Name.Length == t1Name.Length && t2Name[0] == t1Name[0] && t1Name == t2Name)
                    {
                        unique = false;
                        break;
                    }
                }

                // expand array if needed
                if (parentTypes == null || parentTypeCount == parentTypes.Length)
                {
                    Array.Resize(ref parentTypes, Math.Max(parentTypeCount * 2, 12));
                }

                parentTypes[parentTypeCount++] = t1;
            }

            return unique;
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
                mh => InternalGetSerializableMembers(mh._memberType));
        }

        public static void CheckTypeSecurity(Type t, TypeFilterLevel securityLevel)
        {
            // nop
        }

        public static object GetUninitializedObject(Type type) => RuntimeHelpers.GetUninitializedObject(type);

        public static object GetSafeUninitializedObject(Type type) => RuntimeHelpers.GetUninitializedObject(type);

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

            // Special case types like arrays            
            Type attributedType = type;
            while (attributedType.HasElementType)
            {
                attributedType = attributedType.GetElementType();
            }

            foreach (Attribute first in attributedType.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false))
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
