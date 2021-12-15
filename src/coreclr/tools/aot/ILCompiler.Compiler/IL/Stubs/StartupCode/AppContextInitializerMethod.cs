// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace Internal.IL.Stubs.StartupCode
{
    public sealed partial class AppContextInitializerMethod : ILStubMethod
    {
        private TypeDesc _owningType;
        private MethodSignature _signature;
        private IReadOnlyCollection<KeyValuePair<string, string>> _switches;

        public AppContextInitializerMethod(TypeDesc owningType, IEnumerable<string> args)
        {
            _owningType = owningType;
            var switches = new List<KeyValuePair<string, string>>();

            foreach (string s in args)
            {
                int index = s.IndexOf('=');
                if (index <= 0)
                    throw new ArgumentException($"String '{s}' in unexpected format. Expected 'Key=Value'");
                switches.Add(KeyValuePair.Create(s.Substring(0, index), s.Substring(index + 1)));
            }

            _switches = switches;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override string Name
        {
            get
            {
                return "SetAppContextSwitches";
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            MetadataType appContextType = Context.SystemModule.GetKnownType("System", "AppContext");
            MethodDesc setDataMethod = appContextType.GetKnownMethod("SetData", null);
            ILToken setDataToken = emitter.NewToken(setDataMethod);

            foreach (KeyValuePair<string, string> keyValue in _switches)
            {
                codeStream.Emit(ILOpcode.ldstr, emitter.NewToken(keyValue.Key));
                codeStream.Emit(ILOpcode.ldstr, emitter.NewToken(keyValue.Value));
                codeStream.Emit(ILOpcode.call, setDataToken);
            }

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    _signature = new MethodSignature(MethodSignatureFlags.Static, 0,
                            Context.GetWellKnownType(WellKnownType.Void),
                            TypeDesc.EmptyTypes);
                }

                return _signature;
            }
        }
    }
}
