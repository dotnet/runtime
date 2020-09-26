// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

using LoaderLinkTest.Shared;

namespace LoaderLinkTest
{
    public class LoaderLinkTest
    {
        [Fact]
        public static void EnsureTypesLinked() // https://github.com/dotnet/runtime/issues/42207
        {
            string parentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            byte[] bytes = File.ReadAllBytes(Path.Combine(parentDir != null ? parentDir : "/", "LoaderLinkTest.Dynamic.dll"));
            Assembly asm = Assembly.Load(bytes);
            var dynamicType = asm.GetType("LoaderLinkTest.Dynamic.SharedInterfaceImplementation", true);
            var sharedInterface = dynamicType.GetInterfaces().First(e => e.Name == nameof(ISharedInterface));
            Assert.Equal(typeof(ISharedInterface).Assembly, sharedInterface.Assembly);
            Assert.Equal(typeof(ISharedInterface), sharedInterface);

            var instance = Activator.CreateInstance(dynamicType);
            Assert.True(instance is ISharedInterface);

            Assert.NotNull(((ISharedInterface)instance).TestString); // cast should not fail
        }
    }
}
