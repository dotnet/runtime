// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// All RoTypes that return true for IsByRef.
    /// </summary>
    internal sealed class RoByRefType : RoHasElementType
    {
        internal RoByRefType(RoType elementType)
            : base(elementType)
        {
            Debug.Assert(elementType != null);
        }

        protected sealed override bool IsArrayImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        protected sealed override bool IsByRefImpl() => true;
        protected sealed override bool IsPointerImpl() => false;

        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);

        protected sealed override TypeAttributes ComputeAttributeFlags() => TypeAttributes.Public;

        protected sealed override string Suffix => "&";

        internal sealed override RoType? ComputeBaseTypeWithoutDesktopQuirk() => null;
        internal sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => Array.Empty<RoType>();

        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => Array.Empty<ConstructorInfo>();
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => Array.Empty<MethodInfo>();
    }
}
