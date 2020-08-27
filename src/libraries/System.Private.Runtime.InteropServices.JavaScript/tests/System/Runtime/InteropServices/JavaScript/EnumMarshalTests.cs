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
            [EnumExport(EnumValue = ConvertEnum.Default)]
            Default = -1,
            [EnumExport("no-store")]
            NoStore,
            [EnumExport(EnumValue = ConvertEnum.ToUpper)]
            Reload,
            [EnumExport(EnumValue = ConvertEnum.ToLower)]
            NoCache,
            [EnumExport("force-cache")]
            ForceCache,
            OnlyIfCached = -3636,
        }

        public enum StyleColor : uint
        {
            RedNoExport,
            [EnumExport(EnumValue = ConvertEnum.Default)]
            Red = 0xff0000ff,
            [EnumExport(EnumValue = ConvertEnum.Numeric)]
            GreenNumeric = 0x00ff00ff,
            [EnumExport("red", EnumValue = ConvertEnum.ToUpper)]
            RedUpperCase,
            [EnumExport("RED", EnumValue = ConvertEnum.ToLower)]
            RedLowerCase,
            [EnumExport("#ff0000")]
            RedHex
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

        [Fact]
        public static void MarshalColorEnumProps()
        {
            Runtime.InvokeJS(@"
                var color = {};
                App.call_test_method  (""SetColorEnumsProperties"", [ color ]);
                App.call_test_method  (""SetStyleColorEnums"", [ color.Red, color.GreenNumeric, color.RedUpperCase, color.RedLowerCase, color.RedHex, color.RedNoExport ]);
            ");
            Assert.Equal(StyleColor.Red, HelperMarshal.styleColorEnums[0]);
            Assert.Equal(StyleColor.GreenNumeric, HelperMarshal.styleColorEnums[1]);
            Assert.Equal(StyleColor.RedUpperCase, HelperMarshal.styleColorEnums[2]);
            Assert.Equal(StyleColor.RedLowerCase, HelperMarshal.styleColorEnums[3]);
            Assert.Equal(StyleColor.RedHex, HelperMarshal.styleColorEnums[4]);
            Assert.Equal(StyleColor.RedNoExport, HelperMarshal.styleColorEnums[5]);
        }
    }
}
