// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Provides some static methods to aid with the implementation
**          of a Formatter for Serialization.
**
**
============================================================*/
namespace System.Runtime.Serialization {
    
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;    
    using System.Security.Permissions;
    using System.Runtime.Remoting;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.IO;
    using System.Text;
    using System.Globalization;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public static class FormatterServices {
    
        // Gets a new instance of the object.  The entire object is initalized to 0 and no 
        // constructors have been run. **THIS MEANS THAT THE OBJECT MAY NOT BE IN A STATE
        // CONSISTENT WITH ITS INTERNAL REQUIREMENTS** This method should only be used for
        // deserialization when the user intends to immediately populate all fields.  This method
        // will not create an unitialized string because it is non-sensical to create an empty
        // instance of an immutable type.
        //
        public static Object GetUninitializedObject(Type type) {
            if ((object)type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            Contract.EndContractBlock();
    
            if (!(type is RuntimeType)) {
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidType", type.ToString()));
            }

            return nativeGetUninitializedObject((RuntimeType)type);
        }
    
        public static Object GetSafeUninitializedObject(Type type) {
             if ((object)type == null) {
                throw new ArgumentNullException(nameof(type));
            }
             Contract.EndContractBlock();

            if (!(type is RuntimeType)) {
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidType", type.ToString()));
            }

            try {
                return nativeGetSafeUninitializedObject((RuntimeType)type);
            }
            catch(SecurityException e) {
                throw new SerializationException(Environment.GetResourceString("Serialization_Security",  type.FullName), e);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Object nativeGetSafeUninitializedObject(RuntimeType type);
    
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Object nativeGetUninitializedObject(RuntimeType type);
        private static Binder s_binder = Type.DefaultBinder;
        internal static void SerializationSetValue(MemberInfo fi, Object target, Object value)
        {
            Contract.Requires(fi != null);

            RtFieldInfo rtField = fi as RtFieldInfo;

            if (rtField != null)
            {
                rtField.CheckConsistency(target);
                rtField.UnsafeSetValue(target, value, BindingFlags.Default, s_binder, null);
                return;
            }

            SerializationFieldInfo serField = fi as SerializationFieldInfo;
            if (serField != null)
            {
                serField.InternalSetValue(target, value, BindingFlags.Default, s_binder, null);
                return;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFieldInfo"));
        }

        // Fill in the members of obj with the data contained in data.
        // Returns the number of members populated.
        //
        public static Object PopulateObjectMembers(Object obj, MemberInfo[] members, Object[] data) {
            if (obj==null) {
                throw new ArgumentNullException(nameof(obj));
            }

            if (members==null) {
                throw new ArgumentNullException(nameof(members));
            }

            if (data==null) {
                throw new ArgumentNullException(nameof(data));
            }

            if (members.Length!=data.Length) {
                throw new ArgumentException(Environment.GetResourceString("Argument_DataLengthDifferent"));
            }
            Contract.EndContractBlock();

            MemberInfo mi;

            BCLDebug.Trace("SER", "[PopulateObjectMembers]Enter.");

            for (int i=0; i<members.Length; i++) {
                mi = members[i];
    
                if (mi==null) {
                    throw new ArgumentNullException(nameof(members), Environment.GetResourceString("ArgumentNull_NullMember", i));
                }
        
                //If we find an empty, it means that the value was never set during deserialization.
                //This is either a forward reference or a null.  In either case, this may break some of the
                //invariants mantained by the setter, so we'll do nothing with it for right now.
                if (data[i]!=null) {
                    if (mi.MemberType==MemberTypes.Field) {
                        SerializationSetValue(mi, obj, data[i]);
                    } else {
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMemberInfo"));
                    }

                    BCLDebug.Trace("SER", "[PopulateObjectMembers]\tType:", obj.GetType(), "\tMember:", 
                                   members[i].Name, " with member type: ", ((FieldInfo)members[i]).FieldType);
                }
                //Console.WriteLine("X");
            }
            
            BCLDebug.Trace("SER", "[PopulateObjectMembers]Leave.");

            return obj;
        }
    
        // Extracts the data from obj.  members is the array of members which we wish to
        // extract (must be FieldInfos or PropertyInfos).  For each supplied member, extract the matching value and
        // return it in a Object[] of the same size.
        //
        public static Object[] GetObjectData(Object obj, MemberInfo[] members) {
    
            if (obj==null) {
                throw new ArgumentNullException(nameof(obj));
            }
    
            if (members==null) {
                throw new ArgumentNullException(nameof(members));
            }
            Contract.EndContractBlock();
            
            int numberOfMembers = members.Length;
    
            Object[] data = new Object[numberOfMembers];
            MemberInfo mi;
    
            for (int i=0; i<numberOfMembers; i++) {
                mi=members[i];
    
                if (mi==null) {
                    throw new ArgumentNullException(nameof(members), Environment.GetResourceString("ArgumentNull_NullMember", i));
                }
    
                if (mi.MemberType==MemberTypes.Field) {
                    Debug.Assert(mi is RuntimeFieldInfo || mi is SerializationFieldInfo,
                                    "[FormatterServices.GetObjectData]mi is RuntimeFieldInfo || mi is SerializationFieldInfo.");

                    RtFieldInfo rfi = mi as RtFieldInfo;
                    if (rfi != null) {
                        rfi.CheckConsistency(obj);
                        data[i] = rfi.UnsafeGetValue(obj);
                    } else {
                        data[i] = ((SerializationFieldInfo)mi).InternalGetValue(obj);
                    }
                } else {
                    throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMemberInfo"));
                }
            }
    
            return data;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public static ISerializationSurrogate GetSurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            if (innerSurrogate == null)
                throw new ArgumentNullException(nameof(innerSurrogate));
            Contract.EndContractBlock();
            return new SurrogateForCyclicalReference(innerSurrogate);
        }

        /*=============================GetTypeFromAssembly==============================
        **Action:
        **Returns:
        **Arguments:
        **Exceptions:
        ==============================================================================*/
        public static Type GetTypeFromAssembly(Assembly assem, String name) {
            if (assem==null)
                throw new ArgumentNullException(nameof(assem));
            Contract.EndContractBlock();
            return assem.GetType(name, false, false);
        }
    
        /*============================LoadAssemblyFromString============================
        **Action: Loads an assembly from a given string.  The current assembly loading story
        **        is quite confusing.  If the assembly is in the fusion cache, we can load it
        **        using the stringized-name which we transmitted over the wire.  If that fails,
        **        we try for a lookup of the assembly using the simple name which is the first
        **        part of the assembly name.  If we can't find it that way, we'll return null
        **        as our failure result.
        **Returns: The loaded assembly or null if it can't be found.
        **Arguments: assemblyName -- The stringized assembly name.
        **Exceptions: None
        ==============================================================================*/
        internal static Assembly LoadAssemblyFromString(String assemblyName) {
            //
            // Try using the stringized assembly name to load from the fusion cache.
            //
            BCLDebug.Trace("SER", "[LoadAssemblyFromString]Looking for assembly: ", assemblyName);
            Assembly found = Assembly.Load(assemblyName);
            return found;
        }

        internal static Assembly LoadAssemblyFromStringNoThrow(String assemblyName) {
            try {
                return LoadAssemblyFromString(assemblyName);
            }
            catch (Exception e){
                BCLDebug.Trace("SER", "[LoadAssemblyFromString]", e.ToString());
            }
            return null;
        }

        internal static string GetClrAssemblyName(Type type, out bool hasTypeForwardedFrom) {
            if ((object)type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            object[] typeAttributes = type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false);
            if (typeAttributes != null && typeAttributes.Length > 0) {
                hasTypeForwardedFrom = true;
                TypeForwardedFromAttribute typeForwardedFromAttribute = (TypeForwardedFromAttribute)typeAttributes[0];
                return typeForwardedFromAttribute.AssemblyFullName;
            }
            else {
                hasTypeForwardedFrom = false;
                return type.Assembly.FullName;
            }
        }

        internal static string GetClrTypeFullName(Type type) {
            if (type.IsArray) {
                return GetClrTypeFullNameForArray(type);
            }
            else {
                return GetClrTypeFullNameForNonArrayTypes(type);
            }
        }

        static string GetClrTypeFullNameForArray(Type type) {
            int rank = type.GetArrayRank();
            if (rank == 1)
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}{1}", GetClrTypeFullName(type.GetElementType()), "[]");
            }
            else
            {
                StringBuilder builder = new StringBuilder(GetClrTypeFullName(type.GetElementType())).Append("[");
                for (int commaIndex = 1; commaIndex < rank; commaIndex++)
                {
                    builder.Append(",");
                }
                builder.Append("]");
                return builder.ToString();
            }
        }

        static string GetClrTypeFullNameForNonArrayTypes(Type type) {
            if (!type.IsGenericType) {
                return type.FullName;
            }

            Type[] genericArguments = type.GetGenericArguments();
            StringBuilder builder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append("[");
            bool hasTypeForwardedFrom;

            foreach (Type genericArgument in genericArguments) {
                builder.Append("[").Append(GetClrTypeFullName(genericArgument)).Append(", ");
                builder.Append(GetClrAssemblyName(genericArgument, out hasTypeForwardedFrom)).Append("],");
            }

            //remove the last comma and close typename for generic with a close bracket
            return builder.Remove(builder.Length - 1, 1).Append("]").ToString();
        }
    }

    internal sealed class SurrogateForCyclicalReference : ISerializationSurrogate
    {
        ISerializationSurrogate innerSurrogate;
        internal SurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            if (innerSurrogate == null)
                throw new ArgumentNullException(nameof(innerSurrogate));
            this.innerSurrogate = innerSurrogate;
        }

        public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
        {
            innerSurrogate.GetObjectData(obj, info, context);
        }
        
        public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return innerSurrogate.SetObjectData(obj, info, context, selector);
        }
    }
}





