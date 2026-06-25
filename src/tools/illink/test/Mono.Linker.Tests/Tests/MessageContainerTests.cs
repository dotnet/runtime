// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Xunit;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Tests
{
    public class MessageContainerTests
    {
        [Fact]
        public void MSBuildFormat()
        {
            LinkContext context = new LinkContext(new Pipeline(), new ConsoleLogger(), string.Empty);

            var msg = MessageContainer.CreateCustomErrorMessage("text", 6001);
            Assert.Equal("ILLink: error IL6001: text", msg.ToMSBuildString());

            msg = MessageContainer.CreateCustomWarningMessage(context, "message", 6002, new MessageOrigin("logtest", 1, 1), WarnVersion.Latest);
            Assert.Equal("logtest(1,1): warning IL6002: message", msg.ToMSBuildString());

            msg = MessageContainer.CreateInfoMessage("log test");
            Assert.Equal("ILLink: log test", msg.ToMSBuildString());
        }

        [Fact]
        public void OriginBeforeFirstSequencePointUsesFirstAvailableSequencePoint()
        {
            using ModuleDefinition module = ModuleDefinition.CreateModule("test", ModuleKind.Dll);
            var type = new TypeDefinition("Test", "Type", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(type);

            var method = new MethodDefinition("Method", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            type.Methods.Add(method);

            var body = method.Body;
            var firstInstruction = Instruction.Create(OpCodes.Nop);
            var secondInstruction = Instruction.Create(OpCodes.Ret);
            body.Instructions.Add(firstInstruction);
            body.Instructions.Add(secondInstruction);

            method.DebugInformation.SequencePoints.Add(new SequencePoint(secondInstruction, new Document("test.cs"))
            {
                StartLine = 10,
                StartColumn = 5,
                EndLine = 10,
                EndColumn = 6
            });

            using var assemblyStream = new MemoryStream();
            using var symbolStream = new MemoryStream();

            module.Write(assemblyStream, new WriterParameters
            {
                WriteSymbols = true,
                SymbolStream = symbolStream,
                SymbolWriterProvider = new PortablePdbWriterProvider()
            });

            assemblyStream.Position = 0;
            symbolStream.Position = 0;

            using var assembly = AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters
            {
                ReadSymbols = true,
                SymbolStream = symbolStream,
                SymbolReaderProvider = new PortablePdbReaderProvider()
            });

            method = assembly.MainModule.GetType("Test.Type").Methods.Single(m => m.Name == "Method");
            firstInstruction = method.Body.Instructions[0];
            Assert.True(firstInstruction.Offset < method.DebugInformation.SequencePoints[0].Offset);

            var msg = MessageContainer.CreateCustomErrorMessage(
                "message",
                6001,
                origin: new MessageOrigin(method, firstInstruction.Offset));

            Assert.Equal("test.cs(10,5): error IL6001: Test.Type.Method(): message", msg.ToMSBuildString());
        }
    }
}
