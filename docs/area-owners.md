Below table shows the combined area owners on this repository:

| Area        | Owners / experts | Description |
|-------------|------------------|------------------|
| area-AssemblyLoader-coreclr | @jeffschwMSFT @vitek-karas | |
| area-CodeGen-coreclr | @BruceForstall @dotnet/jit-contrib | |
| area-CrossGen/NGEN-coreclr | @nattress | |
| area-crossgen2-coreclr | @nattress @trylek @dotnet/crossgen-contrib | |
| area-DependencyModel | @eerhardt | Microsoft.Extensions.DependencyModel |
| area-Diagnostics-coreclr | @tommcdon | |
| area-ExceptionHandling-coreclr | @janvorli | |
| area-GC-coreclr | @Maoni0 | |
| area-Host | @jeffschwMSFT @vitek-karas @swaroop-sridhar | Issues with dotnet.exe including bootstrapping, framework detection, hostfxr.dll and hostpolicy.dll |
| area-HostModel | @vitek-karas @swaroop-sridhar | |
| area-ILTools-coreclr | @BruceForstall @dotnet/jit-contrib | |
| area-Infrastructure-coreclr | @jeffschwMSFT @jashook @trylek | |
| area-Infrastructure-installer | @dleeapho @NikolaMilosavljevic | |
| area-Infrastructure-libraries | @ViktorHofer @ericstj @safern @Anipik | Covers:<ul><li>Packaging</li><li>Build and test infra for libraries in dotnet/runtime repo</li><li>VS integration</li></ul><br/> |
| area-Infrastructure | @ViktorHofer @jeffschwMSFT @dleeapho | |
| area-Interop-coreclr | @jeffschwMSFT @AaronRobinsonMSFT | |
| area-Meta | @joperezr | Issues without clear association to any specific API/contract, e.g. <ul><li>new contract proposals</li><li>cross-cutting code/test pattern changes (e.g. FxCop failures)</li><li>project-wide docs</li></ul><br/> |
| area-PAL-coreclr | @janvorli | |
| area-R2RDump-coreclr | @nattress | |
| area-ReadyToRun-coreclr | @nattress | |
| area-Setup | @NikolaMilosavljevic @dleeapho | Distro-specific (Linux, Mac and Windows) setup packages and msi files  |
| area-Single-File | @swaroop-sridhar | |
| area-SDK | @janvorli | General development issues and overlap with the SDK and CLI |
| area-Serialization | @StephenMolloy @HongGit | Packages:<ul><li>System.Runtime.Serialization.Xml</li><li>System.Runtime.Serialization.Json</li><li>System.Private.DataContractSerialization</li><li>System.Xml.XmlSerializer</li></ul> Excluded:<ul><li>System.Runtime.Serialization.Formatters</li></ul> |
| area-Snap | @dleeapho @leecow @MichaelSimons | |
| area-TieredCompilation-coreclr | @kouvel | |
| area-Tizen | @alpencolt @gbalykov | |
| area-Tracing-coreclr | @sywhang @josalem | |
| area-TypeSystem-coreclr | @davidwrighton @MichalStrehovsky @janvorli @mangod9 | |
| area-UWP | @nattress | UWP-specific issues including Microsoft.NETCore.UniversalWindowsPlatform and Microsoft.Net.UWPCoreRuntimeSdk |
| area-VM-coreclr | @mangod9 | |
| area-AssemblyLoader-mono | @CoffeeFlux | |
| area-Codegen-meta-mono | @vargaz | |
| area-Codegen-JIT-mono | @SamMonoRT | |
| area-Codegen-AOT-mono | @SamMonoRT | |
| area-Codegen-Interpreter-mono | @BrzVlad | |
| area-CodeGen-LLVM-mono | @imhameed | |
| area-CoreLib-mono | @steveisok | |
| area-GC-mono | @BrzVlad | |
| area-Build-mono | @akoeplinger | |
| area-Infrastructure-mono | @directhex | |
| area-Debugger-mono | @thaystg | |
| area-VM-meta-mono | @lambdageek | |
| area-Threading-mono | @lambdageek | |
| area-Tracing-mono | @lambdageek | |
| area-Performance-mono | @SamMonoRT | |
| ** Extensions namespaces ** | | |
| area-Extensions-Caching | @maryamariyan | |
| area-Extensions-Configuration | @maryamariyan | |
| area-Extensions-DependencyInjection | @maryamariyan | |
| area-Extensions-FileSystem | @maryamariyan | |
| area-Extensions-Hosting | @maryamariyan | |
| area-Extensions-HttpClientFactory | @maryamariyan | |
| area-Extensions-Logging | @maryamariyan | |
| area-Extensions-Options | @maryamariyan | |
| area-Extensions-Primitives | @maryamariyan | |
| area-Microsoft.Extensions | @maryamariyan | |
| **System namespaces** | | |
| area-System.Buffers | @tannergooding @GrabYourPitchforks @pgovind | |
| area-System.CodeDom | @buyaa-n @krwq | |
| area-System.Collections | @eiriktsarpalis @layomia | </ul>Excluded:<ul><li>System.Array -> System.Runtime</li></ul> |
| area-System.ComponentModel | @safern | |
| area-System.ComponentModel.Composition | @maryamariyan @ViktorHofer | |
| area-System.ComponentModel.DataAnnotations | @lajones @ajcvickers | |
| area-System.Composition | @maryamariyan @ViktorHofer | |
| area-System.Configuration | @maryamariyan @safern | |
| area-System.Console | @carlossanlop @eiriktsarpalis | |
| area-System.Data | @ajcvickers @cheenamalhotra @david-engel | <ul><li>Odbc, OleDb - [@saurabh500](https://github.com/saurabh500)</li></ul> |
| area-System.Data.Odbc | @ajcvickers | |
| area-System.Data.OleDB | @ajcvickers | |
| area-System.Data.SqlClient | @cheenamalhotra @david-engel @karinazhou @JRahnama | Archived component - limited churn/contributions (see https://devblogs.microsoft.com/dotnet/introducing-the-new-microsoftdatasqlclient/) |
| area-System.Diagnostics | @tommcdon @krwq | <ul><li>System.Diagnostics.EventLog - [@Anipik](https://github.com/Anipik)</li></ul> |
| area-System.Diagnostics.Process | @adamsitnik @eiriktsarpalis | |
| area-System.Diagnostics.Tracing | @noahfalk @tommcdon @tarekgh @Anipik | Packages:<ul><li>System.Diagnostics.DiagnosticSource</li><li>System.Diagnostics.PerformanceCounter - [@Anipik](https://github.com/Anipik)</li><li>System.Diagnostics.Tracing</li><li>System.Diagnostics.TraceSource - [@Anipik](https://github.com/Anipik)</li></ul><br/> |
| area-System.DirectoryServices | @tquerec @josephisenhour | |
| area-System.Drawing | @safern @tannergooding | |
| area-System.Dynamic.Runtime | @cston @333fred | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| area-System.Globalization | @safern @tarekgh @krwq | |
| area-System.IO | @carlossanlop @jozkee | |
| area-System.IO.Compression | @carlossanlop @ericstj | <ul><li>Also includes System.IO.Packaging</li></ul> |
| area-System.IO.Pipelines | @davidfowl @halter73 @jkotalik @anurse | |
| area-System.Linq | @eiriktsarpalis @adamsitnik | |
| area-System.Linq.Expressions | @cston @333fred | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| area-System.Linq.Parallel | @tarekgh @kouvel | |
| area-System.Management | @Anipik | WMI |
| area-System.Memory | @GrabYourPitchforks @adamsitnik | |
| area-System.Net | @dotnet/ncl | Included:<ul><li>System.Uri</li></ul> |
| area-System.Net.Http | @dotnet/ncl | |
| area-System.Net.Security | @dotnet/ncl | |
| area-System.Net.Sockets | @dotnet/ncl | |
| area-System.Numerics | @tannergooding @pgovind | |
| area-System.Numerics.Tensors | @pgovind @eiriktsarpalis | |
| area-System.Reflection | @steveharter @GrabYourPitchforks | |
| area-System.Reflection.Emit | @steveharter @GrabYourPitchforks | |
| area-System.Reflection.Metadata | @tmat | |
| area-System.Resources | @buyaa-n @tarekgh @krwq | |
| area-System.Runtime | @bartonjs @joperezr | Included:<ul><li>System.Runtime.Serialization.Formatters</li><li>System.Runtime.InteropServices.RuntimeInfo</li><li>System.Array</li></ul>Excluded:<ul><li>Path -> System.IO</li><li>StopWatch -> System.Diagnostics</li><li>Uri -> System.Net</li><li>WebUtility -> System.Net</li></ul> |
| area-System.Runtime.Caching | @StephenMolloy @HongGit | |
| area-System.Runtime.CompilerServices | @Anipik @steveharter | |
| area-System.Runtime.InteropServices | @AaronRobinsonMSFT @jkoritzinsky | Excluded:<ul><li>System.Runtime.InteropServices.RuntimeInfo</li></ul> |
| area-System.Runtime.Intrinsics | @tannergooding @CarolEidt @echesakovMSFT | |
| area-System.Security | @bartonjs @GrabYourPitchforks @krwq | |
| area-System.ServiceModel | @HongGit @mconnew | Repo: https://github.com/dotnet/WCF<br>Packages:<ul><li>System.ServiceModel.Primitives</li><li>System.ServiceModel.Http</li><li>System.ServiceModel.NetTcp</li><li>System.ServiceModel.Duplex</li><li>System.ServiceModel.Security</li></ul> |
| area-System.ServiceModel.Syndication | @StephenMolloy @HongGit | |
| area-System.ServiceProcess | @Anipik | |
| area-System.Text.Encoding | @layomia @krwq @tarekgh | |
| area-System.Text.Encodings.Web | @GrabYourPitchforks @layomia @tarekgh | |
| area-System.Text.Json | @layomia @steveharter @jozkee | |
| area-System.Text.RegularExpressions | @pgovind @eerhardt | |
| area-System.Threading | @kouvel | |
| area-System.Threading.Channels | @tarekgh @stephentoub | |
| area-System.Threading.Tasks | @tarekgh @stephentoub | |
| area-System.Transactions | @dasetser @HongGit | |
| area-System.Xml | @buyaa-n @krwq | |
| **Microsoft contract assemblies** | | |
| area-Microsoft.CSharp | @cston @333fred | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| area-Microsoft.VisualBasic | @cston @333fred | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| area-Microsoft.Win32 | @maryamariyan @Anipik | |
