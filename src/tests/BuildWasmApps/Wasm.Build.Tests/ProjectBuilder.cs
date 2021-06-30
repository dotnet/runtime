using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Wasm.Build.Tests
{
    public class ProjectBuilder
    {
        List<string> _properties = new();
        List<string> _items = new();
        string _directoryPropsContents = "<Project />";
        string _directoryTargetsContents = "<Project />";
        string _prefixText = string.Empty;
        string _suffixText = string.Empty;
        private string _projectTemplate;

        private const string s_simpleProjectTemplate =
            @$"<Project Sdk=""Microsoft.NET.Sdk"">
              ##INSERT_AT_START##
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <WasmGenerateRunV8Script>true</WasmGenerateRunV8Script>
                <WasmMainJSPath>runtime-test.js</WasmMainJSPath>
                ##EXTRA_PROPERTIES##
              </PropertyGroup>
              <ItemGroup>
                ##EXTRA_ITEMS##
              </ItemGroup>
              ##INSERT_AT_END##
            </Project>";

        public ProjectBuilder(string template=s_simpleProjectTemplate)
        {
            _projectTemplate = template;
        }

        public ProjectBuilder WithProperties(string propertyElement)
        {
            _properties.Add(propertyElement);
            return this;
        }

        public ProjectBuilder WithItems(string itemElement)
        {
            _items.Add(itemElement);
            return this;
        }

        public ProjectBuilder WithDirectoryBuildProps(string propsContents)
        {
            _directoryPropsContents = propsContents;
            return this;
        }

        public ProjectBuilder WithDirectoryBuildTargets(string propsContents)
        {
            _directoryTargetsContents = propsContents;
            return this;
        }

        public ProjectBuilder WithPrefixText(string prefix)
        {
            _prefixText = prefix;
            return this;
        }

        public ProjectBuilder WithSuffixText(string suffix)
        {
            _suffixText = suffix;
            return this;
        }

        public BuildArgs Generate(string dir, BuildArgs buildArgs)
        {
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), _directoryPropsContents);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), _directoryTargetsContents);

            if (buildArgs.AOT)
            {
                _properties.Add("$<RunAOTCompilation>true</RunAOTCompilation>");
                _properties.Add("<EmccVerbose>false</EmccVerbose>\n");
            }

            string projectContents = _projectTemplate
                                        .Replace("##EXTRA_PROPERTIES##", string.Join(Environment.NewLine, _properties))
                                        .Replace("##EXTRA_ITEMS##", string.Join(Environment.NewLine, _items))
                                        .Replace("##INSERT_AT_START##", _prefixText)
                                        .Replace("##INSERT_AT_END##", _suffixText);

            buildArgs = buildArgs with { ProjectFileContents = projectContents };
            File.WriteAllText(Path.Combine(dir, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);

            return buildArgs;
        }
    }
}
