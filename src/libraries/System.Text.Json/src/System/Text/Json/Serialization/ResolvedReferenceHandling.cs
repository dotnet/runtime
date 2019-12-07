// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    //Consider adding WriteReference value to use that instead of output param writeAsReference in ReferenceHandlingStrategy.
    internal enum ResolvedReferenceHandling
    {
        // When using ReferenceLoopHandling.Ignore, indicates that the JSON object/array should be ignored since it was previously found in the object graph branch.
        Ignore,
        // When using PreserveReferencesHandlingOnSerialize.All, indicates that the JSON object/array will be written with metadata ($id) to idntify it in further hits.
        Preserve,
        // When using PreserveReferencesHandlingOnSerialize.All, indicates that the JSON object/array has been previously serialized somewhere in the payload and now we will only write a pointer ($ref) to it.
        IsReference,
        // When using ReferenceLoopHandling.Ignore or Error, indicates that the JSON object/array has not been previously spotted in the current graph's branch and serialization can proceed.
        // When using PreserveReferencesHandlingOnSerialize.All, indicates that the JSON object/array is a CLR value type and there is no need to emit metadata to identify it.
        None
    }
}
