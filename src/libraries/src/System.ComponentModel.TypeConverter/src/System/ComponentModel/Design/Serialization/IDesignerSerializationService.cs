// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// This service provides a way to exchange a collection of objects
    /// for a serializable object that represents them. The returned
    /// object contains live references to objects in the collection.
    /// This returned object can then be passed to any runtime
    /// serialization mechanism. The object itself serializes
    /// components the same way designers write source for them; by picking
    /// them apart property by property. Many objects do not support
    /// runtime serialization because their internal state cannot be
    /// adequately duplicated. All components that support a designer,
    /// however, must support serialization by walking their public
    /// properties, methods and events. This interface uses this
    /// technique to convert a collection of components into a single
    /// opaque object that does support runtime serialization.
    /// </summary>
    public interface IDesignerSerializationService
    {
        /// <summary>
        /// Deserializes the provided serialization data object and
        /// returns a collection of objects contained within that data.
        /// </summary>
        ICollection Deserialize(object serializationData);

        /// <summary>
        /// Serializes the given collection of objects and 
        /// stores them in an opaque serialization data object.
        /// The returning object fully supports runtime serialization.
        /// </summary>
        object Serialize(ICollection objects);
    }
}
