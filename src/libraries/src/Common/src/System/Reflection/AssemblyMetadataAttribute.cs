// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    internal sealed class AssemblyMetadataAttribute : Attribute 
    {
        private String m_key;
        private String m_value;
		
        public AssemblyMetadataAttribute(string key, string value) 
        {
            m_key = key;
            m_value = value;
        }
        
        public string Key
        {
            get { return m_key; }
        }
		
        public string Value
        {
            get { return m_value;}
        }
    }
}