// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

namespace ILCompiler
{
    internal sealed class ConfigurablePInvokePolicy : PInvokeILEmitterConfiguration
    {
        private readonly TargetDetails _target;
        private readonly DirectPInvokeSet _directPInvokes;

        public ConfigurablePInvokePolicy(TargetDetails target, IReadOnlyList<string> directPInvokes, IReadOnlyList<string> directPInvokeLists)
        {
            _directPInvokes = new DirectPInvokeSet(target, directPInvokes, directPInvokeLists);

            // * is always a direct call
            _directPInvokes.Add("*");

            _target = target;
        }

        private IEnumerable<string> EntryPointNameVariations(string name, PInvokeFlags flags)
        {
            if (_target.IsWindows && !flags.ExactSpelling)
            {
                // Mirror CharSet normalization from Marshaller.CreateMarshaller
                bool isAnsi = flags.CharSet switch
                {
                    CharSet.Ansi => true,
                    CharSet.Unicode => false,
                    CharSet.Auto => false,
                    _ => true
                };

                if (isAnsi)
                {
                    // For ANSI, look for the user-provided entry point name first.
                    // If that does not exist, try the charset suffix.
                    yield return name;
                    yield return name + "A";
                }
                else
                {
                    // For Unicode, look for the entry point name with the charset suffix first.
                    // The 'W' API takes precedence over the undecorated one.
                    yield return name + "W";
                    yield return name;
                }
            }
            else
            {
                yield return name;
            }
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

            foreach (var moduleName in MarshalHelpers.GetPInvokeModuleNameVariations(_target, pInvokeMetadata.Module))
            {
                if (_directPInvokes.TryGetEntryPoints(moduleName, out IReadOnlySet<string> entrypoints))
                {
                    string entryPointMetadataName = pInvokeMetadata.Name ?? method.GetName();

                    if (entrypoints == null)
                    {
                        return new LinkageMetadata(new Utf8String(entryPointMetadataName));
                    }

                    foreach (var entryPointName in EntryPointNameVariations(entryPointMetadataName, pInvokeMetadata.Flags))
                    {
                        if (entrypoints.Contains(entryPointName))
                        {
                            return new LinkageMetadata(new Utf8String(entryPointName));
                        }
                    }
                }
            }

            return null;
        }
    }
}
