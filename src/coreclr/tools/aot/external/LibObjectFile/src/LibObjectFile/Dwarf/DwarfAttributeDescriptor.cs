// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{Kind} {Form}")]
    public readonly struct DwarfAttributeDescriptor : IEquatable<DwarfAttributeDescriptor>
    {
        public static readonly DwarfAttributeDescriptor Empty = new DwarfAttributeDescriptor();

        public DwarfAttributeDescriptor(DwarfAttributeKindEx kind, DwarfAttributeFormEx form)
        {
            Kind = kind;
            Form = form;
        }
        
        public readonly DwarfAttributeKindEx Kind;

        public readonly DwarfAttributeFormEx Form;

        public bool IsNull => Kind.Value == 0 && Form.Value == 0;

        public bool Equals(DwarfAttributeDescriptor other)
        {
            return Kind.Equals(other.Kind) && Form.Equals(other.Form);
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Kind.GetHashCode() * 397) ^ Form.GetHashCode();
            }
        }

        public static bool operator ==(DwarfAttributeDescriptor left, DwarfAttributeDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeDescriptor left, DwarfAttributeDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}