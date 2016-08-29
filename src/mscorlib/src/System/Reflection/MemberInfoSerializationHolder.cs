// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Reflection 
{
    [Serializable]
    internal class MemberInfoSerializationHolder : ISerializable, IObjectReference 
    {
        #region Staitc Public Members
        public static void GetSerializationInfo(SerializationInfo info, String name, RuntimeType reflectedClass, String signature, MemberTypes type)
        {
            GetSerializationInfo(info, name, reflectedClass, signature, null, type, null);
        }

        public static void GetSerializationInfo(
            SerializationInfo info,
            String name,
            RuntimeType reflectedClass,
            String signature,
            String signature2,
            MemberTypes type,
            Type[] genericArguments)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            String assemblyName = reflectedClass.Module.Assembly.FullName;
            String typeName = reflectedClass.FullName;

            info.SetType(typeof(MemberInfoSerializationHolder));
            info.AddValue("Name", name, typeof(String));
            info.AddValue("AssemblyName", assemblyName, typeof(String));
            info.AddValue("ClassName", typeName, typeof(String));
            info.AddValue("Signature", signature, typeof(String));
            info.AddValue("Signature2", signature2, typeof(String));
            info.AddValue("MemberType", (int)type);
            info.AddValue("GenericArguments", genericArguments, typeof(Type[]));
        }
        #endregion

        #region Private Data Members
        private String m_memberName;
        private RuntimeType m_reflectedType;
        // m_signature stores the ToString() representation of the member which is sometimes ambiguous.
        // Mulitple overloads of the same methods or properties can identical ToString().
        // m_signature2 stores the SerializationToString() representation which should be unique for each member.
        // It is only written and used by post 4.0 CLR versions.
        private String m_signature;
        private String m_signature2;
        private MemberTypes m_memberType;
        private SerializationInfo m_info;
        #endregion
    
        #region Constructor
        internal MemberInfoSerializationHolder(SerializationInfo info, StreamingContext context) 
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            String assemblyName = info.GetString("AssemblyName");
            String typeName = info.GetString("ClassName");
            
            if (assemblyName == null || typeName == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));

            Assembly assem = FormatterServices.LoadAssemblyFromString(assemblyName);
            m_reflectedType = assem.GetType(typeName, true, false) as RuntimeType;
            m_memberName = info.GetString("Name");
            m_signature = info.GetString("Signature");
            // Only v4.0 and later generates and consumes Signature2
            m_signature2 = (string)info.GetValueNoThrow("Signature2", typeof(string));
            m_memberType = (MemberTypes)info.GetInt32("MemberType");
            m_info = info;
        }
        #endregion

        #region ISerializable
        [System.Security.SecurityCritical]  // auto-generated
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Method));
        }
        #endregion
    
        #region IObjectReference
        [System.Security.SecurityCritical]  // auto-generated
        public virtual Object GetRealObject(StreamingContext context) 
        {
            if (m_memberName == null || m_reflectedType == null || m_memberType == 0)
                throw new SerializationException(Environment.GetResourceString(ResId.Serialization_InsufficientState));

            BindingFlags bindingFlags = 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | 
                BindingFlags.Static | BindingFlags.OptionalParamBinding;
    
            switch (m_memberType) 
            {
                #region case MemberTypes.Field:
                case MemberTypes.Field:
                {
                    FieldInfo[] fields = m_reflectedType.GetMember(m_memberName, MemberTypes.Field, bindingFlags) as FieldInfo[];

                    if (fields.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));

                    return fields[0];
                }
                #endregion

                #region case MemberTypes.Event:
                case MemberTypes.Event:
                {
                    EventInfo[] events = m_reflectedType.GetMember(m_memberName, MemberTypes.Event, bindingFlags) as EventInfo[];

                    if (events.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));

                    return events[0];
                }
                #endregion

                #region case MemberTypes.Property:
                case MemberTypes.Property:
                {
                    PropertyInfo[] properties = m_reflectedType.GetMember(m_memberName, MemberTypes.Property, bindingFlags) as PropertyInfo[];

                    if (properties.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));

                    if (properties.Length == 1)
                        return properties[0];

                    if (properties.Length > 1)
                    {
                        for (int i = 0; i < properties.Length; i++)
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimePropertyInfo)properties[i]).SerializationToString().Equals(m_signature2))
                                    return properties[i];
                            }
                            else
                            {
                                if ((properties[i]).ToString().Equals(m_signature))
                                    return properties[i];
                            }
                        }
                    }

                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));            
                }
                #endregion

                #region case MemberTypes.Constructor:
                case MemberTypes.Constructor:
                {
                    if (m_signature == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NullSignature));

                    ConstructorInfo[] constructors = m_reflectedType.GetMember(m_memberName, MemberTypes.Constructor, bindingFlags) as ConstructorInfo[];

                    if (constructors.Length == 1)
                        return constructors[0];

                    if (constructors.Length > 1)
                    {
                        for (int i = 0; i < constructors.Length; i++)
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimeConstructorInfo)constructors[i]).SerializationToString().Equals(m_signature2))
                                    return constructors[i];
                            }
                            else
                            {
                                if (constructors[i].ToString().Equals(m_signature))
                                    return constructors[i];
                            }
                        }
                    }

                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));            
                }
                #endregion

                #region case MemberTypes.Method:
                case MemberTypes.Method:
                {
                    MethodInfo methodInfo = null;

                    if (m_signature == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NullSignature));

                    Type[] genericArguments = m_info.GetValueNoThrow("GenericArguments", typeof(Type[])) as Type[]; 

                    MethodInfo[] methods = m_reflectedType.GetMember(m_memberName, MemberTypes.Method, bindingFlags) as MethodInfo[];

                    if (methods.Length == 1)
                        methodInfo = methods[0];

                    else if (methods.Length > 1)
                    {
                        for (int i = 0; i < methods.Length; i++) 
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimeMethodInfo)methods[i]).SerializationToString().Equals(m_signature2))
                                {
                                    methodInfo = methods[i];
                                    break;
                                }
                            }
                            else
                            {

                                if (methods[i].ToString().Equals(m_signature))
                                {
                                    methodInfo = methods[i];
                                    break;
                                }
                            }

                            // Handle generic methods specially since the signature match above probably won't work (the candidate
                            // method info hasn't been instantiated). If our target method is generic as well we can skip this.
                            if (genericArguments != null && methods[i].IsGenericMethod)
                            {
                                if (methods[i].GetGenericArguments().Length == genericArguments.Length)
                                {
                                    MethodInfo candidateMethod = methods[i].MakeGenericMethod(genericArguments);

                                    if (m_signature2 != null)
                                    {
                                        if (((RuntimeMethodInfo)candidateMethod).SerializationToString().Equals(m_signature2))
                                        {
                                            methodInfo = candidateMethod;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (candidateMethod.ToString().Equals(m_signature))
                                        {
                                            methodInfo = candidateMethod;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (methodInfo == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));            

                    if (!methodInfo.IsGenericMethodDefinition)
                        return methodInfo;

                    if (genericArguments == null)
                        return methodInfo;

                    if (genericArguments[0] == null)
                        return null;

                    return methodInfo.MakeGenericMethod(genericArguments);
                }
                #endregion

                default:
                    throw new ArgumentException(Environment.GetResourceString("Serialization_MemberTypeNotRecognized"));
            }    
        }
        #endregion
    }

    
}
