// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;

namespace System.Runtime.Loader.Tests
{
    public interface IContextualReflectionTestFixture
    {
        AssemblyLoadContext isolatedAlc { get; }
        Assembly isolatedAlcAssembly { get; }
        Type isolatedAlcFixtureType { get; }
        IContextualReflectionTestFixture isolatedAlcFixtureInstance { get; }

        AssemblyLoadContext defaultAlc { get; }
        Assembly defaultAlcAssembly { get; }
        Type defaultAlcFixtureType { get; }

        void SetPreConditions();
        void FixtureSetupAssertions();

        Assembly GetExecutingAssembly();

        Assembly AssemblyLoad(AssemblyName name);

        Assembly AssemblyLoad(string name);

        Assembly AssemblyLoadWithPartialName(string name);

        Type TypeGetType(string typeName);

        Type TypeGetType(string typeName, bool throwOnError);

        Type TypeGetType(string typeName, bool throwOnError, bool ignoreCase);

        Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver);

        Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver, bool throwOnError);

        Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver, bool throwOnError, bool ignoreCase);

        Type AssemblyGetType(Assembly assembly, string typeName);

        Type AssemblyGetType(Assembly assembly, string typeName, bool throwOnError);

        Type AssemblyGetType(Assembly assembly, string typeName, bool throwOnError, bool ignoreCase);

        ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName);

        ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName, object[] activationAttributes);

        ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes);
    }
}
