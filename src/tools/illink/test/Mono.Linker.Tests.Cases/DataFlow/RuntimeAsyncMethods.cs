// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [SetupCompileArgument("/features:runtime-async=on")]
    [SetupCompileArgument("/nowarn:SYSLIB5007")]
    public class RuntimeAsyncMethods
    {
        public static async Task Main()
        {
            await BasicRuntimeAsyncMethod();
            await RuntimeAsyncWithDataFlowAnnotations(null);
            await RuntimeAsyncWithCapturedLocalDataFlow();
            await RuntimeAsyncWithMultipleAwaits();
            await RuntimeAsyncWithReassignment(true);
            await RuntimeAsyncReturningAnnotatedType();
            await RuntimeAsyncWithCorrectParameter(null);
            await RuntimeAsyncWithLocalAll();
        }

        static async Task BasicRuntimeAsyncMethod()
        {
            await Task.Delay(1);
            Console.WriteLine("Basic runtime async");
        }

        [ExpectedWarning("IL2067", "type", nameof(DataFlowTypeExtensions.RequiresAll), nameof(RuntimeAsyncWithDataFlowAnnotations))]
        static async Task RuntimeAsyncWithDataFlowAnnotations([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
            await Task.Delay(1);
            type.RequiresAll();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresAll), nameof(GetWithPublicMethods))]
        static async Task RuntimeAsyncWithCapturedLocalDataFlow()
        {
            Type t = GetWithPublicMethods();
            await Task.Delay(1);
            t.RequiresAll();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresAll), nameof(GetWithPublicMethods))]
        static async Task RuntimeAsyncWithMultipleAwaits()
        {
            Type t = GetWithPublicMethods();
            await Task.Delay(1);
            t.RequiresPublicMethods();
            await Task.Delay(1);
            t.RequiresAll();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresAll), nameof(GetWithPublicMethods))]
        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresAll), nameof(GetWithPublicFields))]
        static async Task RuntimeAsyncWithReassignment(bool condition)
        {
            Type t = GetWithPublicMethods();
            await Task.Delay(1);
            if (condition)
            {
                t = GetWithPublicFields();
            }
            await Task.Delay(1);
            t.RequiresAll();
        }

        [ExpectedWarning("IL2106", nameof(RuntimeAsyncReturningAnnotatedType))]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static async Task<Type> RuntimeAsyncReturningAnnotatedType()
        {
            await Task.Delay(1);
            return GetWithPublicMethods();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type GetWithPublicMethods() => null;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type GetWithPublicFields() => null;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        static Type GetWithAllMembers() => null;

        static async Task RuntimeAsyncWithCorrectParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            type ??= GetWithAllMembers();
            await Task.Delay(1);
            type.RequiresAll();
        }

        static async Task RuntimeAsyncWithLocalAll()
        {
            Type t = GetWithAllMembers();
            await Task.Delay(1);
            t.RequiresAll();
        }
    }
}
