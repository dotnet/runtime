// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CoreFX.TestUtils.TestFileSetup
{
    /// <summary>
    /// A class representing a CoreFX test assembly to be run
    /// </summary>
    public class XUnitTestAssembly
    {
        [JsonRequired]
        [JsonProperty("name")]
        public string Name;

        [JsonRequired]
        [JsonProperty("enabled")]
        public bool IsEnabled;

        [JsonRequired]
        [JsonProperty("exclusions")]
        public Exclusions Exclusions;

        // Used to assign a test url or to override it via the json file definition - mark it as optional in the test definition
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public string Url;

    }
    /// <summary>
    /// Class representing a collection of test exclusions
    /// </summary>
    public class Exclusions
    {
        [JsonProperty("namespaces")]
        public Exclusion[] Namespaces;

        [JsonProperty("classes")]
        public Exclusion[] Classes;

        [JsonProperty("methods")]
        public Exclusion[] Methods;
    }

    /// <summary>
    /// Class representing a single test exclusion
    /// </summary>
    public class Exclusion
    {
        [JsonRequired]
        [JsonProperty("name", Required = Required.DisallowNull)]
        public string Name;

        [JsonRequired]
        [JsonProperty("reason", Required = Required.DisallowNull)]
        public string Reason;
    }
}
