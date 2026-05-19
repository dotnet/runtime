// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.HotReload.Utils.Generator;

/// Properties describing names of the artifacts for a given revision of a baseline assembly
public class DeltaNaming {

    readonly string _baseAssemblyPath;
    readonly int _rev;

    public DeltaNaming (string baseAssemblyPath, int rev) {
        _rev = rev;
        _baseAssemblyPath = baseAssemblyPath;
    }
    public string Dmeta => _baseAssemblyPath + "." + Rev + ".dmeta";
    public string Dil => _baseAssemblyPath + "." + Rev + ".dil";
    public string Dpdb => _baseAssemblyPath + "." + Rev + ".dpdb";

    public string UpdateHandlerInfo => _baseAssemblyPath + "." + Rev + ".handler.json";
    public int Rev => _rev;

    public DeltaNaming Next()
    {
        return new DeltaNaming(_baseAssemblyPath, Rev+1);
    }
}
