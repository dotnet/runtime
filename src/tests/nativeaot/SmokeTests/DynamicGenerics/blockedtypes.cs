// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using RuntimeLibrariesTest;
using TypeOfRepo;

namespace System.Runtime.CompilerServices
{
    internal class __BlockReflectionAttribute : Attribute { }
}


public class BlockedTypesTests
{
    public class My { }

    [System.Runtime.CompilerServices.__BlockReflection]
    private class BlockedGenericType<T>
    {
        public string ArgumentName
        {
            get { return typeof(T).Name; }
        }
    }

    public class GenericType<T>
    {
        public string FetchArgumentName()
        {
            var blocked = new BlockedGenericType<T>();
            return blocked.ArgumentName;
        }
    }

    [TestMethod]
    public static void TestBlockedTypes()
    {
        {
            var t = TypeOf.BTT_GenericType.MakeGenericType(typeof(My));
            var o = Activator.CreateInstance(t);
            var m = t.GetTypeInfo().GetDeclaredMethod("FetchArgumentName");
            var result = (string)m.Invoke(o, null);
            Assert.AreEqual("My", result);
        }
    }
}
