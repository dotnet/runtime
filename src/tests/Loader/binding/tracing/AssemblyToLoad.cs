// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ASSEMBLY_V2
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]
#else
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
#endif

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
