// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;
using System.Linq;

namespace ILCompiler.IBC
{
    public class MibcConfig
    {
        public string FormatVersion = "1.0";
        public string Os;
        public string Arch;
        public string Runtime;

        public override string ToString()
        {
            return
$@"
FormatVersion: {FormatVersion}
Runtime:       {Runtime}
Os:            {Os}
Arch:          {Arch}

";
        }

        public static MibcConfig FromKeyValueMap(Dictionary<string, string> kvMap)
        {
            MibcConfig config = new();
            foreach (var kvPair in kvMap)
            {
                switch (kvPair.Key)
                {
                    case nameof(FormatVersion): config.FormatVersion = kvPair.Value; break;
                    case nameof(Os): config.Os = kvPair.Value; break;
                    case nameof(Arch): config.Arch = kvPair.Value; break;
                    case nameof(Runtime): config.Runtime = kvPair.Value; break;
                }
            }
            return config;
        }
    }

    public class IBCProfileData : ProfileData
    {
        public IBCProfileData(MibcConfig config, bool partialNGen, IEnumerable<MethodProfileData> methodData)
        {
            MethodProfileData[] dataArray = methodData.ToArray();
            foreach (MethodProfileData data in dataArray)
            {
                if (!_methodData.ContainsKey(data.Method))
                {
                    _methodData.Add(data.Method, data);
                }
            }
            _partialNGen = partialNGen;
            _config = config;
        }

        private readonly Dictionary<MethodDesc, MethodProfileData> _methodData = new Dictionary<MethodDesc, MethodProfileData>();
        private readonly bool _partialNGen;
        private readonly MibcConfig _config;

        public override MibcConfig Config => _config;

        public override bool PartialNGen => _partialNGen;

        public override MethodProfileData GetMethodProfileData(MethodDesc m)
        {
            _methodData.TryGetValue(m, out MethodProfileData profileData);
            return profileData;
        }

        public override IEnumerable<MethodProfileData> GetAllMethodProfileData()
        {
            return _methodData.Values;
        }

        public override byte[] GetMethodBlockCount(MethodDesc m)
        {
            throw new NotImplementedException();
        }
    }
}
