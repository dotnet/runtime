# Area Label Heuristics

When checking whether an issue has the correct `area-*` label, use these
heuristics to map issue content to the correct area. Always cross-reference
with the authoritative [`docs/area-owners.md`](../../../../docs/area-owners.md).

## Quick-Reference: Namespace → Area Label

| Issue mentions... | Correct area label |
|---|---|
| `System.Text.Json`, JSON serialization | `area-System.Text.Json` |
| `System.Xml`, XmlReader/Writer, XDocument | `area-System.Xml` |
| `System.Net.Http`, HttpClient | `area-System.Net.Http` |
| `System.Net.Sockets`, Socket, TCP/UDP | `area-System.Net.Sockets` |
| `System.Net.Security`, SslStream, TLS | `area-System.Net.Security` |
| `System.Net.Quic`, QUIC protocol | `area-System.Net.Quic` |
| `System.Net`, Uri, DNS, general networking | `area-System.Net` |
| `System.IO`, File, Stream, Path, Directory | `area-System.IO` |
| `System.IO.Compression`, zip, gzip, brotli, tar | `area-System.IO.Compression` |
| `System.IO.Pipelines`, Pipe, PipeReader/Writer | `area-System.IO.Pipelines` |
| `System.Collections`, List, Dictionary, HashSet | `area-System.Collections` |
| `System.Linq`, LINQ operators, Enumerable | `area-System.Linq` |
| `System.Threading`, Thread, Task, async, locks | `area-System.Threading` |
| `System.Threading.Channels`, Channel | `area-System.Threading.Channels` |
| `System.Threading.Tasks.Dataflow` | `area-System.Threading.Tasks.Dataflow` |
| `System.Reflection`, MethodInfo, TypeInfo | `area-System.Reflection` |
| `System.Reflection.Emit`, IL generation | `area-System.Reflection.Emit` |
| `System.Runtime.InteropServices`, P/Invoke, marshalling | `area-Interop-coreclr` |
| `System.Security.Cryptography`, encryption, hashing, X509 | `area-System.Security` |
| `System.Diagnostics`, Process, EventSource, DiagnosticSource | `area-System.Diagnostics` |
| `System.Diagnostics.Tracing`, EventPipe, ETW | `area-System.Diagnostics.Tracing` |
| `System.Globalization`, CultureInfo, ICU | `area-System.Globalization` |
| `System.Numerics`, Vector, Matrix, BigInteger | `area-System.Numerics` |
| `System.Numerics.Tensors`, TensorPrimitives | `area-System.Numerics.Tensors` |
| `System.Runtime.Intrinsics`, SIMD, AVX, SSE, Arm | `area-System.Runtime.Intrinsics` |
| `System.Text.RegularExpressions`, Regex | `area-System.Text.RegularExpressions` |
| `System.Text.Encoding`, Encoding, UTF-8 | `area-System.Text.Encoding` |
| `System.Memory`, Span, Memory, ArrayPool | `area-System.Memory` |
| `System.Buffers`, IBufferWriter, ReadOnlySequence | `area-System.Buffers` |
| `System.Formats.Asn1`, ASN.1 | `area-System.Formats.Asn1` |
| `System.Formats.Cbor`, CBOR | `area-System.Formats.Cbor` |
| `System.Formats.Tar`, tar archives | `area-System.Formats.Tar` |
| `System.Console`, Console I/O | `area-System.Console` |
| `System.Drawing`, GDI+, basic graphics | `area-System.Drawing` |
| `Microsoft.Extensions.DependencyInjection` | `area-Extensions-DependencyInjection` |
| `Microsoft.Extensions.Logging`, ILogger | `area-Extensions-Logging` |
| `Microsoft.Extensions.Configuration` | `area-Extensions-Configuration` |
| `Microsoft.Extensions.Hosting`, IHost | `area-Extensions-Hosting` |
| `Microsoft.Extensions.Options`, IOptions | `area-Extensions-Options` |
| `Microsoft.Extensions.Caching`, IMemoryCache | `area-Extensions-Caching` |
| `Microsoft.Extensions.FileProviders` | `area-Extensions-FileSystem` |
| `Microsoft.Extensions.Http`, IHttpClientFactory | `area-Extensions-HttpClientFactory` |
| GC, garbage collection, memory pressure | `area-GC-coreclr` or `area-GC-mono` |
| JIT, code generation, inlining, tiered compilation | `area-CodeGen-coreclr` |
| NativeAOT, ahead-of-time compilation | `area-NativeAOT-coreclr` |
| Crossgen2, R2R, ReadyToRun, R2RDump | `area-ReadyToRun` |
| Assembly loading, AssemblyLoadContext, host, hostfxr, hostpolicy, HostModel | `area-assemblyloading` |
| Interop, COM, P/Invoke, marshalling (runtime) | `area-Interop-coreclr` |
| Single-file deployment | `area-Single-File` |
| Exception handling (runtime-level), PAL, platform abstraction layer | `area-vm-coreclr` |
| Debugger, debugging support | `area-Diagnostics-coreclr` |
| `System.ComponentModel`, component model base types | `area-System.ComponentModel` |
| `System.ComponentModel.DataAnnotations`, validation attributes | `area-System.ComponentModel.DataAnnotations` |
| `System.ComponentModel.Composition`, MEF | `area-System.ComponentModel.Composition` |
| `System.Data`, ADO.NET, DbConnection, DbCommand | `area-System.Data` |
| `System.Data.Odbc`, ODBC provider | `area-System.Data.Odbc` |
| `System.DirectoryServices`, Active Directory, LDAP | `area-System.DirectoryServices` |
| `System.Linq.Expressions`, expression trees | `area-System.Linq.Expressions` |
| `System.Linq.Parallel`, PLINQ | `area-System.Linq.Parallel` |
| `System.Management`, WMI | `area-System.Management` |
| `System.Reflection.Metadata`, metadata reading | `area-System.Reflection.Metadata` |
| `System.Resources`, resource management, .resx | `area-System.Resources` |
| `System.Runtime`, core runtime types | `area-System.Runtime` |
| `System.Runtime.CompilerServices`, compiler services | `area-System.Runtime.CompilerServices` |
| `System.Runtime.InteropServices.JavaScript`, JS interop, WASM | `area-System.Runtime.InteropServices.JavaScript` |
| `System.ServiceProcess`, Windows services | `area-System.ServiceProcess` |
| `System.Text.Encodings.Web`, HtmlEncoder, JavaScriptEncoder | `area-System.Text.Encodings.Web` |
| `System.Threading.RateLimiting`, rate limiters | `area-System.Threading.RateLimiting` |
| `System.Threading.Tasks`, Task, ValueTask (not channels/dataflow) | `area-System.Threading.Tasks` |
| `System.Transactions`, distributed transactions | `area-System.Transactions` |
| `System.DateTime`, DateOnly, TimeOnly, DateTimeOffset | `area-System.DateTime` |
| `DataContractSerializer`, XML serialization, `System.Runtime.Serialization` | `area-Serialization` |
| `System.IO.Hashing`, non-crypto hashing (XxHash, Crc32) | `area-System.IO.Hashing` |
| `System.IO.Ports`, serial port communication | `area-System.IO.Ports` |
| `System.Formats.Nrbf`, .NET Remoting Binary Format | `area-System.Formats.Nrbf` |
| `System.Configuration`, app.config, ConfigurationManager | `area-System.Configuration` |
| `System.Diagnostics.Process`, Process class | `area-System.Diagnostics.Process` |
| `System.Diagnostics.Activity`, distributed tracing, OpenTelemetry | `area-System.Diagnostics.Activity` |
| `System.Diagnostics.Metrics`, Meter, Counter, Histogram | `area-System.Diagnostics.Metric` |
| EventPipe, ICorProfiler, ETW tracing | `area-Tracing-coreclr` |
| Tiered compilation, on-stack replacement (OSR) | `area-TieredCompilation-coreclr` |
| Mono interpreter, Blazor WASM runtime | `area-Codegen-Interpreter-mono` |
| Mono AOT, iOS/Android ahead-of-time | `area-Codegen-AOT-mono` |
| IL Linker, trimming, `ILLink` | `area-Tools-ILLink` |
| `Microsoft.Extensions.DependencyModel` | `area-DependencyModel` |
| `Microsoft.Extensions.Primitives`, ChangeToken | `area-Extensions-Primitives` |
| `Microsoft.Win32`, Registry, SystemEvents | `area-Microsoft.Win32` |
| `Microsoft.VisualBasic` runtime | `area-Microsoft.VisualBasic` |
| Build/test infrastructure | `area-Infrastructure` |

## Wrong-Repo Heuristics

| Issue mentions... | Correct repo |
|---|---|
| ASP.NET Core, Blazor, SignalR, MVC, Razor Pages, Kestrel (web server), middleware, `Microsoft.AspNetCore.*` | `dotnet/aspnetcore` |
| `dotnet` CLI commands, project creation, `dotnet build`/`publish`/`restore`, SDK workloads, NuGet restore | `dotnet/sdk` |
| C# language features, `new` language syntax, compiler errors CS*, Roslyn analyzers | `dotnet/roslyn` |
| Entity Framework, DbContext, EF migrations, LINQ-to-SQL | `dotnet/efcore` |
| Windows Forms, `System.Windows.Forms.*` | `dotnet/winforms` |
| WPF, XAML, `System.Windows.*` (non-Forms) | `dotnet/wpf` |
| .NET MAUI, cross-platform mobile/desktop UI | `dotnet/maui` |
| API reference docs on learn.microsoft.com, XML doc comment errors, missing API docs | `dotnet/dotnet-api-docs` |
| Conceptual docs, tutorials, how-to guides on learn.microsoft.com | `dotnet/docs` |
| .NET Framework (not .NET Core / .NET 5+), `mscorlib`, old CLR | Developer Community (`developercommunity.visualstudio.com`) |
| NuGet package management, package sources, `nuget.config` | `NuGet/Home` |
| Visual Studio features, IDE behavior | Developer Community |

## Ambiguous Cases

Some issues are genuinely borderline. In these cases, prefer keeping the issue
in dotnet/runtime and noting the ambiguity rather than suggesting a move.

- **`HttpClient` behavior in ASP.NET context** -- Keep in runtime (`area-System.Net.Http`)
  unless the issue is specifically about `IHttpClientFactory` middleware configuration.
- **`System.Text.Json` with ASP.NET model binding** -- If the issue is about the
  serializer behavior itself, keep in runtime. If it's about ASP.NET integration
  (e.g., `AddJsonOptions`), suggest `dotnet/aspnetcore`.
- **Build/test infrastructure** -- If it's about the runtime repo's CI/build system,
  use `area-Infrastructure`. If it's about `dotnet build` itself, suggest `dotnet/sdk`.
- **`System.Runtime.Serialization`** -- Check the specific type. `DataContractSerializer`
  → `area-Serialization`. `BinaryFormatter` → `area-System.Runtime` (it's deprecated).
