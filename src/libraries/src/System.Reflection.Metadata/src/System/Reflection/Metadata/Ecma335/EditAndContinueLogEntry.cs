// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata.Ecma335
{
    public readonly struct EditAndContinueLogEntry : IEquatable<EditAndContinueLogEntry>
    {
        public EntityHandle Handle { get; }
        public EditAndContinueOperation Operation { get; }

        public EditAndContinueLogEntry(EntityHandle handle, EditAndContinueOperation operation)
        {
            Handle = handle;
            Operation = operation;
        }

        public override bool Equals(object obj) => 
            obj is EditAndContinueLogEntry && Equals((EditAndContinueLogEntry)obj);

        public bool Equals(EditAndContinueLogEntry other) => 
            Operation == other.Operation && Handle == other.Handle;

        public override int GetHashCode() => 
            (int)Operation ^ Handle.GetHashCode();
    }
}
