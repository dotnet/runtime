// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// MailAddressCollectionTest.cs - NUnit Test Cases for System.Net.MailAddress.MailAddressCollection
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
    public class MailAddressCollectionTest
    {
        MailAddressCollection ac;
        MailAddress a;

        public MailAddressCollectionTest()
        {
            ac = new MailAddressCollection();
            a = new MailAddress("foo@bar.com");
        }

        [Fact]
        public void InitialCount()
        {
            Assert.Equal(0, ac.Count );
        }

        [Fact]
        public void AddCount()
        {
            ac.Add(a);
            Assert.Equal(1, ac.Count);
        }

        [Fact]
        public void RemoveCount()
        {
            ac.Remove(a);
            Assert.Equal(0, ac.Count);
        }
    }
}
