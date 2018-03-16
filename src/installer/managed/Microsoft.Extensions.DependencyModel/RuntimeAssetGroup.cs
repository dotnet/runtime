// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeAssetGroup
    {
        private IReadOnlyList<string> _assetPaths;
        private IReadOnlyList<RuntimeFile> _runtimeFiles;

        public RuntimeAssetGroup(string runtime, params string[] assetPaths) : this(runtime, (IEnumerable<string>)assetPaths) { }

        public RuntimeAssetGroup(string runtime, IEnumerable<string> assetPaths)
        {
            Runtime = runtime;
            _assetPaths = assetPaths.ToArray();
        }

        public RuntimeAssetGroup(string runtime, IEnumerable<RuntimeFile> runtimeFiles)
        {
            Runtime = runtime;
            _runtimeFiles = runtimeFiles.ToArray();
        }

        /// <summary>
        /// The runtime ID associated with this group (may be empty if the group is runtime-agnostic)
        /// </summary>
        public string Runtime { get; }

        /// <summary>
        /// Gets a list of asset paths provided in this runtime group
        /// </summary>
        public IReadOnlyList<string> AssetPaths
        {
            get
            {
                if (_assetPaths != null)
                {
                    return _assetPaths;
                }

                return _runtimeFiles.Select(file => file.Path).ToArray();
            }
        }

        /// <summary>
        /// Gets a list of RuntimeFiles provided in this runtime group
        /// </summary>
        public IReadOnlyList<RuntimeFile> RuntimeFiles
        {
            get
            {
                if (_runtimeFiles != null)
                {
                    return _runtimeFiles;
                }

                return _assetPaths.Select(path => new RuntimeFile(path, null, null)).ToArray();
            }
        }
    }
}