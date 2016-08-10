// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace System
{
    [ComVisible(true)]
    internal class ResolveEventArgs : EventArgs
    {
    }


    [ComVisible(true)]
    internal delegate Assembly ResolveEventHandler(Object sender, ResolveEventArgs args);

    internal class AppDomain
    {
        public static AppDomain CurrentDomain
        {
            get { return null; }
        }

        public Assembly[] GetAssemblies()
        {
            return null;
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess acess)
        {
            return null;
        }

        [System.Security.SecurityCritical]
        private ResolveEventHandler _AssemblyResolve;

        public event ResolveEventHandler AssemblyResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _AssemblyResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _AssemblyResolve -= value;
                }
            }
        }
    }
}
