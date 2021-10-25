# Code generation and execution strategies in CoreCLR

When a .NET language compiler (such as C#, VB.net, or F#) compiles source code into a binary form, the source of the program gets expressed as a sequence of instructions for an abstract processor ("IL instructions"). The abstract processor deals with the program's code at a very high level, with deep understanding of types, fields, and methods in the program. This abstraction makes it easy for different .NET languages to interoperate, and it also makes it possible to execute the same program binary on different physical processors, with different versions of the program's dependencies.

One of the most important tasks a .NET runtime has is turning instructions for the abstract machine into actions on a concrete hardware. There are many ways to do this.

### Just in time compilation

One of the techniques that CoreCLR employs is Just in time compilation ("JIT"). This strategy translates instructions for the abstract processor into native instructions for the processor that the program is running on "just in time". The term "just in time" means that the translation happens when the need arises. For example, a method can be translated when it's first called.

The actual transformation of IL into native code is handled by the code generator. Code generator is a component of the CoreCLR virtual machine that (with the help of other components of CoreCLR, such as the type system) translates IL into native code. The code generator talks to the rest of the virtual machine over a well-defined interface: this allows code generators to be relatively pluggable. The code generator used by the CoreCLR is [RyuJIT](../coreclr/jit/ryujit-overview.md). Over the years, CLR has had many other code generators serving different purposes, such as the simplified [fjit](https://github.com/SSCLI/sscli20_20060311/tree/master/clr/src/fjit), LLVM-based [LLIC](https://github.com/dotnet/llilc), or the closed-source jit32 and jit64.

Big advantage of Just in time compilation is that the generated native code can be tailored for the specific physical processor model. RyuJIT currently uses information about the processor to e.g. unlock the use of AVX instructions on x64 processors that support it.

Just in time compilation needs to carefully balance the time it takes to compile the method with the level of code generation optimizations performed.

### Ahead of time compilation

Another technique to run IL that CoreCLR employs is Ahead of time compilation ("AOT"). With this strategy, the instructions for the abstract processor are transformed into native instructions before the program runs.

The code generator used for ahead of time compilation is typically the same one that would be used as a JIT, although it could be a different one if it follows the same ABI.

The format of ahead of time compiled binaries is called [Ready To Run](../coreclr/botr/readytorun-overview.md) ("R2R"). The format amends the IL with pregenerated native code for a specific operating system and CPU architecture. It preserves the version resiliance of the original IL assemblies.

Ahead of time compilation gives the code generator more time to perform optimizations.

### Interpreter

CoreCLR also has an interpreter that executes code by interpreting individual IL instructions. The interpreter is not enabled by default and not actively maintained. Look for `FEATURE_INTERPRETER` #defines in the CoreCLR source base.

## Mixed strategies

The best results are typically achieved by mixed execution strategies - CoreCLR typically uses a mixed strategy of compiling certain parts of the program ahead of time (for example, the base class libraries), and using JIT for the remaining parts.

## Tiered compilation

A feature of CoreCLR that spans the whole spectrum of execution strategies is [tiered compilation](tiered-compilation.md). Tiered compilation uses the runtime [profiling](../coreclr/botr/profiling.md) infrastructure to measure how often particular method runs. When the method appears "hot" (often called), CoreCLR is able to recompile the method with higher optimization settings. This helps with balancing the time it takes to compile the method and the amount of optimizations applied.
