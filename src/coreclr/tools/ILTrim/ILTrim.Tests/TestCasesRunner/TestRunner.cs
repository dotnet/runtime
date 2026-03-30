// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public partial class TestRunner
    {
        partial void IgnoreTest(string reason)
        {
            Assert.Ignore(reason);
        }

        private partial IEnumerable<string>? GetAdditionalDefines() => null;

        private static T GetResultOfTaskThatMakesAssertions<T>(Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException e)
            {
                if (e.InnerException is AssertionException
                    || e.InnerException is SuccessException
                    || e.InnerException is IgnoreException
                    || e.InnerException is InconclusiveException)
                    throw e.InnerException;

                throw;
            }
        }

        protected virtual partial TrimmingCustomizations? CustomizeTrimming(TrimmingDriver linker, TestCaseMetadataProvider metadataProvider)
            => null;

        protected partial void AddDumpDependenciesOptions(TestCaseLinkerOptions caseDefinedOptions, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder, TestCaseMetadataProvider metadataProvider)
        {
        }

        static partial void AddOutputDirectory(TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder)
        {
            builder.AddOutputDirectory(sandbox.OutputDirectory);
        }

        static partial void AddInputReference(NPath inputReference, TrimmingArgumentBuilder builder)
        {
            if (inputReference.FileNameWithoutExtension != "Mono.Linker.Tests.Cases.Expectations")
                builder.AddLinkAssembly(inputReference);
            builder.AddReference(inputReference);
        }
    }
}
