// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CodeDataLayoutMode
{
    // Some platforms (today only Wasm32) require code and data to be placed in separate address spaces.
    // The object writer must be aware of this distinction so that implementations can handle the placement of dependency
    // nodes properly in the final object file.
    public enum CodeDataLayout
    {
        Unified,
        Separate
    }
}
