// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class EnumMarshalTests
    {
        public enum RequestCache
        {
            [Export(EnumValue = ConvertEnum.Default)]
            Default = -1,
            [Export("no-store")]
            NoStore,
            [Export(EnumValue = ConvertEnum.ToUpper)]
            Reload,
            [Export(EnumValue = ConvertEnum.ToLower)]
            NoCache,
            [Export("force-cache")]
            ForceCache,
            OnlyIfCached = -3636,
        }

        [Fact]
        public static void MarshalRequestEnums()
        {
            Runtime.InvokeJS(@"
                var dflt = ""Default"";
                var nostore = ""no-store"";
                var reload = ""RELOAD"";
                var nocache = ""nocache"";
                var force = 3;
                var onlyif = -3636;
                App.call_test_method (""SetRequestEnums"", [ dflt, nostore, reload, nocache, force, onlyif ]);
            ");
            Assert.Equal(RequestCache.Default, HelperMarshal.requestEnums[0]);
            Assert.Equal(RequestCache.NoStore, HelperMarshal.requestEnums[1]);
            Assert.Equal(RequestCache.Reload, HelperMarshal.requestEnums[2]);
            Assert.Equal(RequestCache.NoCache, HelperMarshal.requestEnums[3]);
            Assert.Equal(RequestCache.ForceCache, HelperMarshal.requestEnums[4]);
            Assert.Equal(RequestCache.OnlyIfCached, HelperMarshal.requestEnums[5]);
        }

        [Fact]
        public static void MarshalRequestEnumProps()
        {
            Runtime.InvokeJS(@"
                var obj = {};
                App.call_test_method  (""SetRequestEnumsProperties"", [ obj ]);
                App.call_test_method  (""SetRequestEnums"", [ obj.dflt, obj.nostore, obj.reload, obj.nocache, obj.force, obj.onlyif ]);
            ");
            Assert.Equal(RequestCache.Default, HelperMarshal.requestEnums[0]);
            Assert.Equal(RequestCache.NoStore, HelperMarshal.requestEnums[1]);
            Assert.Equal(RequestCache.Reload, HelperMarshal.requestEnums[2]);
            Assert.Equal(RequestCache.NoCache, HelperMarshal.requestEnums[3]);
            Assert.Equal(RequestCache.ForceCache, HelperMarshal.requestEnums[4]);
            Assert.Equal(RequestCache.OnlyIfCached, HelperMarshal.requestEnums[5]);
        }
    }
}
