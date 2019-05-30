// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Mail;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class HeaderCollectionTest
    {
        private MailMessage mail = new MailMessage();
        
        [Fact]
        public void Set_ValidNameAndValue_Success()
        {
          string validName = "foo";
          string validValue = "bar";
          mail.Headers.Set(validName, validValue);
          Assert.Equal(validValue, mail.Headers.Get(validName));
          mail.Headers.Remove(validName);
        }
        
        [Fact]
        public void Set_EmptyName_Throws()
        {
          AssertExtensions.Throws<ArgumentException>("name", () => mail.Headers.Set(string.Empty, "value"));
        }
        
        [Fact]
        public void Set_EmptyValue_Throws()
        {
          AssertExtensions.Throws<ArgumentException>("value", () => mail.Headers.Set("name", string.Empty));
        }
        
        [Fact]
        public void Add_ValidNameAndValue_Success()
        {
          string validName = "foo";
          string validValue = "bar";
          mail.Headers.Add(validName, validValue);
          Assert.Equal(validValue, mail.Headers.Get(validName));
          mail.Headers.Remove(validName);
        }
        
        [Fact]
        public void Add_EmptyName_Throws()
        {
          AssertExtensions.Throws<ArgumentException>("name", () => mail.Headers.Add(string.Empty, "value"));
        }
        
        [Fact]
        public void Add_EmptyValue_Throws()
        {
          AssertExtensions.Throws<ArgumentException>("value", () => mail.Headers.Add("name", string.Empty));
        }
    }
}
