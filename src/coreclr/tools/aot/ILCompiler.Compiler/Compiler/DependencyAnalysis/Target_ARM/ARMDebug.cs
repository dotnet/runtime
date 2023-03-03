// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.ARM;

namespace ILCompiler.DependencyAnalysis
{
    public static class ARMDebug
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void EmitNYIAssert(NodeFactory factory, ref ARMEmitter encoder, string message,
                                         [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = null,
                                         [System.Runtime.CompilerServices.CallerMemberName] string memberName = null,
                                         [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            ISymbolNode NYI_Assert = factory.ExternSymbol("NYI_Assert");
            string CallInfoPrefix = " " + sourceFilePath + "(" + sourceLineNumber.ToString() + "): method " + memberName + ": ";
            ISymbolNode messageSymbol = factory.ConstantUtf8String(CallInfoPrefix + message);
            encoder.EmitMOV(encoder.TargetRegister.Arg0, messageSymbol);
            encoder.EmitJMP(NYI_Assert);
        }

       [System.Diagnostics.Conditional("DEBUG")]
        public static void EmitHelperNYIAssert(NodeFactory factory, ref ARMEmitter encoder, ReadyToRunHelperId hId,
                                               [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = null,
                                               [System.Runtime.CompilerServices.CallerMemberName] string memberName = null,
                                               [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            EmitNYIAssert(factory, ref encoder, hId.ToString() + " is not implemented", sourceFilePath, memberName, sourceLineNumber);
        }
    }
}
