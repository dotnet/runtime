// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace TypeSystemTests
{
    public class ILDisassemblerTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public ILDisassemblerTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = _context.GetModuleForSimpleName("ILTestAssembly");
        }

        [Fact]
        public void TestGenericNameFormatting()
        {
            MetadataType testClass = _testModule.GetType("ILDisassembler", "TestGenericClass`1");
            EcmaMethod testMethod = (EcmaMethod)testClass.GetMethod("TestMethod", null);
            EcmaMethodIL methodIL = EcmaMethodIL.Create(testMethod);

            Dictionary<int, string> interestingLines = new Dictionary<int, string>
            {
                { 4, "IL_0003:  ldstr       \"Hello \\\"World\\\"!\\n\"" },
                { 9, "IL_000D:  call        instance void class ILDisassembler.TestGenericClass`1<!TClassParam>::VoidGenericMethod<string, valuetype ILDisassembler.TestStruct>(!!0, int32, native int, class ILDisassembler.TestClass&)" },
                { 14, "IL_0017:  initobj     !TClassParam" },
                { 16, "IL_001E:  call        !!0 class ILDisassembler.TestGenericClass`1<!TClassParam>::MethodParamGenericMethod<class ILDisassembler.TestClass>(class ILDisassembler.TestGenericClass`1<!!0>, class ILDisassembler.TestGenericClass`1/Nested<!0>, valuetype ILDisassembler.TestStruct*[], !0)" },
                { 24, "IL_0030:  call        !!0 class ILDisassembler.TestGenericClass`1<!TClassParam>::MethodParamGenericMethod<!0>(class ILDisassembler.TestGenericClass`1<!!0>, class ILDisassembler.TestGenericClass`1/Nested<!0>, valuetype ILDisassembler.TestStruct*[], !0)" },
                { 26, "IL_0036:  ldtoken     !TClassParam" },
                { 28, "IL_003C:  ldtoken     valuetype [CoreTestAssembly]System.Nullable`1<int32>" },
                { 31, "IL_0043:  ldc.r8      3.14" },
                { 32, "IL_004C:  ldc.r4      1.68" },
                { 34, "IL_0053:  call        instance valuetype ILDisassembler.TestStruct class ILDisassembler.TestGenericClass`1<!TClassParam>::NonGenericMethod(float64, float32, int16)" },
                { 37, "IL_005A:  ldflda      !0 class ILDisassembler.TestGenericClass`1<!TClassParam>::somefield" },
                { 41, "IL_0067:  stfld       class ILDisassembler.TestClass class ILDisassembler.TestGenericClass`1<!TClassParam>::otherfield" },
                { 44, "IL_006E:  stfld       class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestClass>> class ILDisassembler.TestGenericClass`1<!TClassParam>::genericfield" },
                { 47, "IL_0075:  stfld       !0[] class ILDisassembler.TestGenericClass`1<!TClassParam>::arrayfield" },
                { 48, "IL_007A:  call        void ILDisassembler.TestClass::NonGenericMethod()" },
                { 49, "IL_007F:  ldsflda     valuetype ILDisassembler.TestStruct ILDisassembler.TestClass::somefield" },
                { 50, "IL_0084:  initobj     ILDisassembler.TestStruct" }
            };

            ILDisassembler disasm = new ILDisassembler(methodIL);

            int numLines = 1;
            while (disasm.HasNextInstruction)
            {
                string line = disasm.GetNextInstruction();
                string expectedLine;
                if (interestingLines.TryGetValue(numLines, out expectedLine))
                {
                    Assert.Equal(expectedLine, line);
                }
                numLines++;
            }

            Assert.Equal(52, numLines);
        }
    }
}
