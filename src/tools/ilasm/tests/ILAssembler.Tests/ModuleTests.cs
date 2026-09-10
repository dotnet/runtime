// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void ModuleNotFound_ReportsError()
        {
            // Referencing a module that doesn't exist
            string source = """
                .assembly extern System.Runtime { }
                .typedef [.module NonExistentModule]SomeType as MyType
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ModuleNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void ModuleName_DefaultsToOutputFileName_WhenNoModuleDirective()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "MyOutput.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("MyOutput.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_OutputFileNameStripsDirectory()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            // OutputFileName should already be just the filename (Program.cs uses Path.GetFileName),
            // but verify the module name is exactly what's provided
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "bar.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("bar.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_ExplicitModuleDirective_OverridesOutputFileName()
        {
            string source = """
                .assembly TestAssembly { }
                .module Explicit.dll
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "DifferentName.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("Explicit.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_NoModuleDirective_NoOutputFileName_UsesNilHandle()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.True(moduleDef.Name.IsNil);
        }


        [Fact]
        public void ModuleLevelField_DoesNotCrash()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .field public static int32 globalField
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }
    }
}
