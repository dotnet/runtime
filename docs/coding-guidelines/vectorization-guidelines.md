- [Introduction to vectorization with Vector128 and Vector256](#introduction-to-vectorization-with-vector128-and-vector256)
  * [Code structure](#code-structure)
    + [Checking for Hardware Acceleration](#checking-for-hardware-acceleration)
    + [Example Code Structure](#example-code-structure)
    + [Testing](#testing)
    + [Benchmarking](#benchmarking)
      - [Custom config](#custom-config)
      - [Memory alignment](#memory-alignment)
        * [Enforcing memory alignment](#enforcing-memory-alignment)
        * [Memory randomization](#memory-randomization)
  * [Loops](#loops)
    + [Scalar remainder handling](#scalar-remainder-handling)
    + [Vectorized remainder handling](#vectorized-remainder-handling)
    + [Access violation testing](#access-violation-av-testing)
  * [Loading and storing vectors](#loading-and-storing-vectors)
    + [Loading](#loading)
    + [Storing](#storing)
    + [Casting](#casting)
  * [Mindset](#mindset)
    + [Edge cases](#edge-cases)
    + [Scalar solution](#scalar-solution)
    + [Vectorized solution](#vectorized-solution)
  * [Tool-Chain](#tool-chain)
    + [Creation](#creation)
    + [Bit operations](#bit-operations)
    + [Equality](#equality)
    + [Comparison](#comparison)
    + [Math](#math)
    + [Conversion](#conversion)
    + [Widening and Narrowing](#widening-and-narrowing)
    + [Shuffle](#shuffle)
      - [Vector256.Shuffle vs Avx2.Shuffle](#vector256shuffle-vs-avx2shuffle)
  * [Summary](#summary)
    + [Best practices](#best-practices)

TL;DR: Go to [Summary](#summary)

# Introduction to vectorization with Vector128 and Vector256

Vectorization is the art of converting an algorithm from operating on a single value per iteration to operating on a set of values (vector) per iteration. It can greatly improve performance at a cost of increased code complexity.

In recent releases, .NET has introduced many new APIs for vectorization. The vast majority of them are hardware specific, so they require users to provide an implementation per processor architecture (such as x86, x64, Arm64, WASM, or other platforms), with the option of using the most optimal instructions for hardware that is executing the code.

.NET 7 introduced a set of new APIs for `Vector64<T>`, `Vector128<T>` and `Vector256<T>` for writing hardware-agnostic, cross platform vectorized code. Similarly, .NET 8 introduced `Vector512<T>`. The purpose of this document is to introduce you to the new APIs and provide a set of best practices.

## Code structure

`Vector128<T>` is the "common denominator" across all platforms that support vectorization (and this is expected to always be the case). It represents a 128-bit vector containing elements of type `T`.

`T` is constrained to specific primitive types:

* `byte` and `sbyte` (8 bits).
* `short` and `ushort` (16 bits).
* `int`, `uint` and `float` (32 bits).
* `long`, `ulong` and `double` (64 bits).
* `nint` and `nuint` (32 or 64 bits, depending on the architecture, available in .NET 7+)

.NET 8 introduced a `Vector128<T>.IsSupported` that indicates whether a given `T` will throw to help identify what works per runtime, including from generic contexts.

A single `Vector128` operation allows you to operate on: 16 (s)bytes, 8 (u)shorts, 4 (u)ints/floats, or 2 (u)longs/double(s).

```
------------------------------128-bits---------------------------
|             64                |               64              |
-----------------------------------------------------------------
|      32       |      32       |      32       |      32       |
----------------------------------------------------------------|
|  16   |  16   |  16   |  16   |  16   |  16   |  16   |  16   |
-----------------------------------------------------------------
| 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 |
-----------------------------------------------------------------
```

`Vector256<T>` is twice as big as `Vector128<T>`, so when it is hardware accelerated, the data is large enough, and the benchmarks prove that it offers better performance, you should consider using it instead of `Vector128<T>`. Benchmarking your code can be important as not all platforms treat larger vectors the same.

For example, `Vector256<T>` on x86/x64 is mostly treated as `2x Vector128<T>` rather than `1x Vector256<T>`, where each `Vector128<T>` is considered a "lane".  For most operations, this doesn't present any additional considerations they only operate on individual elements of the vector. However, some operations could "cross lanes" such as shuffling or pairwise operations and that may require additional overhead to handle.

As an example, consider `Add(Vector128<float> lhs, Vector128<float> rhs)` where you end up effectively doing (pseudo-code):
```csharp
result[0] = lhs[0] + rhs[0];
result[1] = lhs[1] + rhs[1];
result[2] = lhs[2] + rhs[2];
result[3] = lhs[3] + rhs[3];
```

With this algorithm it doesn't matter what size vector we have as we're accessing the same index of the input vectors and only one at a time. So regardless of whether we have `Vector128<T>` or `Vector256<T>` or `Vector512<T>`, it all operates the same.

However, if you then consider `AddPairwise(Vector128<float> lhs, Vector128<float> rhs)` (sometimes called `HorizontalAdd`) where you instead end up effectively doing:
```csharp
// process left
result[0] = lhs[0] + lhs[1];
result[1] = lhs[2] + lhs[3];
// process right
result[2] = rhs[0] + rhs[1];
result[3] = rhs[2] + rhs[3];
```

You may notice that this algorithm would change behavior if expanded up to operate on a single 256-bit vector (note `result[2]` is now `lhs[4] + lhs[6]` and not `rhs[0] + rhs[1]`):
```csharp
// process left
result[0] = lhs[0] + lhs[1];
result[1] = lhs[2] + lhs[3];
result[2] = lhs[4] + lhs[5];
result[3] = lhs[6] + lhs[7];
// process right
result[4] = rhs[0] + rhs[1];
result[5] = rhs[2] + rhs[3];
result[6] = rhs[4] + rhs[5];
result[7] = rhs[6] + rhs[7];
```

Because this behavior would change, the x86/x64 platform opted to treat the operation as `2x Vector128<float>` inputs giving you instead:
```csharp
// process lower left
result[0] = lhs[0] + lhs[1];
result[1] = lhs[2] + lhs[3];
// process lower right
result[2] = rhs[0] + rhs[1];
result[3] = rhs[2] + rhs[3];
// process upper left
result[4] = lhs[4] + lhs[5];
result[5] = lhs[6] + lhs[7];
// process upper right
result[6] = rhs[4] + rhs[5];
result[7] = rhs[6] + rhs[7];
```

This ends up preserving behavior and making it much easier to transition from `128-bit` to `256-bit` or higher as you're effectively just unrolling the loop again. It does, however, mean that some algorithms may need additional handling if you need to truly do anything involving the upper and lower lanes together. The exact additional expense here depends on what is being done, what the underlying hardware supports, and several other factors covered in more detail later.

### Checking for Hardware Acceleration

To check if a given vector size is hardware accelerated, use the `IsHardwareAccelerated` property on the relevant non-generic vector class. For example, `Vector128.IsHardwareAccelerated` or `Vector256.IsHardwareAccelerated`. Note that even when a vector size is accelerated, there may still be some operations that are not hardware-accelerated; e.g. floating-point division can be accelerated on some hardware while integer division is not.

The size of the input also matters. It needs to be at least of the size of a single vector to be able to execute the vectorized code path (there are some advanced tricks that can allow you to operate on smaller inputs, but we won't describe them here). The `Count` properties (for example `Vector128<T>.Count` or `Vector256<T>.Count`) return the number of elements of the given type T in a single vector.

When `Vector256` is accelerated, `Vector128` generally will be as well, but there's no guarantee of that. The best practice is to always check `IsHardwareAccelerated` explicitly. You may be tempted to cache the values from the `IsHardwareAccelerated` and `Count` properties, but this is not needed or recommended. Both `IsHardwareAccelerated` and `Count` are turned into constants by the Just-In-Time compiler and no method call is required to retrieve the information.

### Example Code Structure

```csharp
void CodeStructure(ReadOnlySpan<byte> buffer)
{
    if (Vector256.IsHardwareAccelerated && buffer.Length >= Vector256<byte>.Count)
    {
        // Vector256 code path
    }
    else if (Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<byte>.Count)
    {
        // Vector128 code path
    }
    else
    {
        // non-vectorized && small inputs code path
    }
}
```

To reduce the number of comparisons for small inputs, we can re-arrange it in the following way:

```csharp
void OptimalCodeStructure(ReadOnlySpan<byte> buffer)
{
    if (!Vector128.IsHardwareAccelerated || buffer.Length < Vector128<byte>.Count)
    { 
        // scalar code path
    } 
    else if (!Vector256.IsHardwareAccelerated || buffer.Length < Vector256<byte>.Count)
    { 
        // Vector128 code path
    } 
    else
    { 
        // Vector256 code path
    }
}
```

**Both vector types provide the same functionality**, but arm64 hardware does not support `Vector256`, so for the sake of simplicity we will be using `Vector128` in all examples. All examples shown also assume **little endian** architecture and/or do not need to deal with endianness. `BitConverter.IsLittleEndian` is available (and turned into a constant by the JIT) for algorithms that need to consider endianness.

With these assumptions, all examples shown in the document assume that they are being executed as part of the following `if` block:

```csharp
else if (Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<byte>.Count)
{
    // Vector128 code path
}
```

### Testing

Such a code structure requires us to **test all possible code paths**:

* `Vector256` is accelerated:
  * The input is large enough to benefit from vectorization with `Vector256`.
  * The input is not large enough to benefit from vectorization with `Vector256`, but it can benefit from vectorization with `Vector128`.
  * The input is too small to benefit from any kind of vectorization.
* `Vector128` is accelerated
  * The input is large enough to benefit from vectorization with `Vector128`.
  * The input is too small to benefit from any kind of vectorization.
* Neither `Vector128` or  `Vector256` are accelerated.

It's possible to implement tests that cover some of the scenarios based on the size, but it's impossible to toggle hardware acceleration at the unit test level. It can be controlled with environment variables before .NET process is started:

* When `DOTNET_EnableAVX2` is set to `0`, `Vector256.IsHardwareAccelerated` returns `false`.
* When `DOTNET_EnableHWIntrinsic` is set to `0`, not only do both mentioned APIs return `false`, but so also do `Vector64.IsHardwareAccelerated` and `Vector.IsHardwareAccelerated`.

Assuming that we run the tests on an `x64` machine that supports `Vector256`, we need to write tests that cover all size scenarios and run them with:
* no custom settings
* `DOTNET_EnableAVX2=0`
* `DOTNET_EnableHWIntrinsic=0`

The alternative is running tests on enough variation of hardware to cover all the paths.

### Benchmarking

All that complexity needs to pay off. We need to **benchmark the code to verify that the investment is beneficial**. We can do that with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

#### Custom config

It's possible to define a config that instructs the harness to run the benchmarks for all three scenarios:

```csharp
static void Main(string[] args)
{
    Job enough = Job.Default
        .WithWarmupCount(1)
        .WithIterationTime(TimeInterval.FromSeconds(0.25))
        .WithMaxIterationCount(20);

    IConfig config = DefaultConfig.Instance
        .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
        .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig
            (exportGithubMarkdown: true, printInstructionAddresses: false)))
        .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableHWIntrinsic", "0").WithId("Scalar").AsBaseline());

    if (Vector256.IsHardwareAccelerated)
    {
        config = config
            .AddJob(enough.WithId("Vector256"))
            .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableAVX2", "0").WithId("Vector128"));

    }
    else if (Vector128.IsHardwareAccelerated)
    {
        config = config.AddJob(enough.WithId("Vector128"));
    }

    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
        .Run(args, config);
}
```

**Note:** the config defines a [disassembler](https://adamsitnik.com/Disassembly-Diagnoser/), which exports a disassembly in GitHub markdown format (supported on both x64 and arm64, Windows and Linux). It is very often an invaluable tool when working with high-performance code where inspecting generated assembly code is required.

#### Memory alignment

BenchmarkDotNet does a lot of heavy lifting for the end users, but it cannot protect us from the random memory alignment which can be different per each benchmark run and can affect the stability of the benchmarks.

We have three possibilities:

* We can enforce the alignment ourselves and have very stable results.
* We can ask the harness to try to randomize the memory and observe the entire possible distribution with each run.
* We can do nothing and wonder why the results have additional noise across many runs.

##### Enforcing memory alignment

We can allocate aligned unmanaged memory by using the [NativeMemory.AlignedAlloc](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.nativememory.alignedalloc).

```csharp
public unsafe class Benchmarks
{
    private void* _pointer;

    [Params(6, 32, 1024)] // test various sizes
    public uint Size;

    [GlobalSetup]
    public void Setup()
    {
        _pointer = NativeMemory.AlignedAlloc(byteCount: Size * sizeof(int), alignment: 32);
        NativeMemory.Clear(_pointer, byteCount: Size * sizeof(int)); // ensure it's all zeros, so 1 is never found
    }

    [Benchmark]
    public bool Contains()
    {
        ReadOnlySpan<int> buffer = new (_pointer, (int)Size);
        return buffer.Contains(1);
    }

    [GlobalCleanup]
    public void Cleanup() => NativeMemory.AlignedFree(_pointer);
}
```

Sample results (please mind the AVX2, AVX and SSE4.2 information printed in the summary):

```ini
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
AMD Ryzen Threadripper PRO 3945WX 12-Cores, 1 CPU, 24 logical and 12 physical cores
.NET SDK=8.0.100-alpha.1.22558.1
  [Host]    : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
  Scalar    : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT
  Vector128 : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX
  Vector256 : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
```

```
|   Method |       Job | Size |       Mean |    StdDev | Ratio | Code Size |
|--------- |---------- |----- |-----------:|----------:|------:|----------:|
| Contains |    Scalar | 1024 | 143.844 ns | 0.6234 ns |  1.00 |     206 B |
| Contains | Vector128 | 1024 | 104.544 ns | 1.2792 ns |  0.73 |     335 B |
| Contains | Vector256 | 1024 |  55.769 ns | 0.6720 ns |  0.39 |     391 B |
```

**Note:** as you can see, even such simple method like [Contains](https://learn.microsoft.com/dotnet/api/system.memoryextensions.contains) **did not observe a perfect performance boost**: x8 for `Vector256` (256/32) and x4 for `Vector128` (128/32). To understand why, we would need to use a profiler that provides information on CPU instruction level, which depending on the hardware could be [Intel VTune](https://www.intel.com/content/www/us/en/developer/tools/oneapi/vtune-profiler.html) or [amd uprof](https://developer.amd.com/amd-uprof/).

The results should be very stable (flat distributions), but on the other hand we are measuring the performance of the best case scenario (the input is large, aligned and its entire contents are searched through, as the value is never found).

Explaining benchmark design guidelines is outside of the scope of this document, but we have a [dedicated document](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md#benchmarks-are-not-unit-tests) about it. To make a long story short, **you should benchmark all scenarios that are realistic for your production environment**, so your customers can actually benefit from your improvements.

##### Memory randomization

The alternative is to enable memory randomization. Before every iteration, the harness is going to allocate random-size objects, keep them alive and re-run the setup that should allocate the actual memory.

You can read more about it [here](https://github.com/dotnet/BenchmarkDotNet/pull/1587). It requires an understanding of what distribution is and how to read it. It's also out of scope of this document, but a book on statistics, such as [Pro .NET Benchmarking](https://aakinshin.net/prodotnetbenchmarking/) can help you get a very good understanding of the subject.

No matter how you are going to benchmark your code, you need to keep in mind that **the larger the input, the more you can benefit from vectorization**. If your code uses small buffers, performance might even get worse.

## Loops

To work with inputs that are bigger than a single vector, you typically need to loop over the entire input. This should be split into two parts:

* vectorized loop that operates on multiple values at a time
* handling of the remainder

Example: our input is a buffer of ten integers, assuming that `Vector128` is accelerated, we handle the first four values in the first loop iteration, the next four in the second iteration and then we stop, as only two are left. Depending on how we can handle the remainder, we distinguish two approaches.

### Scalar remainder handling

Imagine that we want to calculate the sum of all the numbers in given buffer. We definitely want to add every element just once, without repetitions. That is why in the first loop, we add four (128 bits / 32 bits) integers in one iteration. In the second loop, we handle the remaining values.


```csharp
int Sum(Span<int> buffer)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    // The initial sum is zero, so we need a vector with all elements initialized to zero.
    Vector128<int> sum = Vector128<int>.Zero;

    // We need to obtain the reference to first value in the buffer, it's used later for loading vectors from memory.
    ref int searchSpace = ref MemoryMarshal.GetReference(buffer);
    // And an offset, that is going to be used by vectorized and scalar loops.
    nuint elementOffset = 0;
    // And the last valid offset from which we can load the values
    nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
    for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector128<int>.Count)
    {
        // We load a vector from given offset.
        Vector128<int> loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset);
        // We add 4 integers at a time:
        sum += loaded;
    }

    // We sum all 4 integers from the vector to one
    int result = Vector128.Sum(sum);

    // And handle the remaining elements, in a non-vectorized way:
    while (elementOffset < (nuint)buffer.Length)
    {
        result += buffer[(int)elementOffset];
        elementOffset++;
    }

    return result;
}
```

**Note:** Use `ref MemoryMarshal.GetReference(span)` instead of `ref span[0]` and `ref MemoryMarshal.GetArrayDataReference(array)` instead of `ref array[0]` to handle empty buffer scenarios (which would throw `IndexOutOfRangeException`). If the buffer is empty, these methods return a reference to the location where the 0th element would have been stored. Such a reference may or may not be null. You can use it for pinning but you must never de-reference it.

**Note:** The `GetReference` method has an overload that accepts a `ReadOnlySpan` and returns mutable reference. Please use it with caution! To get a `readonly` reference, you can use [ReadOnlySpan<T>.GetPinnableReference](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1.getpinnablereference) or just do the following:

```csharp
ref readonly T searchSpace = ref MemoryMarshal.GetReference(buffer);
```

**Note:** Please keep in mind that `Vector128.Sum` is a static method. `Vectior128<T>` and `Vector256<T>` provide both instance and static methods (operators like `+` are just static methods in C#). `Vector128` and `Vector256` are non-generic static classes with static methods only. It's important to know about their existence when searching for methods.

### Vectorized remainder handling

There are scenarios and advanced techniques that can allow for vectorized remainder handling instead of resorting to the non-vectorized approach illustrated above. Some algorithms could use an approach of backtracking to load one more vector's worth of elements and masking off elements that have already been processed. For idempotent algorithms, it is preferable to simply backtrack and process one last vector, repeating the operation for elements as needed.

In the example below, we need to check whether the given buffer contains a specific number; processing values more than once is completely acceptable. The buffer contains six 32-bit integers, `Vector128` is accelerated, and it can work with four integers at a time. In the first loop iteration, we handle the first four elements. In the second (and last) iteration we need to handle the remaining two elements. Since the remainder is smaller than one `Vector128` and we are not mutating the input, we perform a vectorized operation on a `Vector128` containing the last four elements.

```csharp
bool Contains(Span<int> buffer, int searched)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    Vector128<int> loaded;
    // We need a vector for storing the searched value.
    Vector128<int> values = Vector128.Create(searched);

    ref int searchSpace = ref MemoryMarshal.GetReference(buffer);
    nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
    nuint elementOffset = 0;
    for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector128<int>.Count)
    {
        loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset);
        // compare the loaded vector with searched value vector
        if (Vector128.Equals(loaded, values) != Vector128<int>.Zero)
        {
            return true; // return true if a difference was found
        }
    }

    // If any elements remain, process the last vector in the search space.
    if (elementOffset != (uint)buffer.Length)
    {
        loaded = Vector128.LoadUnsafe(ref searchSpace, oneVectorAwayFromEnd);
        if (Vector128.Equals(loaded, values) != Vector128<int>.Zero)
        {
            return true;
        }
    }

    return false;
}
```

`Vector128.Create(value)` creates a new vector with all elements initialized to the specified value. So `Vector128<int>.Zero` is equivalent to `Vector128.Create(0)`.

`Vector128.Equals(Vector128 left, Vector128 right)` compares two vectors and returns a vector where each element is either all-bits-set or zero, depending on if the corresponding elements in left and right were equal. If the result of comparison is non zero, it means that there was at least one match.

### Access violation (AV) testing

Handling the remainder in an invalid way may lead to non-deterministic and hard to diagnose issues.

Let's look at the following code:

```diff
nuint elementOffset = 0;
while (elementOffset < (nuint)buffer.Length)
{
    loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset); // BUG!

    elementOffset += (nuint)Vector128<int>.Count;
}
```

How many times will the loop execute for a buffer of six integers? Twice! The first time it will load the first four elements, but the second time it will load the random content of the memory following the buffer!

Writing tests that detect that issue is hard, but not impossible. The .NET Team uses a helper utility called [BoundedMemory](/src/libraries/Common/tests/TestUtilities/System/Buffers/BoundedMemory.Creation.cs) that allocates a memory region which is immediately preceded by or immediately followed by a poison (`MEM_NOACCESS`) page. Attempting to read the memory immediately before or after it results in `AccessViolationException`.

## Loading and storing vectors

### Loading

Both `Vector128` and `Vector256` provide at least five ways of loading them from memory:

```csharp
public static class Vector128
{
    public static Vector128<T> Load<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadAligned<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadAlignedNonTemporal<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadUnsafe<T>(ref T source) where T : struct
    public static Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct
}
```

The first three overloads require a pointer to the source. To be able to use a pointer to a managed buffer in a safe way, the buffer needs to be pinned first. This is because the GC cannot track unmanaged pointers. It needs help to ensure that it doesn't move the memory while you're using it, as the pointers would silently become invalid. The tricky part here is doing the pointer arithmetic right:

```csharp
unsafe int UnmanagedPointersSum(Span<int> buffer)
{
    fixed (int* pBuffer = buffer)
    {
        int* pEnd = pBuffer + buffer.Length;
        int* pOneVectorFromEnd = pEnd - Vector128<int>.Count;
        int* pCurrent = pBuffer;

        Vector128<int> sum = Vector128<int>.Zero;

        while (pCurrent <= pOneVectorFromEnd)
        {
            sum += Vector128.Load(pCurrent);

            pCurrent += Vector128<int>.Count;
        }

        int result = Vector128.Sum(sum);

        while (pCurrent < pEnd)
        {
            result += *pCurrent;

            pCurrent++;
        }

        return result;
    }
}
```

`LoadAligned` and `LoadAlignedNonTemporal` require the input to be aligned. Aligned reads and writes should be slightly faster but using them comes at a price of increased complexity. "NonTemporal" means that the hardware is allowed (but not required) to bypass the cache. Non-temporal reads provide a speedup when working with very large amounts of data as it avoids repeatedly filling the cache with values that will never be used again.

Currently .NET exposes only one API for allocating unmanaged aligned memory: [NativeMemory.AlignedAlloc](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.nativememory.alignedalloc). In the future, we might provide [a dedicated API](https://github.com/dotnet/runtime/issues/27146) for allocating managed, aligned and hence pinned memory buffers.

The alternative to creating aligned buffers (we don't always have the control over input) is to pin the buffer, find first aligned address, handle non-aligned elements, then start aligned loop and afterwards handle the remainder. Adding such complexity to our code may not always be worth it and needs to be proved with proper benchmarking on various hardware.

The fourth method expects only a managed reference (`ref T source`). We don't need to pin the buffer (GC is tracking managed references and updates them if memory gets moved), but it still requires us to properly handle managed pointer arithmetic:

```csharp
int ManagedReferencesSum(int[] buffer)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    ref int current = ref MemoryMarshal.GetArrayDataReference(buffer);
    ref int end = ref Unsafe.Add(ref current, buffer.Length);
    ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

    Vector128<int> sum = Vector128<int>.Zero;

    while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd))
    {
        sum += Vector128.LoadUnsafe(ref current);

        current = ref Unsafe.Add(ref current, Vector128<int>.Count);
    }

    int result = Vector128.Sum(sum);

    while (Unsafe.IsAddressLessThan(ref current, ref end))
    {
        result += current;

        current = ref Unsafe.Add(ref current, 1);
    }

    return result;
}
```

**Note:** `Unsafe` does not expose a method called `IsLessThanOrEqualTo`, so we are using a negation of `Unsafe.IsAddressGreaterThan` to achieve desired effect.

**Pointer arithmetic can always go wrong, even if you are an experienced engineer and get a very detailed code review from .NET architects**. In [#73768](https://github.com/dotnet/runtime/pull/73768) a GC hole was introduced. The code looked simple:

```csharp
ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

do
{
    equals = Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace));
    if (equals == Vector128<TValue>.Zero)
    {
        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
        continue;
    }

    return ...;
}
while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));
```

It was part of `LastIndexOf` implementation, where we were iterating from the end to the beginning of the buffer. In the last iteration of the loop, `currentSearchSpace` could become a pointer to unknown memory that lied before the beginning of the buffer:

```csharp
currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
```

And it was fine until GC kicked right after that, moved objects in memory, updated all valid managed references and resumed the execution, which run following condition:

```csharp
while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));
```

Which could return true because `currentSearchSpace` was invalid and not updated. If you are interested in more details, you can check the [issue](https://github.com/dotnet/runtime/issues/75792#issuecomment-1249973858) and the [fix](https://github.com/dotnet/runtime/pull/75857).

That is why **we recommend using the overload that takes a managed reference and an element offset. It does not require pinning or doing any pointer arithmetic. It still requires care as passing an incorrect offset results in a GC hole.**

```csharp
public static Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct
```

**The only thing we need to keep in mind is potential `nuint` overflow when doing unsigned integer arithmetic.**

```csharp
Span<int> buffer = new int[2] { 1, 2 };
nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
Console.WriteLine(oneVectorAwayFromEnd);
```

Can you guess the result? For a 64 bit process it's `FFFFFFFFFFFFFFFE` (a hex representation of `18446744073709551614`)! That is why the length of the buffer needs to be always checked before doing similar computations!

### Storing

Similarly to loading, both `Vector128` and `Vector256` provide at least five ways of storing them in memory:

```csharp
public static class Vector128
{
    public static void Store<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreAligned<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreAlignedNonTemporal<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination) where T : struct
    public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset) where T : struct
}
```

For the reasons described for loading, we recommend using the overload that takes managed reference and element offset:

```csharp
public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset) where T : struct
```

**Note**: when loading values from one buffer and storing them into another, we need to consider whether they overlap or not. [MemoryExtensions.Overlap](https://learn.microsoft.com/dotnet/api/system.memoryextensions.overlaps#system-memoryextensions-overlaps-1(system-readonlyspan((-0))-system-readonlyspan((-0)))) is an API for doing that.

### Casting

As mentioned before, `Vector128<T>` and `Vector256<T>` are constrained to a specific set of primitive types. Currently, `char` is not one of them, but it does not mean that we can't implement vectorized text operations with the new APIs. For primitive types of the same size (and value types that don't contain references), casting is the solution.

[Unsafe.As<TFrom, TTo>](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafe.as#system-runtime-compilerservices-unsafe-as-2(-0@)) can be used to get a reference to supported type:

```csharp
void CastingReferences(Span<char> buffer)
{
    ref char charSearchSpace = ref MemoryMarshal.GetReference(buffer);
    ref short searchSpace = ref Unsafe.As<char, short>(ref charSearchSpace);
    // from now on we can use Vector128<short> or Vector256<short>
}
```

Or [MemoryMarshal.Cast<TFrom, TTo>](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.memorymarshal.cast#system-runtime-interopservices-memorymarshal-cast-2(system-readonlyspan((-0)))), which casts a span of one primitive type to a span of another primitive type:

```csharp
void CastingSpans(Span<char> chars)
{
    Span<short> shorts = MemoryMarshal.Cast<char, short>(chars);
}
```

It's also possible to get managed references from unmanaged pointers:

```csharp
void PointerToReference(char* pUtf16Buffer, byte* pAsciiBuffer)
{
    // of the same type:
    ref byte asciiBuffer = ref *pAsciiBuffer;
    // of different types:
    ref ushort utf16Buffer = ref *(ushort*)pUtf16Buffer;
}
```

It's only safe to convert a managed reference to a pointer if it's known that the reference is already pinned. If it's not, the moment after you get the pointer it could be invalid.

## Mindset

Vectorizing real-world algorithms seems complex at the beginning. And what do software engineers do with complex problems? We break them down into sub-problems until these become simple enough to be solved directly.

Let's implement a vectorized method for checking whether a given byte buffer consists only from valid ASCII characters to see how similar problems can be solved.

### Edge cases

Before we start working on the implementation, let's list all edge cases for our `IsAcii(ReadOnlySpan<byte> buffer)` method (and ideally write tests):

* It does not need to throw any argument exceptions, as `ReadOnlySpan` is `struct` and it can never be `null` or invalid.
* It should return `true` for an empty buffer.
* It should detect invalid characters in the entire buffer, regardless of the buffer's length or whether its length is an even multiple of a vector width.
* It should not read any bytes that don't belong to the provided buffer.

### Scalar solution

Once we know all edge cases, we need to understand our problem and find a scalar solution.

ASCII characters are values in the range from `0` to `127` (inclusive). It means that we can find invalid ASCII bytes by just searching for values that are larger than `127`. If we treat `byte` (unsigned, range from 0 to 255) as `sbyte` (signed, range from -128 to 127), it's a matter of performing "is less than zero" check.

The binary representation of 0-127 range is following:

```log
00000000
01111111
^
most significant bit
```

When we look at it, we can realize that another way is checking whether the most significant bit is equal `1`. For the scalar version, we could perform a logical AND:

```csharp
bool IsValidAscii(byte c) => (c & 0b1000_0000) == 0;
```

### Vectorized solution

Another step is vectorizing our scalar solution and choosing the best way of doing that based on data.

If we reuse one of the loops presented in the previous sections, all we need to implement is a method that accepts `Vector128<byte>` and returns `bool` and does exactly the same thing that our scalar method did, but for a vector rather than single value:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
bool IsValidAscii(Vector128<byte> vector)
{
    // to perform "> 127" check we can use GreaterThanAny method:
    return !Vector128.GreaterThanAny(vector, Vector128.Create((byte)127))
    // to perform "< 0" check, we need to use AsSByte and LessThanAny methods:
    return !Vector128.LessThanAny(vector.AsSByte(), Vector128<sbyte>.Zero)
    // to perform an AND operation, we need to use & operator
    return (vector & Vector128.Create((byte)0b_1000_0000)) == Vector128<byte>.Zero;
    // we can also just use ExtractMostSignificantBits method:
    return vector.ExtractMostSignificantBits() == 0;
}
```

We can also use the hardware-specific instructions if they are available:

```csharp
if (Sse41.IsSupported)
{
    return Sse41.TestZ(vector, Vector128.Create((byte)0b_1000_0000));
}
else if (AdvSimd.Arm64.IsSupported)
{
    Vector128<byte> maxBytes = AdvSimd.Arm64.MaxPairwise(vector, vector);
    return (maxBytes.AsUInt64().ToScalar() & 0x8080808080808080) == 0;
}
```

Benchmark all available solutions, and choose the one that is the best for us.

```ini
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
AMD Ryzen Threadripper PRO 3945WX 12-Cores, 1 CPU, 24 logical and 12 physical cores
.NET SDK=8.0.100-alpha.1.22558.1
  [Host]   : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
```

```
|                     Method | Size |      Mean | Ratio | Code Size |
|--------------------------- |----- |----------:|------:|----------:|
|                     Scalar | 1024 | 252.13 ns |  1.00 |      69 B |
|             GreaterThanAny | 1024 |  32.49 ns |  0.13 |     178 B |
|                LessThanAny | 1024 |  29.33 ns |  0.12 |     146 B |
|                        And | 1024 |  26.13 ns |  0.10 |     138 B |
|                      TestZ | 1024 |  27.26 ns |  0.11 |     129 B |
| ExtractMostSignificantBits | 1024 |  27.33 ns |  0.11 |     141 B |
```

Even such a simple problem can be solved in at least 5 different ways and each of them can perform significantly different on different hardware. Using sophisticated hardware-specific instructions does not always provide the best performance, so **with the new `Vector128` and `Vector256` APIs we don't need to become assembly language experts to write fast, vectorized code**.

## Tool-Chain

`Vector128`, `Vector128<T>`, `Vector256` and `Vector256<T>` expose a LOT of APIs. We are constrained by time, so we won't describe all of them with examples. Instead, we have grouped them into categories to give you an overview of their capabilities. It's not required to remember what each of these methods is doing, but it's important to remember what kind of operations they allow for and check the details when needed.

**Note:** all of these methods have "software fallbacks", which are executed when they cannot be vectorized on given platform.

### Creation

Each of the vector types provides a `Create` method that accepts a single value and returns a vector with all elements initialized to this value.

```csharp
public static Vector128<T> Create<T>(T value) where T : struct
```

`CreateScalar` initializes first element to the specified value, and the remaining elements to zero.

```csharp
public static Vector128<int> CreateScalar(int value)
```

`CreateScalarUnsafe` is similar, but the remaining elements are left uninitialized. It's dangerous!


We also have an overload that allows for specifying every value in given vector:

```csharp
public static Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
```

And last but not least we have a `Create` overload which accepts a buffer. It creates a vector with its elements set to the first `VectorXYZ<T>.Count` elements of the buffer. It's not recommended to use it in a loop, where `Load` methods should be used instead (for performance).

```csharp
public static Vector128<T> Create<T>(ReadOnlySpan<T> values) where T : struct
```

to perform a copy in the other direction, we can use one of the `CopyTo` extension methods:

```csharp
public static void CopyTo<T>(this Vector128<T> vector, Span<T> destination) where T : struct
```

### Bit operations

All size-specific vector types provide a set of APIs for common bit operations.

`BitwiseAnd` computes the bitwise-and of two vectors, `BitwiseOr` computes the bitwise-or of two vectors. They can both be expressed by using the corresponding operators (`&` and `|`). The same goes for `Xor` which can be expressed with `^` operator and `Negate` (`~`).

**Note:** The **operators should be preferred where possible**, as it helps avoid bugs around operator precedence and can improve readability.

```csharp
public static Vector128<T> BitwiseAnd<T>(Vector128<T> left, Vector128<T> right) where T : struct => left & right;
public static Vector128<T> BitwiseOr<T>(Vector128<T> left, Vector128<T> right) where T : struct => left | right;
public static Vector128<T> Xor<T>(Vector128<T> left, Vector128<T> right) => left ^ right;
public static Vector128<T> Negate<T>(Vector128<T> vector) => ~vector;
```

`AndNot` computes the bitwise-and of a given vector and the ones' complement of another vector.

```csharp
public static Vector128<T> AndNot<T>(Vector128<T> left, Vector128<T> right) => left & ~right;
```

`ShiftLeft` shifts each element of a vector left by the specified number of bits.
`ShiftRightArithmetic` performs a **signed** shift right and `ShiftRightLogical` performs an **unsigned** shift:

```csharp
public static Vector128<sbyte> ShiftLeft(Vector128<sbyte> vector, int shiftCount) => vector << shiftCount;
public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> vector, int shiftCount) => vector >> shiftCount;
public static Vector128<byte> ShiftRightLogical(Vector128<byte> vector, int shiftCount) => vector >>> shiftCount;
```

### Equality

`EqualsAll` compares two vectors to determine if all elements are equal. `EqualsAny` compares two vectors to determine if any elements are equal.

```csharp
public static bool EqualsAll<T>(Vector128<T> left, Vector128<T> right) where T : struct => left == right;
public static bool EqualsAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

`Equals` compares two vectors to determine if they are equal on a per-element basis. It returns a vector whose elements are all-bits-set or zero, depending on whether the corresponding elements in the `left` and `right` arguments were equal.

```csharp
public static Vector128<T> Equals<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

How do we calculate the index of the first match? Let's take a closer look at the result of following equality check:

```csharp
Vector128<int> left = Vector128.Create(1, 2, 3, 4);
Vector128<int> right = Vector128.Create(0, 0, 3, 0);
Vector128<int> equals = Vector128.Equals(left, right);
Console.WriteLine(equals);
```

```log
<0, 0, -1, 0>
```

`-1` is just `0xFFFFFFFF` (all-bits-set). We could use `GetElement` to get the first non-zero element.

```csharp
public static T GetElement<T>(this Vector128<T> vector, int index) where T : struct
```

But it would not be an optimal solution. We should instead extract the most significant bits:

```csharp
uint mostSignificantBits = equals.ExtractMostSignificantBits();
Console.WriteLine(Convert.ToString(mostSignificantBits, 2).PadLeft(32, '0'));
```

```log
00000000000000000000000000000100
```

and use [BitOperations.TrailingZeroCount](https://learn.microsoft.com/dotnet/api/system.numerics.bitoperations.trailingzerocount) or [uint.TrailingZeroCount](https://learn.microsoft.com/dotnet/api/system.uint32.trailingzerocount) (introduced in .NET 7) to get the trailing zero count.

To calculate the last index, we should use [BitOperations.LeadingZeroCount](https://learn.microsoft.com/dotnet/api/system.numerics.bitoperations.leadingzerocount) or [uint.LeadingZeroCount](https://learn.microsoft.com/dotnet/api/system.uint32.leadingzerocount) (introduced in .NET 7). But the returned value needs to be subtracted from 31 (32 bits in an `unit`, indexed from 0).

If we were working with a buffer loaded from memory (example: searching for the last index of a given character in the buffer) both results would be relative to the `elementOffset` provided to the `Load` method that was used to load the vector from the buffer.

```csharp
int ComputeLastIndex<T>(nint elementOffset, Vector128<T> equals) where T : struct
{
    uint mostSignificantBits = equals.ExtractMostSignificantBits();

    int index = 31 - BitOperations.LeadingZeroCount(mostSignificantBits); // 31 = 32 (bits in UInt32) - 1 (indexing from zero)

    return (int)elementOffset + index;
}
```

If we were using the `Load` overload that takes only the managed reference, we could use [Unsafe.ByteOffset<T>(ref T, ref T)](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafe.byteoffset) to calculate the element offset.

```csharp
unsafe int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
{
    int elementOffset = (int)Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(T);

    uint mostSignificantBits = equals.ExtractMostSignificantBits();
    int index = BitOperations.TrailingZeroCount(mostSignificantBits);
    
    return elementOffset + index;
}
```

### Comparison

Beside equality checks, vector APIs allow for comparison. The `bool`-returning overloads return `true` when the given condition is true:

```csharp
public static bool GreaterThanAll<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool GreaterThanAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool GreaterThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool GreaterThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool LessThanAll<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool LessThanAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool LessThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static bool LessThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

Similarly to `Equals`, vector-returning overloads return a vector whose elements are all-bits-set or zero, depending on whether the corresponding elements in `left` and `right` meet the given condition.

```csharp
public static Vector128<T> GreaterThan<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static Vector128<T> GreaterThanOrEqual<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static Vector128<T> LessThan<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static Vector128<T> LessThanOrEqual<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

`ConditionalSelect` Conditionally selects a value from two vectors on a bitwise basis.

```csharp
public static Vector128<T> ConditionalSelect<T>(Vector128<T> condition, Vector128<T> left, Vector128<T> right)
    => (left & condition) | (right & ~condition);
```

This method deserves a self-describing example:

```csharp
Vector128<float> left = Vector128.Create(1.0f, 2, 3, 4);
Vector128<float> right = Vector128.Create(4.0f, 3, 2, 1);

Vector128<float> result = Vector128.ConditionalSelect(Vector128.GreaterThan(left, right), left, right);

Assert.Equal(Vector128.Create(4.0f, 3, 3, 4), result);
```

### Math

Very simple math operations can be also expressed by using the operators. The operators should be preferred where possible, as it helps avoid bugs around operator precedence and can improve readability.

```csharp
public static Vector128<T> Add<T>(Vector128<T> left, Vector128<T> right) where T : struct => left + right;
public static Vector128<T> Divide<T>(Vector128<T> left, Vector128<T> right) => left / right;
public static Vector128<T> Divide<T>(Vector128<T> left, T right) => left / right;
public static Vector128<T> Multiply<T>(Vector128<T> left, Vector128<T> right) => left * right;
public static Vector128<T> Multiply<T>(Vector128<T> left, T right) => left * right;
public static Vector128<T> Subtract<T>(Vector128<T> left, Vector128<T> right) => left - right;
```

**Note:** Some of the methods accept a single value as the second argument.

`Abs`, `Ceiling`, `Floor`, `Max`, `Min`, `Sqrt` and `Sum` are also provided:

```csharp
public static Vector128<T> Abs<T>(Vector128<T> vector) where T : struct
public static Vector128<double> Ceiling(Vector128<double> vector)
public static Vector128<float> Ceiling(Vector128<float> vector)
public static Vector128<double> Floor(Vector128<double> vector)
public static Vector128<float> Floor(Vector128<float> vector)
public static Vector128<T> Max<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static Vector128<T> Min<T>(Vector128<T> left, Vector128<T> right) where T : struct
public static Vector128<T> Sqrt<T>(Vector128<T> vector) where T : struct
public static T Sum<T>(Vector128<T> vector) where T : struct
```

### Conversion

Vector types provide a set of methods dedicated to number conversions:

```csharp
public static unsafe Vector128<double> ConvertToDouble(Vector128<long> vector)
public static unsafe Vector128<double> ConvertToDouble(Vector128<ulong> vector)
public static unsafe Vector128<int> ConvertToInt32(Vector128<float> vector)
public static unsafe Vector128<long> ConvertToInt64(Vector128<double> vector)
public static unsafe Vector128<float> ConvertToSingle(Vector128<int> vector)
public static unsafe Vector128<float> ConvertToSingle(Vector128<uint> vector)
public static unsafe Vector128<uint> ConvertToUInt32(Vector128<float> vector)
public static unsafe Vector128<ulong> ConvertToUInt64(Vector128<double> vector)
```

And for reinterpretation (no values are being changed, they can be just used as if they were of a different type):

```csharp
public static Vector128<TTo> As<TFrom, TTo>(this Vector128<TFrom> vector)
public static Vector128<byte> AsByte<T>(this Vector128<T> vector)
public static Vector128<double> AsDouble<T>(this Vector128<T> vector)
public static Vector128<short> AsInt16<T>(this Vector128<T> vector)
public static Vector128<int> AsInt32<T>(this Vector128<T> vector)
public static Vector128<long> AsInt64<T>(this Vector128<T> vector)
public static Vector128<nint> AsNInt<T>(this Vector128<T> vector)
public static Vector128<nuint> AsNUInt<T>(this Vector128<T> vector)
public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector)
public static Vector128<float> AsSingle<T>(this Vector128<T> vector)
public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector)
public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector)
public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector)
```

### Widening and Narrowing

The first half of every vector is called "lower", the second is "upper".

```
------------------------------128-bits---------------------------
|           LOWER               |             UPPER             |
-----------------------------------------------------------------
|      32       |      32       |      32       |      32       |
----------------------------------------------------------------|
|  16   |  16   |  16   |  16   |  16   |  16   |  16   |  16   |
-----------------------------------------------------------------
| 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 |
-----------------------------------------------------------------
```

In case of `Vector128`, `GetLower` gets the value of the lower 64-bits as a new `Vector64<T>` and `GetUpper` gets the upper 64-bits.

```csharp
public static Vector64<T> GetLower<T>(this Vector128<T> vector)
public static Vector64<T> GetUpper<T>(this Vector128<T> vector)
```

Each vector type provides a `Create` method that allows for the creation from lower and upper:

```csharp
public static unsafe Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
public static Vector256<byte> Create(Vector128<byte> lower, Vector128<byte> upper)
```

`Lower` and `Upper` are also used by `Widen`. This method widens a `Vector128<T1>` into two `Vector128<T2>` where `sizeof(T2) == 2 * sizeof(T1)`.

```csharp
public static unsafe (Vector128<ushort> Lower, Vector128<ushort> Upper) Widen(Vector128<byte> source)
public static unsafe (Vector128<int> Lower, Vector128<int> Upper) Widen(Vector128<short> source)
public static unsafe (Vector128<long> Lower, Vector128<long> Upper) Widen(Vector128<int> source)
public static unsafe (Vector128<short> Lower, Vector128<short> Upper) Widen(Vector128<sbyte> source)
public static unsafe (Vector128<double> Lower, Vector128<double> Upper) Widen(Vector128<float> source)
public static unsafe (Vector128<uint> Lower, Vector128<uint> Upper) Widen(Vector128<ushort> source)
public static unsafe (Vector128<ulong> Lower, Vector128<ulong> Upper) Widen(Vector128<uint> source)
```

It's also possible to widen only the lower or upper part:

```csharp
public static Vector128<ushort> WidenLower(Vector128<byte> source)
public static Vector128<ushort> WidenUpper(Vector128<byte> source)
```

An example of widening is converting a buffer of ASCII bytes into characters:

```csharp
byte[] byteBuffer = Enumerable.Range('A', 128 / 8).Select(i => (byte)i).ToArray();
Vector128<byte> byteVector = Vector128.Create(byteBuffer);
Console.WriteLine(byteVector);
(Vector128<ushort> Lower, Vector128<ushort> Upper) = Vector128.Widen(byteVector);
Console.Write(Lower.AsByte());
Console.WriteLine(Upper.AsByte());

Vector256<ushort> ushortVector = Vector256.Create(Lower, Upper);
Span<ushort> ushortBuffer = stackalloc ushort[256 / 16];
ushortVector.CopyTo(ushortBuffer);
Span<char> charBuffer = MemoryMarshal.Cast<ushort, char>(ushortBuffer);
Console.WriteLine(new string(charBuffer));
```

```log
<65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80>
<65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 70, 0, 71, 0, 72, 0><73, 0, 74, 0, 75, 0, 76, 0, 77, 0, 78, 0, 79, 0, 80, 0>
ABCDEFGHIJKLMNOP
```

`Narrow` is the opposite of `Widen`.

```csharp
public static unsafe Vector128<float> Narrow(Vector128<double> lower, Vector128<double> upper)
public static unsafe Vector128<sbyte> Narrow(Vector128<short> lower, Vector128<short> upper)
public static unsafe Vector128<short> Narrow(Vector128<int> lower, Vector128<int> upper)
public static unsafe Vector128<int> Narrow(Vector128<long> lower, Vector128<long> upper)
public static unsafe Vector128<byte> Narrow(Vector128<ushort> lower, Vector128<ushort> upper)
public static unsafe Vector128<ushort> Narrow(Vector128<uint> lower, Vector128<uint> upper)
public static unsafe Vector128<uint> Narrow(Vector128<ulong> lower, Vector128<ulong> upper)
```

In contrast to [Sse2.PackUnsignedSaturate](https://learn.microsoft.com/dotnet/api/system.runtime.intrinsics.x86.sse2.packunsignedsaturate) and [AdvSimd.Arm64.UnzipEven](https://learn.microsoft.com/dotnet/api/system.runtime.intrinsics.arm.advsimd.arm64.unzipeven), `Narrow` applies a mask via AND to cut anything above the max value of returned vector:


```csharp
Vector256<ushort> ushortVector = Vector256.Create((ushort)300);
Console.WriteLine(ushortVector);
unchecked { Console.WriteLine((byte)300); }
Console.WriteLine(300 & byte.MaxValue);
Console.WriteLine(Vector128.Narrow(ushortVector.GetLower(), ushortVector.GetUpper()));

if (Sse2.IsSupported)
{
    Console.WriteLine(Sse2.PackUnsignedSaturate(ushortVector.GetLower().AsInt16(), ushortVector.GetUpper().AsInt16()));
}
```

```log
<300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300, 300>
44
44
<44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44, 44>
<255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255>
```

### Shuffle

`Shuffle` creates a new vector by selecting values from an input vector using a set of indices (values that represent indexes of the input vector).

```csharp
public static Vector128<int> Shuffle(Vector128<int> vector, Vector128<int> indices)
public static Vector128<uint> Shuffle(Vector128<uint> vector, Vector128<uint> indices)
public static Vector128<float> Shuffle(Vector128<float> vector, Vector128<int> indices)
public static Vector128<long> Shuffle(Vector128<long> vector, Vector128<long> indices)
public static Vector128<ulong> Shuffle(Vector128<ulong> vector, Vector128<ulong> indices)
public static Vector128<double> Shuffle(Vector128<double> vector, Vector128<long> indices)
```

It can be used for many things, including reversing the input:

```csharp
Vector128<int> intVector = Vector128.Create(100, 200, 300, 400);
Console.WriteLine(intVector);
Console.WriteLine(Vector128.Shuffle(intVector, Vector128.Create(3, 2, 1, 0)));
```

```log
<100, 200, 300, 400>
<400, 300, 200, 100>
```

#### Vector256.Shuffle vs Avx2.Shuffle

`Vector256.Shuffle` and `Avx2.Shuffle` are not identical.

`Avx2.Shuffle` is effectively `2x128-bit ops` while `Vector256.Shuffle` treats it as a "single 256-bit vector" (rather than "2x128-bit vectors"). This was done for consistency and to better map to a cross-platform mentality where `AVX-512` and `SVE` all operate on "full width".

## Summary

The main goal of the new `Vector128` and `Vector256` APIs is to make writing fast, vectorized code possible without becoming familiar with hardware-specific instructions and becoming an assembly language expert. Our recommendations depend on your current expertise level, software you maintain and the one you need to create:

- If you are already an expert and you have vectorized your code for both `x64/x86` and `arm64/arm` code you can use the new APIs to simplify your code, but you most likely won't observe any performance gains. [#64451](https://github.com/dotnet/runtime/issues/64451) lists the places where it was/can be done in dotnet/runtime. You can use links to the merged PRs to see real-life examples.
- If you have already vectorized your code, but only for `x64/x86` or `arm64/arm`, you can use the  new APIs to have a single, cross-platform implementation.
- If you have already vectorized your code with `Vector<T>` you can use the new APIs to check if they can produce better code-gen.
- If you are not familiar with hardware specific instructions or you are about to vectorize a scalar algorithm, you should start with the new `Vector128` and `Vector256` APIs. Get a solid and working implementation and eventually consider using hardware-specific methods for performance critical code paths.
- Both managed references and unsafe pointers are dangerous to use incorrectly and each comes with their own tradeoff.

### Best practices

1. Implement tests that cover all code paths, including Access Violations.
2. Run tests for all hardware acceleration scenarios, use the existing environment variables to do that.
3. Implement benchmarks that mimic real life scenarios, do not increase the complexity of your code when it's not beneficial for your end users.
4. Use `ref MemoryMarshal.GetReference(span)` instead `ref span[0]` and `ref MemoryMarshal.GetArrayDataReference(array)` instead `ref array[0]` to handle empty buffers correctly.
5. Prefer `LoadUnsafe(ref T, nuint elementOffset)` and `StoreUnsafe(this Vector128<T> source, ref T destination, nuint elementOffset)` over other methods for loading and storing vectors as they avoid pinning and the need of doing pointer arithmetic. Be aware of unsigned integer overflow!
6. Always handle the vectorized loop remainder.
7. When storing values in memory, be aware of a potential buffer overlap.
8. When writing a vectorized algorithm, start with writing the tests for edge cases, then implement a scalar solution and afterwards try to express what the scalar code is doing with Vector128/256 APIs.
9. Vector types provide APIs for creating, loading, storing, comparing, converting, reinterpreting, widening, narrowing and shuffling vectors. It's also possible to perform equality checks, various bit and math operations. Don't try to memorize all the details, treat these APIs as a cookbook that you come back to when needed.
