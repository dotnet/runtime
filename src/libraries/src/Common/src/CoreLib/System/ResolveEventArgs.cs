// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System
{
    public class ResolveEventArgs : EventArgs
    {
        public ResolveEventArgs(string name)
        {
            Name = name;
        }

        public ResolveEventArgs(string name, Assembly requestingAssembly)
        {
            Name = name;
            RequestingAssembly = requestingAssembly;
        }

        public string Name { get; }
        public Assembly RequestingAssembly { get; }
    }
}
