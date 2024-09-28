using System.Diagnostics.Tracing.Tests.GenerateTest;
using Xunit;

namespace System.Diagnostics.Tracing.Tests.GenerateTest
{
    public class EventSourceGeneratedTest
    {
        [Fact]
        public void GenerateEmptyEvent()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                namespace evev
                {
                    [GeneratedEventSourceEvent]
                    internal unsafe partial class TestEventSource : EventSource
                    {
                        [Event(1)]
                        public partial void Event0();
                    }
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.Event0.txt");
        }

        [Fact]
        public void GenerateEmptyEvent_NoNamespace()
        {
            var code = """
                using System.Diagnostics.Tracing;
                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0();
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.NoNs.Event0.txt");
        }

        [Fact]
        public void GenerateOnePrimitive()
        {
            var code = """
                using System.Diagnostics.Tracing;
                

                namespace evev
                {
                    [GeneratedEventSourceEvent]
                    internal unsafe partial class TestEventSource : EventSource
                    {
                        [Event(1)]
                        public partial void Event0(int arg0);
                    }
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.Event1.txt");
        }

        [Fact]
        public void GenerateOnePrimitive_NoNamespace()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0(int arg0);
                }                
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.NoNs.Event1.txt");
        }

        [Fact]
        public void GenerateTwoPrimitive()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                namespace evev
                {
                    [GeneratedEventSourceEvent]
                    internal unsafe partial class TestEventSource : EventSource
                    {
                        [Event(1)]
                        public partial void Event0(int arg0, double arg1);
                    }
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.Event2.txt");
        }

        [Fact]
        public void GenerateTwoPrimitive_NoNamespace()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0(int arg0, double arg1);
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.NoNs.Event2.txt");
        }

        [Fact]
        public void GenerateOneString()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                namespace evev
                {
                    [GeneratedEventSourceEvent]
                    internal unsafe partial class TestEventSource : EventSource
                    {
                        [Event(1)]
                        public partial void Event0(string arg0);
                    }
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.Event3.txt");
        }

        [Fact]
        public void GenerateOneString_NoNamespace()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0(string arg0);
                }                
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.NoNs.Event3.txt");
        }

        [Fact]
        public void GenerateTwoString()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                namespace evev
                {
                    [GeneratedEventSourceEvent]
                    internal unsafe partial class TestEventSource : EventSource
                    {
                        [Event(1)]
                        public partial void Event0(string arg0, string arg1);
                    }
                }
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.Event4.txt");
        }

        [Fact]
        public void GenerateTwoString_NoNamespace()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0(string arg0, string arg1);
                }                
                """;

            Compiler.CheckGeneratedSingle(code, "TestEventSource.NoNs.Event4.txt");
        }

        [Fact]
        public void ParamterIsObject_MustReportEventSourceNoSupportTypeDignostic()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe partial class TestEventSource : EventSource
                {
                    [Event(1)]
                    public partial void Event0(object arg0);
                }                
                """;

            var dignositcs = Compiler.GetDiagnostics(code);
            Assert.Equivalent(1, dignositcs.Length);
            var dignositc = dignositcs[0];
            Assert.Equivalent("SYSLIB2000", dignositc.Id);
            Assert.Equivalent(6, dignositc.Location.GetLineSpan().StartLinePosition.Line);
        }

        [Fact]
        public void NoPartial_MustReportContextClassesMustBePartial()
        {
            var code = """
                using System.Diagnostics.Tracing;                

                [GeneratedEventSourceEvent]
                internal unsafe class TestEventSource : EventSource
                {
                }                
                """;

            var dignositcs = Compiler.GetDiagnostics(code);
            Assert.Equivalent(1, dignositcs.Length);
            var dignositc = dignositcs[0];
            Assert.Equivalent("SYSLIB2001", dignositc.Id);
            Assert.Equivalent(2, dignositc.Location.GetLineSpan().StartLinePosition.Line);
        }
    }
}
