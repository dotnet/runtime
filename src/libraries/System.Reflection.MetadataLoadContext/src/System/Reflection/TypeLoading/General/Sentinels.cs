// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    // 
    // These sentinel instances are used only for lazy-evaluation latches when "null" is a valid value for that property.
    //
    internal static class Sentinels
    {
        public static readonly RoType RoType = new SentinelType();
        public static readonly RoMethod RoMethod = new SentinelMethod();

        private sealed class SentinelType : RoStubType
        {
            internal SentinelType() : base() { }
        }

        private sealed class SentinelAssembly : RoStubAssembly
        {
            internal SentinelAssembly() : base() { }
        }

        private sealed class SentinelMethod : RoMethod
        {
            internal SentinelMethod() : base(Sentinels.RoType) { }
            internal sealed override RoType GetRoDeclaringType() => throw null;
            internal sealed override RoModule GetRoModule() => throw null;
            public sealed override int MetadataToken => throw null;
            public sealed override IEnumerable<CustomAttributeData> CustomAttributes => throw null;
            public sealed override bool IsConstructedGenericMethod => throw null;
            public sealed override bool IsGenericMethodDefinition => throw null;
            public sealed override bool Equals(object obj) => throw null;
            public sealed override MethodInfo GetGenericMethodDefinition() => throw null;
            public sealed override int GetHashCode() => throw null;
            public sealed override MethodBody GetMethodBody() => throw null;
            public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments) => throw null;
            protected sealed override MethodAttributes ComputeAttributes() => throw null;
            protected sealed override CallingConventions ComputeCallingConvention() => throw null;
            protected sealed override RoType[] ComputeGenericArgumentsOrParameters() => throw null;
            protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => throw null;
            protected sealed override MethodSig<RoParameter> ComputeMethodSig() => throw null;
            protected sealed override MethodSig<RoType> ComputeCustomModifiers() => throw null;
            protected sealed override MethodSig<string> ComputeMethodSigStrings() => throw null;
            protected sealed override string ComputeName() => throw null;
            internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => throw null;
            internal sealed override RoType[] GetGenericTypeParametersNoCopy() => throw null;
            public sealed override TypeContext TypeContext => throw null;
        }
    }
}
