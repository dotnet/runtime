// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// LinkedResourceCollectionTest.cs - NUnit Test Cases for System.Net.MailAddress.LinkedResourceCollection
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using System.Net.Mime;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class LinkedResourceCollectionTest
    {
        LinkedResourceCollection lrc;
        LinkedResource lr;

        public LinkedResourceCollectionTest()
        {
            lrc = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain")).LinkedResources;
            lr = LinkedResource.CreateLinkedResourceFromString("test", new ContentType("text/plain"));
        }

        [Fact]
        public void InitialCount()
        {
            Assert.Equal(0, lrc.Count);
        }

        [Fact]
        public void AddCount()
        {
            lrc.Add(lr);
            Assert.Equal(1, lrc.Count);
        }

        [Fact]
        public void RemoveCount()
        {
            lrc.Remove(lr);
            Assert.Equal(0, lrc.Count);
        }
    }
}
