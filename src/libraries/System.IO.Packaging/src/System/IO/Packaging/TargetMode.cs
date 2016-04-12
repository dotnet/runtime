// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Packaging
{
    /// <summary>
    ///  The TargetMode enumeration is used to interpret the 
    ///  "base" uri for the PackageRelationship target Uri. 
    ///  Possible values - 
    ///              0: "Internal" - target points to a part within the package
    ///                              PackageRelationship target uri must be relative.
    ///              1: "External" - target points to an external resource. The
    ///                              resource can be relative to the package entity
    ///                              or an absolute URI
    /// </summary>
    public enum TargetMode : int
    {
        /// <summary>
        /// TargetMode is "Internal".
        /// PackageRelationship target points to a part within the package
        /// PackageRelationship target uri must be relative.
        /// </summary>
        Internal = 0,

        /// <summary>
        /// TargetMode is "External".
        /// PackageRelationship target points to an external resource. 
        /// PackageRelationship target uri can be relative or absolute.
        /// The resource can be relative to the package entity or an absolute URI.
        /// </summary>
        External = 1,
    }
}
