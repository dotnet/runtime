// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib {}
.assembly VarInterfaceLib {}


// NEGATIVE
// Inherited interface : Non variant
// Implementing interface: Covariant

.class interface public abstract auto ansi I`1<T> {} 

.class interface public abstract auto ansi IPos1`1<+M> implements class I`1<!M> {} 


// NEGATIVE
// Inherited interface : Non variant
// Implementing interface: Contravariant

.class interface public abstract auto ansi INeg1`1<-M> implements class I`1<!M> {} 


// POSITIVE
// Inherited interface : covariant
// Implementing interface: Non variant

.class interface public abstract auto ansi IPos2`1<+T> {} 

.class interface public abstract auto ansi I2`1<M> implements class IPos2`1<!M> {} 


// POSITIVE
// Inherited interface : covariant
// Implementing interface: covariant

.class interface public abstract auto ansi IPos3`1<+M> implements class IPos2`1<!M> {} 


// NEGATIVE
// Inherited interface : covariant
// Implementing interface: Contravariant

.class interface public abstract auto ansi INeg2`1<-M> implements class IPos2`1<!M> {} 


// POSITIVE
// Inherited interface : Contravariant
// Implementing interface: Non variant

.class interface public abstract auto ansi INeg3`1<-T> {} 

.class interface public abstract auto ansi I3`1<M> implements class INeg3`1<!M> {} 


// NEGATIVE
// Inherited interface : Contravariant
// Implementing interface: covariant

.class interface public abstract auto ansi IPos4`1<+M> implements class INeg3`1<!M> {} 


// POSITIVE
// Inherited interface : Contravariant
// Implementing interface: Contravariant

.class interface public abstract auto ansi INeg4`1<-M> implements class INeg3`1<!M> {} 

