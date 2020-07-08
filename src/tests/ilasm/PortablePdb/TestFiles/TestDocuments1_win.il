// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
}

.assembly TestDocuments
{
}

.class public auto ansi beforefieldinit TestDocumentsType
       extends [System.Runtime]System.Object
{
    .method public hidebysig instance void Foo() cil managed
    {
    .line 1,1 : 9,10 'C:\\tmp\\non_existent_source1.cs'
        nop
    .line 2,2 : 9,10 'C:\\tmp\\non_existent_source2.cs'
        ret
    }
}
