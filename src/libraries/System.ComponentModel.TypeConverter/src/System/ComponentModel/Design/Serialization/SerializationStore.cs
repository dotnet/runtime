// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.IO;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// The SerializationStore class is an implementation-specific class that stores 
    /// serialization data for the component serialization service. The 
    /// service adds state to this serialization store. Once the store is 
    /// closed it can be saved to a stream. A serialization store can be 
    /// deserialized at a later date by the same type of serialization service. 
    /// SerializationStore implements the IDisposable interface such that Dispose 
    /// simply calls the Close method. Dispose is implemented as a private 
    /// interface to avoid confusion. The IDisposable pattern is provided 
    /// for languages that support a "using" syntax like C# and VB .NET.
    /// </summary>
    public abstract class SerializationStore : IDisposable
    {
        /// <summary>
        /// If there were errors generated during serialization or deserialization of the store, they will be
        /// added to this collection.
        /// </summary>
        public abstract ICollection Errors { get; }

        /// <summary>
        /// The Close method closes this store and prevents any objects 
        /// from being serialized into it. Once closed, the serialization store may be saved.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// The Save method saves the store to the given stream. If the store 
        /// is open, Save will automatically close it for you. You 
        /// can call save as many times as you wish to save the store 
        /// to different streams.
        /// </summary>
        public abstract void Save(Stream stream);

        /// <summary>
        /// Disposes this object by calling the Close method.
        /// </summary>
        void IDisposable.Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }
    }
}

