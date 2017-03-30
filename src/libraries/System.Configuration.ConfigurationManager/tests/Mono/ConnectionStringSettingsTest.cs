// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.ConnectionStringSettingsTest.cs - Unit tests
// for System.Configuration.ConnectionStringSettings
//
// Author:
//	Chris Toshok  <toshok@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Configuration;
using Xunit;

namespace MonoTests.System.Configuration
{
    public class ConnectionStringSettingsTest
    {
        [Fact]
        public void Defaults()
        {
            ConnectionStringSettings s;

            s = new ConnectionStringSettings();

            Assert.Equal(null, s.Name);
            Assert.Equal("", s.ProviderName);
            Assert.Equal("", s.ConnectionString);

            s = new ConnectionStringSettings("name", "connectionString");
            Assert.Equal("name", s.Name);
            Assert.Equal("", s.ProviderName);
            Assert.Equal("connectionString", s.ConnectionString);

            s = new ConnectionStringSettings("name", "connectionString", "provider");
            Assert.Equal("name", s.Name);
            Assert.Equal("provider", s.ProviderName);
            Assert.Equal("connectionString", s.ConnectionString);
        }

        [Fact]
        public void NameNull()
        {
            ConnectionStringSettings s;

            s = new ConnectionStringSettings("name", "connectionString", "provider");
            Assert.Equal("name", s.Name);
            s.Name = null;
            Assert.Null(s.Name);
        }

        [Fact]
        // This test fails on Mono
        // [Category("NotWorking")]
        public void Validators_Name1()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            Assert.Throws<ConfigurationErrorsException>(() => s.Name = "");
        }

        [Fact]
        public void Validators_Name2()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            s.Name = null;
        }

        [Fact]
        public void Validators_ProviderName1()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            s.ProviderName = "";
        }

        [Fact]
        public void Validators_ProviderName2()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            s.ProviderName = null;
        }

        [Fact]
        public void Validators_ConnectionString1()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            s.ConnectionString = "";
        }

        [Fact]
        public void Validators_ConnectionString2()
        {
            ConnectionStringSettings s = new ConnectionStringSettings();
            s.ConnectionString = null;
        }

        [Fact]
        public void ToStringTest()
        {
            ConnectionStringSettings s = new ConnectionStringSettings(
                "name", "connectionString", "provider");
            Assert.Equal("connectionString", s.ToString());
        }
    }
}

