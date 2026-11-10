# Security design doc for `System.HashCode`

## Summary

The `System.HashCode` type is not intended to be secure in the face of adversarial input. The biggest risk exposure for consumers who use the type with adversarial input is Denial of Service via hash flooding.

The type's internal logic treats all input as opaque and thus is intended to be resilient to any form of direct attack. (Though see the section titled _General data flow and security considerations_.) However, the type disclaims any guarantee that the generated hash codes are appropriate for use in contexts which require hash flooding protection.

## Outline

This document discusses:

- The design philosophy of `HashCode`
- Security promises and non-promises made by the type
- Data flow through the type and its expected consumers
- Analysis of the type's current security characteristics
- Discussion of how making security promises would impact the API surface and implementation

## Introduction and scope

The `System.HashCode` type is a utility type for distilling multi-dimensional values (typically, a tuple) into a single 32-bit hash code. The general use case is to facilitate utilizing such types as keys in a dictionary. (See [the `Dictionary<TKey, TValue>` security design doc](System.Collections.Generic.Dictionary.md) for further discussion.)

This document covers the `HashCode` type only as it exists as part of the .NET 6+ shared framework or within the [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode) NuGet package. All other distributions are out of scope of this document. The NuGet package allows installation in downlevel versions of .NET Framework. As of this writing, the latest package version is [v1.1.1](https://www.nuget.org/packages/Microsoft.Bcl.HashCode/1.1.1).

### Audience

This document is intended for:

- **Maintainers of `HashCode`**, who need to understand and preserve the type's design principles.
- **Consumers of `HashCode`**, who depend on these principles.

## General data flow and security considerations

It is useful to think of the `HashCode` type not as an isolated component, but within the context of how a typical developer will consume it. General usage typically involves three steps: generating the data to provide to the hasher, running the hasher logic itself, and consuming the output of the hasher.

### Step 1 - Converting inputs to binary (optional)

The `Add` and `Combine` generic methods allow ingestion of arbitrary data types into the hasher. Since the hasher itself can only work with binary buffers, these methods rely on an intermediate hash code function - typically `object.GetHashCode()` - to convert the input objects into a binary representation.

The safety of allowing arbitrary non-binary inputs depends on the implementation of those types' own hash code functions. If adversarial input is allowed, then those hash code routines should be written with that in mind, including that they terminate in roughly linear time proportional to the size of the input value.

### Step 2 - Internal round functions

If `AddBytes` is called with an existing binary buffer, or once Step 1 above has converted the arbitrary input object into a usable intermediate hash code, the hasher can utilize this raw data in its internal round functions.

This data is consumed opaquely. The contents of the byte buffer (or the values of the arbitrary object intermediate hash codes) will not affect the internal workings of the round functions. The hasher may keep track of the cumulative length of data it has seen, but this uses modular arithmetic and is resilient against integer overflow. The end result is that many of the attacks an adversary might attempt against the hasher object itelf - Denial of Service, buffer overrun, etc. - are not viable.

Through this, `HashCode` makes an explicit security claim that the round and mixing functions are resilient against adversarial input causing misbehavior. The internal workings _will_ successfully produce some 32-bit hash code, though see the Step 3 discussion below.

### Step 3 - Generating and consuming the hash code

Once all data has been ingested, the `ToHashCode` method returns a 32-bit hash code. The hash code computation uses a seeded function, where the seed is chosen randomly at app start. The computation is stable for any set of inputs for the lifetime of the process, but the hash code has no meaning outside the current process. It is not intended that these hash codes be transmitted outside the current process or otherwise persisted.

The most common use of these hash codes is for bucket-based keyed collections to choose a bucket index where this entry will be stored. For non-adversarial inputs, these hash codes are sufficient and will result in a generally uniform selection of bucket. For adversarial inputs, these hash codes might be insufficient, and the adversary could attempt to exploit inefficiencies in the data structure's layout, significantly affecting the collection's performance. This is the crux of a hash flooding attack.

`HashCode` explicitly disclaims that a hash code generated from adversarial input is fit for consumption by the caller.

## Background definitions and assumptions

A **hash code function** is any deterministic function $f : T \to \text{int32}$ which, given inputs $t, t' \in T$, satisfies the clause:

$$
t = t' \implies f(t) = f(t')
$$

(This is a slight oversimplification. See later in this document for further discussion.) Hash code functions ideally have certain desirable properties (see [\[1\]](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), [\[2\]](https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-object-gethashcode)), but the strict definition of a legal hash code function does not require anything beyond satisfying the original constraint above. This means that $f(t) \coloneqq 0$, though not terribly useful, is in fact a legal hash code function for any type $T$ since it trivially satisfies the necessary clause.

To make hash code functions more useful, a desirable property is **non-adversarial collision resistance**. That is, given a hash code function $f$ and two inputs $t, t' \in T \mid t' \neq t'$, it would be beneficial to have $f(t_1) \neq f(t_2)$.

For the Boolean type, where $t \in \text{bool}$, one example might be:

$$
f(t) \coloneqq
\begin{cases}
0 & \mid t = \textit{true} \\
1 & \mid t = \textit{false}
\end{cases}
$$

For the Int32 type, where $t \in \text{int32}$, one example might be:

$$
f(t) \coloneqq t
$$

When the domain of all possible inputs is $> 2^{32}$ elements, the [pigeonhole principle](https://en.wikipedia.org/wiki/Pigeonhole_principle) states that some non-equal inputs will produce identical outputs, resulting in collision. (This assumes the function $f$ will terminate; see section _General data flow and security considerations_.)

> **Note**
>
> The term "non-adversarial collision resistance" is used here to distinguish this concept from collision resistance in true cryptographic hash functions.

When pigeonholing is unavoidable, a desirable property of hash code functions is **uniform distribution**: that all outputs occur with approximately equal probability. Consider this hash code function for $t \in \text{uint64}$.

$$
f(t) \coloneqq
\begin{cases}
t & \mid t < 2^{31} \\
0 & \mid \text{otherwise}
\end{cases}
$$

Since the overwhelming majority of all inputs go down the "otherwise" case (vanishingly few elements from the set of all possible uint64 values actually fit into an int32!), this biases heavily toward a return value of 0. It is a legal but undesirable hash code function. Fortunately, this can be easily modified to restore the desirable distribution property.

$$
f(t) \coloneqq \mathop{bitcast}\_\text{int32} \left( t \mod 2^{32} \right)
$$

In real-world applications, inputs into the hash code function are not randomly chosen from the domain of all possible inputs $T$. Inputs representing numbers or dates may be clustered around a predictable range of values. Inputs representing strings have structure depending on the protocol being implemented or the written language of the end user. This could cause the generated hash codes to be clustered around a central value or to reflect the same pattern as the inputs. Data structures like dictionaries - which consume hash codes and perform modulus operations on the values - may suffer performance impacts due to biased bucket selection.

To mitigate this, it is desirable for hash code functions to implement **avalanching**. Even a single bit change in the input value should affect the output in a substantial manner. Define $H$ to be the Hamming distance function and choose $t, t' \in T$ such that they are minimally different (e.g., if $t, t'$ are strings, then $H(t, t') = 1$). The hash code function $f$ is said to have good avalanching if $H(f(t), f(t')) \approx \text{16 bits}$ for all such pairs $(t, t')$.

Finally, since keyed collection types are expected to invoke hash code functions multiple times per collection instance, **performance** is a key metric. If the input value $t \in T$ has length $l$ (where $l$ can be thought of as the number of bits in the in-memory representation of $t$), the hash code function ideally executes in $O(l)$ time with very few clock cycles per input bit. For example, both [CityHash](https://github.com/google/cityhash) and [HighwayHash](https://github.com/google/highwayhash) advertise a steady state of approx. 4 bytes per cycle over large inputs. If inputs are expected to be small, implementers may wish to optimize for lower latency rather than raw throughput.

## Goals and non-goals

It is a **goal** for the implementation of `HashCode` to have the four desirable qualities mentioned previously: non-adversarial collision resistance, uniform distribution of outputs, good avalanching, and high performance. In this context, acceptable performance is usually a function of business goals rather than something with a fixed value.

It is a **non-goal** for the `HashCode` type to have stable output across unique invocations of an application. .NET reserves the right to change the implementation of this type. This might be in response to changes in usage scenarios, introduction of new algorithms, or newly discovered ways to better achieve the goals laid out here.

To facilitate this, it is a **goal** to make `HashCode`'s output nonpredictable across application invocations. This helps alert developers early that they should not be persisting or transmitting the results of hash code calculations. This should prevent another situation like what occurred during .NET Framework 4.5 development, where the team modified the implementation of `string.GetHashCode()` but had to revert the change before product release because so many applications had persisted these values and were broken by the change.

Finally, even though `HashCode` utilizes randomness, it is a **non-goal** to make the type resilient against adversarial input.

Concretely, in the face of adversarial input:

- **No collision resistance is claimed.** It may be trivial to generate multiple non-equal inputs which all result in the same output hash code, even without knowing the random seed. Similarly, no claim is made that output hash codes will be uniformly distributed over the output space, that the output hash codes are free from predictable patterns, etc.
- **No secrecy of the random seed is claimed.** It may be trivial to generate chosen inputs, observe the resulting hash codes, and deduce the random seed.

## Implementation

The `HashCode` type uses the [**xxHash32**](https://github.com/Cyan4973/xxHash) algorithm, which is a non-cryptographic hash algorithm with a 32-bit seed and a 32-bit digest. All instances of the `HashCode` type use the same seed value, generated randomly at app start. This value is chosen independently of other random seed values in the runtime, such as the global 64-bit seed used in `string.GetHashCode`'s Marvin32 routine.

The xxHash32 repo's README file touts good performance and avalanching. This can be validated through a simple C# program.

```cs
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

const int SAMPLE_SIZE = 50000;
List<int> hammingDistances = new List<int>();

Span<byte> scratch = stackalloc byte[sizeof(uint)];
for (int i = 0; i < SAMPLE_SIZE; i++)
{
    // Create t, t', where t' is t with a single random bit flipped
    RandomNumberGenerator.Fill(scratch);
    uint randomValue = BinaryPrimitives.ReadUInt32LittleEndian(scratch);
    uint withRandomBitFlipped = randomValue ^ (1u << RandomNumberGenerator.GetInt32(32));

    // Compute HashCode hashes over t and t'
    uint hash1 = (uint)HashCode.Combine(randomValue);
    uint hash2 = (uint)HashCode.Combine(withRandomBitFlipped);

    // Compute Hamming distance between them
    int hammingDistance = BitOperations.PopCount(hash1 ^ hash2);
    hammingDistances.Add(hammingDistance);
}

Console.WriteLine($"Sample size: {SAMPLE_SIZE}");
Console.WriteLine($"Average Hamming distance: {hammingDistances.Average():N4}");
```
```txt
Sample size: 50000
Average Hamming distance: 15.9982
```

The output shows that the `HashCode` type achieves very close to the ideal average of 16 bits for each input pair, confirming the high quality of avalanching.

### API design considerations

The API is designed to accept arbitrary-length input buffers, and callers can pass as many of these buffers as desired into a `HashCode` instance over the lifetime of that instance. There is no upper bound to the amount of data that can be hashed.

```cs
HashCode hasher = new HashCode();
hasher.AddBytes([ /* buffer 1 */ ]);
hasher.AddBytes([ /* buffer 2 */ ]);
// ...
int hashCode = hasher.ToHashCode();
```

There is also a generic `Add` method which accepts a typed input and factors it into the current `HashCode` instance. The `Combine` generic static method is an accelerator which performs the equivalent of calling this in a loop, once per input, before finalizing the current hash code value.

Since xxHash32 cannot directly ingest a typed _T_ object into its computation, the `Add` and `Combine` methods call `object.GetHashCode` on their inputs in order to generate intermediate 32-bit hash codes, then these intermediate hash codes are then treated as raw binary data and passed in to xxHash32's core algortihm. This intermediate computation means that any pigeonholing in hash code generation takes place _immediately_, before xxHash32's seeded operation is able to look at the data.

Consider int64's naive hash code calculation, copied below for reference.

```cs
long.GetHashCode() => unchecked((int)((long)m_value)) ^ (int)(m_value >> 32);
```

Because this hash code function has deficiencies in its distribution and avalanching properties, it can produce low quality intermediate hash codes. Passing an int64 value _as a typed int64 value_ to `HashCode` causes it to generate and consume this low quality intermediate value, which negatively impacts the quality of the final results.

```cs
using System;

for (int i = 0; i < 10; i++)
{
    long l = 0x100000001L * i;
    HashCode hasher = new HashCode();
    hasher.Add(l);
    Console.WriteLine($"HashCode of 0x{l:X16} (as long) = 0x{hasher.ToHashCode():X8}");
}
```
```txt
HashCode of 0x0000000000000000 (as long) = 0xDB296EE8
HashCode of 0x0000000100000001 (as long) = 0xDB296EE8
HashCode of 0x0000000200000002 (as long) = 0xDB296EE8
HashCode of 0x0000000300000003 (as long) = 0xDB296EE8
HashCode of 0x0000000400000004 (as long) = 0xDB296EE8
HashCode of 0x0000000500000005 (as long) = 0xDB296EE8
HashCode of 0x0000000600000006 (as long) = 0xDB296EE8
HashCode of 0x0000000700000007 (as long) = 0xDB296EE8
HashCode of 0x0000000800000008 (as long) = 0xDB296EE8
HashCode of 0x0000000900000009 (as long) = 0xDB296EE8
```

Contrast this against the sample which passes the int64 value _as a binary buffer_, avoiding the intermediate hash code computation and allowing a higher quality final result.

```cs
using System;
using System.Buffers.Binary;

Span<byte> scratch = stackalloc byte[sizeof(long)];
for (int i = 0; i < 10; i++)
{
    long l = 0x100000001L * i;
    BinaryPrimitives.WriteInt64LittleEndian(scratch, l);
    HashCode hasher = new HashCode();
    hasher.AddBytes(scratch);
    Console.WriteLine($"HashCode of 0x{l:X16} (as bytes) = 0x{hasher.ToHashCode():X8}");
}
```
```txt
HashCode of 0x0000000000000000 (as bytes) = 0xBA24E81B
HashCode of 0x0000000100000001 (as bytes) = 0x8E49D77E
HashCode of 0x0000000200000002 (as bytes) = 0xD34743FE
HashCode of 0x0000000300000003 (as bytes) = 0x3A4F23B9
HashCode of 0x0000000400000004 (as bytes) = 0x62E7BB3A
HashCode of 0x0000000500000005 (as bytes) = 0xFEE210C3
HashCode of 0x0000000600000006 (as bytes) = 0x9FD896AC
HashCode of 0x0000000700000007 (as bytes) = 0xFA50A66E
HashCode of 0x0000000800000008 (as bytes) = 0x757F14C9
HashCode of 0x0000000900000009 (as bytes) = 0xC52EEBCC
```

> **Caution**
>
> Using `BinaryPrimitives` or `MemoryMarshal` or equivalent in this manner is in general _not_ an acceptable way to get a binary buffer for hashing, except in certain known safe cases. Discussion on this subject is outside the scope of this document.

## Potential for collision and seed recovery

As mentioned earlier, **`HashCode` does not make any claims of collision resistance.** This is easily seen if the inputs $f(t)$ and $f(t')$ are caller-provided. When operating on small inputs, the inner hash routine's `QueueRound` function is called twice in a row (see [\[1\]](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/HashCode.cs#L106-L107), [\[2\]](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/HashCode.cs#L275-L278)). This means that the intermediate state is

$$
\begin{align*}
h &\coloneqq \left( \left( s + f(t) \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 & \pmod{2^{32}} \\
h' &\coloneqq \left( \left( h + f(t') \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 & \pmod{2^{32}}
\end{align*}
$$

Where $s \in_R \text{uint32}$ is a fixed seed unknown to the caller, $t, t' \in T$ are inputs to the inner hash routine, and $P_3 \coloneqq \texttt{0xC2B2AE3D}$ and $P_4 \coloneqq \texttt{0x27D4EB2F}$ are taken from [the `HashCode` sources](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/HashCode.cs#L63-L64). All operations below are also performed mod $2^{32}$, but that annotation will be omitted to improve readability.

An interesting property of the $(x) \mathop{rol_N} Z$ construction is that it is partially distributive. That is, there are some cases where $(x + y) \mathop{rol_N} Z = \left( (x) \mathop{rol_N} Z \right) + \left( (y) \mathop{rol_N} Z \right)$. Allowing that $(x) \mathop{rol_N} Z$ is equivalent to $(x) \mathop{ror_N} \left( N - Z \right)$, and allowing $y \coloneqq i \cdot 2^{(N-Z)}$ for $i \in \mathbb{Z}$, this results in

$$
\begin{align*}
\left( x + y \right) \mathop{rol_N} Z &= \left( x + i \cdot 2^{(N-Z)} \right) \mathop{rol_N} Z \\
&= \left( x \right) \mathop{rol_N} Z + \left( i \cdot 2^{(N-Z)} \right) \mathop{rol_N} Z \\
&= \left( x \right) \mathop{rol_N} Z + \left( i \cdot 2^{(N-Z)} \right) \mathop{ror_N} \left( N - Z \right) \\
&= \left( x \right) \mathop{rol_N} Z + i
\end{align*}
$$

Note that these reductions are only legal if $i$ is chosen such that the operation $x + i \cdot 2^{(N-Z)}$ does not integer overflow or underflow. This trivially holds for $i = 0$. One insight is that the sum $x + i \cdot 2^{(N-Z)}$ only ever differs from the value $x$ in the high $Z$ bits. So by setting $i = 1, 2, 3, \ldots$, this effectively treats the high $Z$ bits of $x$ as a counter and increments that counter. This will eventually result in integer overflow, at which point one can try $i = -1, -2, -3, \ldots$, stopping when integer underflow is observed.

For any given $Z$-bit integer, there are $2^Z$ distinct values that can be added to it without resulting in integer overflow / underflow. This means there are $2^Z$ distinct values of $i$ for which the reduction outlined above is legal.

Let $P_3^{-1} \coloneqq \mathtt{0xA89ED915}$ be the [multiplicative inverse](https://en.wikipedia.org/wiki/Modular_multiplicative_inverse) of $P_3$. Choose $t_i, t_i'$ such that

$$
\begin{align*}
f(t_i) &= i \cdot 2^{15} \cdot P_3^{-1} \\
f(t_i') &= i \cdot P_3^{-1} \cdot (-P_4)
\end{align*}
$$

And substitute these into the original calculations for $h$ and $h'$, keeping in mind that there are exactly $2^{17}$ distinct values of $i$ for which the reduction outlined below is legal.

$$
\begin{align*}
h &\coloneqq \left( \left( s + f(t_i) \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( s + i \cdot 2^{15} \cdot P_3^{-1} \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( s + i \cdot 2^{15} \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( \left( s \right) \mathop{rol_{32}} 17 \right) + \left( \left( i \cdot 2^{15} \right) \mathop{rol_{32}} 17 \right) \right) \cdot P_4 \\
&\coloneqq \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 + \left( \left( i \cdot 2^{15} \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 + \left( \left( i \cdot 2^{15} \right) \mathop{ror_{32}} 15 \right) \cdot P_4 \\
&\coloneqq \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 + i \cdot P_4 \\
h' &\coloneqq \left( \left( h + f(t_i') \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 + i \cdot P_4 + i \cdot P_3^{-1} \cdot (-P_4) \cdot P_3 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 + i \cdot P_4 - i \cdot P_4 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
&\coloneqq \left( \left( \left( \left( s \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \right) \mathop{rol_{32}} 17 \right) \cdot P_4 \\
\end{align*}
$$

The final result is not dependent on the input value $i$, resulting in a collision for all $2^{17}$ values $i$. Furthermore, since the final result is based solely on the starting seed $s$, and since $s$ is only a 32-bit integer, it is possible for somebody who has observed the output to recover the original seed by reversing these operations or through brute force.

A simple C# console application can be written to demonstrate these collisions. The exact output will differ based on the random seed, but the collision is easily observed.

```cs
using System;

// from HashCode sources
const uint Prime3 = 0xC2B2AE3D;
const uint Prime4 = 0x27D4EB2F;

// multiplicative inverse of Prime3 mod 2**32
const uint Prime3Inv = 0xA89ED915;

for (int i = 0; i < 100; i++)
{
    uint a = (uint)i * Prime3Inv << 15;
    uint b = (uint)i * unchecked(Prime3Inv * (uint)(-Prime4));
    Console.WriteLine($"HashCode.Combine(0x{a:X8}, 0x{b:X8}) = 0x{HashCode.Combine(a, b):X8}");
}
```
```txt
HashCode.Combine(0x00000000, 0x00000000) = 0xE1BD6A47
HashCode.Combine(0x6C8A8000, 0x412BDE25) = 0xE1BD6A47
HashCode.Combine(0xD9150000, 0x8257BC4A) = 0xE1BD6A47
HashCode.Combine(0x459F8000, 0xC3839A6F) = 0xE1BD6A47
HashCode.Combine(0xB22A0000, 0x04AF7894) = 0xE1BD6A47
HashCode.Combine(0x1EB48000, 0x45DB56B9) = 0xE1BD6A47
HashCode.Combine(0x8B3F0000, 0x870734DE) = 0xE1BD6A47
HashCode.Combine(0xF7C98000, 0xC8331303) = 0xE1BD6A47
HashCode.Combine(0x64540000, 0x095EF128) = 0xE1BD6A47
HashCode.Combine(0xD0DE8000, 0x4A8ACF4D) = 0xE1BD6A47
...
```

The above analysis was performed under the assumption that the adversary can control the values $f(t_i)$ and $f(t_i')$. This might not be the case. Perhaps the adversary controls $t_i$ and $t_i'$ but the hash code function $f$ is randomized in unpredictable ways. That the specific demonstration in this section falls apart under these conditions should not be taken as evidence that randomizing $f$ is sufficient to mitigate any hash collision attack vector. Remember: **`HashCode` does not make any claims of collision resistance.**

## Future improvements and considerations

### Improving collision resistance through API modifications

The proposal here is a **breaking API change**. The `HashCode.Add` and `HashCode.Combine` methods would be removed, marked obsolete-as-error, or have their functionality severely curtailed.

One might consider improving collision resistance by switching to a stronger function like [SipHash](https://en.wikipedia.org/wiki/SipHash) or [HighwayHash](https://github.com/google/highwayhash), both of which claim collision resistance. Switching to a true keyed cryptographic hash function like HMACSHA256 would also work, but it falls far short of `HashCode`'s performance goals. This is one case where the type is willing to make some compromises in the name of practical performance requirements.

The issue with all of these - including HMACSHA256 - stems from the _API design considerations_ section earlier in this document. The `HashCode.Add` and `HashCode.Combine` generic methods encourage the pattern of passing arbitrary values into the `HashCode` instance, requiring an intermediate hash code calculation. Some overloads even take an `IEqualityComparer<T>` comparer, exposing throught the API signature itself the expectation that this intermediate calculation will be performed. If these hash code functions are themselves subject to simple collisions, there's no algorithm the `HashCode` class can leverage to mitigate this. The issue occurs in the code that runs prior to the hasher's core logic.

If there is a need to add these types into a dictionary safely, there are several options available. [The `Dictionary<TKey, TValue>` security design doc](System.Collections.Generic.Dictionary.md) covers this in more detail, including contemplating changes to the dictionary data structures to improve resiliency.

Assuming there is not an appetite for changing the internal layout of dictionary instances, and assuming the broader ecosystem would benefit from being able to compute collision-resistant digests over a wide collection of types, one might consider addressing this through a series of new interfaces.

```cs
interface IHashable
{
    void WriteTo(Action<ReadOnlySpan<byte>> receiver);
}

interface IHashableProxy<T>
{
    void WriteTo(T value, Action<ReadOnlySpan<byte>> receiver);
}
```

Primitive types, numeric types, `string`, `Guid`, and several other types would implement the base `IHashable` interface. The caller would allocate the buffer and would be responsible for choosing the appropriate collision-resistant algorithm; the types simply write their own binary representations into the buffer. Implementations would be expected to provide the entire contents of their hashable buffer to the receiver over one or more calls. No pre-digest would take place, eliminating the potential for pigeonholing on inputs. Floating-point types would want to special-case NaN and zeroes within their implementations, just as functions like `float.GetHashCode()` [do today](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Single.cs#L327). Classes like `StringComparer` would implement `IHashableProxy<string>` so that they could provide appropriate implementations based on their captured culture and comparison options, just as `StringComparer.GetHashCode(string)`'s overridden methods do today.

Sample implementations for `Guid` and `double` are provided below.

```cs
public readonly struct Guid : IHashable
{
    void IHashable.WriteTo(Action<ReadOnlySpan<byte>> receiver)
    {
        // endianness doesn't matter
        receiver(MemoryMarshal.AsBytes(new ReadOnlySpan<Guid>(ref this)));
    }
}

public readonly struct Double : IHashable
{
    void IHashable.WriteTo(Action<ReadOnlySpan<byte>> receiver)
    {
        // adapted from double.GetHashCode()
        ulong bits = BitConverter.DoubletoUInt64Bits(m_value);
        if (IsNaNOrZero(m_value)) { bits &= PositiveInfinityBits; }
        Span<byte> buffer = stackalloc byte[sizeof(ulong));
        BinaryPrimitives.WriteUInt64(bits, buffer); // endianness doesn't matter
        receiver(buffer);
    }
}
```

This becomes a bit complicated once multi-dimensional values like tuples are involved, as `Tuple<T1, T2>` would only be able to reliably implement `IHashable` if _both_ `T1` _and_ `T2` implement it. Otherwise the tuple's implementation of `IHashable.WriteTo` would need to throw, which is user-hostile because the error is unlikely to be seen until test; or the implementation would need to fall back to `object.GetHashCode()` for the provided inputs, which could reintroduce the problem of poor intermediate hash codes described earlier in this document.

An additional complication with multi-dimensional types like `Tuple<...>` is that it may be necessary to delimit the constituent components. For example, given `Tuple<string, string>`, the implementation would not want to allow trivial collisions via `("abcd", "ef")` and `("ab", "cdef")`. To prevent this, either `Tuple<...>` would need to write an unambiguous delimiter between the two values, or `string` would need to length-prefix its binary representation, or both.

See also the discussion in the next section regarding the consequences of using a static per-app instance seed. Changing the `HashCode` algorithm implementation would not address this issue unless callers pass a dedicated per-context seed. This would necessitate changing most of the static APIs to instance methods. Such a change would certainly have a domino effect, potentially even requiring significant refactoring of callers' code.

### Modifying primitive types' hash codes

If collision resistance is desired, another option would be to modify methods like `int.GetHashCode()`, `Guid.GetHashCode()`, and `long.GetHashCode()` so that they utilize a high quality non-cryptographic hash function directly. One benefit is that it keeps most of the consuming code simple, as consumers would be able to use existing framework APIs. Another benefit is that it truly drives home the concept that hash codes calculations are implementation details and that hash code values have no meaning outside the current application instance.

However, this is not without its drawbacks. This would impact the performance of existing applications, including those for which DoS through hash code collision is not a realistic concern. This performance impact could be substantial. History also suggests that this would be breaking to customers, as changing `string.GetHashCode()` was during .NET Framework 4.5's development. Affecting a wide range of types at once could cause the same compatibility issues to play out on a larger scale.

There is also the risk that this change could be defeated in practice. If the type's `GetHashCode` implementation is changed to depend on a random seed, that seed must remain fixed for the lifetime of the app. (This matches `string.GetHashCode()` in .NET 6+, where the 64-bit Marvin32 seed is chosen at app start and remains static for the life of the app.) The possibility exists that information about the static seed could be disclosed through side channels. Contrast this against `Dictionary<string, ...>` today, where a unique Marvin32 seed is chosen for each dictionary instantiation, limiting the adversary's ability to learn information about the seed.

Finally, this still does not address an issue facing multi-dimensional values. If a tuple consists of large values (e.g., tuples of strings, tuples of tuples), each individual value will first be hashed to a single 32-bit integer, then the final hash code value will be generated off of those intermediate values. This compression of the possible output space is potentially so significant that it could reduce some of the security claims of an otherwise high-quality hash function.

### Ideal hash code function selection

This requires revisiting the definition of a hash code function provided earlier and creating a more accurate definition.

For a given type $T$, define a transitive equality function $E : (T, T) \to \text{bool}$. Any deterministic function $f : T \to \text{int32}$ which satisfies the constraint $E(t, t') = \text{true} \implies f(t) = f(t')$ is a valid hash code function over $T$ following the equality function $E$. Let $F_E$ be the set of all such functions. If a different equality function $E'$ is provided, any given hash code function $f \in F_E$ is not automatically a member of $F_{E'}$ unless it also satisfies the constraint $E'(t, t') = \text{true} \implies f(t) = f(t')$.

In other words, the concept of equality could be contextual and might be divorced from the type's built-in equality operator (`==` or `!=`). There could be many alternate ways to define equality over the type $T$. An alternate string equality function might perform a case-insensitive comparison rather than a pure bitwise comparison. An alternate collection equality function might compare two collections for unordered equality (as sets) rather than ordered equality (as lists). If some equality function over a data type $T$ evaluates two inputs as being equal - and thus producing the same hash code - there is no requirement that some _other_ equality function over the same type $T$ must evaluate those same two inputs as being equal.

Let $\left| T_E \right|$ be the number of distinct values that type $T$ might take as seen by some equality comparer $E$. For example, given an equality comparer `E(short a, short b) => (a == b);`, $\left| \text{int16}\_E \right| = 2^{16}$. For the equality comparer `E'(short a, short b) => (Math.Abs(a) == Math.Abs(b));`, disallowing `short.MinValue`, $\left| \text{int16}\_{E'} \right| = 2^{15}$.

For any given $E$ and $T$, the total number of hash code functions which might exist is:

$$
\left| F_E \right| \coloneqq \left( 2^{32} \right) ^ {\left| T_E \right|}
$$

This value gets very large very quickly. For $T = \text{bool}$ and a simple equality function with distinct elements $\text{bool}\_E = \\{ \textit{true}, \textit{false} \\}$, then $\left| F_{E_\text{bool}} \right| = 2^{64}$. If $T = \text{bool?}$ and $\text{bool?}\_E = \\{ \textit{true}, \textit{false}, \textit{null} \\}$, then $\left| F_{E_\text{bool?}} \right| = 2^{96}$. If $T = \text{uint32}$ and $\text{uint32}\_E = \\{ 0, 1, 2, \ldots, \texttt{uint.MaxValue} \\}$, then $\left| F_{E_\text{uint32}} \right| = 2^{32 \cdot 2^{32}}$.

What makes this model ideal is that if a hash code function $f$ is chosen randomly from this set, it trivially meets all the desirable qualities mentioned in the _Background definitions and assumptions_ section. Because the internal structure of $T$ is not used in the calculation, it's impossible for the generated hash code to inadvertently reflect any patterns in the input values. In fact, given $t_0, t_1, t_2, \ldots, t_n \in T_E$, even if the caller knew the corresponding outputs $f(t_0), f(t_1), f(t_2), \ldots, f(t_{n-1})$, they wouldn't be able to predict the output $f(t_n)$ better than a purely random guess. This implies perfect uniform distribution and perfect avalanching.

> **Note**
>
> In cryptographic parlance, such a function $f$ is a _random oracle_ $\mathcal{O}$. The goal here is not to build a cryptographically secure hash function, but the overall concepts run parallel.

If the function $f$ is chosen randomly, one might ponder the possibility of randomly selecting the degenerate function $f(t) \coloneqq C$ for some constant integer $C$, where all inputs produce the same output. After all, any family $F_E$ trivially contains $2^{32}$ degenerate functions. But even for the tiniest Boolean data type considered above, if $\left| F_{E_\text{bool}} \right| = 2^{64}$, this still makes the odds of randomly choosing some degenerate function $f \in F_{E_\text{bool}}$ only $2^{-32}$. This is not worth worrying about.

### Per-instance randomization of hashers for n-tuples

Assume the existence of a type `RandomizingHashCode<T1, T2, T3, ...>` whose ctor takes equality functions $E_1, E_2, E_3, \ldots$. The ctor selects $f_1 \in_R F_{E_1}, f_2 \in_R F_{E_2}, f_3 \in_R F_{E_3}, \ldots$. Since hash functions $f$ are chosen randomly, $E_i = E_j$ does not imply $f_i = f_j$. That is, even if the ordinal positions $i$ and $j$ utilize the same data type and equality function, the chosen hash function for each ordinal will differ. The ctor also randomly selects a seed appropriate for the outermost mixing algorithm, using a reasonably collision-resistant mixing function. There is an instance method `int Combine(T1 t1, T2 t2, T3 t3, ...)` which invokes the chosen hash functions and runs the intermediate values through the mixer, producing a final 32-bit hash code.

This design allows the possibility of using keyed hash functions for the intermediate hash code calculation over each input. A keyed hash algorithm with key space $S$ can effectively be thought of as defining a family of functions $F'_E$ with $F'_E \subseteq F_E$ and $\left| F'_E \right| = \left| S \right|$. Initializing the algorithm with a seed $s \in_R S$ is the equivalent of choosing $f \in_R F'_E$. For a large enough key space and a quality hash function, this could be a practical way to achieve something close to the $f \in_R F_E$ ideal.

It's evident that for any two unique instantiations of `RandomingHashCode<...>`, they'll produce substantially different and unpredictable results when given the same input values, but any given instantiation will return a stable result for any given input. This makes the randomizing hasher appropriate for use in dictionaries with n-tuple keys, since the hash codes will be self-consistent within any given dictionary instance, and two different dictionary instances needn't share hash codes.

This solves many of the shortcomings in `HashCode` and has potential to make dictionary instances containing n-tuple keys resistant to hash flooding attacks when keys are populated by adversarial input. A full security analysis of such a proposal is outside the scope of this document.