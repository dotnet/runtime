using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using FluentAssertions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextJsonWriterTests
    {
        public JObject Save(DependencyContext dependencyContext)
        {
            using (var memoryStream = new MemoryStream())
            {
                new DependencyContextWriter().Write(dependencyContext, memoryStream);
                using (var readStream = new MemoryStream(memoryStream.ToArray()))
                {
                    using (var textReader = new StreamReader(readStream))
                    {
                        using (var reader = new JsonTextReader(textReader))
                        {
                            return JObject.Load(reader);
                        }
                    }
                }
            }
        }

        [Fact]
        public void SavesRuntimeGraph()
        {
            var result = Save(new DependencyContext(
                            "Target",
                            "Target/runtime",
                            false,
                            CompilationOptions.Default,
                            new CompilationLibrary[0],
                            new RuntimeLibrary[0],
                            new[]
                            {
                                new KeyValuePair<string, string[]>("win7-x64", new [] { "win6", "win5"}),
                                new KeyValuePair<string, string[]>("win8-x64", new [] { "win7-x64"}),
                            }));

            var runtimes = result.Should().HaveProperty("runtimes")
                .Subject.Should().BeOfType<JObject>().Subject;

            var rids = runtimes.Should().HaveProperty("Target")
                .Subject.Should().BeOfType<JObject>().Subject;

            rids.Should().HaveProperty("win7-x64")
                .Subject.Should().BeOfType<JArray>()
                .Which.Values<string>().ShouldBeEquivalentTo(new[] { "win6", "win5" });

            rids.Should().HaveProperty("win8-x64")
                .Subject.Should().BeOfType<JArray>()
                .Which.Values<string>().ShouldBeEquivalentTo(new[] { "win7-x64" });
        }

        [Fact]
        public void WritesRuntimeTargetPropertyIfNotPortable()
        {
            var result = Save(new DependencyContext(
                            "Target",
                            "runtime",
                            false,
                            CompilationOptions.Default,
                            new CompilationLibrary[0],
                            new RuntimeLibrary[0],
                            new KeyValuePair<string, string[]>[0])
                            );

            var runtimeTarget = result.Should().HaveProperty("runtimeTarget")
                .Subject.Should().BeOfType<JObject>().Subject;

            runtimeTarget.Should().HaveProperty("name")
                .Subject.Value<string>().Should().Be("Target/runtime");

            runtimeTarget.Should().HaveProperty("portable")
                .Subject.Value<bool>().Should().Be(false);
        }
        [Fact]
        public void DoesNotWritesRuntimeTargetPropertyIfPortable()
        {
            var result = Save(new DependencyContext(
                            "Target",
                            "runtime",
                            false,
                            CompilationOptions.Default,
                            new CompilationLibrary[0],
                            new RuntimeLibrary[0],
                            new KeyValuePair<string, string[]>[0])
                            );

            result.Should().NotHaveProperty("runtimeTarget");
        }
    }
}
