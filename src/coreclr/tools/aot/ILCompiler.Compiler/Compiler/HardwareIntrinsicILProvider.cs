// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class HardwareIntrinsicILProvider : ILProvider
    {
        private readonly InstructionSetSupport _isaSupport;
        private readonly TypeSystemContext _context;
        private readonly FieldDesc _isSupportedField;
        private readonly ILProvider _nestedProvider;
        private readonly Dictionary<string, InstructionSet> _instructionSetMap;

        public HardwareIntrinsicILProvider(InstructionSetSupport isaSupport, FieldDesc isSupportedField, ILProvider nestedProvider)
        {
            _isaSupport = isaSupport;
            _context = isSupportedField.Context;
            _isSupportedField = isSupportedField;
            _nestedProvider = nestedProvider;

            _instructionSetMap = new Dictionary<string, InstructionSet>();
            foreach (var instructionSetInfo in InstructionSetFlags.ArchitectureToValidInstructionSets(_context.Target.Architecture))
            {
                if (instructionSetInfo.ManagedName != "")
                    _instructionSetMap.Add(instructionSetInfo.ManagedName, instructionSetInfo.InstructionSet);
            }
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;
            string intrinsicId = InstructionSetSupport.GetHardwareIntrinsicId(_context.Target.Architecture, owningType);
            if (!string.IsNullOrEmpty(intrinsicId)
                && HardwareIntrinsicHelpers.IsIsSupportedMethod(method))
            {
                InstructionSet instructionSet = _instructionSetMap[intrinsicId];

                bool isSupported = _isaSupport.IsInstructionSetSupported(instructionSet);
                bool isOptimisticallySupported = _isaSupport.OptimisticFlags.HasInstructionSet(instructionSet);

                // If this is an instruction set that is optimistically supported, but is not one of the
                // intrinsics that are known to be always available, emit IL that checks the support level
                // at runtime.
                if (!isSupported && isOptimisticallySupported)
                {
                    return HardwareIntrinsicHelpers.EmitIsSupportedIL(method, _isSupportedField, instructionSet);
                }
                else
                {
                    ILOpcode flag = isSupported ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0;
                    return new ILStubMethodIL(method,
                        new byte[] { (byte)flag, (byte)ILOpcode.ret },
                        Array.Empty<LocalVariableDefinition>(),
                        null);
                }
            }

            return _nestedProvider.GetMethodIL(method);
        }
    }
}
