// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class RuntimeConfig
    {
        public class Framework
        {
            public string Name { get; }
            public string Version { get; set;  }

            public string RollForward { get; set; }
            public int? RollForwardOnNoCandidateFx { get; set; }
            public bool? ApplyPatches { get; set; }

            public Framework(string name, string version)
            {
                Name = name;
                Version = version;
            }

            public Framework WithRollForward(string value)
            {
                RollForward = value;
                return this;
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
                JObject frameworkReference = new JObject();

                if (Name != null)
                {
                    frameworkReference.Add("name", Name);
                }

                if (Version != null)
                {
                    frameworkReference.Add("version", Version);
                }

                if (RollForward != null)
                {
                    frameworkReference.Add(
                        Constants.RollForwardSetting.RuntimeConfigPropertyName,
                        RollForward);
                }

                if (RollForwardOnNoCandidateFx.HasValue)
                {
                    frameworkReference.Add(
                        Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName,
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
                    RollForward = (string)jobject[Constants.RollForwardSetting.RuntimeConfigPropertyName],
                    RollForwardOnNoCandidateFx = (int?)jobject[Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName],
                    ApplyPatches = (bool?)jobject[Constants.ApplyPatchesSetting.RuntimeConfigPropertyName]
                };
            }
        }

        private string _rollForward;
        private int? _rollForwardOnNoCandidateFx;
        private bool? _applyPatches;
        private string _tfm;
        private readonly string _path;
        private readonly List<Framework> _frameworks = new List<Framework>();
        private readonly List<Framework> _includedFrameworks = new List<Framework>();
        private readonly List<Tuple<string, string>> _properties = new List<Tuple<string, string>>();

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
                using (var reader = new JsonTextReader(textReader) { MaxDepth = null })
                {
                    JObject root = (JObject)JToken.ReadFrom(reader);
                    JObject runtimeOptions = (JObject)root["runtimeOptions"];
                    var singleFramework = runtimeOptions["framework"] as JObject;
                    if (singleFramework != null)
                    {
                        runtimeConfig.WithFramework(Framework.FromJson(singleFramework));
                    }

                    var frameworks = runtimeOptions["frameworks"];
                    if (frameworks != null)
                    {
                        foreach (JObject framework in frameworks)
                        {
                            runtimeConfig.WithFramework(Framework.FromJson(framework));
                        }
                    }

                    var includedFrameworks = runtimeOptions["includedFrameworks"];
                    if (includedFrameworks != null)
                    {
                        foreach (JObject includedFramework in includedFrameworks)
                        {
                            runtimeConfig.WithFramework(Framework.FromJson(includedFramework));
                        }
                    }

                    var configProperties = runtimeOptions["configProperties"] as JObject;
                    if (configProperties != null)
                    {
                        foreach (KeyValuePair<string, JToken> property in configProperties)
                        {
                            runtimeConfig.WithProperty(property.Key, (string)property.Value);
                        }
                    }

                    runtimeConfig._rollForward = (string)runtimeOptions[Constants.RollForwardSetting.RuntimeConfigPropertyName];
                    runtimeConfig._rollForwardOnNoCandidateFx = (int?)runtimeOptions[Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName];
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

        public RuntimeConfig RemoveFramework(string name)
        {
            Framework framework = GetFramework(name);
            if (framework != null)
            {
                _frameworks.Remove(framework);
            }

            return this;
        }

        public RuntimeConfig WithIncludedFramework(Framework framework)
        {
            _includedFrameworks.Add(framework);
            return this;
        }

        public RuntimeConfig WithIncludedFramework(string name, string version)
        {
            return WithIncludedFramework(new Framework(name, version));
        }

        public RuntimeConfig WithRollForward(string value)
        {
            _rollForward = value;
            return this;
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

        public RuntimeConfig WithTfm(string tfm)
        {
            _tfm = tfm;
            return this;
        }

        public RuntimeConfig WithProperty(string name, string value)
        {
            _properties.Add(new Tuple<string, string>(name, value));
            return this;
        }

        public void Save()
        {
            JObject runtimeOptions = new JObject();
            if (_frameworks.Any())
            {
                runtimeOptions.Add(
                    "frameworks",
                    new JArray(_frameworks.Select(f => f.ToJson()).ToArray()));
            }

            if (_includedFrameworks.Any())
            {
                runtimeOptions.Add(
                    "includedFrameworks",
                    new JArray(_includedFrameworks.Select(f => f.ToJson()).ToArray()));
            }

            if (_rollForward != null)
            {
                runtimeOptions.Add(
                    Constants.RollForwardSetting.RuntimeConfigPropertyName,
                    _rollForward);
            }

            if (_rollForwardOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add(
                    Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName,
                    _rollForwardOnNoCandidateFx.Value);
            }

            if (_applyPatches.HasValue)
            {
                runtimeOptions.Add(
                    Constants.ApplyPatchesSetting.RuntimeConfigPropertyName,
                    _applyPatches.Value);
            }

            if (_tfm is not null)
            {
                runtimeOptions.Add(
                    Constants.Tfm.RuntimeConfigPropertyName,
                    _tfm);
            }

            if (_properties.Count > 0)
            {
                JObject configProperties = new JObject();
                foreach (var property in _properties)
                {
                    var tokenValue = (property.Item2 == "false" || property.Item2 == "true") ?
                        JToken.Parse(property.Item2) : property.Item2;
                    configProperties.Add(property.Item1, tokenValue);
                }

                runtimeOptions.Add("configProperties", configProperties);
            }

            JObject json = new JObject()
                {
                    { "runtimeOptions", runtimeOptions }
                };

            File.WriteAllText(_path, json.ToString());
        }
    }
}
