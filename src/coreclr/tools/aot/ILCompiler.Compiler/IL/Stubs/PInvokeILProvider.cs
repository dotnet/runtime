// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    /// <summary>
    /// Wraps the API and configuration for a particular PInvoke IL emitter. Eventually this will
    /// allow ILProvider to switch out its PInvoke IL generator with another, such as MCG.
    /// </summary>
    public class PInvokeILProvider : ILProvider
    {
        private readonly PInvokeILEmitterConfiguration _pInvokeILEmitterConfiguration;
        private readonly InteropStateManager _interopStateManager;

        public PInvokeILProvider(PInvokeILEmitterConfiguration pInvokeILEmitterConfiguration, InteropStateManager interopStateManager)
        {
            _pInvokeILEmitterConfiguration = pInvokeILEmitterConfiguration;
            _interopStateManager = interopStateManager;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            return PInvokeILEmitter.EmitIL(method, _pInvokeILEmitterConfiguration, _interopStateManager);
        }

        public MethodDesc GetCalliStub(MethodSignature signature, ModuleDesc moduleContext)
        {
            return _interopStateManager.GetPInvokeCalliStub(signature, moduleContext);
        }

        public string GetDirectCallExternName(MethodDesc method)
        {
            bool directCall = _pInvokeILEmitterConfiguration.GenerateDirectCall(method, out string externName);
            Debug.Assert(directCall);
            Debug.Assert(externName != null);
            return externName;
        }
    }
}
