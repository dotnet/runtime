// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.Pgo
{
    public struct TypeSystemEntityOrUnknown : IEquatable<TypeSystemEntityOrUnknown>
    {
        public TypeSystemEntityOrUnknown(int unknownIndex)
        {
            _data = unknownIndex;
        }

        public TypeSystemEntityOrUnknown(TypeDesc type)
        {
            _data = type;
        }

        public TypeSystemEntityOrUnknown(MethodDesc method)
        {
            _data = method;
        }

        readonly object _data;
        public TypeDesc AsType => _data as TypeDesc;
        public MethodDesc AsMethod => _data as MethodDesc;
        public FieldDesc AsField => _data as FieldDesc;
        public int AsUnknown => (!(_data is int)) || _data == null ? 0 : (int)_data;
        public bool IsNull => _data == null;
        public bool IsUnknown => _data is int;

        public bool Equals(TypeSystemEntityOrUnknown other)
        {
            if ((_data is int) && (other._data is int))
            {
                return other.AsUnknown == AsUnknown;
            }
            else
            {
                return object.ReferenceEquals(_data, other._data);
            }
        }

        public override int GetHashCode() => _data?.GetHashCode() ?? 0;

        public override bool Equals(object obj) => obj is TypeSystemEntityOrUnknown other && other.Equals(this);

        public override string ToString()
        {
            if (_data == null)
            {
                return "null";
            }

            if ((_data is int))
            {
                return $"UnknownType({(int)_data})";
            }

            return _data.ToString();
        }
    }
}
