// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // Abstract base for all ParameterInfo objects created by the Runtime.
    //
    internal abstract partial class RuntimeParameterInfo : ParameterInfo
    {
        protected RuntimeParameterInfo(MemberInfo member, int position)
        {
            _member = member;
            _position = position;
        }

        public abstract override ParameterAttributes Attributes { get; }
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }
        public abstract override object DefaultValue { get; }
        public abstract override object RawDefaultValue { get; }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimeParameterInfo other))
                return false;
            if (_position != other._position)
                return false;
            if (!(_member.Equals(other._member)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return _member.GetHashCode();
        }

        public abstract override Type[] GetOptionalCustomModifiers();

        public abstract override Type[] GetRequiredCustomModifiers();

        public abstract override bool HasDefaultValue { get; }

        public abstract override int MetadataToken
        {
            get;
        }

        public sealed override MemberInfo Member
        {
            get
            {
                return _member;
            }
        }

        public abstract override string Name { get; }
        public abstract override Type ParameterType { get; }

        public sealed override int Position
        {
            get
            {
                return _position;
            }
        }

        public sealed override string ToString()
        {
            return this.ParameterType.FormatTypeNameForReflection() + " " + this.Name;
        }

        private readonly MemberInfo _member;
        private readonly int _position;
    }
}
