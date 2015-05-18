// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodTests
    {
        //Verify ConstructorInfo.Invoke return a new Object
        [Fact]
        public void TestEquals1()
        {
            ConstructorInfo[] cis1 = GetConstructor(typeof(ConstructorInfoMethodSample));
            ConstructorInfo[] cis2 = GetConstructor(typeof(ConstructorInfoMethodSample));
            Assert.Equal(cis1[0], cis2[0]);
            Assert.Equal(cis1[1], cis2[1]);
            Assert.Equal(cis1[2], cis2[2]);
        }

        //Verify ConstructorInfo.Invoke return a new Object
        [Fact]
        public void TestEquals2()
        {
            ConstructorInfo[] cis1 = GetConstructor(typeof(ConstructorInfoMethodSample));
            ConstructorInfo[] cis2 = GetConstructor(typeof(ConstructorInfoClassB));
            Assert.NotEqual(cis1[0], cis2[0]);
        }

        //Verify ConstructorInfo.Invoke return a new Object
        [Fact]
        public void TestGetHashCode()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoMethodSample));
            int hcode = cis[0].GetHashCode();
            Assert.NotEqual(hcode, 0);
        }

        //Gets ConstructorInfo object from a Type
        public static ConstructorInfo[] GetConstructor(Type t)
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

    //Metadata for Reflection
    public class ConstructorInfoMethodSample
    {
        public int intValue = 0;
        public string strValue = "";

        public ConstructorInfoMethodSample()
        {
        }

        public ConstructorInfoMethodSample(int i)
        {
            this.intValue = i;
        }

        public ConstructorInfoMethodSample(int i, string s)
        {
            this.intValue = i;
            this.strValue = s;
        }

        public string Method1(DateTime t)
        {
            return "";
        }
    }

    public class ConstructorInfoClassB
    {
        static ConstructorInfoClassB()
        {
        }
    }
}
