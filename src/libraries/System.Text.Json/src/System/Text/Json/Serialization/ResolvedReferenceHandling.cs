// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    //Consider adding WriteReference value to use that instead of output param writeAsReference in ReferenceHandlingStrategy.
    internal enum ResolvedReferenceHandling
    {
        // Indicates that the JSON object/array will be written with metadata ($id) to idntify it in further hits.
        Preserve,

        // Indicates that the JSON object/array has been previously serialized somewhere in the payload and now we will only write a pointer ($ref) to it.
        IsReference,

        // Indicates that the JSON object/array is a CLR value type and there is no need to emit metadata to identify it.
        None
    }
}
