// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// ConfigurationSectionGroupTest.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using Config = System.Configuration.Configuration;

namespace MonoTests.System.Configuration
{
    public class ConfigurationSectionGroupTest
    {
        [Fact]
        public void EditBeforeAdd()
        {
            UserSettingsGroup u = new UserSettingsGroup();
            ClientSettingsSection c = new ClientSettingsSection();
            Assert.Throws<InvalidOperationException>(() => u.Sections.Add("mine", c));
        }

        [Fact]
        [ActiveIssue(21000, TargetFrameworkMonikers.UapAot)]
        public void EditAfterAdd()
        {
            Config cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            UserSettingsGroup u = new UserSettingsGroup();
            cfg.SectionGroups.Add("userSettings", u);
            ClientSettingsSection c = new ClientSettingsSection();
            u.Sections.Add("mine", c);
        }
    }
}

