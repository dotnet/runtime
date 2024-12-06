// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace WebAssemblyInfo
{
    public class WasmEditContext : WasmContext
    {
        public bool DataSectionAutoSplit;
        public string DataSectionFile = "";
        public DataMode DataSectionMode = DataMode.Active;
        public int DataOffset;
    }
}
