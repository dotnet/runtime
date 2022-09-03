// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Controls the way calling convention conversion is performed in
    /// <see cref="System.Reflection.MethodBase.Invoke(object, object[])"/>
    /// scenarios.
    /// </summary>
    public abstract class DynamicInvokeThunkGenerationPolicy
    {
        /// <summary>
        /// Gets a value indicating whether reflection-invokable method '<paramref name="targetMethod"/>'
        /// should get a static calling convention conversion thunk. Static calling convention
        /// conversion thunks speed up reflection invoke of the method at the cost of extra code generation.
        /// </summary>
        public abstract bool HasStaticInvokeThunk(MethodDesc targetMethod);
    }

    /// <summary>
    /// A thunk generation policy that generates no static invocation thunks.
    /// </summary>
    public sealed class NoDynamicInvokeThunkGenerationPolicy : DynamicInvokeThunkGenerationPolicy
    {
        public override bool HasStaticInvokeThunk(MethodDesc targetMethod) => false;
    }

    /// <summary>
    /// A thunk generation policy that uses static invocation thunks whenever possible.
    /// </summary>
    public sealed class DefaultDynamicInvokeThunkGenerationPolicy : DynamicInvokeThunkGenerationPolicy
    {
        public override bool HasStaticInvokeThunk(MethodDesc targetMethod) => true;
    }
}
