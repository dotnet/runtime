// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
using System;
using System.Reflection;
using System.IO;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Globalization;

class RuntimeHelperTest 
{
    public static int Main(string[] args)
    {
        AssemblyLoadContext resolver0 = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        Assembly asm0 = resolver0.LoadFromAssemblyName(new AssemblyName("moduleCctor"));
        Module mod = asm0.ManifestModule;
        
        RuntimeHelpers.RunModuleConstructor(mod.ModuleHandle);
        var oType   = asm0.GetType("IntHolder",true);
        MethodInfo check = oType.GetMethod("Check");
        MethodInfo assign = oType.GetMethod("Assign");

        object[] initial = {1};
        object[] final   = {100};

        check.Invoke(null, initial);    
        assign.Invoke(null, final);    
        check.Invoke(null, final);    
        RuntimeHelpers.RunModuleConstructor(mod.ModuleHandle);
        check.Invoke(null, final);    

            
        return 100;

    }
}
