// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.Pgo
{
    public struct TypeSystemEntityOrUnknown
    {
        public TypeSystemEntityOrUnknown(int unknownIndex)
        {
            _data = unknownIndex;
        }

        public TypeSystemEntityOrUnknown(TypeDesc type)
        {
            _data = type;
        }

        readonly object _data;
        public TypeDesc AsType => _data as TypeDesc;
        public MethodDesc AsMethod => _data as MethodDesc;
        public FieldDesc AsField => _data as FieldDesc;
        public int AsUnknown => (!(_data is int)) || _data == null ? 0 : (int)_data;
        public bool IsNull => _data == null;
    }
}
