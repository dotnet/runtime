// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.Serialization;
using System.Reflection;
using System.Globalization;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System
{
    [Serializable]
    // Holds classes (Empty, Null, Missing) for which we guarantee that there is only ever one instance of.
#if CORECLR
    internal
#else
    public  // On CoreRT, this must be public because of the Reflection.Core/CoreLib divide and the need to whitelist past the ReflectionBlock.
#endif
    class UnitySerializationHolder : ISerializable, IObjectReference
    {
#region Internal Constants
        internal const int EmptyUnity = 0x0001;
        internal const int NullUnity = 0x0002;
        internal const int MissingUnity = 0x0003;
        internal const int RuntimeTypeUnity = 0x0004;
        public const int ModuleUnity = 0x0005;
        public const int AssemblyUnity = 0x0006;
        internal const int GenericParameterTypeUnity = 0x0007;
        internal const int PartialInstantiationTypeUnity = 0x0008;

        internal const int Pointer = 0x0001;
        internal const int Array = 0x0002;
        internal const int SzArray = 0x0003;
        internal const int ByRef = 0x0004;
#endregion

#region Internal Static Members
        internal static void GetUnitySerializationInfo(SerializationInfo info, Missing missing)
        {
            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue("UnityType", MissingUnity);
        }

        internal static Type AddElementTypes(SerializationInfo info, Type type)
        {
            List<int> elementTypes = new List<int>();
            while (type.HasElementType)
            {
                if (type.IsSZArray)
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

                type = type.GetElementType();
            }

            info.AddValue("ElementTypes", elementTypes.ToArray(), typeof(int[]));

            return type;
        }

        internal Type MakeElementTypes(Type type)
        {
            for (int i = _elementTypes.Length - 1; i >= 0; i--)
            {
                if (_elementTypes[i] == SzArray)
                {
                    type = type.MakeArrayType();
                }
                else if (_elementTypes[i] == Array)
                {
                    type = type.MakeArrayType(_elementTypes[--i]);
                }
                else if ((_elementTypes[i] == Pointer))
                {
                    type = type.MakePointerType();
                }
                else if ((_elementTypes[i] == ByRef))
                {
                    type = type.MakeByRefType();
                }
            }

            return type;
        }

        public static void GetUnitySerializationInfo(SerializationInfo info, Type type)
        {
            Type rootElementType = type;
            while (rootElementType.HasElementType)
            {
                rootElementType = rootElementType.GetElementType();
            }

            if (rootElementType.IsGenericParameter)
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
                type = type.GetGenericTypeDefinition();
            }

            GetUnitySerializationInfo(info, unityType, type.FullName, type.Assembly);
        }

        public static void GetUnitySerializationInfo(
            SerializationInfo info, int unityType, string data, Assembly assembly)
        {
            // A helper method that returns the SerializationInfo that a class utilizing 
            // UnitySerializationHelper should return from a call to GetObjectData.  It contains
            // the unityType (defined above) and any optional data (used only for the reflection
            // types.)

            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue("Data", data, typeof(string));
            info.AddValue("UnityType", unityType);

            string assemName;

            if (assembly == null)
            {
                assemName = string.Empty;
            }
            else
            {
                assemName = assembly.FullName;
            }

            info.AddValue("AssemblyName", assemName);
        }
#endregion

#region Private Data Members
        private readonly Type[] _instantiation;
        private readonly int[] _elementTypes;
        private readonly int _genericParameterPosition;
        private readonly Type _declaringType;
        private readonly MethodBase _declaringMethod;
        private readonly string _data;
        private readonly string _assemblyName;
        private int _unityType;
#endregion

#region Constructor
        public UnitySerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            _unityType = info.GetInt32("UnityType");

            if (_unityType == MissingUnity)
                return;

            if (_unityType == GenericParameterTypeUnity)
            {
                _declaringMethod = info.GetValue("DeclaringMethod", typeof(MethodBase)) as MethodBase;
                _declaringType = info.GetValue("DeclaringType", typeof(Type)) as Type;
                _genericParameterPosition = info.GetInt32("GenericParameterPosition");
                _elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];

                return;
            }

            if (_unityType == PartialInstantiationTypeUnity)
            {
                _instantiation = info.GetValue("GenericArguments", typeof(Type[])) as Type[];
                _elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];
            }

            _data = info.GetString("Data");
            _assemblyName = info.GetString("AssemblyName");
        }
#endregion

#region Private Methods
        private void ThrowInsufficientInformation(string field)
        {
            throw new SerializationException(
                SR.Format(SR.Serialization_InsufficientDeserializationState, field));
        }
#endregion

#region ISerializable
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(SR.NotSupported_UnitySerHolder);
        }
#endregion

#region IObjectReference
        public virtual object GetRealObject(StreamingContext context)
        {
            // GetRealObject uses the data we have in _data and _unityType to do a lookup on the correct 
            // object to return.  We have specific code here to handle the different types which we support.
            // The reflection types (Assembly, Module, and Type) have to be looked up through their static
            // accessors by name.

            Assembly assembly;

            switch (_unityType)
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
                        _unityType = RuntimeTypeUnity;
                        Type definition = GetRealObject(context) as Type;
                        _unityType = PartialInstantiationTypeUnity;

                        if (_instantiation[0] == null)
                            return null;

                        return MakeElementTypes(definition.MakeGenericType(_instantiation));
                    }

                case GenericParameterTypeUnity:
                    {
                        if (_declaringMethod == null && _declaringType == null)
                            ThrowInsufficientInformation("DeclaringMember");

                        if (_declaringMethod != null)
                            return _declaringMethod.GetGenericArguments()[_genericParameterPosition];

                        return MakeElementTypes(_declaringType.GetGenericArguments()[_genericParameterPosition]);
                    }

                case RuntimeTypeUnity:
                    {
                        if (_data == null || _data.Length == 0)
                            ThrowInsufficientInformation("Data");

                        if (_assemblyName == null)
                            ThrowInsufficientInformation("AssemblyName");

                        if (_assemblyName.Length == 0)
                            return Type.GetType(_data, true, false);

                        assembly = Assembly.Load(_assemblyName);

                        Type t = assembly.GetType(_data, true, false);

                        return t;
                    }

                case ModuleUnity:
                    {
                        if (_data == null || _data.Length == 0)
                            ThrowInsufficientInformation("Data");

                        if (_assemblyName == null)
                            ThrowInsufficientInformation("AssemblyName");

                        assembly = Assembly.Load(_assemblyName);

                        Module namedModule = assembly.GetModule(_data);

                        if (namedModule == null)
                            throw new SerializationException(
                                SR.Format(SR.Serialization_UnableToFindModule, _data, _assemblyName));

                        return namedModule;
                    }

                case AssemblyUnity:
                    {
                        if (_data == null || _data.Length == 0)
                            ThrowInsufficientInformation("Data");

                        if (_assemblyName == null)
                            ThrowInsufficientInformation("AssemblyName");

                        assembly = Assembly.Load(_assemblyName);

                        return assembly;
                    }

                default:
                    throw new ArgumentException(SR.Argument_InvalidUnity);
            }
        }
#endregion
    }
}
