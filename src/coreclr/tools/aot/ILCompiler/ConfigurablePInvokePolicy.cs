// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class ConfigurablePInvokePolicy : PInvokeILEmitterConfiguration
    {
        private readonly TargetDetails _target;
        private readonly DirectPInvokeSet _directPInvokes;

        public ConfigurablePInvokePolicy(TargetDetails target, IReadOnlyList<string> directPInvokes, IReadOnlyList<string> directPInvokeLists)
        {
            _directPInvokes = new DirectPInvokeSet(target, directPInvokes, directPInvokeLists);
            _target = target;
        }

        public override LinkageMetadata GetLinkageMetadata(MethodDesc method)
        {
            var pInvokeMetadata = method.GetPInvokeMethodMetadata();
            MethodDesc target = method is PInvokeTargetNativeMethod nativeMethod ? nativeMethod.Target : method;
            if (_target.IsWasm && target.HasCustomAttribute("System.Runtime.InteropServices", "WasmImportLinkageAttribute"))
            {
                Utf8String importName = pInvokeMetadata.Name is null
                    ? new Utf8String(method.Name.AsSpan())
                    : new Utf8String(pInvokeMetadata.Name);
                return new WasmLinkageMetadata(importName, new Utf8String(pInvokeMetadata.Module));
            }

            if (_directPInvokes.TryGetDirectEntryPoint(method, out string entryPoint))
            {
                return new LinkageMetadata(new Utf8String(entryPoint));
            }

            return null;
        }
    }
}
