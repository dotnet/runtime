// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace System.Resources.Tests
{
    public static class NeutralResourcesLanguageAttributeTests
    {
        [Theory]
        [InlineData("en-us")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("")]
        public static void ConstructorBasic(string cultureName)
        {
            NeutralResourcesLanguageAttribute nrla = new NeutralResourcesLanguageAttribute(cultureName);
            Assert.Equal(cultureName, nrla.CultureName);
        }

        [Fact]
        public static void ConstructorArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NeutralResourcesLanguageAttribute(null));
        }

        [Theory]
        [InlineData(typeof(object))] // System.Private.CoreLib,
        [InlineData(typeof(System.Collections.Stack))] // System.Collections.NonGeneric
        [InlineData(typeof(System.Collections.Specialized.ListDictionary))] // System.Collections.Specialized
        [InlineData(typeof(System.Collections.Immutable.IImmutableList<string>))] // System.Collections.Immutable
        [InlineData(typeof(System.Collections.Concurrent.ConcurrentBag<string>))] // System.Collections.Concurrent
        [InlineData(typeof(System.Console))] // System.Console
        [InlineData(typeof(System.IO.Directory))] // System.IO.FileSystem
        [InlineData(typeof(System.Linq.Enumerable))] // System.Linq
        [InlineData(typeof(System.Net.Http.HttpClient))] // System.Net.Http
        [InlineData(typeof(System.Text.RegularExpressions.Regex))] // System.Text.RegularExpressions
        [InlineData(typeof(System.Threading.Barrier))] // System.Threading
        public static void TestAttributeExistence(Type type) => Assert.Equal("en-US", ChildResourceManager.GetNeutralResourcesCulture(type.Assembly).Name);

        public class ChildResourceManager : ResourceManager
        {
            public static CultureInfo GetNeutralResourcesCulture(Assembly a) => ResourceManager.GetNeutralResourcesLanguage(a);
        }
    }
}
