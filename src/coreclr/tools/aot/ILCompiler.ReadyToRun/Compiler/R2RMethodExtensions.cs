// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public static class R2RMethodExtensions
    {
        /// <summary>
        /// Gets the EcmaMethod that defines metadata for this method.
        /// Unlike <see cref="MethodDesc.GetTypicalMethodDefinition"/>, this method returns the
        /// EcmaMethod that can be used to get method metadata, including tokens and module information.
        /// For methods like <see cref="AsyncMethodVariant"/> or <see cref="PInvokeTargetNativeMethod"/>,
        /// this returns the wrapped EcmaMethod rather than the synthetic method itself.
        /// </summary>
        /// <param name="method">The method to get the EcmaMethod definition for.</param>
        /// <returns>The EcmaMethod definition, or null if the method is not backed by ECMA metadata.</returns>
        public static EcmaMethod GetEcmaDefinition(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() switch
            {
                EcmaMethod ecmaMethod => ecmaMethod,
                AsyncMethodVariant asyncVariant => asyncVariant.Target,
                PInvokeTargetNativeMethod pinvokeTarget => pinvokeTarget.Target.GetTypicalMethodDefinition() as EcmaMethod,
                _ => null
            };
        }
    }
}
