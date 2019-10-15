// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security;

namespace System.Xaml.Permissions
{
    public class XamlAccessLevel
    {
        private XamlAccessLevel(string assemblyName, string typeName)
        {
            AssemblyNameString = assemblyName;
            PrivateAccessToTypeName = typeName;
        }

        public static XamlAccessLevel AssemblyAccessTo(Assembly assembly)
        {
            return new XamlAccessLevel(assembly.FullName, null);
        }

        public static XamlAccessLevel AssemblyAccessTo(AssemblyName assemblyName)
        {
            return new XamlAccessLevel(assemblyName.FullName, null);
        }

        public static XamlAccessLevel PrivateAccessTo(Type type)
        {
            return new XamlAccessLevel(type.Assembly.FullName, type.FullName);
        }

        public static XamlAccessLevel PrivateAccessTo(string assemblyQualifiedTypeName)
        {
            int nameBoundary = assemblyQualifiedTypeName.IndexOf(',');
            string typeName = assemblyQualifiedTypeName.Substring(0, nameBoundary).Trim();
            string assemblyFullName = assemblyQualifiedTypeName.Substring(nameBoundary + 1).Trim();
            AssemblyName assemblyName = new AssemblyName(assemblyFullName);
            return new XamlAccessLevel(assemblyName.FullName, typeName);
        }

        public AssemblyName AssemblyAccessToAssemblyName
        {
            get { return new AssemblyName(AssemblyNameString); }
        }

        public string PrivateAccessToTypeName { get; private set; }

        internal string AssemblyNameString { get; private set; }
    }
}
