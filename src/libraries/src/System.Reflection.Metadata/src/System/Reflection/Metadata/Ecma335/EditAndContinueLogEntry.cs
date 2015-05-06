// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    public struct EditAndContinueLogEntry : IEquatable<EditAndContinueLogEntry>
    {
        private readonly EntityHandle _handle;
        private readonly EditAndContinueOperation _operation;

        public EditAndContinueLogEntry(EntityHandle handle, EditAndContinueOperation operation)
        {
            _handle = handle;
            _operation = operation;
        }

        public EntityHandle Handle
        {
            get { return _handle; }
        }

        public EditAndContinueOperation Operation
        {
            get { return _operation; }
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