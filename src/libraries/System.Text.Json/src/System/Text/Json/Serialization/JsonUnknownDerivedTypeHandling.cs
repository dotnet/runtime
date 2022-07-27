// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Defines how objects of a derived runtime type that has not been explicitly declared for polymorphic serialization should be handled.
    /// </summary>
    public enum JsonUnknownDerivedTypeHandling
    {
        /// <summary>
        /// An object of undeclared runtime type will fail polymorphic serialization.
        /// </summary>
        FailSerialization = 0,
        /// <summary>
        /// An object of undeclared runtime type will fall back to the serialization contract of the base type.
        /// </summary>
        FallBackToBaseType = 1,
        /// <summary>
        /// An object of undeclared runtime type will revert to the serialization contract of the nearest declared ancestor type.
        /// Certain interface hierarchies are not supported due to diamond ambiguity constraints.
        /// </summary>
        FallBackToNearestAncestor = 2
    }
}
