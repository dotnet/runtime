// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Reflection;
using System.Globalization;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System {
    
    [Serializable]
    // Holds classes (Empty, Null, Missing) for which we guarantee that there is only ever one instance of.
    internal class UnitySerializationHolder : ISerializable, IObjectReference
    {   
        #region Internal Constants
        internal const int EmptyUnity       = 0x0001;
        internal const int NullUnity        = 0x0002;
        internal const int MissingUnity     = 0x0003;
        internal const int RuntimeTypeUnity = 0x0004;
        internal const int ModuleUnity      = 0x0005;
        internal const int AssemblyUnity    = 0x0006;
        internal const int GenericParameterTypeUnity = 0x0007;
        internal const int PartialInstantiationTypeUnity = 0x0008;
        
        internal const int Pointer          = 0x0001;
        internal const int Array            = 0x0002;
        internal const int SzArray          = 0x0003;
        internal const int ByRef            = 0x0004;
        #endregion

        #region Internal Static Members
        internal static void GetUnitySerializationInfo(SerializationInfo info, Missing missing)
        {
            info.SetType(typeof(UnitySerializationHolder));            
            info.AddValue("UnityType", MissingUnity);
        }

        internal static RuntimeType AddElementTypes(SerializationInfo info, RuntimeType type)
        {
            List<int> elementTypes = new List<int>();
            while(type.HasElementType)
            {
                if (type.IsSzArray)
                {
                    elementTypes.Add(SzArray);
                }
                else if (type.IsArray)
                {
                    elementTypes.Add(type.GetArrayRank());
                    elementTypes.Add(Array);
                }
                else if (type.IsPointer)
                {
                    elementTypes.Add(Pointer);
                }
                else if (type.IsByRef)
                {
                    elementTypes.Add(ByRef);
                }
                
                type = (RuntimeType)type.GetElementType();
            }

            info.AddValue("ElementTypes", elementTypes.ToArray(), typeof(int[]));

            return type;
        }

        internal Type MakeElementTypes(Type type)
        {
            for (int i = m_elementTypes.Length - 1; i >= 0; i --)
            {
                if (m_elementTypes[i] == SzArray)
                {
                    type = type.MakeArrayType();
                }
                else if (m_elementTypes[i] == Array)
                {
                    type = type.MakeArrayType(m_elementTypes[--i]);
                }
                else if ((m_elementTypes[i] == Pointer))
                {
                    type = type.MakePointerType();
                }
                else if ((m_elementTypes[i] == ByRef))
                {
                    type = type.MakeByRefType();
                }
            }

            return type;
        }
            
        internal static void GetUnitySerializationInfo(SerializationInfo info, RuntimeType type)
        {
            if (type.GetRootElementType().IsGenericParameter)
            {
                type = AddElementTypes(info, type);
                info.SetType(typeof(UnitySerializationHolder));
                info.AddValue("UnityType", GenericParameterTypeUnity);
                info.AddValue("GenericParameterPosition", type.GenericParameterPosition);
                info.AddValue("DeclaringMethod", type.DeclaringMethod, typeof(MethodBase));
                info.AddValue("DeclaringType", type.DeclaringType, typeof(Type));

                return;
            }

            int unityType = RuntimeTypeUnity;

            if (!type.IsGenericTypeDefinition && type.ContainsGenericParameters)
            {
                // Partial instantiation
                unityType = PartialInstantiationTypeUnity;
                type = AddElementTypes(info, type);
                info.AddValue("GenericArguments", type.GetGenericArguments(), typeof(Type[]));
                type = (RuntimeType)type.GetGenericTypeDefinition();
            }

            GetUnitySerializationInfo(info, unityType, type.FullName, type.GetRuntimeAssembly());
        }

        internal static void GetUnitySerializationInfo(
            SerializationInfo info, int unityType, String data, RuntimeAssembly assembly)
        {
            // A helper method that returns the SerializationInfo that a class utilizing 
            // UnitySerializationHelper should return from a call to GetObjectData.  It contains
            // the unityType (defined above) and any optional data (used only for the reflection
            // types.)

            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue("Data", data, typeof(String));
            info.AddValue("UnityType", unityType);

            String assemName;

            if (assembly == null) 
            {
                assemName = String.Empty;
            } 
            else 
            {
                assemName = assembly.FullName;
            }

            info.AddValue("AssemblyName", assemName);
        }
        #endregion

        #region Private Data Members
        private Type[] m_instantiation;
        private int[] m_elementTypes;
        private int m_genericParameterPosition;
        private Type m_declaringType;
        private MethodBase m_declaringMethod;
        private String m_data;
        private String m_assemblyName;
        private int m_unityType;
        #endregion  

        #region Constructor
        internal UnitySerializationHolder(SerializationInfo info, StreamingContext context) 
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            
            m_unityType = info.GetInt32("UnityType");

            if (m_unityType == MissingUnity)
                return; 

            if (m_unityType == GenericParameterTypeUnity)
            {
                m_declaringMethod = info.GetValue("DeclaringMethod", typeof(MethodBase)) as MethodBase;
                m_declaringType = info.GetValue("DeclaringType", typeof(Type)) as Type;
                m_genericParameterPosition = info.GetInt32("GenericParameterPosition");
                m_elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];

                return;
            }

            if (m_unityType == PartialInstantiationTypeUnity)
            {
                m_instantiation = info.GetValue("GenericArguments", typeof(Type[])) as Type[];
                m_elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];
            }

            m_data = info.GetString("Data");
            m_assemblyName = info.GetString("AssemblyName");
        }
        #endregion

        #region Private Methods
        private void ThrowInsufficientInformation(string field)
        {
            throw new SerializationException(
                Environment.GetResourceString("Serialization_InsufficientDeserializationState", field));
        }
        #endregion

        #region ISerializable
        [System.Security.SecurityCritical]  // auto-generated
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnitySerHolder"));
        }
        #endregion

        #region IObjectReference
        [System.Security.SecurityCritical]  // auto-generated
        public virtual Object GetRealObject(StreamingContext context) 
        {
            // GetRealObject uses the data we have in m_data and m_unityType to do a lookup on the correct 
            // object to return.  We have specific code here to handle the different types which we support.
            // The reflection types (Assembly, Module, and Type) have to be looked up through their static
            // accessors by name.

            Assembly assembly;

            switch (m_unityType) 
            {
                case EmptyUnity:
                {
                    return Empty.Value;
                }

                case NullUnity:
                {
                    return DBNull.Value;
                }

                case MissingUnity:
                {
                    return Missing.Value;
                }

                case PartialInstantiationTypeUnity:
                {
                    m_unityType = RuntimeTypeUnity;
                    Type definition = GetRealObject(context) as Type;
                    m_unityType = PartialInstantiationTypeUnity;

                    if (m_instantiation[0] == null)
                        return null;
                   
                    return MakeElementTypes(definition.MakeGenericType(m_instantiation));
                }

                case GenericParameterTypeUnity:
                {
                    if (m_declaringMethod == null && m_declaringType == null) 
                        ThrowInsufficientInformation("DeclaringMember");
                    
                    if (m_declaringMethod != null)
                        return m_declaringMethod.GetGenericArguments()[m_genericParameterPosition];
                        
                    return MakeElementTypes(m_declaringType.GetGenericArguments()[m_genericParameterPosition]);
                }

                case RuntimeTypeUnity:
                {
                    if (m_data == null || m_data.Length == 0) 
                        ThrowInsufficientInformation("Data");
                    
                    if (m_assemblyName == null)
                        ThrowInsufficientInformation("AssemblyName");

                    if (m_assemblyName.Length == 0) 
                        return Type.GetType(m_data, true, false);
                    
                    assembly = Assembly.Load(m_assemblyName);
                    
                    Type t = assembly.GetType(m_data, true, false);

                    return t;
                }

                case ModuleUnity:
                {
                    if (m_data == null || m_data.Length == 0)
                        ThrowInsufficientInformation("Data");

                    if (m_assemblyName == null)
                        ThrowInsufficientInformation("AssemblyName");

                    assembly = Assembly.Load(m_assemblyName);
                    
                    Module namedModule = assembly.GetModule(m_data);
                    
                    if (namedModule == null)
                        throw new SerializationException(
                            Environment.GetResourceString("Serialization_UnableToFindModule", m_data, m_assemblyName));
                    
                    return namedModule;
                }

                case AssemblyUnity:
                {
                    if (m_data == null || m_data.Length == 0)
                        ThrowInsufficientInformation("Data");

                    if (m_assemblyName == null)
                        ThrowInsufficientInformation("AssemblyName");

                    assembly = Assembly.Load(m_assemblyName);
     
                    return assembly;
                }

                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUnity"));
            }
        }
        #endregion
    }

}





































