// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security;

namespace System.Xaml.Permissions
{
    public class XamlAccessLevel
    {
        private XamlAccessLevel(string assemblyName, string? typeName)
        {
            AssemblyNameString = assemblyName;
            PrivateAccessToTypeName = typeName;
        }

        public static XamlAccessLevel AssemblyAccessTo(Assembly assembly)
        {
            return new XamlAccessLevel(assembly.FullName!, null);
        }

        public static XamlAccessLevel AssemblyAccessTo(AssemblyName assemblyName)
        {
            return new XamlAccessLevel(assemblyName.FullName, null);
        }

        public static XamlAccessLevel PrivateAccessTo(Type type)
        {
            return new XamlAccessLevel(type.Assembly.FullName!, type.FullName);
        }

        public static XamlAccessLevel PrivateAccessTo(string assemblyQualifiedTypeName)
        {
            int nameBoundary = assemblyQualifiedTypeName.IndexOf(',');
            string typeName = assemblyQualifiedTypeName.AsSpan(0, nameBoundary).Trim().ToString();
            string assemblyFullName = assemblyQualifiedTypeName.AsSpan(nameBoundary + 1).Trim().ToString();
            AssemblyName assemblyName = new AssemblyName(assemblyFullName);
            return new XamlAccessLevel(assemblyName.FullName, typeName);
        }

        public AssemblyName AssemblyAccessToAssemblyName
        {
            get { return new AssemblyName(AssemblyNameString); }
        }

        public string? PrivateAccessToTypeName { get; private set; }

        internal string AssemblyNameString { get; private set; }
    }
}
