// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class BodySubstitution
    {
        private object _value;

        private static readonly object Throw = new object();

        public static readonly BodySubstitution ThrowingBody = new BodySubstitution(Throw);
        public static readonly BodySubstitution EmptyBody = new BodySubstitution(null);

        public object Value
        {
            get
            {
                Debug.Assert(_value != Throw);
                return _value;
            }
        }

        private BodySubstitution(object value) => _value = value;

        public static BodySubstitution Create(object value) => new BodySubstitution(value);
        public MethodIL EmitIL(MethodDesc method)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codestream = emit.NewCodeStream();

            if (_value == Throw)
            {
                codestream.EmitCallThrowHelper(emit, method.Context.GetHelperEntryPoint("ThrowHelpers", "ThrowFeatureBodyRemoved"));
            }
            else if (_value == null)
            {
                Debug.Assert(method.Signature.ReturnType.IsVoid);
                codestream.Emit(ILOpcode.ret);
            }
            else
            {
                Debug.Assert(_value is int);
                codestream.EmitLdc((int)_value);
                codestream.Emit(ILOpcode.ret);
            }

            return emit.Link(method);
        }
    }
}
