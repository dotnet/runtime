// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// AlternateViewCollectionTest.cs - NUnit Test Cases for System.Net.MailAddress.AlternateViewCollection
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using Xunit;
using System.Net.Mime;

namespace System.Net.Mail.Tests
{
    public class AlternateViewCollectionTest
    {
        AlternateViewCollection avc;
        AlternateView av;

        public AlternateViewCollectionTest()
        {
            avc = new MailMessage("foo@bar.com", "foo@bar.com").AlternateViews;
            av = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
        }

        [Fact]
        public void TestDispose()
        {
            avc.Add(av);
            avc.Dispose();

            // The individual items also disposed
            Assert.Throws<ObjectDisposedException>(() => av.LinkedResources);

            AlternateView av1 = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/plain"));
            Assert.Throws<ObjectDisposedException>(() => avc.Add(av1));
            Assert.Throws<ObjectDisposedException>(() => avc.Clear());
            
            // No throw on second dispose
            avc.Dispose();
        }

        [Fact]
        public void InitialCount()
        {
            Assert.True(avc.Count == 0);
        }

        [Fact]
        public void AddCount()
        {
            avc.Add(av);
            Assert.True(avc.Count == 1);
        }

        [Fact]
        public void RemoveCount()
        {
            avc.Remove(av);
            Assert.True(avc.Count == 0);
        }
    }
}