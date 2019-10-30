// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib {}
.assembly VarInterfaceLib2 {}


// Inherited interface : covariant
// Implementing interface: Non variant

.class interface public abstract auto ansi IPos1`1<+T> {} 

.class interface public abstract auto ansi I1`1<M> implements class IPos1`1<!M> 
{

    .method public hidebysig newslot abstract virtual 
            instance void consume (!M t) cil managed
    {
    }	

    .method public hidebysig newslot abstract virtual 
            instance !M produce() cil managed
    {
    }
} 




// Inherited interface : contravariant
// Implementing interface: Non variant

.class interface public abstract auto ansi INeg1`1<-T> {} 

.class interface public abstract auto ansi I2`1<M> implements class INeg1`1<!M> 
{

    .method public hidebysig newslot abstract virtual 
            instance void consume (!M t) cil managed
    {
    }	

    .method public hidebysig newslot abstract virtual 
            instance !M produce() cil managed
    {
    }
} 




