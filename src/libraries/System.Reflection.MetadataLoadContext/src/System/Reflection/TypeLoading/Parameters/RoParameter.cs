// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all ParameterInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoParameter : ParameterInfo
    {
        private readonly MemberInfo _member;
        private readonly int _position;

        protected RoParameter(MemberInfo member, int position)
        {
            Debug.Assert(member != null);

            _member = member;
            _position = position;
        }

        public sealed override MemberInfo Member => _member;
        public sealed override int Position => _position;
        public abstract override int MetadataToken { get; }

        public abstract override string Name { get; }
        public abstract override Type ParameterType { get; }
        public abstract override ParameterAttributes Attributes { get; }
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }

        public abstract override bool HasDefaultValue { get; }
        public sealed override object DefaultValue => throw new InvalidOperationException(SR.Arg_ReflectionOnlyParameterDefaultValue);
        public abstract override object RawDefaultValue { get; }

        public abstract override Type[] GetOptionalCustomModifiers();
        public abstract override Type[] GetRequiredCustomModifiers();

        public abstract override string ToString();

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RoParameter other))
                return false;

            if (_member != other._member)
                return false;

            if (_position != other._position)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => _member.GetHashCode() ^ _position.GetHashCode();

        // Operations that are illegal on ReflectionOnly objects.
        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
    }
}
