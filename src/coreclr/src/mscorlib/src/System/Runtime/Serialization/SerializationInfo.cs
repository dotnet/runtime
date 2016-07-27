// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The structure for holding all of the data needed
**          for object serialization and deserialization.
**
**
===========================================================*/
namespace System.Runtime.Serialization
{

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Remoting;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Proxies;
#endif
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Security;
#if FEATURE_CORECLR
    using System.Runtime.CompilerServices;
#endif 

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SerializationInfo
    {
        private const int defaultSize = 4;
        private const string s_mscorlibAssemblySimpleName = System.CoreLib.Name;
        private const string s_mscorlibFileName = s_mscorlibAssemblySimpleName + ".dll";
        
        // Even though we have a dictionary, we're still keeping all the arrays around for back-compat. 
        // Otherwise we may run into potentially breaking behaviors like GetEnumerator() not returning entries in the same order they were added.
        internal String[] m_members;
        internal Object[] m_data;
        internal Type[] m_types;
        private Dictionary<string, int> m_nameToIndex;
        internal int m_currMember;
        internal IFormatterConverter m_converter;
        private String m_fullTypeName;
        private String m_assemName;
        private Type objectType;
        private bool isFullTypeNameSetExplicit;
        private bool isAssemblyNameSetExplicit;
        private bool requireSameTokenInPartialTrust;

        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter)
            : this(type, converter, false)
        {
        }

        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter, bool requireSameTokenInPartialTrust)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }

            Contract.EndContractBlock();

            objectType = type;
            m_fullTypeName = type.FullName;
            m_assemName = type.Module.Assembly.FullName;

            m_members = new String[defaultSize];
            m_data = new Object[defaultSize];
            m_types = new Type[defaultSize];

            m_nameToIndex = new Dictionary<string, int>();

            m_converter = converter;

            this.requireSameTokenInPartialTrust = requireSameTokenInPartialTrust;
        }

        public String FullTypeName
        {
            get
            {
                return m_fullTypeName;
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();
           
                m_fullTypeName = value;
                isFullTypeNameSetExplicit = true;
            }
        }

        public String AssemblyName
        {
            get
            {
                return m_assemName;
            }
            [SecuritySafeCritical]
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();
                if (this.requireSameTokenInPartialTrust)
                {
                    DemandForUnsafeAssemblyNameAssignments(this.m_assemName, value);
                }
                m_assemName = value;
                isAssemblyNameSetExplicit = true;
            }
        }

        [SecuritySafeCritical]
        public void SetType(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            if (this.requireSameTokenInPartialTrust)
            {
                DemandForUnsafeAssemblyNameAssignments(this.ObjectType.Assembly.FullName, type.Assembly.FullName);
            }

            if (!Object.ReferenceEquals(objectType, type))
            {
                objectType = type;
                m_fullTypeName = type.FullName;
                m_assemName = type.Module.Assembly.FullName;
                isFullTypeNameSetExplicit = false;
                isAssemblyNameSetExplicit = false;
            }
        }

        private static bool Compare(byte[] a, byte[] b)
        {
            // if either or both assemblies do not have public key token, we should demand, hence, returning false will force a demand
            if (a == null || b == null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }

                return true;
            }
        }

        [SecuritySafeCritical]
        internal static void DemandForUnsafeAssemblyNameAssignments(string originalAssemblyName, string newAssemblyName)
        {
#if !FEATURE_CORECLR
            if (!IsAssemblyNameAssignmentSafe(originalAssemblyName, newAssemblyName))
            {
                CodeAccessPermission.Demand(PermissionType.SecuritySerialization);
            }
#endif
        }

        internal static bool IsAssemblyNameAssignmentSafe(string originalAssemblyName, string newAssemblyName)
        {
            if (originalAssemblyName == newAssemblyName)
            {
                return true;
            }

            AssemblyName originalAssembly = new AssemblyName(originalAssemblyName);
            AssemblyName newAssembly = new AssemblyName(newAssemblyName);

            // mscorlib will get loaded by the runtime regardless of its string casing or its public key token,
            // so setting the assembly name to mscorlib must always be protected by a demand
            if (string.Equals(newAssembly.Name, s_mscorlibAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newAssembly.Name, s_mscorlibFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Compare(originalAssembly.GetPublicKeyToken(), newAssembly.GetPublicKeyToken());
        }

        public int MemberCount
        {
            get
            {
                return m_currMember;
            }
        }

        public Type ObjectType
        {
            get
            {
                return objectType;
            }
        }

        public bool IsFullTypeNameSetExplicit
        {
            get
            {
                return isFullTypeNameSetExplicit;
            }
        }

        public bool IsAssemblyNameSetExplicit
        {
            get
            {
                return isAssemblyNameSetExplicit;
            }
        }

        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(m_members, m_data, m_types, m_currMember);
        }

        private void ExpandArrays()
        {
            int newSize;
            Contract.Assert(m_members.Length == m_currMember, "[SerializationInfo.ExpandArrays]m_members.Length == m_currMember");

            newSize = (m_currMember * 2);

            //
            // In the pathological case, we may wrap
            //
            if (newSize < m_currMember)
            {
                if (Int32.MaxValue > m_currMember)
                {
                    newSize = Int32.MaxValue;
                }
            }

            //
            // Allocate more space and copy the data
            //
            String[] newMembers = new String[newSize];
            Object[] newData = new Object[newSize];
            Type[] newTypes = new Type[newSize];

            Array.Copy(m_members, newMembers, m_currMember);
            Array.Copy(m_data, newData, m_currMember);
            Array.Copy(m_types, newTypes, m_currMember);

            //
            // Assign the new arrys back to the member vars.
            //
            m_members = newMembers;
            m_data = newData;
            m_types = newTypes;
        }

        public void AddValue(String name, Object value, Type type)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            AddValueInternal(name, value, type);
        }

        public void AddValue(String name, Object value)
        {
            if (null == value)
            {
                AddValue(name, value, typeof(Object));
            }
            else
            {
                AddValue(name, value, value.GetType());
            }
        }

        public void AddValue(String name, bool value)
        {
            AddValue(name, (Object)value, typeof(bool));
        }

        public void AddValue(String name, char value)
        {
            AddValue(name, (Object)value, typeof(char));
        }


        [CLSCompliant(false)]
        public void AddValue(String name, sbyte value)
        {
            AddValue(name, (Object)value, typeof(sbyte));
        }

        public void AddValue(String name, byte value)
        {
            AddValue(name, (Object)value, typeof(byte));
        }

        public void AddValue(String name, short value)
        {
            AddValue(name, (Object)value, typeof(short));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, ushort value)
        {
            AddValue(name, (Object)value, typeof(ushort));
        }

        public void AddValue(String name, int value)
        {
            AddValue(name, (Object)value, typeof(int));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, uint value)
        {
            AddValue(name, (Object)value, typeof(uint));
        }

        public void AddValue(String name, long value)
        {
            AddValue(name, (Object)value, typeof(long));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, ulong value)
        {
            AddValue(name, (Object)value, typeof(ulong));
        }

        public void AddValue(String name, float value)
        {
            AddValue(name, (Object)value, typeof(float));
        }

        public void AddValue(String name, double value)
        {
            AddValue(name, (Object)value, typeof(double));
        }

        public void AddValue(String name, decimal value)
        {
            AddValue(name, (Object)value, typeof(decimal));
        }

        public void AddValue(String name, DateTime value)
        {
            AddValue(name, (Object)value, typeof(DateTime));
        }

        internal void AddValueInternal(String name, Object value, Type type)
        {
            if (m_nameToIndex.ContainsKey(name))
            {
                BCLDebug.Trace("SER", "[SerializationInfo.AddValue]Tried to add ", name, " twice to the SI.");
                throw new SerializationException(Environment.GetResourceString("Serialization_SameNameTwice"));
            }
            m_nameToIndex.Add(name, m_currMember);

            //
            // If we need to expand the arrays, do so.
            //
            if (m_currMember >= m_members.Length)
            {
                ExpandArrays();
            }

            //
            // Add the data and then advance the counter.
            //
            m_members[m_currMember] = name;
            m_data[m_currMember] = value;
            m_types[m_currMember] = type;
            m_currMember++;
        }

        /*=================================UpdateValue==================================
        **Action: Finds the value if it exists in the current data.  If it does, we replace
        **        the values, if not, we append it to the end.  This is useful to the 
        **        ObjectManager when it's performing fixups.
        **Returns: void
        **Arguments: name  -- the name of the data to be updated.
        **           value -- the new value.
        **           type  -- the type of the data being added.
        **Exceptions: None.  All error checking is done with asserts. Although public in coreclr,
        **            it's not exposed in a contract and is only meant to be used by corefx.
        ==============================================================================*/
#if FEATURE_CORECLR
        // This should not be used by clients: exposing out this functionality would allow children
        // to overwrite their parent's values. It is public in order to give corefx access to it for
        // its ObjectManager implementation, but it should not be exposed out of a contract.
        public
#else
        internal
#endif
        void UpdateValue(String name, Object value, Type type)
        {
            Contract.Assert(null != name, "[SerializationInfo.UpdateValue]name!=null");
            Contract.Assert(null != value, "[SerializationInfo.UpdateValue]value!=null");
            Contract.Assert(null != (object)type, "[SerializationInfo.UpdateValue]type!=null");

            int index = FindElement(name);
            if (index < 0)
            {
                AddValueInternal(name, value, type);
            }
            else
            {
                m_data[index] = value;
                m_types[index] = type;
            }

        }

        private int FindElement(String name)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }
            Contract.EndContractBlock();
            BCLDebug.Trace("SER", "[SerializationInfo.FindElement]Looking for ", name, " CurrMember is: ", m_currMember);
            int index;
            if (m_nameToIndex.TryGetValue(name, out index))
            {
                return index;
            }
            return -1;
        }

        /*==================================GetElement==================================
        **Action: Use FindElement to get the location of a particular member and then return
        **        the value of the element at that location.  The type of the member is
        **        returned in the foundType field.
        **Returns: The value of the element at the position associated with name.
        **Arguments: name -- the name of the element to find.
        **           foundType -- the type of the element associated with the given name.
        **Exceptions: None.  FindElement does null checking and throws for elements not 
        **            found.
        ==============================================================================*/
        private Object GetElement(String name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_NotFound", name));
            }

            Contract.Assert(index < m_data.Length, "[SerializationInfo.GetElement]index<m_data.Length");
            Contract.Assert(index < m_types.Length, "[SerializationInfo.GetElement]index<m_types.Length");

            foundType = m_types[index];
            Contract.Assert((object)foundType != null, "[SerializationInfo.GetElement]foundType!=null");
            return m_data[index];
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        private Object GetElementNoThrow(String name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                foundType = null;
                return null;
            }

            Contract.Assert(index < m_data.Length, "[SerializationInfo.GetElement]index<m_data.Length");
            Contract.Assert(index < m_types.Length, "[SerializationInfo.GetElement]index<m_types.Length");

            foundType = m_types[index];
            Contract.Assert((object)foundType != null, "[SerializationInfo.GetElement]foundType!=null");
            return m_data[index];
        }

        //
        // The user should call one of these getters to get the data back in the 
        // form requested.  
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Object GetValue(String name, Type type)
        {

            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            RuntimeType rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
#if FEATURE_REMOTING
            if (RemotingServices.IsTransparentProxy(value))
            {
                RealProxy proxy = RemotingServices.GetRealProxy(value);
                if (RemotingServices.ProxyCheckCast(proxy, rt))
                    return value;
            }
            else
#endif
                if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
                {
                    return value;
                }

            Contract.Assert(m_converter != null, "[SerializationInfo.GetValue]m_converter!=null");

            return m_converter.Convert(value, type);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        internal Object GetValueNoThrow(String name, Type type)
        {
            Type foundType;
            Object value;

            Contract.Assert((object)type != null, "[SerializationInfo.GetValue]type ==null");
            Contract.Assert(type is RuntimeType, "[SerializationInfo.GetValue]type is not a runtime type");

            value = GetElementNoThrow(name, out foundType);
            if (value == null)
                return null;
#if FEATURE_REMOTING
            if (RemotingServices.IsTransparentProxy(value))
            {
                RealProxy proxy = RemotingServices.GetRealProxy(value);
                if (RemotingServices.ProxyCheckCast(proxy, (RuntimeType)type))
                    return value;
            }
            else
#endif
                if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
                {
                    return value;
                }

            Contract.Assert(m_converter != null, "[SerializationInfo.GetValue]m_converter!=null");

            return m_converter.Convert(value, type);
        }

        public bool GetBoolean(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(bool)))
            {
                return (bool)value;
            }
            return m_converter.ToBoolean(value);
        }

        public char GetChar(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(char)))
            {
                return (char)value;
            }
            return m_converter.ToChar(value);
        }

        [CLSCompliant(false)]
        public sbyte GetSByte(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(sbyte)))
            {
                return (sbyte)value;
            }
            return m_converter.ToSByte(value);
        }

        public byte GetByte(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(byte)))
            {
                return (byte)value;
            }
            return m_converter.ToByte(value);
        }

        public short GetInt16(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(short)))
            {
                return (short)value;
            }
            return m_converter.ToInt16(value);
        }

        [CLSCompliant(false)]
        public ushort GetUInt16(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(ushort)))
            {
                return (ushort)value;
            }
            return m_converter.ToUInt16(value);
        }

        public int GetInt32(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(int)))
            {
                return (int)value;
            }
            return m_converter.ToInt32(value);
        }

        [CLSCompliant(false)]
        public uint GetUInt32(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(uint)))
            {
                return (uint)value;
            }
            return m_converter.ToUInt32(value);
        }

        public long GetInt64(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(long)))
            {
                return (long)value;
            }
            return m_converter.ToInt64(value);
        }

        [CLSCompliant(false)]
        public ulong GetUInt64(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(ulong)))
            {
                return (ulong)value;
            }
            return m_converter.ToUInt64(value);
        }

        public float GetSingle(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(float)))
            {
                return (float)value;
            }
            return m_converter.ToSingle(value);
        }


        public double GetDouble(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(double)))
            {
                return (double)value;
            }
            return m_converter.ToDouble(value);
        }

        public decimal GetDecimal(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(decimal)))
            {
                return (decimal)value;
            }
            return m_converter.ToDecimal(value);
        }

        public DateTime GetDateTime(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(DateTime)))
            {
                return (DateTime)value;
            }
            return m_converter.ToDateTime(value);
        }

        public String GetString(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(String)) || value == null)
            {
                return (String)value;
            }
            return m_converter.ToString(value);
        }

        internal string[] MemberNames
        {
            get
            {
                return m_members;
            }

        }

        internal object[] MemberValues
        {
            get
            {
                return m_data;
            }
        }

    }
}
