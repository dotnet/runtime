Issue Guide
===========

This page outlines how the CoreFx team thinks about and handles issues.  For us, issues on GitHub represent actionable work that should be done at some future point.  It may be as simple as a small product or test bug or as large as the work tracking the design of a new feature.  However, it should be work that falls under the charter of CoreFx, which is a collection of foundational libraries that make up the .NET Core development stack.  We will keep issues open even if the CoreFx team internally has no plans to address them in an upcoming release, as long as we consider the issue to fall under our purview.

### When we close issues
As noted above, we don't close issues just because we don't plan to address them in an upcoming release.  So why do we close issues?  There are few major reasons:

1. Issues unrelated to CoreFx.  When possible, we'll try to find a better home for the issue and point you to it.
2. Cross cutting work better suited for another team.  Sometimes the line between the framework, languages and runtime blurs.  For some issues, we may feel that the work is better suited for the runtime team, language team or other partner.  In these cases, we'll close the issue and open it with the partner team.  If they end up not deciding to take on the issue, we can reconsider it here.
3. Nebulous and Large open issues.  Large open issues are sometimes better suited for [User Voice](http://visualstudio.uservoice.com/forums/121579-visual-studio/category/31481--net), especially when the work will cross the boundaries of the framework, language and runtime.  A good example of this is the SIMD support we recently added to CoreFx.  This started as a [User Voice request](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2212443-c-and-simd), and eventually turned into work for both the core libraries and runtime.

Sometimes after debate, we'll decide an issue isn't a good fit for CoreFx.  In that case, we'll also close it.  Because of this, we ask that you don't start working on an issue until it's tagged with [up-for-grabs](https://github.com/dotnet/corefx/labels/up-for-grabs) or [api-approved](https://github.com/dotnet/corefx/labels/api-approved).  Both you and the team will be unhappy if you spend time and effort working on a change we'll ultimately be unable to take. We try to avoid that.

### Labels
We use GitHub [labels](https://github.com/dotnet/corefx/labels) on our issues in order to classify them.  We have the following categories per issue:
* **Area**: These area-&#42; labels (e.g. [area-System.Collections](https://github.com/dotnet/corefx/labels/area-System.Collections)) call out the assembly or assemblies the issue applies to. In addition to labels per assembly, we have a few other area labels: [area-Infrastructure](https://github.com/dotnet/corefx/labels/area-Infrastructure), for issues that relate to our build or test infrastructure, and [area-Meta](https://github.com/dotnet/corefx/labels/area-Meta) for issues that deal with the repository itself, the direction of the .NET Core Platform, our processes, etc. See [full list of areas](#areas).
* **Issue Type**: These labels classify the type of issue.  We use the following types:
  * [bug](https://github.com/dotnet/corefx/labels/bug): Bugs in an assembly.
  * api-&#42; ([api-approved](https://github.com/dotnet/corefx/labels/api-approved), [api-needs-work](https://github.com/dotnet/corefx/labels/api-needs-work), [api-ready-for-review](https://github.com/dotnet/corefx/labels/api-ready-for-review)): Issues which would add APIs to an assembly (see [API Review process](api-review-process.md) for details).
  * [enhancement](https://github.com/dotnet/corefx/labels/enhancement): Improvements to an assembly which do not add new APIs (e.g. performance improvements, code cleanup).
  * [test bug](https://github.com/dotnet/corefx/labels/test%20bug): Bugs in the tests for a specific assembly.
  * [test enhancement](https://github.com/dotnet/corefx/labels/test%20enhancement): Improvements in the tests for a specific assembly (e.g. improving test coverage).
  * [documentation](https://github.com/dotnet/corefx/labels/documentation): Issues related to documentation (e.g. incorrect documentation, enhancement requests).
  * [question](https://github.com/dotnet/corefx/labels/question): Questions about the product, source code, etc.
* **Other**:
  * [up-for-grabs](https://github.com/dotnet/corefx/labels/up-for-grabs): Small sections of work which we believe are well scoped.  These sorts of issues are a good place to start if you are new.  Anyone is free to work on these issues.
  * [needs more info](https://github.com/dotnet/corefx/labels/needs%20more%20info): Issues which need more information to be actionable.  Usually this will be because we can't reproduce a reported bug.  We'll close these issues after a little bit if we haven't gotten actionable information, but we welcome folks who have acquired more information to reopen the issue.

In addition to the above, we have a handful of other labels we use to help classify our issues.  Some of these tag cross cutting concerns (e.g. [tenet-performance](https://github.com/dotnet/corefx/labels/tenet-performance)), whereas others are used to help us track additional work needed before closing an issue (e.g. [api-needs-exposed](https://github.com/dotnet/corefx/labels/api-needs-exposed)).

### Milestones
We use [milestones](https://github.com/dotnet/corefx/milestones) to prioritize work for each upcoming release to NuGet.org.

### Assignee
We assign each issue to assignee, when the assignee is ready to pick up the work and start working on it.  If the issue is not assigned to anyone and you want to pick it up, please say so - we will assign the issue to you.  If the issue is already assigned to someone, please coordinate with the assignee before you start working on it.

### Areas
Areas are tracked by labels area-&#42; (e.g. area-System.Collections). Each area typically corresponds to one or more contract assemblies.

| Area                                                                                          | Owners / experts | Description |
|-----------------------------------------------------------------------------------------------|------------------|-------------|
| [area-Infrastructure](https://github.com/dotnet/corefx/labels/area-Infrastructure)            | [@ericstj](https://github.com/ericstj), [@karelz](https://github.com/karelz), [@wtgodbe](https://github.com/wtgodbe) | Covers:<ul><li>Packaging</li><li>Build and test infra for CoreFX repo</li><li>VS integration</li></ul><br/> |
| [area-Meta](https://github.com/dotnet/corefx/labels/area-Meta)                                | [@joperezr](https://github.com/joperezr) | Issues without clear association to any specific API/contract, e.g. <ul><li>new contract proposals</li><li>cross-cutting code/test pattern changes (e.g. FxCop failures)</li><li>project-wide docs</li></ul><br/> |
| [area-Serialization](https://github.com/dotnet/corefx/labels/area-Serialization)              | [StephenMolloy](https://github.com/StephenMolloy), [@HongGit](https://github.com/HongGit) | Packages:<ul><li>System.Runtime.Serialization.Xml</li><li>System.Runtime.Serialization.Json</li><li>System.Private.DataContractSerialization</li><li>System.Xml.XmlSerializer</li></ul> Excluded:<ul><li>System.Runtime.Serialization.Formatters</li></ul> |
| **System contract assemblies** | | |
| [System.AppContext](https://github.com/dotnet/corefx/labels/area-System.AppContext)           | **[@safern](https://github.com/safern)**, [@Anipik](https://github.com/Anipik) |  |  |
| [System.Buffers](https://github.com/dotnet/corefx/labels/area-System.Buffers)                 | **[@layomia](https://github.com/layomia)**, [@JeremyKuhne](https://github.com/JeremyKuhne), [@ahsonkhan](https://github.com/ahsonkhan) |  |
| [System.CodeDom](https://github.com/dotnet/corefx/labels/area-System.CodeDom)                 | **[@buyaa-n](https://github.com/buyaa-n)**, [@krwq](https://github.com/krwq) |  |
| [System.Collections](https://github.com/dotnet/corefx/labels/area-System.Collections)         | [@safern](https://github.com/safern) | </ul>Excluded:<ul><li>System.Array -> System.Runtime</li></ul> |
| [System.ComponentModel](https://github.com/dotnet/corefx/labels/area-System.ComponentModel)   | **[@maryamariyan](https://github.com/maryamariyan)**, [@safern](https://github.com/safern) |  |
| [System.ComponentModel.DataAnnotations](https://github.com/dotnet/corefx/labels/area-System.ComponentModel.DataAnnotations) | [@lajones](https://github.com/lajones), [@divega](https://github.com/divega), [@ajcvickers](https://github.com/ajcvickers) | |
| [System.Composition](https://github.com/dotnet/corefx/labels/area-System.Composition)         | **[@maryamariyan](https://github.com/maryamariyan)**, [@ViktorHofer](https://github.com/ViktorHofer) |  |
| [System.Configuration](https://github.com/dotnet/corefx/labels/area-System.Configuration)     | **[@maryamariyan](https://github.com/maryamariyan)**, [@safern](https://github.com/safern) |  |
| [System.Console](https://github.com/dotnet/corefx/labels/area-System.Console)                 | [@wtgodbe](https://github.com/wtgodbe) |  |
| [System.Data](https://github.com/dotnet/corefx/labels/area-System.Data)                       | **[@divega](https://github.com/divega)**, [@ajcvickers](https://github.com/ajcvickers), [@afsanehr](https://github.com/afsanehr), [@david-engel](https://github.com/david-engel), [@Gary-Zh ](https://github.com/Gary-Zh) | <ul><li>Odbc, OleDb - [@saurabh](https://github.com/saurabh)</li></ul> |
| [System.Data.SqlClient](https://github.com/dotnet/corefx/labels/area-System.Data.SqlClient)   | **[@afsanehr](https://github.com/afsanehr)**, [@Gary-Zh ](https://github.com/Gary-Zh), [@david-engel](https://github.com/david-engel) | |
| [System.Diagnostics](https://github.com/dotnet/corefx/labels/area-System.Diagnostics)         | **[@wtgodbe](https://github.com/wtgodbe)**, [@krwq](https://github.com/krwq) | <ul><li>System.Diagnostics.EventLog - [@Anipik](https://github.com/Anipik)</li></ul> |
| [System.Diagnostics.Process](https://github.com/dotnet/corefx/labels/area-System.Diagnostics.Process) | **[@wtgodbe](https://github.com/wtgodbe)**, [@krwq](https://github.com/krwq) |  |
| [System.Diagnostics.Tracing](https://github.com/dotnet/corefx/labels/area-System.Diagnostics.Tracing) | [@noahfalk](https://github.com/noahfalk), [@tommcdon](https://github.com/tommcdon), [@Anipik](https://github.com/Anipik) | Packages:<ul><li>System.Diagnostics.DiagnosticSource</li><li>System.Diagnostics.PerformanceCounter - [@Anipik](https://github.com/Anipik)</li><li>System.Diagnostics.Tracing</li><li>System.Diagnostics.TraceSource - [@Anipik](https://github.com/Anipik)</li></ul><br/> |
| [System.DirectoryServices](https://github.com/dotnet/corefx/labels/area-System.DirectoryServices) | [@tquerec](https://github.com/tquerec), [@josephisenhour](https://github.com/josephisenhour), [@bongiovimatthew-microsoft](https://github.com/bongiovimatthew-microsoft) |  |
| [System.Drawing](https://github.com/dotnet/corefx/labels/area-System.Drawing)                 | **[@safern](https://github.com/safern)**, [@maryamariyan](https://github.com/maryamariyan) |  |
| [System.Dynamic.Runtime](https://github.com/dotnet/corefx/labels/area-System.Dynamic.Runtime) | [@cston](https://github.com/cston), [@333fred](https://github.com/333fred) | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| [System.Globalization](https://github.com/dotnet/corefx/labels/area-System.Globalization)     | **[@krwq](https://github.com/krwq)**, [@tarekgh](https://github.com/tarekgh) |  |
| [System.IO](https://github.com/dotnet/corefx/labels/area-System.IO)                           | **[@JeremyKuhne](https://github.com/JeremyKuhne)**, [@carlossanlop](https://github.com/carlossanlop) |  |
| [System.IO.Compression](https://github.com/dotnet/corefx/labels/area-System.IO.Compression)   | **[@carlossanlop](https://github.com/carlossanlop)**, [@ahsonkhan](https://github.com/ahsonkhan), [@ViktorHofer](https://github.com/ViktorHofer) |  |
| [System.IO.Packaging](https://github.com/dotnet/corefx/labels/area-System.IO.Packaging)       | [@JeremyKuhne](https://github.com/JeremyKuhne) |  |
| [System.IO.Pipelines](https://github.com/dotnet/corefx/labels/area-System.IO.Pipelines)       | [@davidfowl](https://github.com/davidfowl), [@halter73](https://github.com/halter73), [@jkotalik](https://github.com/jkotalik), [@anurse](https://github.com/anurse) |  |
| [System.Linq](https://github.com/dotnet/corefx/labels/area-System.Linq)                       | [@maryamariyan](https://github.com/maryamariyan) |  |
| [System.Linq.Expressions](https://github.com/dotnet/corefx/labels/area-System.Linq.Expressions) | [@cston](https://github.com/cston), [@333fred](https://github.com/333fred) | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| [System.Linq.Parallel](https://github.com/dotnet/corefx/labels/area-System.Linq.Parallel)     | **[@tarekgh](https://github.com/tarekgh)**, [@kouvel](https://github.com/kouvel) |  |
| [System.Management](https://github.com/dotnet/corefx/labels/area-System.Management)           | [@Anipik](https://github.com/Anipik) | WMI |
| [System.Memory](https://github.com/dotnet/corefx/labels/area-System.Memory)                   | [@ahsonkhan](https://github.com/ahsonkhan) |  |
| [System.Net](https://github.com/dotnet/corefx/labels/area-System.Net)                         | [@davidsh](https://github.com/davidsh), [@wfurt](https://github.com/wfurt), [@karelz](https://github.com/karelz) | Included:<ul><li>System.Uri - [@wtgodbe](https://github.com/wtgodbe)</li></ul> |
| [System.Net.Http](https://github.com/dotnet/corefx/labels/area-System.Net.Http)               | [@davidsh](https://github.com/davidsh), [@wfurt](https://github.com/wfurt), [@karelz](https://github.com/karelz) |  |
| [System.Net.Http.SocketsHttpHandler](https://github.com/dotnet/corefx/labels/area-System.Net.Http.SocketsHttpHandler) | [@geoffkizer](https://github.com/geoffkizer), [@wfurt](https://github.com/wfurt), [@davidsh](https://github.com/davidsh), [@karelz](https://github.com/karelz) |  |
| [System.Net.Security](https://github.com/dotnet/corefx/labels/area-System.Net.Security)       | [@davidsh](https://github.com/davidsh), [@wfurt](https://github.com/wfurt), [@karelz](https://github.com/karelz) |  |
| [System.Net.Sockets](https://github.com/dotnet/corefx/labels/area-System.Net.Sockets)         | [@davidsh](https://github.com/davidsh), [@wfurt](https://github.com/wfurt), [@karelz](https://github.com/karelz) |  |
| [System.Numerics](https://github.com/dotnet/corefx/labels/area-System.Numerics)               | [@tannergooding](https://github.com/tannergooding), [@ViktorHofer](https://github.com/ViktorHofer) |  |
| [System.Numerics.Tensors](https://github.com/dotnet/corefx/labels/area-System.Numerics.Tensors) | [@tannergooding](https://github.com/tannergooding) |  |
| [System.Reflection](https://github.com/dotnet/corefx/labels/area-System.Reflection)           | [@steveharter](https://github.com/steveharter), [@GrabYourPitchforks](https://github.com/GrabYourPitchforks) |  |
| [System.Reflection.Emit](https://github.com/dotnet/corefx/labels/area-System.Reflection.Emit) | [@steveharter](https://github.com/steveharter), [@GrabYourPitchforks](https://github.com/GrabYourPitchforks) |  |
| [System.Reflection.Metadata](https://github.com/dotnet/corefx/labels/area-System.Reflection.Metadata) | [@tmat](https://github.com/tmat), [@nguerrera](https://github.com/nguerrera) |  |
| [System.Resources](https://github.com/dotnet/corefx/labels/area-System.Resources)             | **[@krwq](https://github.com/krwq)**, [@tarekgh](https://github.com/tarekgh) | |
| [System.Runtime](https://github.com/dotnet/corefx/labels/area-System.Runtime)                 | **[@bartonjs](https://github.com/bartonjs)**, [@joperezr](https://github.com/joperezr) | Included:<ul><li>System.Runtime.Serialization.Formatters</li><li>System.Runtime.InteropServices.RuntimeInfo</li><li>System.Array</li></ul>Excluded:<ul><li>Path -> System.IO</li><li>StopWatch -> System.Diagnostics</li><li>Uri -> System.Net</li><li>WebUtility -> System.Net</li></ul> |
| [System.Runtime.Caching](https://github.com/dotnet/corefx/labels/area-System.Runtime.Caching) | [@KKhurin](https://github.com/KKhurin), [@HongGit](https://github.com/HongGit) |  |
| [System.Runtime.CompilerServices](https://github.com/dotnet/corefx/labels/area-System.Runtime.CompilerServices) | [@Anipik](https://github.com/Anipik) |  |
| [System.Runtime.Extensions](https://github.com/dotnet/corefx/labels/area-System.Runtime.Extensions) |  [@Anipik](https://github.com/Anipik) | |
| [System.Runtime.InteropServices](https://github.com/dotnet/corefx/labels/area-System.Runtime.InteropServices) | [@AaronRobinsonMSFT](https://github.com/AaronRobinsonMSFT), [@jkoritzinsky](https://github.com/jkoritzinsky) | Excluded:<ul><li>System.Runtime.InteropServices.RuntimeInfo</li></ul> |
| [System.Runtime.Intrinsics](https://github.com/dotnet/corefx/labels/area-System.Runtime.Intrinsics) | [@tannergooding](https://github.com/tannergooding), [@CarolEidt](https://github.com/CarolEidt), [@RussKeldorph](https://github.com/RussKeldorph) |  |
| [System.Security](https://github.com/dotnet/corefx/labels/area-System.Security)               | **[@bartonjs](https://github.com/bartonjs)**, [@GrabYourPitchforks](https://github.com/GrabYourPitchforks) |  |
| System.ServiceModel                                                                           | N/A | [dotnet/wcf](https://github.com/dotnet/wcf) (except System.ServiceModel.Syndication) |
| [System.ServiceModel.Syndication](https://github.com/dotnet/corefx/labels/area-System.ServiceModel.Syndication) | [StephenMolloy](https://github.com/StephenMolloy), [@HongGit](https://github.com/HongGit) |  |
| [System.ServiceProcess](https://github.com/dotnet/corefx/labels/area-System.ServiceProcess)   | **[@maryamariyan](https://github.com/maryamariyan)**, [@Anipik](https://github.com/Anipik) |  |
| [System.Text.Encoding](https://github.com/dotnet/corefx/labels/area-System.Text.Encoding)     | **[@layomia](https://github.com/layomia)**, [@krwq](https://github.com/krwq), [@tarekgh](https://github.com/tarekgh) |  |
| [System.Text.Encodings.Web](https://github.com/dotnet/corefx/labels/area-System.Text.Encodings.Web) | **[@GrabYourPitchforks](https://github.com/GrabYourPitchforks)**, [@layomia](https://github.com/layomia), [@tarekgh](https://github.com/tarekgh) |  |
| [System.Text.Json](https://github.com/dotnet/corefx/labels/area-System.Text.Json)             | **[@ahsonkhan](https://github.com/ahsonkhan)**, [@steveharter](https://github.com/steveharter) |  |
| [System.Text.RegularExpressions](https://github.com/dotnet/corefx/labels/area-System.Text.RegularExpressions) | **[@ViktorHofer](https://github.com/ViktorHofer)**, [@maryamariyan](https://github.com/maryamariyan) |  |
| [System.Threading](https://github.com/dotnet/corefx/labels/area-System.Threading)             | **[@kouvel](https://github.com/kouvel)** |  |
| [System.Threading.Channels](https://github.com/dotnet/corefx/labels/area-System.Threading.Channels) | **[@tarekgh](https://github.com/tarekgh)**, [@stephentoub](https://github.com/stephentoub) |  |
| [System.Threading.Tasks](https://github.com/dotnet/corefx/labels/area-System.Threading.Tasks) | **[@tarekgh](https://github.com/tarekgh)**, [@stephentoub](https://github.com/stephentoub) |  |
| [System.Transactions](https://github.com/dotnet/corefx/labels/area-System.Transactions)       | [@jimcarley](https://github.com/jimcarley), [@qizhanMS](https://github.com/qizhanMS) |  |
| [System.Xml](https://github.com/dotnet/corefx/labels/area-System.Xml)                         | **[@buyaa-n](https://github.com/buyaa-n)**, [@krwq](https://github.com/krwq) |  |
| **Microsoft contract assemblies** | | |
| [Microsoft.CSharp](https://github.com/dotnet/corefx/labels/area-Microsoft.CSharp)             | [@cston](https://github.com/cston), [@333fred](https://github.com/333fred) | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| [Microsoft.VisualBasic.Core](https://github.com/dotnet/corefx/labels/area-Microsoft.VisualBasic.Core)   | [@cston](https://github.com/cston), [@333fred](https://github.com/333fred) | Archived component - limited churn/contributions (see [#33170](https://github.com/dotnet/corefx/issues/33170)) |
| [Microsoft.Win32](https://github.com/dotnet/corefx/labels/area-Microsoft.Win32)               | **[@maryamariyan](https://github.com/maryamariyan)**, [@Anipik](https://github.com/Anipik) |  |


Note: Area triage will apply the new scheme (issue types and assignee) throughout 2016.

### Community Partner Experts

| Area        | Owners / experts | Description |
|-------------|------------------|-------------|
| Tizen | [@alpencolt](https://github.com/alpencolt), [@gbalykov](https://github.com/gbalykov) | For issues around Tizen CI and build issues |

### Triage rules - simplified

1. Each issue has exactly one **area-&#42;** label
1. Issue has no **Assignee**, unless someone is working on the issue at the moment
1. Use **up-for-grabs** as much as possible, ideally with a quick note about next steps / complexity of the issue
1. Set milestone to **Future**, unless you can 95%-commit you can fund the issue in specific milestone
1. Each issue has exactly one "*issue type*" label (**bug**, **enhancement**, **api-needs-work**, **test bug**, **test enhancement**, **question**, **documentation**, etc.)
1. Don't be afraid to say no, or close issues - just explain why and be polite
1. Don't be afraid to be wrong - just be flexible when new information appears

Feel free to use other labels if it helps your triage efforts (e.g. **needs more info**, **Design Discussion**, **blocked**, **blocking-partner**, **tenet-performance**, **tenet-compatibility**, **os-linux**/**os-windows**/**os-mac-os-x**/**os-unsupported**, **os-windows-uwp**/**os-windows-wsl**, **arch-arm32**/**arch-arm64**)

#### Motivation for triage rules

1. Each issue has exactly one **area-\*** label
    * Motivation: Issues with multiple areas have loose responsibility (everyone blames the other side) and issues are double counted in reports.
1. Issue has no **Assignee**, unless someone is working on the issue at the moment
    * Motivation: Observation is that contributors are less likely to grab assigned issues, no matter what the repo rules say.
1. Use **up-for-grabs** as much as possible, ideally with a quick note about next steps / complexity of the issue
    * Note: Per http://up-for-grabs.net, such issues should be no longer than few nights' worth of work. They should be actionable (i.e. no misterious CI failures or UWP issues that can't be tested in the open).
1. Set milestone to **Future**, unless you can 95%-commit you can fund the issue in specific milestone
    * Motivation: Helps communicate desire/timeline to community. Can spark further priority/impact discussion.
1. Each issue has exactly one "*issue type*" label (**bug**, **enhancement**, **api-needs-work**, **test bug**, **test enhancement**, **question**, **documentation**, etc.)
    * Don't be afraid to be wrong when deciding 'bug' vs. 'test bug' (flip a coin if you must). The most useful values for tracking are 'api-&#42;' vs. 'enhancement', 'question', and 'documentation'.
1. Don't be afraid to say no, or close issues - just explain why and be polite
1. Don't be afraid to be wrong - just be flexible when new information appears

#### PR rules

1. Each PR has exactly one **area-\*** label
    * Movitation: Area owners will get email notification about new issue in their area.
1. PR has **Assignee** set to author of the PR, if it is non-CoreFX engineer, then area owners are co-assignees
    * Motivation: Area owners are responsible to do code reviews for PRs from external contributors. CoreFX engineers know how to get code reviews from others.
1. [Optional] Set milestone according to the branch (master = 2.1, release/2.0.0 = 2.0, release/1.0.0 = 1.0.x, release/1.1.0 = 1.1.x)
    * Motivation: Easier to track where which fix ended and if it needs to be ported into another branch
    * Note: This is easily done after merge via simple queries & bulk-edits, you don't have to do this one.
1. Any other labels on PRs are superfluous and not needed (exceptions: **blocked**, **NO MERGE**)
    * Motivation: All the important info (*issue type* label, api approval label, OS label, etc.) is already captured on the associated issue.
1. Push PRs forward, don't let them go stale (response every 5+ days, ideally no PRs older than 2 weeks)
1. Stuck or long-term blocked PRs (e.g. due to missing API approval, etc.) should be closed and reopened once they are unstuck
    * Motivation: Keep only active PRs. WIP (work-in-progress) PRs should be rare and should not become stale (2+ weeks old). If a PR is stale and there is not immediate path forward, consider closing the PR until it is unblocked/unstuck.
1. PR should be linked to related issue, use [auto-closing](https://help.github.com/articles/closing-issues-via-commit-messages/) (add "Fixes #12345" into your PR description)
