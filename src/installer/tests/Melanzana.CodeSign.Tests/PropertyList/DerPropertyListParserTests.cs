using Claunia.PropertyList;
using Melanzana.CodeSign.PropertyList;
using Melanzana.Streams;
using System;
using Xunit;

namespace Melanzana.CodeSign.Tests.PropertyList
{
    public class DerPropertyListParserTests
    {
        /// <summary>
        /// Tests the <see cref="DerPropertyListParser.Parse(ReadOnlyMemory{byte})"/> method for
        /// embedded entitlements.
        /// </summary>
        [Fact]
        public void ParseEntitlementsTest()
        {
            var stream = typeof(DerPropertyListParserTests).Assembly.GetManifestResourceStream("Melanzana.CodeSign.Tests.Data.entitlements.der")!;
            var data = new byte[stream.Length];
            stream.ReadFully(data);

            var value = DerPropertyListParser.Parse(data);
            var dict = Assert.IsType<NSDictionary>(value);

            Assert.Collection(
                dict,
                e =>
                {
                    Assert.Equal("get-task-allow", e.Key);
                    Assert.True(Assert.IsType<NSNumber>(e.Value).ToBool());
                },
                e =>
                {
                    Assert.Equal("com.apple.developer.team-identifier", e.Key);
                    Assert.Equal("0123456789", Assert.IsType<NSString>(e.Value).ToString());
                },
                e =>
                {
                    Assert.Equal("application-identifier", e.Key);
                    Assert.Equal("0123456789.com.example.MyApplication", Assert.IsType<NSString>(e.Value).ToString());
                },
                e =>
                {
                    Assert.Equal("keychain-access-groups", e.Key);

                    var array = Assert.IsType<NSArray>(e.Value);
                    var value = Assert.Single(array);
                    Assert.Equal("0123456789.com.example.MyApplication", Assert.IsType<NSString>(value).ToString());
                });
        }
    }
}
