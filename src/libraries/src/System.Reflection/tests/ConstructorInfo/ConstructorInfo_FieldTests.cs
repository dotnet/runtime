// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class ConstructorFieldTests
    {
        //Verify Ctors 
        [Fact]
        public void TestConstructorName()
        {
            ConstructorInfo[] cis = getConstructor(typeof(ConstructorInfoFieldSample));
            Assert.Equal(cis.Length, 1);
            Assert.True(cis[0].Name.Equals(ConstructorInfo.ConstructorName));
        }

        //Verify IsConstructor returns True 
        [Fact]
        public void TestIsConstructor()
        {
            ConstructorInfo[] cis = getConstructor(typeof(ConstructorInfoFieldSample));
            Assert.Equal(cis.Length, 1);
            Assert.True(cis[0].IsConstructor);
        }

        //Verify IsConstructor returns True 
        [Fact]
        public void TestIsPublic()
        {
            ConstructorInfo[] cis = getConstructor(typeof(ConstructorInfoFieldSample));
            Assert.Equal(cis.Length, 1);
            Assert.True(cis[0].IsPublic);
        }

        //Gets ConstructorInfo object from a Type
        public static ConstructorInfo[] getConstructor(Type t)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<ConstructorInfo> allctors = ti.DeclaredConstructors.GetEnumerator();
            List<ConstructorInfo> clist = new List<ConstructorInfo>();
            while (allctors.MoveNext())
            {
                clist.Add(allctors.Current);
            }

            return clist.ToArray();
        }
    }

    public class ConstructorInfoFieldSample
    {
        public ConstructorInfoFieldSample() { }

        public string Method1(DateTime t)
        {
            return "";
        }
    }
}
