// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0067

namespace System.Reflection.Tests
{
    // System.Threading Reference is needed to create events for Reflection Testing

    public class TypeInfoDeclaredEventTests
    {
        // Verify Declared events for a Base class
        [Fact]
        public static void TestBaseClassEvents1()
        {
            VerifyEvent(typeof(TypeInfoEventBaseClass), "EventPublic", true);
        }

        // Verify Declared events for a Base class
        [Fact]
        public static void TestBaseClassEvents2()
        {
            VerifyEvent(typeof(TypeInfoEventBaseClass), "EventPublicStatic", true);
        }

        // Verify Declared events for a Base class
        [Fact]
        public static void TestBaseClassEvents3()
        {
            VerifyEvent(typeof(TypeInfoEventBaseClass), "NoSuchEvent", false);
        }


        // Verify Declared events for a Derived class
        [Fact]
        public static void TestDerivedClassEvents1()
        {
            VerifyEvent(typeof(TypeInfoEventSubClass), "EventPublicNew", true);
        }


        // Verify Declared events for a Derived class
        [Fact]
        public static void TestDerivedClassEvents2()
        {
            VerifyEvent(typeof(TypeInfoEventSubClass), "EventPublic", true);
        }


        // Verify Declared events for a Derived class
        [Fact]
        public static void TestDerivedClassEvents3()
        {
            VerifyEvent(typeof(TypeInfoEventSubClass), "EventPublicStatic", false);
        }

        //private helper methods
        private static void VerifyEvent(Type t, string eventName, bool expected)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<EventInfo> allevents = ti.DeclaredEvents.GetEnumerator();
            bool found = false;
            while (allevents.MoveNext())
            {
                if (eventName.Equals(allevents.Current.Name))
                {
                    found = true;
                }
            }

            if (expected)
                Assert.True(found, String.Format("Failed!! to find event {0} in type {1}", eventName, t));
            else
                Assert.False(found, String.Format("Failed!! found an unexpected event {0} in type {1}", eventName, t));
        }
    }

    //Metadata for Reflection
    public class TypeInfoEventBaseClass
    {
        public event EventHandler EventPublic;					// inherited
        public static event EventHandler EventPublicStatic;
    }

    public class TypeInfoEventSubClass : TypeInfoEventBaseClass
    {
        public new event EventHandler EventPublic;	//overrides event				
        public event EventHandler EventPublicNew;  // new event
    }
}
