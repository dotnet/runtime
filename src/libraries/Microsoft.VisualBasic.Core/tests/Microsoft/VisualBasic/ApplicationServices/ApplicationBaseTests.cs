// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.ApplicationServices.Tests
{
    public class ApplicationBaseTests
    {
        [Fact]
        public void Culture()
        {
            var app = new ApplicationBase();
            var culture = app.Culture;
            Assert.Equal(System.Threading.Thread.CurrentThread.CurrentCulture, culture);
            try
            {
                app.ChangeCulture("en-US");
                Assert.Equal(System.Threading.Thread.CurrentThread.CurrentCulture, app.Culture);
                Assert.Equal("en-US", app.Culture.Name);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Fact]
        public void UICulture()
        {
            var app = new ApplicationBase();
            var culture = app.UICulture;
            Assert.Equal(System.Threading.Thread.CurrentThread.CurrentUICulture, culture);
            try
            {
                app.ChangeUICulture("en-US");
                Assert.Equal(System.Threading.Thread.CurrentThread.CurrentUICulture, app.UICulture);
                Assert.Equal("en-US", app.UICulture.Name);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            }
        }

        [Fact]
        public void GetEnvironmentVariable()
        {
            var app = new ApplicationBase();
            var variables = System.Environment.GetEnvironmentVariables().Keys;
            foreach (string variable in variables)
            {
                Assert.Equal(System.Environment.GetEnvironmentVariable(variable), app.GetEnvironmentVariable(variable));
                break;
            }
        }

        [Fact]
        public void Info()
        {
            var app = new ApplicationBase();
            var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetCallingAssembly();
            var assemblyName = assembly.GetName();
            Assert.Equal(assemblyName.Name, app.Info.AssemblyName);
            Assert.Equal(assemblyName.Version, app.Info.Version);
        }
    }
}
