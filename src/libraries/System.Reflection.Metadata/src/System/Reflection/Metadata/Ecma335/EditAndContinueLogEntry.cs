// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    public struct EditAndContinueLogEntry : IEquatable<EditAndContinueLogEntry>
    {
        public readonly EntityHandle Handle;
        public readonly EditAndContinueOperation Operation;

        public EditAndContinueLogEntry(EntityHandle handle, EditAndContinueOperation operation)
        {
            this.Handle = handle;
            this.Operation = operation;
        }

        public override bool Equals(object obj)
        {
            return obj is EditAndContinueLogEntry && Equals((EditAndContinueLogEntry)obj);
        }

        public bool Equals(EditAndContinueLogEntry other)
        {
            return this.Operation == other.Operation &&
                   this.Handle == other.Handle;
        }

        public override int GetHashCode()
        {
            return (int)Operation ^ Handle.GetHashCode();
        }
    }
}