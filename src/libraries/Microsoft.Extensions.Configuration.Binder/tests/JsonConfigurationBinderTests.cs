// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.Test
{
    public class JsonConfigurationBinderTests
    {
        [Fact]
        public void BindComplexJson()
        {
            var json = @"{
                ""prop1"": {
                    ""subprop1"": [
                        {
                           ""name"" : ""element1""
                        },
                        {
                           ""name"" : ""element2""
                        }
                    ],
                    ""subprop2"": ""subvalue""
                },
                ""prop2"": {
                    ""subprop1"": [ ]
                },
                ""prop3"": { }
            }";

            var jsonConfigSource = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json) };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource);
            var config = configurationBuilder.Build();

            var prop = new Prop();
            config.Bind(prop);

            Assert.Equal(3, prop.Keys.Count);
            Assert.NotNull(prop["prop1"]);
            Assert.NotNull(prop["prop2"]);
            Assert.NotNull(prop["prop3"]);

            Assert.Equal(2, prop["prop1"].SubProp1.Count);
            Assert.Equal("subvalue", prop["prop1"].SubProp2);

            Assert.Equal("element1", prop["prop1"].SubProp1[0].Name);
            Assert.Equal("element2", prop["prop1"].SubProp1[1].Name);

            Assert.Equal(0, prop["prop2"].SubProp1.Count);
            Assert.Equal("default", prop["prop2"].SubProp2);
            Assert.Equal(0, prop["prop3"].SubProp1.Count);
            Assert.Equal("default", prop["prop3"].SubProp2);

        }

        private class Prop : Dictionary<string,SubProp>
        {

        }

        private class SubProp
        {
            public List<Element> SubProp1 { get; set; } = new List<Element>();
            public string SubProp2 { get; set; } = "default";
        }

        private class Element
        {
            public string Name { get; set; } = "default";
        }


    }
}
