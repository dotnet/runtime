# Browser/WASM CoreCLR Library Tests Plan

## Overview

This document tracks progress for running .NET library tests on the Browser/WASM target with the **CoreCLR virtual machine** (interpreter mode, no JIT, single-thread).

For detailed execution instructions, see 
- [before-testing.md](before-testing.md)
- [test-suite.md](test-suite.md)
- [fixing-problems.md](fixing-problems.md)

## Target Platform Characteristics

| Characteristic | Value |
|----------------|-------|
| OS | Browser (WebAssembly) |
| VM | CoreCLR (interpreter only, no JIT) |
| Reflection | Reflection and Reflection.Emit should be working on CoreCLR interpreter |
| Threading | **Not supported** - no thread creation, no blocking waits |
| Known Issues | C# finalizers don't work, GC memory corruption bugs |
| Test Runner | Xharness (local web server + Chrome browser) |

## Reference Baseline

The same tests already pass on **Mono + Browser**. Results are in:
- [Mono-chrome-workitems.json](Mono-chrome-workitems.json)

Each work item has a `DetailsUrl` that links to Helix logs with `ConsoleOutputUri` showing test summaries.

## Goals

1. Run all library test suites on Browser/WASM + CoreCLR
2. Compare results with Mono baseline
3. Mark failing tests with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
4. Document each failure with full test name and stack trace in `/browser-tests/failures/`

## Decisions Made

| Question | Decision |
|----------|----------|
| GitHub Issue | Use single umbrella issue **#123011** for all Browser+CoreCLR failures |
| Build Configuration | **Release** - faster |
| Failure Categories | Decide when all failures collected (threading, gc, finalizer, interpreter, other) |
| Automation | Keep simple, improve as we go |
| Timeouts | Keep current defaults (`WasmXHarnessTestsTimeout` = 00:30:00) |

## Progress Tracking

### Status Legend

- ⬜ Not started
- 🔄 In progress
- ✅ All tests passing (matches or exceeds Mono baseline)
- ⚠️ Tests marked with ActiveIssue
- ❌ Blocked

### Completed Test Suites

| Test Suite | CoreCLR | Mono Baseline | Status | Notes |
|------------|---------|---------------|--------|-------|
| System.Runtime.InteropServices.JavaScript.Tests | 457 run, 455 pass, 2 skip | 454 run, 452 pass, 2 skip | ✅ | |
| System.Net.Http.Functional.Tests | 901 run, 781 pass, 120 skip | 901 run, 781 pass, 120 skip | ✅ | **Requires Release config** |
| System.Net.WebSockets.Tests | 268 run, 266 pass, 2 skip | 268 run, 266 pass, 2 skip | ✅ | Interpreter assert (non-fatal) |
| System.Linq.AsyncEnumerable.Tests | 613 run, 613 pass, 0 skip | 613 run, 613 pass, 0 skip | ✅ | Interpreter assert (non-fatal) |
| System.Collections.Immutable.Tests | 22420 run, 22279 pass, 85 fail, 56 skip | 22497 run, 22441 pass, 56 skip | ⚠️ | EnumComparer<T> bug - 85 failures |

### Suites Skipped (Windows-only or N/A)

| Test Suite | Reason |
|------------|--------|
| System.Security.AccessControl.Tests | Windows-only (612 failures) |
| System.Security.Principal.Windows.Tests | Windows-only (26 failures) |
| System.ServiceProcess.ServiceController.Tests | Windows-only |

### In Progress

| Test Suite | Status | Notes |
|------------|--------|-------|
| System.ObjectModel.Tests | ⚠️ | 707 failures - BadImageFormatException in KeyedCollection |

### Needs Investigation

| Test Suite | Issue |
|------------|-------|
| System.ObjectModel.Tests | 707 failures with BadImageFormatException in KeyedCollection tests |

## Test Suites to Run (Sorted by Mono Duration)

Total: **203 test suites** (sorted by Mono baseline duration, longest first)

| Mono (min) | Test Suite | csproj Path | Status |
|------------|------------|-------------|--------|
| 0.01 | IcuAppLocal.Tests | src/libraries/System.Runtime/tests/System.Globalization.Tests/IcuAppLocal/IcuAppLocal.Tests.csproj | ✅ |
| 0.01 | MetricOuterLoop1.Tests | src/libraries/System.Diagnostics.DiagnosticSource/tests/MetricOuterLoopTests/MetricOuterLoop1.Tests.csproj | ✅ |
| 0.01 | Microsoft.Bcl.TimeProvider.Tests | src/libraries/Microsoft.Bcl.TimeProvider/tests/Microsoft.Bcl.TimeProvider.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Configuration.UserSecrets.Tests | src/libraries/Microsoft.Extensions.Configuration.UserSecrets/tests/Microsoft.Extensions.Configuration.UserSecrets.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Diagnostics.Abstractions.Tests | src/libraries/Microsoft.Extensions.Diagnostics.Abstractions/tests/Microsoft.Extensions.Diagnostics.Abstractions.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.FileProviders.Composite.Tests | src/libraries/Microsoft.Extensions.FileProviders.Composite/tests/Microsoft.Extensions.FileProviders.Composite.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.HostFactoryResolver.Tests | src/libraries/Microsoft.Extensions.HostFactoryResolver/tests/Microsoft.Extensions.HostFactoryResolver.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Hosting.Abstractions.Tests | src/libraries/Microsoft.Extensions.Hosting.Abstractions/tests/Microsoft.Extensions.Hosting.Abstractions.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Hosting.Systemd.Tests | src/libraries/Microsoft.Extensions.Hosting.Systemd/tests/Microsoft.Extensions.Hosting.Systemd.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Logging.Testing.Tests | src/libraries/Microsoft.Extensions.Logging/tests/DI.Common/Common/tests/Microsoft.Extensions.Logging.Testing.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Options.ConfigurationExtensions.SourceGeneration.Tests | src/libraries/Microsoft.Extensions.Options.ConfigurationExtensions/tests/SourceGenerationTests/Microsoft.Extensions.Options.ConfigurationExtensions.SourceGeneration.Tests.csproj | ✅ |
| 0.01 | Microsoft.Extensions.Options.SourceGeneration.Unit.Tests | src/libraries/Microsoft.Extensions.Options/tests/SourceGeneration.Unit.Tests/Microsoft.Extensions.Options.SourceGeneration.Unit.Tests.csproj | ✅ |
| 0.01 | Microsoft.Win32.Primitives.Tests | src/libraries/Microsoft.Win32.Primitives/tests/Microsoft.Win32.Primitives.Tests.csproj | ✅ |
| 0.01 | Microsoft.XmlSerializer.Generator.Tests | src/libraries/Microsoft.XmlSerializer.Generator/tests/Microsoft.XmlSerializer.Generator.Tests.csproj | ✅ |
| 0.01 | System.ComponentModel.EventBasedAsync.Tests | src/libraries/System.ComponentModel.EventBasedAsync/tests/System.ComponentModel.EventBasedAsync.Tests.csproj | ✅ |
| 0.01 | System.ComponentModel.Tests | src/libraries/System.ComponentModel/tests/System.ComponentModel.Tests.csproj | ✅ |
| 0.01 | System.Composition.AttributeModel.Tests | src/libraries/System.Composition.AttributedModel/tests/System.Composition.AttributeModel.Tests.csproj | ✅ |
| 0.01 | System.Console.Manual.Tests | src/libraries/System.Console/tests/ManualTests/System.Console.Manual.Tests.csproj | ✅ |
| 0.01 | System.Diagnostics.Contracts.Tests | src/libraries/System.Diagnostics.Contracts/tests/System.Diagnostics.Contracts.Tests.csproj | ✅ |
| 0.01 | System.Diagnostics.DiagnosticSource.Switches.Tests | src/libraries/System.Diagnostics.DiagnosticSource/tests/TestWithConfigSwitches/System.Diagnostics.DiagnosticSource.Switches.Tests.csproj | ✅ |
| 0.01 | System.Diagnostics.Tools.Tests | src/libraries/System.Runtime/tests/System.Diagnostics.Tools.Tests/System.Diagnostics.Tools.Tests.csproj | ✅ |
| 0.01 | System.Diagnostics.TraceSource.Config.Tests | src/libraries/System.Diagnostics.TraceSource/tests/System.Diagnostics.TraceSource.Config.Tests/System.Diagnostics.TraceSource.Config.Tests.csproj | ✅ |
| 0.01 | System.Formats.Tar.Manual.Tests | src/libraries/System.Formats.Tar/tests/Manual/System.Formats.Tar.Manual.Tests.csproj | ✅ |
| 0.01 | System.IO.FileSystem.DriveInfo.Tests | src/libraries/System.IO.FileSystem.DriveInfo/tests/System.IO.FileSystem.DriveInfo.Tests.csproj | ✅ |
| 0.01 | System.IO.FileSystem.Manual.Tests | src/libraries/System.Runtime/tests/System.IO.FileSystem.Tests/ManualTests/System.IO.FileSystem.Manual.Tests.csproj | ✅ |
| 0.01 | System.IO.FileSystem.Primitives.Tests | src/libraries/System.Runtime/tests/System.IO.FileSystem.Primitives.Tests/System.IO.FileSystem.Primitives.Tests.csproj | ✅ |
| 0.01 | System.Net.Http.Enterprise.Tests | src/libraries/System.Net.Http/tests/EnterpriseTests/System.Net.Http.Enterprise.Tests.csproj | ✅ |
| 0.01 | System.Net.Primitives.Pal.Tests | src/libraries/System.Net.Primitives/tests/PalTests/System.Net.Primitives.Pal.Tests.csproj | ✅ |
| 0.01 | System.Net.Security.Enterprise.Tests | src/libraries/System.Net.Security/tests/EnterpriseTests/System.Net.Security.Enterprise.Tests.csproj | ✅ |
| 0.01 | System.Reflection.CoreCLR.Tests | src/libraries/System.Runtime/tests/System.Reflection.Tests/CoreCLR/System.Reflection.CoreCLR.Tests.csproj | ✅ |
| 0.01 | System.Resources.Reader.Tests | src/libraries/System.Runtime/tests/System.Resources.Reader.Tests/System.Resources.Reader.Tests.csproj | ✅ |
| 0.01 | System.Resources.Writer.Tests | src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj | ✅ |
| 0.01 | System.Runtime.CompilerServices.VisualC.Tests | src/libraries/System.Runtime.CompilerServices.VisualC/tests/System.Runtime.CompilerServices.VisualC.Tests.csproj | ✅ |
| 0.01 | System.Runtime.Handles.Tests | src/libraries/System.Runtime/tests/System.Runtime.Handles.Tests/System.Runtime.Handles.Tests.csproj | ✅ |
| 0.01 | System.Runtime.InteropServices.RuntimeInformation.Tests | src/libraries/System.Runtime/tests/System.Runtime.InteropServices.RuntimeInformation.Tests/System.Runtime.InteropServices.RuntimeInformation.Tests.csproj | ✅ |
| 0.01 | System.Runtime.InvariantTimezone.Tests | src/libraries/System.Runtime/tests/System.Runtime.Tests/InvariantTimezone/System.Runtime.InvariantTimezone.Tests.csproj | ✅ |
| 0.01 | System.Runtime.Loader.DefaultContext.Tests | src/libraries/System.Runtime.Loader/tests/DefaultContext/System.Runtime.Loader.DefaultContext.Tests.csproj | ✅ |
| 0.01 | System.Runtime.Loader.RefEmitLoadContext.Tests | src/libraries/System.Runtime.Loader/tests/RefEmitLoadContext/System.Runtime.Loader.RefEmitLoadContext.Tests.csproj | ✅ |
| 0.01 | System.Security.Cryptography.ProtectedData.Tests | src/libraries/System.Security.Cryptography.ProtectedData/tests/System.Security.Cryptography.ProtectedData.Tests.csproj | ⬜ |
| 0.01 | System.Text.Encoding.Extensions.Tests | src/libraries/System.Text.Encoding.Extensions/tests/System.Text.Encoding.Extensions.Tests.csproj | ⬜ |
| 0.01 | System.Threading.ThreadPool.Tests | src/libraries/System.Threading.ThreadPool/tests/System.Threading.ThreadPool.Tests.csproj | ⬜ |
| 0.01 | System.Xml.Linq.Axes.Tests | src/libraries/System.Private.Xml.Linq/tests/axes/System.Xml.Linq.Axes.Tests.csproj | ⬜ |
| 0.01 | System.Xml.Schema.Extensions.Tests | src/libraries/System.Private.Xml.Linq/tests/Schema/System.Xml.Schema.Extensions.Tests.csproj | ⬜ |
| 0.02 | MetricOuterLoop.Tests | src/libraries/System.Diagnostics.DiagnosticSource/tests/MetricOuterLoopTests/MetricOuterLoop.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.CommandLine.Tests | src/libraries/Microsoft.Extensions.Configuration.CommandLine/tests/Microsoft.Extensions.Configuration.CommandLine.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.EnvironmentVariables.Tests | src/libraries/Microsoft.Extensions.Configuration.EnvironmentVariables/tests/Microsoft.Extensions.Configuration.EnvironmentVariables.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.FileExtensions.Tests | src/libraries/Microsoft.Extensions.Configuration.FileExtensions/tests/Microsoft.Extensions.Configuration.FileExtensions.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.Functional.Tests | src/libraries/Microsoft.Extensions.Configuration/tests/FunctionalTests/Microsoft.Extensions.Configuration.Functional.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.Ini.Tests | src/libraries/Microsoft.Extensions.Configuration.Ini/tests/Microsoft.Extensions.Configuration.Ini.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.Json.Tests | src/libraries/Microsoft.Extensions.Configuration.Json/tests/Microsoft.Extensions.Configuration.Json.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Configuration.Xml.Tests | src/libraries/Microsoft.Extensions.Configuration.Xml/tests/Microsoft.Extensions.Configuration.Xml.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Diagnostics.Tests | src/libraries/Microsoft.Extensions.Diagnostics/tests/Microsoft.Extensions.Diagnostics.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Logging.Console.Tests | src/libraries/Microsoft.Extensions.Logging.Console/tests/Microsoft.Extensions.Logging.Console.Tests/Microsoft.Extensions.Logging.Console.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Logging.EventSource.Tests | src/libraries/Microsoft.Extensions.Logging.EventSource/tests/Microsoft.Extensions.Logging.EventSource.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Options.SourceGeneration.Tests | src/libraries/Microsoft.Extensions.Options/tests/SourceGenerationTests/Microsoft.Extensions.Options.SourceGeneration.Tests.csproj | ⬜ |
| 0.02 | Microsoft.Extensions.Options.Tests | src/libraries/Microsoft.Extensions.Options/tests/Microsoft.Extensions.Options.Tests/Microsoft.Extensions.Options.Tests.csproj | ⬜ |
| 0.02 | System.ComponentModel.Primitives.Tests | src/libraries/System.ComponentModel.Primitives/tests/System.ComponentModel.Primitives.Tests.csproj | ⬜ |
| 0.02 | System.Composition.Convention.Tests | src/libraries/System.Composition.Convention/tests/System.Composition.Convention.Tests.csproj | ⬜ |
| 0.02 | System.Composition.Hosting.Tests | src/libraries/System.Composition.Hosting/tests/System.Composition.Hosting.Tests.csproj | ⬜ |
| 0.02 | System.Composition.Runtime.Tests | src/libraries/System.Composition.Runtime/tests/System.Composition.Runtime.Tests.csproj | ⬜ |
| 0.02 | System.Composition.TypedParts.Tests | src/libraries/System.Composition.TypedParts/tests/System.Composition.TypedParts.Tests.csproj | ⬜ |
| 0.02 | System.Data.DataSetExtensions.Tests | src/libraries/System.Data.Common/tests/System.Data.DataSetExtensions.Tests/System.Data.DataSetExtensions.Tests.csproj | ⬜ |
| 0.02 | System.Formats.Nrbf.Tests | src/libraries/System.Formats.Nrbf/tests/System.Formats.Nrbf.Tests.csproj | ⬜ |
| 0.02 | System.IO.UnmanagedMemoryStream.Tests | src/libraries/System.Runtime/tests/System.IO.UnmanagedMemoryStream.Tests/System.IO.UnmanagedMemoryStream.Tests.csproj | ⬜ |
| 0.02 | System.Memory.Data.Tests | src/libraries/System.Memory.Data/tests/System.Memory.Data.Tests.csproj | ⬜ |
| 0.02 | System.Net.Http.Json.Functional.Tests | src/libraries/System.Net.Http.Json/tests/FunctionalTests/System.Net.Http.Json.Functional.Tests.csproj | ⬜ |
| 0.02 | System.Net.Primitives.UnitTests.Tests | src/libraries/System.Net.Primitives/tests/UnitTests/System.Net.Primitives.UnitTests.Tests.csproj | ⬜ |
| 0.02 | System.Net.WebHeaderCollection.Tests | src/libraries/System.Net.WebHeaderCollection/tests/System.Net.WebHeaderCollection.Tests.csproj | ⬜ |
| 0.02 | System.Net.WebProxy.Tests | src/libraries/System.Net.WebProxy/tests/System.Net.WebProxy.Tests.csproj | ⬜ |
| 0.02 | System.Private.Uri.ExtendedFunctional.Tests | src/libraries/System.Private.Uri/tests/ExtendedFunctionalTests/System.Private.Uri.ExtendedFunctional.Tests.csproj | ⬜ |
| 0.02 | System.Reflection.Context.Tests | src/libraries/System.Reflection.Context/tests/System.Reflection.Context.Tests.csproj | ⬜ |
| 0.02 | System.Reflection.DispatchProxy.Tests | src/libraries/System.Reflection.DispatchProxy/tests/System.Reflection.DispatchProxy.Tests.csproj | ⬜ |
| 0.02 | System.Reflection.Emit.Lightweight.Tests | src/libraries/System.Reflection.Emit.Lightweight/tests/System.Reflection.Emit.Lightweight.Tests.csproj | ⬜ |
| 0.02 | System.Reflection.Extensions.Tests | src/libraries/System.Reflection.Extensions/tests/System.Reflection.Extensions.Tests.csproj | ⬜ |
| 0.02 | System.Reflection.TypeExtensions.Tests | src/libraries/System.Reflection.TypeExtensions/tests/System.Reflection.TypeExtensions.Tests.csproj | ⬜ |
| 0.02 | System.Runtime.ReflectionInvokeEmit.Tests | src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Reflection/InvokeEmit/System.Runtime.ReflectionInvokeEmit.Tests.csproj | ⬜ |
| 0.02 | System.Runtime.ReflectionInvokeInterpreted.Tests | src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Reflection/InvokeInterpreted/System.Runtime.ReflectionInvokeInterpreted.Tests.csproj | ⬜ |
| 0.02 | System.Runtime.Serialization.Primitives.Tests | src/libraries/System.Runtime.Serialization.Primitives/tests/System.Runtime.Serialization.Primitives.Tests.csproj | ⬜ |
| 0.02 | System.Security.Claims.Tests | src/libraries/System.Security.Claims/tests/System.Security.Claims.Tests.csproj | ⬜ |
| 0.02 | System.Security.SecureString.Tests | src/libraries/System.Runtime/tests/System.Security.SecureString.Tests/System.Security.SecureString.Tests.csproj | ⬜ |
| 0.02 | System.Threading.Overlapped.Tests | src/libraries/System.Threading.Overlapped/tests/System.Threading.Overlapped.Tests.csproj | ⬜ |
| 0.02 | System.Threading.Thread.Tests | src/libraries/System.Threading.Thread/tests/System.Threading.Thread.Tests.csproj | ⬜ |
| 0.02 | System.Threading.Timer.Tests | src/libraries/System.Runtime/tests/System.Threading.Timer.Tests/System.Threading.Timer.Tests.csproj | ⬜ |
| 0.02 | System.ValueTuple.Tests | src/libraries/System.Runtime/tests/System.ValueTuple.Tests/System.ValueTuple.Tests.csproj | ⬜ |
| 0.02 | System.Xml.Linq.SDMSample.Tests | src/libraries/System.Private.Xml.Linq/tests/SDMSample/System.Xml.Linq.SDMSample.Tests.csproj | ⬜ |
| 0.02 | System.Xml.Linq.TreeManipulation.Tests | src/libraries/System.Private.Xml.Linq/tests/TreeManipulation/System.Xml.Linq.TreeManipulation.Tests.csproj | ⬜ |
| 0.02 | System.Xml.Linq.xNodeBuilder.Tests | src/libraries/System.Private.Xml.Linq/tests/xNodeBuilder/System.Xml.Linq.xNodeBuilder.Tests.csproj | ⬜ |
| 0.03 | Invariant.Tests | src/libraries/System.Runtime/tests/System.Globalization.Tests/Invariant/Invariant.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Bcl.AsyncInterfaces.Tests | src/libraries/Microsoft.Bcl.AsyncInterfaces/tests/Microsoft.Bcl.AsyncInterfaces.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Bcl.Numerics.Tests | src/libraries/Microsoft.Bcl.Numerics/tests/Microsoft.Bcl.Numerics.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Extensions.Configuration.Tests | src/libraries/Microsoft.Extensions.Configuration/tests/Microsoft.Extensions.Configuration.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Extensions.DependencyInjection.ExternalContainers.Tests | src/libraries/Microsoft.Extensions.DependencyInjection/tests/DI.External.Tests/Microsoft.Extensions.DependencyInjection.ExternalContainers.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Extensions.Http.Tests | src/libraries/Microsoft.Extensions.Http/tests/Microsoft.Extensions.Http.Tests/Microsoft.Extensions.Http.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Extensions.Logging.Tests | src/libraries/Microsoft.Extensions.Logging/tests/Common/Microsoft.Extensions.Logging.Tests.csproj | ⬜ |
| 0.03 | Microsoft.Extensions.Primitives.Tests | src/libraries/Microsoft.Extensions.Primitives/tests/Microsoft.Extensions.Primitives.Tests.csproj | ⬜ |
| 0.03 | System.ComponentModel.Composition.Registration.Tests | src/libraries/System.ComponentModel.Composition.Registration/tests/System.ComponentModel.Composition.Registration.Tests.csproj | ⬜ |
| 0.03 | System.Diagnostics.StackTrace.Tests | src/libraries/System.Diagnostics.StackTrace/tests/System.Diagnostics.StackTrace.Tests.csproj | ⬜ |
| 0.03 | System.Diagnostics.TextWriterTraceListener.Tests | src/libraries/System.Diagnostics.TextWriterTraceListener/tests/System.Diagnostics.TextWriterTraceListener.Tests.csproj | ⬜ |
| 0.03 | System.Diagnostics.TraceSource.Tests | src/libraries/System.Diagnostics.TraceSource/tests/System.Diagnostics.TraceSource.Tests/System.Diagnostics.TraceSource.Tests.csproj | ⬜ |
| 0.03 | System.Net.Http.Json.Unit.Tests | src/libraries/System.Net.Http.Json/tests/UnitTests/System.Net.Http.Json.Unit.Tests.csproj | ⬜ |
| 0.03 | System.Net.Mail.Functional.Tests | src/libraries/System.Net.Mail/tests/Functional/System.Net.Mail.Functional.Tests.csproj | ⬜ |
| 0.03 | System.Net.Mail.Unit.Tests | src/libraries/System.Net.Mail/tests/Unit/System.Net.Mail.Unit.Tests.csproj | ⬜ |
| 0.03 | System.Reflection.Emit.ILGeneration.Tests | src/libraries/System.Reflection.Emit.ILGeneration/tests/System.Reflection.Emit.ILGeneration.Tests.csproj | ⬜ |
| 0.03 | System.Reflection.InvokeEmit.Tests | src/libraries/System.Runtime/tests/System.Reflection.Tests/InvokeEmit/System.Reflection.InvokeEmit.Tests.csproj | ⬜ |
| 0.03 | System.Reflection.InvokeInterpreted.Tests | src/libraries/System.Runtime/tests/System.Reflection.Tests/InvokeInterpreted/System.Reflection.InvokeInterpreted.Tests.csproj | ⬜ |
| 0.03 | System.Runtime.Serialization.Xml.Canonicalization.Tests | src/libraries/System.Runtime.Serialization.Xml/tests/Canonicalization/System.Runtime.Serialization.Xml.Canonicalization.Tests.csproj | ⬜ |
| 0.03 | System.Threading.Tasks.Parallel.Tests | src/libraries/System.Threading.Tasks.Parallel/tests/System.Threading.Tasks.Parallel.Tests.csproj | ⬜ |
| 0.03 | System.Transactions.Local.Tests | src/libraries/System.Transactions.Local/tests/System.Transactions.Local.Tests.csproj | ⬜ |
| 0.03 | System.Xml.Linq.Properties.Tests | src/libraries/System.Private.Xml.Linq/tests/Properties/System.Xml.Linq.Properties.Tests.csproj | ⬜ |
| 0.03 | System.Xml.Linq.xNodeReader.Tests | src/libraries/System.Private.Xml.Linq/tests/xNodeReader/System.Xml.Linq.xNodeReader.Tests.csproj | ⬜ |
| 0.04 | Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests | src/libraries/Microsoft.Extensions.Configuration.Binder/tests/SourceGenerationTests/Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests.csproj | ⬜ |
| 0.04 | Microsoft.Extensions.Configuration.Binder.Tests | src/libraries/Microsoft.Extensions.Configuration.Binder/tests/UnitTests/Microsoft.Extensions.Configuration.Binder.Tests.csproj | ⬜ |
| 0.04 | System.Diagnostics.DiagnosticSource.Tests | src/libraries/System.Diagnostics.DiagnosticSource/tests/System.Diagnostics.DiagnosticSource.Tests.csproj | ⬜ |
| 0.04 | System.Net.ServerSentEvents.Tests | src/libraries/System.Net.ServerSentEvents/tests/System.Net.ServerSentEvents.Tests.csproj | ⬜ |
| 0.04 | System.Runtime.Loader.Tests | src/libraries/System.Runtime.Loader/tests/System.Runtime.Loader.Tests.csproj | ⬜ |
| 0.04 | System.Text.Encoding.CodePages.Tests | src/libraries/System.Text.Encoding.CodePages/tests/System.Text.Encoding.CodePages.Tests.csproj | ⬜ |
| 0.04 | System.Web.HttpUtility.Tests | src/libraries/System.Web.HttpUtility/tests/System.Web.HttpUtility.Tests.csproj | ⬜ |
| 0.05 | System.ComponentModel.Annotations.Tests | src/libraries/System.ComponentModel.Annotations/tests/System.ComponentModel.Annotations.Tests.csproj | ⬜ |
| 0.05 | System.Composition.Tests | src/libraries/System.Composition/tests/System.Composition.Tests.csproj | ⬜ |
| 0.05 | System.Drawing.Primitives.Tests | src/libraries/System.Drawing.Primitives/tests/System.Drawing.Primitives.Tests.csproj | ⬜ |
| 0.05 | System.IO.Hashing.Tests | src/libraries/System.IO.Hashing/tests/System.IO.Hashing.Tests.csproj | ⬜ |
| 0.05 | System.IO.MemoryMappedFiles.Tests | src/libraries/System.IO.MemoryMappedFiles/tests/System.IO.MemoryMappedFiles.Tests.csproj | ⬜ |
| 0.05 | System.Runtime.CompilerServices.Unsafe.Tests | src/libraries/System.Runtime/tests/System.Runtime.CompilerServices.Unsafe.Tests/System.Runtime.CompilerServices.Unsafe.Tests.csproj | ⬜ |
| 0.05 | System.Runtime.Serialization.Json.ReflectionOnly.Tests | src/libraries/System.Runtime.Serialization.Json/tests/ReflectionOnly/System.Runtime.Serialization.Json.ReflectionOnly.Tests.csproj | ⬜ |
| 0.05 | System.Runtime.Serialization.Json.Tests | src/libraries/System.Runtime.Serialization.Json/tests/System.Runtime.Serialization.Json.Tests.csproj | ⬜ |
| 0.05 | System.Threading.Tasks.Tests | src/libraries/System.Runtime/tests/System.Threading.Tasks.Tests/System.Threading.Tasks.Tests.csproj | ⬜ |
| 0.06 | System.Buffers.Tests | src/libraries/System.Runtime/tests/System.Buffers.Tests/System.Buffers.Tests.csproj | ⬜ |
| 0.06 | System.Diagnostics.Tracing.Tests | src/libraries/System.Diagnostics.Tracing/tests/System.Diagnostics.Tracing.Tests.csproj | ⬜ |
| 0.06 | System.IO.Packaging.Tests | src/libraries/System.IO.Packaging/tests/System.IO.Packaging.Tests.csproj | ⬜ |
| 0.06 | System.Runtime.Serialization.Schema.Tests | src/libraries/System.Runtime.Serialization.Schema/tests/System.Runtime.Serialization.Schema.Tests.csproj | ⬜ |
| 0.06 | System.Text.RegularExpressions.Unit.Tests | src/libraries/System.Text.RegularExpressions/tests/UnitTests/System.Text.RegularExpressions.Unit.Tests.csproj | ⬜ |
| 0.07 | Microsoft.Bcl.Cryptography.Tests | src/libraries/Microsoft.Bcl.Cryptography/tests/Microsoft.Bcl.Cryptography.Tests.csproj | ⬜ |
| 0.07 | System.Globalization.Calendars.Tests | src/libraries/System.Runtime/tests/System.Globalization.Calendars.Tests/System.Globalization.Calendars.Tests.csproj | ⬜ |
| 0.07 | System.Linq.Queryable.Tests | src/libraries/System.Linq.Queryable/tests/System.Linq.Queryable.Tests.csproj | ⬜ |
| 0.07 | System.Xml.XmlSerializer.ReflectionOnly.Tests | src/libraries/System.Private.Xml/tests/XmlSerializer/ReflectionOnly/System.Xml.XmlSerializer.ReflectionOnly.Tests.csproj | ⬜ |
| 0.08 | Microsoft.Extensions.FileSystemGlobbing.Tests | src/libraries/Microsoft.Extensions.FileSystemGlobbing/tests/Microsoft.Extensions.FileSystemGlobbing.Tests.csproj | ⬜ |
| 0.08 | System.Formats.Cbor.Tests | src/libraries/System.Formats.Cbor/tests/System.Formats.Cbor.Tests.csproj | ⬜ |
| 0.08 | System.Reflection.MetadataLoadContext.Tests | src/libraries/System.Reflection.MetadataLoadContext/tests/System.Reflection.MetadataLoadContext.Tests.csproj | ⬜ |
| 0.08 | System.Threading.Tasks.Extensions.Tests | src/libraries/System.Runtime/tests/System.Threading.Tasks.Extensions.Tests/System.Threading.Tasks.Extensions.Tests.csproj | ⬜ |
| 0.09 | System.Console.Tests | src/libraries/System.Console/tests/System.Console.Tests.csproj | ⬜ |
| 0.09 | System.Formats.Asn1.Tests | src/libraries/System.Formats.Asn1/tests/System.Formats.Asn1.Tests.csproj | ⬜ |
| 0.09 | System.Net.Primitives.Functional.Tests | src/libraries/System.Net.Primitives/tests/FunctionalTests/System.Net.Primitives.Functional.Tests.csproj | ⬜ |
| 0.09 | System.Reflection.Emit.Tests | src/libraries/System.Reflection.Emit/tests/System.Reflection.Emit.Tests.csproj | ⬜ |
| 0.09 | System.Threading.Channels.Tests | src/libraries/System.Threading.Channels/tests/System.Threading.Channels.Tests.csproj | ⬜ |
| 0.09 | System.Threading.RateLimiting.Tests | src/libraries/System.Threading.RateLimiting/tests/System.Threading.RateLimiting.Tests.csproj | ⬜ |
| 0.10 | System.Reflection.Metadata.Tests | src/libraries/System.Reflection.Metadata/tests/System.Reflection.Metadata.Tests.csproj | ⬜ |
| 0.10 | System.Reflection.Tests | src/libraries/System.Runtime/tests/System.Reflection.Tests/System.Reflection.Tests.csproj | ⬜ |
| 0.10 | System.Runtime.InteropServices.Tests | src/libraries/System.Runtime.InteropServices/tests/System.Runtime.InteropServices.UnitTests/System.Runtime.InteropServices.Tests.csproj | ⬜ |
| 0.10 | System.ServiceModel.Syndication.Tests | src/libraries/System.ServiceModel.Syndication/tests/System.ServiceModel.Syndication.Tests.csproj | ⬜ |
| 0.11 | System.Collections.Specialized.Tests | src/libraries/System.Collections.Specialized/tests/System.Collections.Specialized.Tests.csproj | ⬜ |
| 0.11 | System.Net.WebSockets.Tests | src/libraries/System.Net.WebSockets/tests/System.Net.WebSockets.Tests.csproj | ⬜ |
| 0.12 | System.Xml.Linq.Misc.Tests | src/libraries/System.Private.Xml.Linq/tests/misc/System.Xml.Linq.Misc.Tests.csproj | ⬜ |
| 0.13 | System.Xml.Linq.Streaming.Tests | src/libraries/System.Private.Xml.Linq/tests/Streaming/System.Xml.Linq.Streaming.Tests.csproj | ⬜ |
| 0.14 | System.CodeDom.Tests | src/libraries/System.CodeDom/tests/System.CodeDom.Tests.csproj | ⬜ |
| 0.14 | System.IO.Compression.ZipFile.Tests | src/libraries/System.IO.Compression.ZipFile/tests/System.IO.Compression.ZipFile.Tests.csproj | ⬜ |
| 0.15 | System.Collections.Concurrent.Tests | src/libraries/System.Collections.Concurrent/tests/System.Collections.Concurrent.Tests.csproj | ⬜ |
| 0.15 | System.Runtime.Caching.Tests | src/libraries/System.Runtime.Caching/tests/System.Runtime.Caching.Tests.csproj | ⬜ |
| 0.16 | System.Collections.NonGeneric.Tests | src/libraries/System.Collections.NonGeneric/tests/System.Collections.NonGeneric.Tests.csproj | ⬜ |
| 0.16 | System.ComponentModel.TypeConverter.Tests | src/libraries/System.ComponentModel.TypeConverter/tests/System.ComponentModel.TypeConverter.Tests.csproj | ⬜ |
| 0.17 | Microsoft.Extensions.DependencyInjection.Tests | src/libraries/Microsoft.Extensions.DependencyInjection/tests/DI.Tests/Microsoft.Extensions.DependencyInjection.Tests.csproj | ⬜ |
| 0.18 | System.Runtime.Serialization.Xml.ReflectionOnly.Tests | src/libraries/System.Runtime.Serialization.Xml/tests/ReflectionOnly/System.Runtime.Serialization.Xml.ReflectionOnly.Tests.csproj | ⬜ |
| 0.19 | Common.Tests | src/libraries/Common/tests/Common.Tests.csproj | ⬜ |
| 0.19 | System.Runtime.Intrinsics.Tests | src/libraries/System.Runtime.Intrinsics/tests/System.Runtime.Intrinsics.Tests.csproj | ⬜ |
| 0.19 | System.Runtime.Serialization.Xml.Tests | src/libraries/System.Runtime.Serialization.Xml/tests/System.Runtime.Serialization.Xml.Tests.csproj | ⬜ |
| 0.21 | System.IO.Pipelines.Tests | src/libraries/System.IO.Pipelines/tests/System.IO.Pipelines.Tests.csproj | ⬜ |
| 0.21 | System.ObjectModel.Tests | src/libraries/System.ObjectModel/tests/System.ObjectModel.Tests.csproj | ⚠️ |
| 0.22 | Microsoft.CSharp.Tests | src/libraries/Microsoft.CSharp/tests/Microsoft.CSharp.Tests.csproj | ⬜ |
| 0.22 | System.Linq.AsyncEnumerable.Tests | src/libraries/System.Linq.AsyncEnumerable/tests/System.Linq.AsyncEnumerable.Tests.csproj | ⬜ |
| 0.23 | System.Xml.Linq.Events.Tests | src/libraries/System.Private.Xml.Linq/tests/events/System.Xml.Linq.Events.Tests.csproj | ⬜ |
| 0.24 | System.Globalization.Extensions.Tests | src/libraries/System.Runtime/tests/System.Globalization.Extensions.Tests/System.Globalization.Extensions.Tests.csproj | ⬜ |
| 0.28 | System.Runtime.InteropServices.JavaScript.Tests | src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj | ⬜ |
| 0.29 | Microsoft.Extensions.FileProviders.Physical.Tests | src/libraries/Microsoft.Extensions.FileProviders.Physical/tests/Microsoft.Extensions.FileProviders.Physical.Tests.csproj | ⬜ |
| 0.30 | System.Threading.Tasks.Dataflow.Tests | src/libraries/System.Threading.Tasks.Dataflow/tests/System.Threading.Tasks.Dataflow.Tests.csproj | ⬜ |
| 0.30 | System.Threading.Tests | src/libraries/System.Threading/tests/System.Threading.Tests.csproj | ⬜ |
| 0.31 | Microsoft.Bcl.Memory.Tests | src/libraries/Microsoft.Bcl.Memory/tests/Microsoft.Bcl.Memory.Tests.csproj | ⬜ |
| 0.31 | System.Private.Uri.Unit.Tests | src/libraries/System.Private.Uri/tests/UnitTests/System.Private.Uri.Unit.Tests.csproj | ⬜ |
| 0.35 | System.Numerics.Vectors.Tests | src/libraries/System.Numerics.Vectors/tests/System.Numerics.Vectors.Tests.csproj | ⬜ |
| 0.36 | System.Linq.Parallel.Tests | src/libraries/System.Linq.Parallel/tests/System.Linq.Parallel.Tests.csproj | ⬜ |
| 0.37 | Microsoft.VisualBasic.Core.Tests | src/libraries/Microsoft.VisualBasic.Core/tests/Microsoft.VisualBasic.Core.Tests.csproj | ⬜ |
| 0.38 | System.Runtime.Extensions.Tests | src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System.Runtime.Extensions.Tests.csproj | ⬜ |
| 0.38 | System.Text.Encoding.Tests | src/libraries/System.Runtime/tests/System.Text.Encoding.Tests/System.Text.Encoding.Tests.csproj | ⬜ |
| 0.39 | System.Data.Common.Tests | src/libraries/System.Data.Common/tests/System.Data.Common.Tests.csproj | ⬜ |
| 0.46 | System.Net.Http.Unit.Tests | src/libraries/System.Net.Http/tests/UnitTests/System.Net.Http.Unit.Tests.csproj | ⬜ |
| 0.55 | System.IO.FileSystem.Tests | src/libraries/System.Runtime/tests/System.IO.FileSystem.Tests/System.IO.FileSystem.Tests.csproj | ⬜ |
| 0.60 | System.Security.Cryptography.Tests | src/libraries/System.Security.Cryptography/tests/System.Security.Cryptography.Tests.csproj | ⬜ |
| 0.64 | System.Dynamic.Runtime.Tests | src/libraries/System.Runtime/tests/System.Dynamic.Runtime.Tests/System.Dynamic.Runtime.Tests.csproj | ⬜ |
| 0.67 | System.Globalization.Tests | src/libraries/System.Runtime/tests/System.Globalization.Tests/System.Globalization.Tests.csproj | ⬜ |
| 0.79 | System.Net.Http.Functional.Tests | src/libraries/System.Net.Http/tests/FunctionalTests/System.Net.Http.Functional.Tests.csproj | ⬜ |
| 0.80 | System.IO.Compression.Tests | src/libraries/System.IO.Compression/tests/System.IO.Compression.Tests.csproj | ⬜ |
| 0.92 | System.Linq.Expressions.Tests | src/libraries/System.Linq.Expressions/tests/System.Linq.Expressions.Tests.csproj | ⬜ |
| 0.93 | System.Text.RegularExpressions.Tests | src/libraries/System.Text.RegularExpressions/tests/FunctionalTests/System.Text.RegularExpressions.Tests.csproj | ⬜ |
| 1.29 | System.Private.Uri.Functional.Tests | src/libraries/System.Private.Uri/tests/FunctionalTests/System.Private.Uri.Functional.Tests.csproj | ⬜ |
| 1.29 | System.Text.Encodings.Web.Tests | src/libraries/System.Text.Encodings.Web/tests/System.Text.Encodings.Web.Tests.csproj | ⬜ |
| 1.33 | System.Collections.Tests | src/libraries/System.Collections/tests/System.Collections.Tests.csproj | ⬜ |
| 1.94 | System.Runtime.Tests | src/libraries/System.Runtime/tests/System.Runtime.Tests/System.Runtime.Tests.csproj | ⚠️ |
| 2.14 | System.Collections.Immutable.Tests | src/libraries/System.Collections.Immutable/tests/System.Collections.Immutable.Tests.csproj | ⬜ |
| 2.15 | System.Runtime.Numerics.Tests | src/libraries/System.Runtime.Numerics/tests/System.Runtime.Numerics.Tests.csproj | ⬜ |
| 2.16 | System.Private.Xml.Tests | src/libraries/System.Private.Xml/tests/System.Private.Xml.Tests.csproj | ⬜ |
| 2.75 | System.Net.WebSockets.Client.Tests | src/libraries/System.Net.WebSockets.Client/tests/System.Net.WebSockets.Client.Tests.csproj | ⬜ |
| 7.15 | System.Linq.Tests | src/libraries/System.Linq/tests/System.Linq.Tests.csproj | ⬜ |
| 7.24 | System.Memory.Tests | src/libraries/System.Memory/tests/System.Memory.Tests.csproj | ⬜ |
| 8.00 | System.IO.Tests | src/libraries/System.Runtime/tests/System.IO.Tests/System.IO.Tests.csproj | ⬜ |
| 9.00 | System.Text.Json.Tests | src/libraries/System.Text.Json/tests/System.Text.Json.Tests/System.Text.Json.Tests.csproj | ⬜ |

**Legend:** ⬜ Not started | ✅ Passing | ⚠️ Has ActiveIssue | ❌ Blocked
