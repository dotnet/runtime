// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public ILCodeStream CleanupCodeStream { get; }
        public PInvokeILCodeStreams()
        {
            Emitter = new ILEmitter();

            // We have these code streams:
            // - FunctionPointerLoadStream is used to load the function pointer to call
            // - MarshallingCodeStream is used to convert each argument into a native type and
            // store that into the local
            // - CallsiteSetupCodeStream is used to used to load each previously generated local
            // and call the actual target native method.
            // - ReturnValueMarshallingCodeStream is used to convert the native return value
            // to managed one.
            // - UnmarshallingCodestream is used to propagate [out] native arguments values to
            // managed ones.
            // - CleanupCodestream is used to perform a guaranteed cleanup
            FunctionPointerLoadStream = Emitter.NewCodeStream();
            MarshallingCodeStream = Emitter.NewCodeStream();
            CallsiteSetupCodeStream = Emitter.NewCodeStream();
            ReturnValueMarshallingCodeStream = Emitter.NewCodeStream();
            UnmarshallingCodestream = Emitter.NewCodeStream();
            CleanupCodeStream = Emitter.NewCodeStream();
        }

        public PInvokeILCodeStreams(ILEmitter emitter, ILCodeStream codeStream)
        {
            Emitter = emitter;
            MarshallingCodeStream = codeStream;
        }
    }
}
