// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public static class EventInfoTests
    {
        public static IEnumerable<object[]> Equality_TestData()
        {
            yield return new object[] { GetEventInfo(typeof(BaseClass), "EventPublic"), GetEventInfo(typeof(BaseClass), "EventPublic"), true };
            yield return new object[] { GetEventInfo(typeof(BaseClass), "EventPublic"), GetEventInfo(typeof(SubClass), "EventPublic"), false };
            yield return new object[] { GetEventInfo(typeof(BaseClass), "EventPublic"), GetEventInfo(typeof(BaseClass), "EventPublicStatic"), false };
        }
        
        [Theory]
        [MemberData(nameof(Equality_TestData))]
        public static void Test_Equality(EventInfo constructorInfo1, EventInfo constructorInfo2, bool expected)
        {
            Assert.Equal(expected, constructorInfo1 == constructorInfo2);
            Assert.NotEqual(expected, constructorInfo1 != constructorInfo2);
        }

        //private helper methods
        private static EventInfo GetEventInfo(Type t, string eventName)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<EventInfo> allevents = ti.DeclaredEvents.GetEnumerator();
            EventInfo eventFound = null;
            while (allevents.MoveNext())
            {
                if (eventName.Equals(allevents.Current.Name))
                {
                    eventFound = allevents.Current;
                    break;
                }
            }
            return eventFound;
        }
    }

#pragma warning disable 0067  // the event was declared but never used
    // Metadata for Reflection
    public class BaseClass
    {
        public event EventHandler EventPublic;					// inherited
        public static event EventHandler EventPublicStatic;
    }

    public class SubClass : BaseClass
    {
        public new event EventHandler EventPublic;	//overrides event
    }
#pragma warning restore 0067
}