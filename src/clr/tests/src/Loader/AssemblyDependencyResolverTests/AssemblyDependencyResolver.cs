// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

namespace AssemblyDependencyResolverTests
{
    /// <summary>
    /// Temporary until the actual public API gets propagated through CoreFX.
    /// </summary>
    public class AssemblyDependencyResolver
    {
        private object _implementation;
        private Type _implementationType;
        private MethodInfo _resolveAssemblyPathInfo;
        private MethodInfo _resolveUnmanagedDllPathInfo;

        public AssemblyDependencyResolver(string componentAssemblyPath)
        {
            _implementationType = typeof(object).Assembly.GetType("System.Runtime.Loader.AssemblyDependencyResolver");
            _resolveAssemblyPathInfo = _implementationType.GetMethod("ResolveAssemblyToPath");
            _resolveUnmanagedDllPathInfo = _implementationType.GetMethod("ResolveUnmanagedDllToPath");

            try
            {
                _implementation = Activator.CreateInstance(_implementationType, componentAssemblyPath);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public string ResolveAssemblyToPath(AssemblyName assemblyName)
        {
            try
            {
                return (string)_resolveAssemblyPathInfo.Invoke(_implementation, new object[] { assemblyName });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public string ResolveUnmanagedDllToPath(string unmanagedDllName)
        {
            try
            {
                return (string)_resolveUnmanagedDllPathInfo.Invoke(_implementation, new object[] { unmanagedDllName });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }
    }
}
