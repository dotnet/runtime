using Xunit;
using System.Threading.Tasks;
using VerifyCS = IntrinsicsInSystemPrivateCoreLib.Test.CSharpAnalyzerVerifier<
    IntrinsicsInSystemPrivateCoreLib.IntrinsicsInSystemPrivateCoreLibAnalyzer>;

namespace IntrinsicsInSystemPrivateCoreLib.Test
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class IntrinsicsInSystemPrivateCoreLibUnitTest
    {
        string BoilerPlate = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    internal sealed class CompExactlyDependsOnAttribute : Attribute
    {
        public CompExactlyDependsOnAttribute(Type intrinsicsTypeUsedInHelperFunction)
        {
        }
    }
}

namespace System.Runtime.Intrinsics.X86
{
    class Sse
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
        public class X64
        {
            public static bool IsSupported => true;
            public static bool DoSomethingX64() { return true; }
        }
    }
    class Avx : Sse
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
        public class X64
        {
            public static bool IsSupported => true;
            public static bool DoSomethingX64() { return true; }
        }
    }
    class Avx2 : Avx
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
        public class X64
        {
            public static bool IsSupported => true;
            public static bool DoSomethingX64() { return true; }
        }
    }
}
namespace System.Runtime.Intrinsics.Arm
{
    class ArmBase
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
        public class Arm64
        {
            public static bool IsSupported => true;
            public static bool DoSomethingArm64() { return true; }
        }
    }
}

namespace System.Runtime.Intrinsics.Wasm
{
    class PackedSimd
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
    }
}

";
        [Fact]
        public async Task TestMethodUnprotectedUse()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncBad()
            {
                {|#0:Avx2.DoSomething()|};
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodUnprotectedUseWithIntrinsicsHelperAttribute()
        {
            var test = BoilerPlate + @"

namespace ConsoleApplication1
{
    class TypeName
    {
        [CompExactlyDependsOn(typeof(Avx2))]
        static void FuncGood()
        {
            Avx2.DoSomething();
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodUnprotectedUseWithIntrinsicsHelperAttributeComplex()
        {
            var test = BoilerPlate + @"

namespace ConsoleApplication1
{
    class TypeName
    {
        [CompExactlyDependsOn(typeof(Avx))]
        [CompExactlyDependsOn(typeof(Avx2))]
        static void FuncGood()
        {
            // This tests the behavior of a function which behaves differently when Avx2 is supported (Somehting like Vector128.ShuffleUnsafe)
            if (Avx2.IsSupported)
                Avx2.DoSomething();
            else
                Avx.DoSomething();
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodUnprotectedUseInLocalFunctionWithIntrinsicsHelperAttributeNotOnLocalFunction()
        {
            var test = BoilerPlate + @"
namespace ConsoleApplication1
{
    class TypeName
    {
        [CompExactlyDependsOn(typeof(Avx2))]
        static void FuncBad()
        {
            LocalFunc();

            static void LocalFunc()
            {
                {|#0:Avx2.DoSomething()|};
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodUnprotectedUseInLambdaWithIntrinsicsHelperAttributeOnOuterFunction()
        {
            var test = BoilerPlate + @"

namespace ConsoleApplication1
{
    class TypeName
    {
        [CompExactlyDependsOn(typeof(Avx2))]
        static void FuncBad()
        {
            Action act = () =>
            {
                {|#0:Avx2.DoSomething()|};
            };
            act();
        }
    }
}";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodUnprotectedUseInLocalFunctionWithIntrinsicsHelperAttributeOnLocalFunction()
        {
            var test = BoilerPlate + @"
namespace ConsoleApplication1
{
    class TypeName
    {
        static void FuncBad()
        {
            [CompExactlyDependsOn(typeof(Avx2))]
            static void LocalFunc()
            {
                Avx2.DoSomething();
            }

            if (Avx2.IsSupported)
                LocalFunc();
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodUnprotectedNestedTypeUse()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncBad()
            {
                {|#0:Avx2.X64.DoSomethingX64()|};
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2.X64");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodWithIfStatement()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (Avx2.IsSupported)
                    Avx2.DoSomething();
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [Fact]
        public async Task TestMethodWithIfStatementButWithInadequateHelperMethodAttribute()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx))]
            static void FuncBad()
            {
                if ({|#0:Avx2.IsSupported|})
                    Avx2.DoSomething();
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodWithIfStatementButWithAdequateHelperMethodAttribute()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx2))]
            static void FuncBad()
            {
                if (Avx2.IsSupported)
                    Avx2.DoSomething();
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodWithIfStatementWithNestedAndBaseTypeLookupRequired()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (Avx2.X64.IsSupported)
                    Sse.DoSomething();
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodWithTernaryOperator()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static bool FuncGood()
            {
                return Avx2.IsSupported ? Avx2.DoSomething() : false;
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodWithIfStatementWithOrOperationCase()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (ArmBase.IsSupported || (Avx2.IsSupported && BitConverter.IsLittleEndian))
                {
                    if (ArmBase.IsSupported)
                        ArmBase.DoSomething();
                    else
                        Avx2.DoSomething();

                    if (Avx2.IsSupported)
                        Avx2.DoSomething();
                    else
                        ArmBase.DoSomething();
                }
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodWithIfStatementWithOrOperationCaseWithImplicationProcessingRequired()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (ArmBase.Arm64.IsSupported || (Avx2.IsSupported && BitConverter.IsLittleEndian))
                {
                    if (ArmBase.IsSupported)
                        ArmBase.DoSomething();
                    else
                        Avx2.DoSomething();

                    if (Avx2.IsSupported)
                        Avx2.DoSomething();
                    else
                        ArmBase.DoSomething();
                }
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestMethodWithIfStatementAroundLocalFunctionDefinition()
        {
            var test = BoilerPlate + @"

    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (Avx2.IsSupported)
                {
                    LocalFunction();

                    // Local functions should cause an error to be reported, as they are NOT the same function from a runtime point of view
                    void LocalFunction()
                    {
                        {|#0:Avx2.DoSomething()|};
                    }
                }
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestMethodWithIfStatementAroundLambdaFunctionDefinition()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            static void FuncGood()
            {
                if (Avx2.IsSupported)
                {
                    // Lambda functions should cause an error to be reported, as they are NOT the same function from a runtime point of view
                    Action a = () => 
                    {
                        {|#0:Avx2.DoSomething()|};
                    };
                }
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLib").WithLocation(0).WithArguments("System.Runtime.Intrinsics.X86.Avx2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestHelperMethodsCanOnlyBeCalledWithAppropriateIsSupportedChecksError()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx))]
            static void FuncHelper()
            {
            }

            [CompExactlyDependsOn(typeof(Avx))]
            [CompExactlyDependsOn(typeof(ArmBase))]
            static void FuncHelper2()
            {
            }

            static bool SomeIrrelevantProperty => true;

            static void FuncBad()
            {
                {|#0:FuncHelper()|};
                if (Avx2.IsSupported || ArmBase.IsSupported)
                {
                    {|#1:FuncHelper()|};
                }

                if ({|#3:(Avx.IsSupported || ArmBase.IsSupported) && PackedSimd.IsSupported|})
                {
                    {|#2:FuncHelper2()|};
                }


                if (Avx.IsSupported || (SomeIrrelevantProperty && ArmBase.IsSupported))
                {
                    {|#4:FuncHelper()|};
                }
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibHelper").WithLocation(0).WithArguments("ConsoleApplication1.TypeName.FuncHelper()");
            var expected2 = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibHelper").WithLocation(1).WithArguments("ConsoleApplication1.TypeName.FuncHelper()");
            var expected3 = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibHelper").WithLocation(2).WithArguments("ConsoleApplication1.TypeName.FuncHelper2()");
            var expected4 = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibConditionParsing").WithLocation(3);
            var expected5 = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibHelper").WithLocation(4).WithArguments("ConsoleApplication1.TypeName.FuncHelper()");
            await VerifyCS.VerifyAnalyzerAsync(test, expected, expected2, expected3, expected4, expected5);
        }
        [Fact]
        public async Task TestHelperMethodsCanOnlyBeCalledWithAppropriateIsSupportedChecksSuccess()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx))]
            [CompExactlyDependsOn(typeof(ArmBase))]
            static void FuncHelper()
            {
            }

            static bool SomeIrrelevantProperty => true;
            static void FuncGood()
            {
                if (Avx2.IsSupported)
                {
                    FuncHelper();
                }
                if (ArmBase.IsSupported)
                {
                    FuncHelper();
                }
                if (Avx2.IsSupported || ArmBase.IsSupported)
                {
                    FuncHelper();
                }
                if ((Avx2.IsSupported || ArmBase.IsSupported) && SomeIrrelevantProperty)
                {
                    FuncHelper();
                }
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task TestHelperMethodsUnrelatedPropertyDoesntHelp()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx))]
            [CompExactlyDependsOn(typeof(ArmBase))]
            static void FuncHelper()
            {
            }

            static bool HelperIsSupported => true;

            static void FuncBad()
            {
                if (HelperIsSupported)
                {
                    {|#0:FuncHelper()|};
                }
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("IntrinsicsInSystemPrivateCoreLibHelper").WithLocation(0).WithArguments("ConsoleApplication1.TypeName.FuncHelper()");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TestHelperMethodsWithHelperProperty()
        {
            var test = BoilerPlate + @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            [CompExactlyDependsOn(typeof(Avx))]
            [CompExactlyDependsOn(typeof(ArmBase))]
            static void FuncHelper()
            {
            }

            static bool HelperIsSupported => Avx.IsSupported || ArmBase.IsSupported;

            static void FuncGood()
            {
                if (HelperIsSupported)
                {
                    FuncHelper();
                }
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [Fact]
        public async Task TestMethodUseOfIntrinsicsFromWithinOtherMethodOnIntrinsicType()
        {
            var test = @"
namespace System.Runtime.Intrinsics.X86
{
    class Sse
    {
        public static bool IsSupported => true;
        public static bool DoSomething() { return true; }
        public static bool DoSomethingElse() { return !Sse.DoSomething(); }
        public class X64
        {
            public static bool IsSupported => true;
            public static bool DoSomethingX64() { return !Sse.DoSomething(); }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
