// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0067

namespace System.Reflection.Tests
{
    public class EventInfoMethodTests
    {
        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void VerifyAddRemoveEventHandler1()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            ei.AddEventHandler(obj, myhandler);
            //Try to remove event Handler and Verify that no exception is thrown.
            ei.RemoveEventHandler(obj, myhandler);
        }

        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void VerifyAddRemoveEventHandler2()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublicStatic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            ei.AddEventHandler(null, myhandler);
            //Try to remove event Handler and Verify that no exception is thrown.
            ei.RemoveEventHandler(null, myhandler);
        }

        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void VerifyAddRemoveEventHandler3()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublicVirtual");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            ei.AddEventHandler(obj, myhandler);
            //Try to remove event Handler and Verify that no exception is thrown.
            ei.RemoveEventHandler(obj, myhandler);
        }

        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void VerifyAddRemoveEventHandler4()
        {
            EventInfo ei = GetEventInfo(typeof(SubClass), "EventPublicNew");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            SubClass obj = new SubClass();
            ei.AddEventHandler(obj, myhandler);
            //Try to remove event Handler and Verify that no exception is thrown.
            ei.RemoveEventHandler(obj, myhandler);
        }

        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void VerifyAddRemoveEventHandler5()
        {
            EventInfo ei = GetEventInfo(typeof(SubClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            SubClass obj = new SubClass();
            ei.AddEventHandler(obj, myhandler);
            //Try to remove event Handler and Verify that no exception is thrown.
            ei.RemoveEventHandler(obj, myhandler);
        }

        //Negative Tests
        // Target null for AddEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler6()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            //Target is null and event is not static
            // Win8P throws generic Exception         
            try
            {
                ei.AddEventHandler(null, myhandler);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        // Target null for RemoveEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler7()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            ei.AddEventHandler(obj, myhandler);
            //Target is null and event is not static
            // Win8P throws generic Exception
            try
            {
                ei.RemoveEventHandler(null, myhandler);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        // EventHandler null for AddEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler8()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            SomeDelegate d1 = new SomeDelegate(SomeHandler);
            //Handler is not correct.          
            Assert.Throws<ArgumentException>(() =>
            {
                ei.AddEventHandler(obj, d1);
            });
        }

        // EventHandler null for RemoveEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler9()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            ei.AddEventHandler(obj, myhandler);
            SomeDelegate d1 = new SomeDelegate(SomeHandler);

            //Handler is not correct.          
            // Win8P throws generic Exception         
            Assert.Throws<ArgumentException>(() =>
            {
                ei.RemoveEventHandler(obj, d1);
            });
        }

        // Wrong Target for AddEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler10()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            String obj = "hello";
            //Target is wrong. 
            // Win8P throws generic Exception 
            try
            {
                ei.AddEventHandler(obj, myhandler);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        // Target null for RemoveEventHandler 
        [Fact]
        public void VerifyAddRemoveEventHandler11()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            EventHandler myhandler = new EventHandler(MyEventHandler);
            BaseClass obj = new BaseClass();
            ei.AddEventHandler(obj, myhandler);
            //Target is wrong.
            // Win8P throws generic Exception
            try
            {
                ei.RemoveEventHandler((Object)"hello", myhandler);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        // Test for Equals Method
        [Fact]
        public void VerifyEqualsMethod1()
        {
            EventInfo ei1 = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei1);
            EventInfo ei2 = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei2);
            Assert.Equal(ei1, ei2);
        }

        // Test for Equals Method
        [Fact]
        public void VerifyEqualsMethod2()
        {
            EventInfo ei1 = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei1);
            EventInfo ei2 = GetEventInfo(typeof(SubClass), "EventPublic");
            Assert.NotNull(ei2);
            Assert.NotEqual(ei1, ei2);
        }

        // Test for Equals Method
        [Fact]
        public void VerifyEqualsMethod3()
        {
            EventInfo ei1 = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei1);
            EventInfo ei2 = GetEventInfo(typeof(BaseClass), "EventPublicStatic");
            Assert.NotNull(ei2);
            Assert.NotEqual(ei1, ei2);
        }

        // Test for GetHashCode Method
        [Fact]
        public void VerifyGetHashCode()
        {
            EventInfo ei = GetEventInfo(typeof(BaseClass), "EventPublic");
            Assert.NotNull(ei);
            int hcode = ei.GetHashCode();
            Assert.False(hcode <= 0);
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

        //Event Handler for Testing
        private static void MyEventHandler(Object o, EventArgs e)
        {
        }

        //Event Handler for Testing
        private static void SomeHandler(Object o)
        {
        }

        public delegate void SomeDelegate(Object o);
    }

    // Metadata for Reflection
    public class BaseClass
    {
        public event EventHandler EventPublic;					// inherited
        public static event EventHandler EventPublicStatic;
        public virtual event EventHandler EventPublicVirtual;
    }

    public class SubClass : BaseClass
    {
        public new event EventHandler EventPublic;	//overrides event				
        public event EventHandler EventPublicNew;  // new event
    }
}
