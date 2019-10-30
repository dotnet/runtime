// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Virtual
{
    // Represents a inherited property which is identical to the base property except for its ReflectedType.
    internal partial class InheritedPropertyInfo : DelegatingPropertyInfo
    {
        private readonly Type _reflectedType;

        public InheritedPropertyInfo(PropertyInfo baseProperty, Type reflectedType)
            : base(baseProperty)
        {
            Debug.Assert(reflectedType != null);
            Debug.Assert(reflectedType.IsSubclassOf(baseProperty.DeclaringType));
            Debug.Assert(baseProperty is VirtualPropertyBase);

            // Should we require that baseProperty is a declared property?
            Debug.Assert(baseProperty.ReflectedType.Equals(baseProperty.DeclaringType));

            _reflectedType = reflectedType;
        }

        public override Type ReflectedType
        {
            get
            {
                return _reflectedType;
            }
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            MethodInfo underlyingGetter = UnderlyingProperty.GetGetMethod(nonPublic);
            if (underlyingGetter == null)
                return null;
            else
                return new InheritedMethodInfo(underlyingGetter, _reflectedType);
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            MethodInfo underlyingSetter = UnderlyingProperty.GetSetMethod(nonPublic);
            if (underlyingSetter == null)
                return null;
            else
                return new InheritedMethodInfo(underlyingSetter, _reflectedType);
        }

        public override bool Equals(object o)
        {
            return o is InheritedPropertyInfo other &&
                UnderlyingProperty.Equals(other.UnderlyingProperty) &&
                ReflectedType.Equals(other.ReflectedType);
        }

        public override int GetHashCode()
        {
            return UnderlyingProperty.GetHashCode() ^ ReflectedType.GetHashCode();
        }
    }
}
