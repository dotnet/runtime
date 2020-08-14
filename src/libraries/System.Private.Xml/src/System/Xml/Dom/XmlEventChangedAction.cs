// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml
{
    // Specifies the type of node change
    public enum XmlNodeChangedAction
    {
        // A node is being inserted in the tree.
        Insert = 0,

        // A node is being removed from the tree.
        Remove = 1,

        // A node value is being changed.
        Change = 2
    }
}
