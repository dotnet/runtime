// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.JavaScript
{
    internal sealed class ImplicitArgumentGenerator(TypePositionInfo info, StubCodeContext codeContext) : BaseJSGenerator(info, codeContext)
    {
        public override void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup)
            {
                var (_, js) = context.GetIdentifiers(TypeInfo);
                writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_CompilerServices_Unsafe}.SkipInit(out {js});");
                // Unlike the other arguments, the implicit arguments establish ambient state for the import.
                writer.WriteLine($"{js}.Initialize();");
            }
        }
    }
}
