// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public abstract partial class MemberInfo : ICustomAttributeProvider
    {
        protected MemberInfo() { }

        public abstract MemberTypes MemberType { get; }
        public abstract string Name { get; }
        public abstract Type? DeclaringType { get; }
        public abstract Type? ReflectedType { get; }

        public virtual Module Module
        {
            get
            {
                // This check is necessary because for some reason, Type adds a new "Module" property that hides the inherited one instead
                // of overriding.

                if (this is Type type)
                    return type.Module;

                throw NotImplemented.ByDesign;
            }
        }

        public virtual bool HasSameMetadataDefinitionAs(MemberInfo other) { throw NotImplemented.ByDesign; }

        public abstract bool IsDefined(Type attributeType, bool inherit);
        public abstract object[] GetCustomAttributes(bool inherit);
        public abstract object[] GetCustomAttributes(Type attributeType, bool inherit);

        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }
        public virtual bool IsCollectible => true;
        public virtual int MetadataToken => throw new InvalidOperationException();

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MemberInfo? left, MemberInfo? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return left is not null && left.Equals(right);
        }

        public static bool operator !=(MemberInfo? left, MemberInfo? right) => !(left == right);
    }
}
