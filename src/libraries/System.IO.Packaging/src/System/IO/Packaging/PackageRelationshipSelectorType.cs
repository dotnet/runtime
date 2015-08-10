// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  PackageRelationshipSelectorType enum - lists all the possible types based on which
//  PackageRelationships can be selected. Currently we define just two of these -
//  1. Id
//  2. Type
//
//-----------------------------------------------------------------------------

namespace System.IO.Packaging
{
    /// <summary>
    /// Enum to represent the different selector types for PackageRelationshipSelector  
    /// </summary>
    public enum PackageRelationshipSelectorType : int
    {
        /// <summary>
        /// Id
        /// </summary>
        Id = 0,

        /// <summary>
        /// Type 
        /// </summary>
        Type = 1
    }
}
