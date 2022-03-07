// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.IL.Stubs
{
    public static class DebuggerSteppingHelpers
    {
        public static void MarkDebuggerStepThroughPoint(this ILCodeStream codeStream)
        {
            codeStream.DefineSequencePoint("", 0xF00F00);
        }

        public static void MarkDebuggerStepInPoint(this ILCodeStream codeStream)
        {
            codeStream.DefineSequencePoint("", 0xFEEFEE);
        }
    }
}
