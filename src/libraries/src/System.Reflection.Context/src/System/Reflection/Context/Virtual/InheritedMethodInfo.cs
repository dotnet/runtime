// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Virtual
{
    // Represents a inherited method which is identical to the base method except for its ReflectedType.
    internal partial class InheritedMethodInfo : DelegatingMethodInfo
    {
        private readonly Type _reflectedType;

        public InheritedMethodInfo(MethodInfo baseMethod, Type reflectedType)
            : base(baseMethod)
        {
            Debug.Assert(reflectedType != null);
            Debug.Assert(reflectedType.IsSubclassOf(baseMethod.DeclaringType));
            Debug.Assert(baseMethod is VirtualMethodBase);

            // Should we require that baseMethod is a declared method?
            Debug.Assert(baseMethod.ReflectedType.Equals(baseMethod.DeclaringType));

            _reflectedType = reflectedType;
        }

        public override Type ReflectedType
        {
            get
            {
                return _reflectedType;
            }
        }

        public override bool Equals(object o)
        {
            var other = o as InheritedMethodInfo;

            return other != null &&
                   UnderlyingMethod.Equals(other.UnderlyingMethod) &&
                   ReflectedType.Equals(other.ReflectedType);
        }

        public override int GetHashCode()
        {
            return UnderlyingMethod.GetHashCode() ^ ReflectedType.GetHashCode();
        }
    }
}
