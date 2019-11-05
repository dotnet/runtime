// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.IL.Stubs
{
    internal sealed class PInvokeILCodeStreams
    {
        public ILEmitter Emitter { get; }
        public ILCodeStream FunctionPointerLoadStream { get; }
        public ILCodeStream MarshallingCodeStream { get; }
        public ILCodeStream CallsiteSetupCodeStream { get; }
        public ILCodeStream ReturnValueMarshallingCodeStream { get; }
        public ILCodeStream UnmarshallingCodestream { get; }
        public PInvokeILCodeStreams()
        {
            Emitter = new ILEmitter();

            // We have 4 code streams:
            // - _marshallingCodeStream is used to convert each argument into a native type and 
            // store that into the local
            // - callsiteSetupCodeStream is used to used to load each previously generated local
            // and call the actual target native method.
            // - _returnValueMarshallingCodeStream is used to convert the native return value 
            // to managed one.
            // - _unmarshallingCodestream is used to propagate [out] native arguments values to 
            // managed ones.
            FunctionPointerLoadStream = Emitter.NewCodeStream();
            MarshallingCodeStream = Emitter.NewCodeStream();
            CallsiteSetupCodeStream = Emitter.NewCodeStream();
            ReturnValueMarshallingCodeStream = Emitter.NewCodeStream();
            UnmarshallingCodestream = Emitter.NewCodeStream();
        }

        public PInvokeILCodeStreams(ILEmitter emitter, ILCodeStream codeStream)
        {
            Emitter = emitter;
            MarshallingCodeStream = codeStream;
        }
    }
}