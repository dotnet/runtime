// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp
{
    internal enum ExtentFlags
    {
        EF_FULL = 0x00, // Get the full extent of a NODE (default)
        EF_SINGLESTMT = 0x01, // Get just a single statement (for debug info)
        EF_POSSIBLE_GENERIC_NAME = 0x02, // If the parser has parsed out a possible generic name, then allow that to be returned
        EF_PREFER_LEFT_NODE = 0x04, // If there are two possible nodes to return (because the cursor is in between two nodes), choose the left node
        EF_IGNORE_TOKEN_STREAM = 0x08, // Tell "FindLeaf" to ignore token stream content
        EF_POSSIBLE_EXPRESSION = 0x10, // If the parser has parsed out a possible simple dotted name, then allow that to be returned
        EF_TOPLEVELONLY = 0x20, // Get just a top level element (do not go into interior tree)
    }
}
