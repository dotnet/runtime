# Implementation of System.Text.RegularExpressions

The implementation uses a typical NFA approach that supports back references. Patterns are parsed into a tree (`RegexTree`), translated into an intermediate representation (`RegexCode`) by a writer (`RegexWriter`), and then either used in an interpreter (`RegexInterpreter`) or compiled to IL which is executed (`CompiledRegexRunner`). Both of these derive from `RegexRunner`: in the case of the compiled runner, one must generate them from the `RegexCode` using a factory.

Regex engines have different features: .NET regular expressions have a couple that others do not have, such as `Capture`s (distinct from `Group`s). It does not support searching UTF-8 text, nor searching a Span over a buffer.

Unlike some DFA based engines, patterns must be trusted. Text may be untrusted with the use of a timeout to prevent catastrophic backtracking.

Performance is important and we welcome optimizations so long as they preserve the public contract.

## Extensibility

Key types have significant protected (including protected internal) surface area. This is not intended as an general extensibility point, but rather as a detail of implementing saving a compiled regex to disk. Saving to disk is implemented by saving an assembly containing three types, one that derives from each of `Regex`, `RegexRunnerFactory`, and `RegexRunner`. This mechanism accounts for all the protected methods (and even protected fields) on these classes. If we were designing them today, we would likely more carefully limit their public surface, and possibly not rely on derived types.

Protected members are part of the public API which cannot be broken, so they may potentially make some future optimizations more difficult - or even features - especially the fields. For example the string `runtext` is exposed as a protected internal field on `RegexRunner`. If we wanted to expose the text instead as, for example, a `ReadOnlyMemory<char>`, in order to make it possible to use Regex over other sources beyond string, we would have to find a creative way to preserve compatibility.

In particular, we must keep this API stable in order to remain compatible with regexes saved by .NET Framework.

`RegexCompiler` is internal, and it is abstract for a different reason: to share implementation between `RegexLWCGCompiler` (used when `RegexOptions.Compiled` is specified to compile the regular expression in memory) and `RegexAssemblyCompiler` (used when `CompileToAssembly` is called to compile the regular expression to an assembly to persist in the file system): it is based around a field of type `System.Reflection.Emit.ILGenerator` and has protected utility methods and fields to work with it.

## Key types - General

### Regex (public)

* Represents an executable regular expression with some utility static methods
* Several protected fields and methods but no derived classes exist in this implementation (see [Extensibility](#Extensibility) section above).
* Constructor sets `RegexCode` using `RegexParser` and `RegexWriter`; then, if `RegexOptions.Compiled`, compiles and holds a `RegexRunnerFactory` and clears `RegexCode`; these steps only need to be done once for this `Regex` object
* Thread-safe: no state changes are visible from concurrent threads after construction
* Various public entry points converge on `Run()` which uses the held `RegexRunner` if any; if none or in use, creates another with the held `RegexRunnerFactory` if any; if none, interprets with held `RegexCode`
* All static methods (such as `Regex.Match`) attempt to find a pre-existing `Regex` object for the requested pattern and options in the `RegexCache`. This is legitimate, since `Regex` options are thread-safe. If there is a cache hit, execution can begin immediately; if not, the cache is populated first. If the caller uses an instance instead of a static method, they are effectively performing the same caching themselves

### RegexOptions (public)

* `RightToLeft` is supported throughout, but as the less common case it is less optimized.
* `ExplicitCapture` is off by default: this is relevant to performance, as often patterns contain parentheses as a useful grouping mechanism, for example `(something){1,3}` is easier to type than the non capturing form `(?:something){1,3}`. Because explicit capture is off by default, the engine in this case will capture `something` even if it was not needed.
* There are other various options, `CaseInsensitive` in particular is commonly used

### MatchEvaluator (public)

### RegexCompilationInfo (public)

* Parameters to use for regex compilation to disk
* Passed in by app to `Regex.CompileToAssembly(..)` - which is not currently implemented in .NET Core

## Key types - Parsing

### RegexParser

* Converts pattern string to `RegexTree` of `RegexNode`s
* Invoked with `RegexTree Parse(string pattern, RegexOptions options...) {}`
* Also has `Escape(..)` and `Unescape(..)` methods (which serve as the implementation of the public `Regex.Escape/Unescape` methods), and parses into `RegexReplacement`s
* Does a partial prescan to prep capture slots
* As each `RegexNode` is added, it attempts to reduce (optimize) the newly formed subtree. When parsing completes, there is a final optimization of the whole tree.

### RegexReplacement

* Parsed replacement pattern
* Created by `RegexParser`, used in `Regex.Replace`/`Match.Result(..)`

### RegexCharClass

* Representation of a "character class", which defines what characters should be considered a match.  It supports ranges, Unicode categories, and character class subtraction.  As part of reduction / optimization of a `RegexNode` as well as during compilation, trivial character classes may be replaced by faster equivalent forms, e.g. replacing a character class that represents just one character with the corresponding "one" `RegexNode`.
* Created by `RegexParser`
* Creates packed string to be held on `RegexNode`. During execution, this string is passed to `CharInClass` to determine whether a given character is in the set, although the implementation (in particular in the compiler) may emit faster equivalent checks when possible.
* Has utility methods for examining the packed string, in particular for testing membership of the class (`CharInClass(..)`)

### RegexNode

* Node in regex parse tree
* Created by `RegexParser`
* Some nodes represent subsequent optimizations, rather than individual elements of the pattern
* Holds `Children` and `Next`. `Next` ends up pointing to the immediate parent
* Holds char or string (which may be char class), and `M` and `N` constants; these constants are node-specific values, e.g. for a loop they represent minimum and maximum iteration counts, respectively.
* Note: polymorphism was not used here: the interpretation of its fields depends on the integer Type field

### RegexTree

* Simple holder for root `RegexNode`, options, and a captures data structure
* Created by `RegexParser`

### RegexWriter

* Responsible for translating a `RegexTree` to a `RegexCode`
* Invoked by `Regex`
* Creates itself `RegexCode Write(RegexTree tree){}`

### RegexFCD

* Responsible for static pattern prefixes
* Created by `RegexWriter`
* Creates `RegexFC`s
* `FirstChars()` creates `RegexPrefix` from `RegexTree`
* FC means "First chars": not clear what D means...

### RegexPrefix

* Literal string that match must begin with

### RegexBoyerMoore

* Supports searching the text for literals
* Constructed by `RegexWriter`
* Singleton held on `RegexCode`
* `RegexInterpreter` uses it to perform Boyer-Moore search
* `RegexCompiler` uses the tables from this object, but generates its own code for the Boyer-Moore search

### RegexCode

* Abstract representation of the "program" for a particular pattern
* Created by `RegexWriter`
* Code is an array of integers. Within the array, op-codes' types are indicated by integer consts analogous to those on `RegexNode`.
* Has several related data structures such as a string table, a captures table, and prefixes

## Key types - Compilation (if not interpreted)

### RegexCompiler (public abstract)

* Responsible for compiling `RegexCode` to a `RegexRunnerFactory`
* Has a utility method `CompileToAssembly` that invokes `RegexParser` and `RegexWriter` directly then uses `RegexAssemblyCompiler` (see note for that type)
* Key protected methods are `GenerateFindFirstChar()` and `GenerateGo()`
* Created and used only from `RegexRunnerFactory Regex.Compile(RegexCode code, RegexOptions options...)`
* Has a factory method `RegexRunnerFactory RegexCompiler.Compile(RegexCode code, RegexOptions options...)` that is implemented with its derived type `RegexLWCGCompiler`

### RegexLWCGCompiler (is a RegexCompiler)

* Creates a `CompiledRegexRunnerFactory` using `RegexRunnerFactory FactoryInstanceFromCode(RegexCode .. )`

### RegexRunnerFactory (public pure abstract)

* Reusable: creates `RegexRunner`s on demand with `RegexRunner CreateInstance()`
* Not relevant to interpreted mode
* Must be thread-safe, as each `Regex` holds one, and `Regex` is thread-safe

### CompiledRegexRunnerFactory (is a RegexRunnerFactory)

* Created by `RegexLWCGCompiler`
* Creates `CompiledRegexRunner` on request

### RegexAssemblyCompiler

* Created and used by `RegexCompiler.CompileToAssembly(...)` to write compiled regex to disk: at present, writing to disk is not implemented, because Reflection.Emit does not support it.

## Key types - Execution

### RegexRunner (public abstract)

* Responsible for executing a regular expression: not thread-safe
* Reusable: each call to `Scan(..)` begins a new execution
* Lots of protected members: tracking position, execution stacks, and captures:
  * `protected abstract void Go()`
  * `protected abstract bool FindFirstChar()`
  * `Match? Scan(System.Text.RegularExpressions.Regex regex, string text...)` calls `FindFirstChar()` and `Go()`
* Has a "quick" mode that does not instantiate any captures: used by `Regex.IsMatch(..)` which does not expose captures to the caller
* Concrete instances created by `Match? Regex.Run(...)` calling either `RegexRunner CompiledRegexRunnerFactory.CreateInstance()` or newing up a `RegexInterpreter`

### RegexInterpreter (is a RegexRunner)

* See above. Note that this is sealed.

### CompiledRegexRunner (is a RegexRunner)

* See above.

## Results

### Match (public, is a Group)

* Represents one match of the pattern: there may be several
* Holds a `Regex` in order to call `NextMatch()`
* Created by `RegexRunner`
* `Match` and related objects are not thread-safe, unlike `Regex` itself

### Group (public, is a Capture)

* Represents one capturing group from the match
* Simple data holder

### Capture (public)

* Represents one of the potentially several captures from a capturing group; this is a .NET-only concept.
* Simple data holder

### MatchCollection (public)

* Created by `Regex.Matches`
* Lazily provides `Match`es

### GroupCollection (public)

* Created by `Match.Groups`
* Lazily creates `Group`s

### CaptureCollection (public)

* Created by `Group.Captures`
* Lazily creates `Capture`s

### RegexParseException (is a ArgumentException)

* Thrown when pattern is invalid
* Contains `RegexParseError`

### RegexMatchTimeoutException (public)

* Thrown when timeout expires

## Optimizations

### Tree optimization

* Every `RegexNode.AddChild()` calls `Reduce()` to attempt to optimize subtree as it is being assembled, and parsing ends with call to `RegexNode.FinalOptimize()` for some optimizations that require the entire tree. The goal is to make a functionally equivalent tree that can produce a more efficient program. With more detailed analysis of the tree and some creativity, more could be done here.

### Testing character classes

* Testing a character for membership of a character class can take a significant time in aggregate. Numerous optimizations have been made here. For example, originally it used a binary search, and now it attempts to use a bitmap where possible. More improvements here would likely be worthwhile.

### Prefix matching

* If the pattern begins with a literal, `FindFirstChar()` is used to run quickly to the next point in the text that matches that literal or character class, without using the engine. If the literal is a single character, this can use `IndexOf()` which is vectorized; otherwise it uses `RegexBoyerMoore`. Future optimizations could, for example, handle an alternation of leading literals using the Aho-Corasick algorithm; or use `IndexOf` to find a low-probability char before matching the whole literal. These optimizations are likely to most help in the case of a large text, perhaps with few matches, and a pattern with leading large literals or small character class.

// TODO - more here

More optimization opportunities are being tracked [in this issue](https://github.com/dotnet/runtime/issues/1349): you are welcome to offer more ideas, or contribute PR's.

# Tracing and dumping output

If the engine is built in debug configuration, and `RegexOptions.Debug` is passed, some internal datastructures will be written out with `Debug.Write()`. This includes the pattern itself, then `RegexWriter` will write out the input `RegexTree` with its nodes, and the output `RegexCode`. The `RegexBoyerMoore` dumps its tables - this would likely be relevant only if there was a bug in that class. `RegexRunner`s also dump their state as they execute the pattern. `Match` also has the ability to dump state.

For example, if you are working to optimize the `RegexTree` generated from a pattern, this can be a convenient way to visualize the tree without concerning yourself with the subsequent execution.

When you compile your test program, `RegexOptions.Debug` may not be visible to the compiler: you can use `(RegexOptions)0x0080` instead.

# Debugging

// TODO

# Profiling and benchmarks

// TODO

# Test strategy

// TODO
