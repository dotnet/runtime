// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Linq;

using Xunit;

namespace XLinqTests
{
    public class XElementChangedNotificationTests
    {
        public class SkipNotifyTests
        {
            [Fact]
            public void AddEventXElementShouldBeNotified()
            {
                var el = new XElement("test");
                bool changedNotification = false;
                var handler = new EventHandler<XObjectChangeEventArgs>(
                    (sender, cea) =>
                    {
                        changedNotification = true;
                    }
                );
                el.Changed += handler;
                var child = new XElement("test2");
                el.Add(child);
                Assert.True(changedNotification);
                changedNotification = false;
                child.Add(new XAttribute("a", "b"));
                Assert.True(changedNotification);
            }

            [Fact]
            public void AddRemoveEventXElementShouldNotBeNotified()
            {
                var el = new XElement("test");
                bool changedNotification = false;
                var handler = new EventHandler<XObjectChangeEventArgs>(
                    (sender, cea) =>
                    {
                        changedNotification = true;
                    }
                );
                el.Changed += handler;
                el.Changed -= handler;
                var child = new XElement("test2");
                el.Add(child);
                Assert.False(changedNotification);
                changedNotification = false;
                child.Add(new XAttribute("a", "b"));
                Assert.False(changedNotification);
            }

            [Fact]
            public void AddEventXElementShouldBeNotifiedWithAnnotations()
            {
                var el = new XElement("test");
                bool changedNotification = false;
                var handler = new EventHandler<XObjectChangeEventArgs>(
                    (sender, cea) =>
                    {
                        changedNotification = true;
                    }
                );

                el.AddAnnotation(new object());
                el.Changed += handler;
                var child = new XElement("test2");
                el.Add(child);
                Assert.True(changedNotification);
                changedNotification = false;
                child.Add(new XAttribute("a", "b"));
                Assert.True(changedNotification);
            }

            [Fact]
            public void AddRemoveEventXElementShouldNotBeNotifiedWithAnnotations()
            {
                var el = new XElement("test");
                bool changedNotification = false;
                var handler = new EventHandler<XObjectChangeEventArgs>(
                    (sender, cea) =>
                    {
                        changedNotification = true;
                    }
                );

                el.AddAnnotation(new object());
                el.Changed += handler;
                el.Changed -= handler;
                var child = new XElement("test2");
                el.Add(child);
                Assert.False(changedNotification);
                changedNotification = false;
                child.Add(new XAttribute("a", "b"));
                Assert.False(changedNotification);
            }
        }
    }
}
