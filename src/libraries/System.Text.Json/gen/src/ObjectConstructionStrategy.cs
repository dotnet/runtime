// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Indicates which kind of constructor an object is to be created with.
    /// </summary>
    internal enum ObjectConstructionStrategy
    {
        /// <summary>
        /// Object is abstract or an interface.
        /// </summary>
        NotApplicable = 0,
        /// <summary>
        /// Object should be created with a parameterless constructor.
        /// </summary>
        ParameterlessConstructor = 1,
        /// <summary>
        /// Object should be created with a parameterized constructor.
        /// </summary>
        ParameterizedConstructor = 2,
    }
}
