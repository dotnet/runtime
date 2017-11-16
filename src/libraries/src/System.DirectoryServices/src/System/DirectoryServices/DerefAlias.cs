// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <devdoc>
    ///  Specifies the behavior in which aliases are dereferenced.
    /// </devdoc>
    public enum DereferenceAlias
    {
        Never = 0,
    	InSearching = 1,
        FindingBaseObject = 2,
        Always = 3
    }
}
