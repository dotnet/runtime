// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Diagnostics.Contracts;
using System.Security;

namespace System.Runtime.Serialization
{
    [System.Runtime.InteropServices.ComVisible(true)]
    internal sealed class SerializationInfo
    {
        private const int defaultSize = 4;
        private const string s_mscorlibAssemblySimpleName = "mscorlib";
        private const string s_mscorlibFileName = s_mscorlibAssemblySimpleName + ".dll";

        // Even though we have a dictionary, we're still keeping all the arrays around for back-compat. 
        // Otherwise we may run into potentially breaking behaviors like GetEnumerator() not returning entries in the same order they were added.
        internal String[] m_members;
        internal Object[] m_data;
        internal Type[] m_types;
        private Dictionary<string, int> _nameToIndex;
        internal int m_currMember;

        private void ExpandArrays()
        {
            int newSize;
            Contract.Assert(m_members.Length == m_currMember,
                "[SerializationInfo.ExpandArrays]m_members.Length == m_currMember");

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

        public void AddValue(String name, int value)
        {
            AddValue(name, (Object)value, typeof(int));
        }

        internal void AddValueInternal(String name, Object value, Type type)
        {
            if (_nameToIndex.ContainsKey(name))
            {
                //BCLDebug.Trace("SER", "[SerializationInfo.AddValue]Tried to add ", name, " twice to the SI.");
                //throw new SerializationException(Environment.GetResourceString("Serialization_SameNameTwice"));
                throw new Exception("This is supposed to be SerializationException");
            }
            _nameToIndex.Add(name, m_currMember);

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

        private int FindElement(String name)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }
            Contract.EndContractBlock();
            //BCLDebug.Trace("SER", "[SerializationInfo.FindElement]Looking for ", name, " CurrMember is: ", m_currMember);
            int index;
            if (_nameToIndex.TryGetValue(name, out index))
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
                //throw new SerializationException(Environment.GetResourceString("Serialization_NotFound", name));
                throw new Exception("This is supposed to be Serialization Exception");
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

        [System.Security.SecuritySafeCritical] // auto-generated
        public Object GetValue(String name, Type type)
        {
            return null;
            /*if ((object) type == null)
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

            if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
            {
                return value;
            }

            Contract.Assert(m_converter != null, "[SerializationInfo.GetValue]m_converter!=null");

            return m_converter.Convert(value, type);*/
        }

        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(m_members, m_data, m_types, m_currMember);
        }
    }
}
