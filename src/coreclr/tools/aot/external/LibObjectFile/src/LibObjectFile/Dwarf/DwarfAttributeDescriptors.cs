// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public readonly struct DwarfAttributeDescriptors : IEquatable<DwarfAttributeDescriptors>
    {
        private readonly DwarfAttributeDescriptor[] _descriptors;

        public DwarfAttributeDescriptors(DwarfAttributeDescriptor[] descriptors)
        {
            _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        }

        public int Length => _descriptors?.Length ?? 0;

        public DwarfAttributeDescriptor this[int index]
        {
            get
            {
                if (_descriptors == null) throw new ArgumentException("This descriptors instance is not initialized");
                return _descriptors[index];
            }
        }

        public bool Equals(DwarfAttributeDescriptors other)
        {
            if (ReferenceEquals(_descriptors, other._descriptors)) return true;
            if (_descriptors == null || other._descriptors == null) return false;
            if (_descriptors.Length != other._descriptors.Length) return false;

            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i] != other._descriptors[i])
                {
                    return false;
                }
            }
            return true;
        }

       
        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeDescriptors other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashCode = _descriptors == null ? 0 : _descriptors.Length;
            if (hashCode == 0) return hashCode;
            foreach (var descriptor in _descriptors)
            {
                hashCode = (hashCode * 397) ^ descriptor.GetHashCode();
            }
            return hashCode;
        }

        private string DebuggerDisplay => ToString();

        public static bool operator ==(DwarfAttributeDescriptors left, DwarfAttributeDescriptors right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeDescriptors left, DwarfAttributeDescriptors right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Count = {_descriptors.Length}";
        }
    }
}