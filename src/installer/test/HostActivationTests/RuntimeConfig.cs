// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class RuntimeConfig
    {
        public class Framework
        {
            public string Name { get; }
            public string Version { get; set;  }

            public int? RollForwardOnNoCandidateFx { get; set; }
            public bool? ApplyPatches { get; set; }

            public Framework(string name, string version)
            {
                Name = name;
                Version = version;
            }

            public Framework WithRollForwardOnNoCandidateFx(int? value)
            {
                RollForwardOnNoCandidateFx = value;
                return this;
            }

            public Framework WithApplyPatches(bool? value)
            {
                ApplyPatches = value;
                return this;
            }

            internal JObject ToJson()
            {
                JObject frameworkReference =
                    new JObject(
                        new JProperty("name", Name),
                        new JProperty("version", Version)
                        );

                if (RollForwardOnNoCandidateFx.HasValue)
                {
                    frameworkReference.Add(
                        Constants.RollFowardOnNoCandidateFxSetting.RuntimeConfigPropertyName,
                        RollForwardOnNoCandidateFx.Value);
                }

                if (ApplyPatches.HasValue)
                {
                    frameworkReference.Add(
                        Constants.ApplyPatchesSetting.RuntimeConfigPropertyName,
                        ApplyPatches.Value);
                }

                return frameworkReference;
            }

            internal static Framework FromJson(JObject jobject)
            {
                return new Framework((string)jobject["name"], (string)jobject["version"])
                {
                    RollForwardOnNoCandidateFx = (int?)jobject[Constants.RollFowardOnNoCandidateFxSetting.RuntimeConfigPropertyName],
                    ApplyPatches = (bool?)jobject[Constants.ApplyPatchesSetting.RuntimeConfigPropertyName]
                };
            }
        }

        private int? _rollForwardOnNoCandidateFx;
        private bool? _applyPatches;
        private readonly string _path;
        private readonly List<Framework> _frameworks = new List<Framework>();

        /// <summary>
        /// Creates new runtime config - overwrites existing file on Save if any.
        /// </summary>
        public RuntimeConfig(string path)
        {
            _path = path;
        }

        /// <summary>
        /// Creates the object over existing file - reading its content. Save should recreate the file
        /// assuming we can store all the values in it in this class.
        /// </summary>
        public static RuntimeConfig FromFile(string path)
        {
            RuntimeConfig runtimeConfig = new RuntimeConfig(path);
            if (File.Exists(path))
            {
                using (TextReader textReader = File.OpenText(path))
                using (JsonTextReader reader = new JsonTextReader(textReader))
                {
                    JObject root = (JObject)JToken.ReadFrom(reader);
                    JObject runtimeOptions = (JObject)root["runtimeOptions"];
                    foreach (JObject framework in runtimeOptions["frameworks"])
                    {
                        runtimeConfig.WithFramework(Framework.FromJson(framework));
                    }

                    runtimeConfig._rollForwardOnNoCandidateFx = (int?)runtimeOptions[Constants.RollFowardOnNoCandidateFxSetting.RuntimeConfigPropertyName];
                    runtimeConfig._applyPatches = (bool?)runtimeOptions[Constants.ApplyPatchesSetting.RuntimeConfigPropertyName];
                }
            }

            return runtimeConfig;
        }

        public static RuntimeConfig Path(string path)
        {
            return new RuntimeConfig(path);
        }

        public Framework GetFramework(string name)
        {
            return _frameworks.FirstOrDefault(f => f.Name == name);
        }

        public RuntimeConfig WithFramework(Framework framework)
        {
            _frameworks.Add(framework);
            return this;
        }

        public RuntimeConfig WithFramework(string name, string version)
        {
            return WithFramework(new Framework(name, version));
        }

        public RuntimeConfig WithRollForwardOnNoCandidateFx(int? value)
        {
            _rollForwardOnNoCandidateFx = value;
            return this;
        }

        public RuntimeConfig WithApplyPatches(bool? value)
        {
            _applyPatches = value;
            return this;
        }

        public void Save()
        {
            JObject runtimeOptions = new JObject()
                {
                    { "frameworks", new JArray(_frameworks.Select(f => f.ToJson()).ToArray()) }
                };

            if (_rollForwardOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add(
                    Constants.RollFowardOnNoCandidateFxSetting.RuntimeConfigPropertyName,
                    _rollForwardOnNoCandidateFx.Value);
            }

            if (_applyPatches.HasValue)
            {
                runtimeOptions.Add(
                    Constants.ApplyPatchesSetting.RuntimeConfigPropertyName,
                    _applyPatches.Value);
            }

            JObject json = new JObject()
                {
                    { "runtimeOptions", runtimeOptions }
                };

            File.WriteAllText(_path, json.ToString());
        }
    }
}
