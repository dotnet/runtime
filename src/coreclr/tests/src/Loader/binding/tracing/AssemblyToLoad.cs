// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace AssemblyToLoad
{
    public class Program
    {
        public static System.Reflection.Assembly UseDependentAssembly()
        {
            var p = new AssemblyToLoadDependency.Program();
            return System.Reflection.Assembly.GetAssembly(p.GetType());
        }
    }
}
