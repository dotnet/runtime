// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }


.assembly extern mscorlib {}

.assembly varlib {}


.class interface public abstract auto ansi IPos`1<+ T>
{
    .method public hidebysig newslot abstract virtual 
            instance !T produce() cil managed
    {
    }

} 

.class interface public abstract auto ansi INeg`1<- T>
{
    .method public hidebysig newslot abstract virtual 
            instance void consume(!T t) cil managed
    {
    }
} 

