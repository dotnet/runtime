# De-Abstraction and Conditional Escape Analysis

There are interesting, important, and optimizable patterns where objects of several different types collaborate.

For example, consider the abstract enumeration supported by `IEnumerable<T>`. Here an enumerable `o` of some type (say `O`) can produce an enumerator `e` of some type (say `E`) that then operates on `o`. The typical pattern is:
```C#
O o = ...
foreach (T t in o) { ... t }
```
Under the covers, this requires creation of (or access to) a ref class or boxed value class `e` (Footnote 1).

In the case where both `O` and `E` are concrete types known to the C# compiler, many optimizations are possible, and indeed, something like this generates quite efficient code:
```C#
int[] o = ...;
foreach (int i in o) { }
```
and for other collection types, there are various patterns (enumerator structs) that the C# compiler can rely on avoid the overhead of enumerator creation and interface calls. So for instance enumerating over a `List<int>` is also fairly efficient.

But when either type `O` or `E` is unknown, enumeration can involve a fair amount of overhead&mdash;an object allocation for `e` and two interface calls per produced element `t`.

We call this overhead the "abstraction penalty."

This penalty shows up readily in simple benchmarks. For example
```C#
public class ArrayDeAbstraction
{
    static readonly int[] s_ro_array = new int[512];

    [MethodImpl(MethodImplOptions.NoInlining)]
    IEnumerable<int> get_opaque_array() => s_ro_array;

    [Benchmark(Baseline = true)]
    public int foreach_static_readonly_array()
    {
        int sum = 0;
        foreach (int i in s_ro_array) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_readonly_array_via_interface()
    {
        IEnumerable<int> o = s_ro_array;
        int sum = 0;
        foreach (int i in o) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_opaque_array_via_interface()
    {
        IEnumerable<int> o = get_opaque_array();
        int sum = 0;
        foreach (int i in o) sum += i;
        return sum;
    }
}
```
| (.NET 9) Method                                                       | Mean       | Ratio | Allocated |
|------------------------------------------------------------- |-----------:|------:|----------:|
| foreach_static_readonly_array                                |   153.4 ns |  1.00 |         - |
| foreach_static_readonly_array_via_interface                  |   781.2 ns |  5.09 |      32 B |
| foreach_opaque_array_via_interface                           |   843.2 ns |  5.50 |      32 B |
| foreach_opaque_array_via_interface          (no PGO)         | 2,076.4 ns | 13.54 |      32 B |
| foreach_static_readonly_array_via_interface (no PGO)         | 2,304.5 ns | 15.03 |      32 B |

With PGO the JIT can now learn about and optimize for likely types of objects at runtime. That information helps the JIT to reduce the abstraction penalty substantially, but as you can see from the above, even with PGO, the penalty is still quite high, around 5x! (Footnote 2)

Just for reference, here is the inner loop from the `foreach_static_readonly_array` case. We will contrast this with the more abstract cases as we go along. In this loop the array index computation has been strength reduced (hence `rcx` is a pointer into the array incrementing by 4), the loop is down-counting, and the loop top has been aligned.
```asm
       align    [8 bytes for IG03]

G_M1640_IG03:  ;; offset=0x0020
        add      eax, dword ptr [rcx]
        add      rcx, 4
        dec      edx
        jne      SHORT G_M1640_IG03
```

This note explores approaches to fully removing the abstraction penalty in the more abstract cases above. Roughly speaking, we want to be able to do at JIT time what the C# compiler is able to do at compile time, and so have all three benchmarks deliver similar performance.

## Analysis of .NET 9 Codegen

In what follows we're going to use .NET 9 with PGO as our baseline, and see what challenges the JIT faces in removing the remaining abstraction penalty.
Some of these we have already overcome, and some remain.

To do this, let's take a close look at the code produced by .NET 9 for the `opaque` case (which is the most complex).

The C# compiler translates the `foreach` into simpler constructs, and the resulting code the JIT sees is more like the following:
```C#
    IEnumerable<int> o = get_opaque_array();
    int sum = 0;
    IEnumerator<int> e = o.GetEnumerator();
    try
    {
        while (e.MoveNext())
        {
            int i = e.GetCurrent();
            sum += i;
        }
    }
    finally
    {
        e.Dispose();
    }
    return sum;
```
There are four interface call sites, a loop, and a try-finally.

In the "tier0-instr" phase of TieredPGO, the JIT adds class probes at each interface call site, and as this version of code runs, these probes observe  that `O` (the concrete type of `o`) is very likely `int[]` and `E` is very likely `SZGenericArrayEnumerator<int>` (see Footnote 3).

Based on this PGO data, the JIT first translates the above into something like the following, where each interface call is now expanded by Guarded Devirtualization (aka GDV):
```C#
    IEnumerable<int> o = get_opaque_array();
    int sum = 0;

    // -------------- Begin GDV "diamond"
    IEnumerator<int> tt = null;
    if (o.GetType().Equals(typeof(int[])))
    {
        var a = (int[]) o;
        tt = a.GetEnumerator();
    }
    else
    {
        tt = o.GetEnumerator();
    }
    var e = tt;
    // -------------- End GDV "diamond"

    try
    {
        // -------------- Begin GDV "diamond"
        bool b0 = false;
        if (e.GetType().Equals(typeof(SZGenericArrayEnumerator<int>)))
        {
            var ea = (SZGenericArrayEnumerator<int>) e;
            b0 = ea.MoveNext();
        }
        else
        {
            b0 = e.MoveNext();
        }
        // -------------- End GDV "diamond"

        if (b0)
        {
            do
            {
                int i = e.GetCurrent(); // also a "diamond"
                sum += i;
            }
            while (e.MoveNext()); // also a "diamond"
        }
    }
    finally
    {
        e.Dispose(); // also a "diamond"
    }
    return sum;
```
Here we have shown the GDV expansions for the first two call sites, but left
the other three unexpanded. so as not to clutter things too much. (Note the call to `MoveNext` has been duplicated here by the JIT, so there are now 5 interface call sites; this duplication comes from an optimization known as loop inversion).

This may not look like an improvement, but the key point is that in the `if` part of each GDV diamond, the interface calls are now made on objects whose types are known exactly, so these calls can be devirtualized and inlined. And thanks to PGO, we know that the `if` true cases for the diamonds are the important cases for performance.

And that's almost what happens.

It turns out that in .NET, arrays implement interfaces differently than most types do (see Footnote 4). So in .NET 9 the normal interface devirtualization logic does not work on array interface calls. And the JIT has a heuristic that says that a GDV expansion is not beneficial unless the call can be both devirtualized and inlined. So the above is a bit of a lie&mdash;the .NET 9 JIT does not produce the first diamond.

And that un-optimized call usually allocates an enumerator object, which is something we'd like to avoid. So that leads us to the first challenge in removing the abstraction overhead.

#### Challenge 1: Array Interface Call Devirtualization

We are going to gloss over the details in this writeup. For more information, see [dotnet/runtime#108153](https://github.com/dotnet/runtime/pull/108153).

Devirtualization of array interface calls is supported in .NET 10.

There's something else to note in the example above (now we're assuming the initial GDV diamond is in fact created by the JIT). We see that the behavior of all these GDV tests is highly correlated. If the upper test discovers that `o` is `int[]`, the lower tests will all discover that `e` is `SZGenericArrayEnumerator<int>`. And even if we didn't know this was the case, we could trace the connection because the result of the upper GDV diamond (`e`) is the thing that is tested in the lower GDV diamonds. So this pattern (where one GDV diamond feeds others) is going to be something we key off in later on.

### The "Simple" Case

Before looking more deeply at the `opaque` case, let's dig into the `...readonly_array_via_interface` version. Here the program starts with an `int[]` but casts it to `IEnumerable<int>` for the `foreach`. This seems like a small thing&mdash;the type of the collection has been only temporarily obscured&mdash;but as you can see from the overhead numbers, it's not.

The big difference between these versions is in `readonly_array_via_interface` the JIT is able to figure out an exact type for `o` without GDV; it simply has to propagate the type forward. That is, it more or less changes
```C#
    IEnumerable<int> o = s_ro_array;
```
into
```C#
    int[] o = s_ro_array;
```
And that, coupled with the devirtualization and inlining of `o.GetEnumerator`, lets the JIT resolve the lower GDVs, as the inline body reveals the exact type for `E` as well. (Footnote 5).

You would think the resolution of the lower GDVs, and the inlines of the relatively simple `MoveNext`, `GetCurrent`, and (the do-nothing) `Dispose` methods would enable the JIT to know everything about how the enumerator object is used, and in particular, see that that object cannot "escape" (that is, if newly allocated, it need not live past the end of the method).

But here we run into a phase ordering problem. It takes a little while for the JIT to propagate these types around, and escape analysis runs fairly early, and so it turns out without some help the JIT learns too late what type of enumerator it has, so the lower GDVs are still there when escape analysis runs, and the JIT has to honor the fact that at that point it still thinks those GDVs can fail, and so invoke an unknown `MoveNext` method, which might cause the enumerator to escape.

All that is a long winded way of saying that while the JIT eventually is able to do a lot of optimization, it doesn't happen in the right order, and some decisions get locked in early.

#### Challenge 2: Slow Type Propagation

While inelegant, there is some prior art for giving the JIT a boost in such cases, at least for some key types. We can annotate the type as `[Intrinsic]` and have the JIT consult an oracle when it sees this annotation, asking the runtime if it happens to know what type of enumerator will be produced.

[dotnet/runtime#108153](https://github.com/dotnet/runtime/pull/108153) added this annotation and the necessary oracular support in the JIT and runtime.

#### Challenge 3: Inlining of GetEnumerator for Arrays

Now it turns out that for arrays, interface methods are implemented by a companion `SZArrayHelper` class, and the body of the `GetEnumerator` is a bit more complicated than you might guess:
```C#
    [Intrinsic]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IEnumerator<T> GetEnumerator<T>()
    {
        // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
        // ! or you may introduce a security hole!
        T[] @this = Unsafe.As<T[]>(this);
        int length = @this.Length;
        return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(@this, length);
    }
```
To ensure this method gets inlined we also added `AggressiveInlining`; this was also done in [dotnet/runtime#108153](https://github.com/dotnet/runtime/pull/108153).

Here `Empty` is a static field in a generic class. The examples we have been discussing all have `T == int`, but in general `T` could be a ref class (say `string`) in which case the `SZArrayHelper` class is a shared class, and the JIT generally was reluctant to inline methods that access statics of shared classes. We relaxed that restriction in [dotnet/runtime#109256](https://github.com/dotnet/runtime/pull/109256).

Now with `GetEnumerator` inlined the JIT can see almost everything about how the enumerator is produced when the collection type is an array. This plus turns out to be enough to unblock stack allocation of the enumerator in the `...readonly_array_via_interface` example. But it's still not sufficient to allow "promotion" of the object fields (aka scalar replacement)&mdash;for full optimization the JIT must be able to treat the fields of the enumerator as if they were local variables.

#### Challenge 4: Empty Array Enumeration

The generic static `Empty` is there because there is a special case optimization in the BCL for empty arrays. Since enumeration of an empty array does not require mutable state, an immutable static enumerator object can be used instead of allocating a new object. So although the JIT knows the exact type of `e` it does not know (in general) which object instance is being used. This ambiguity blocks promotion.

To resolve this we again dip back into our special knowledge of `GetEnumerator`. We want the JIT to "undo" this empty array optimization if it is able to stack allocate the enumerator, because the whole point of the optimization was to avoid heap allocation, and the JIT has avoided heap allocation by means of escape analysis (in other words, what used to be expensive is now cheap). But we don't want the JIT to undo this unless escape analysis succeeds. So if the JIT is able to successfully stack allocate an `SZGenericArrayEnumerator` it then checks if that allocation was from a call to `SZArrayHelper.GetEnumerator` and if so, it simply patches the code to always go down the "allocation" path. (Footnote 6)

[dotnet/runtime#109237](https://github.com/dotnet/runtime/pull/109237) contains the logic needed for the JIT to recognize and prune away the empty array path, if the enumerator is stack allocated.

#### Challenge 5: Address Exposure

We're getting closer to our goal, in the case of `...readonly_array_via_interface`. However promotion is still not happening. Before the JIT will promote a local object's fields it must ensure it knows about all the accesses to the fields. But in our case the propagation of the "stack address" from the allocation site in the inlined `GetEnumerator` to the uses in the inlined `MoveNext` and other methods is not happening in a timely fashion, because those uses are in a loop.

And, furthermore, there is a use in the `finally` clause. Even though the `Dispose` method doesn't do anything, there is a residual null check and so the enumerator is referenced there.

So we made some enhancements to our early address propagation to resolve both these issues; see [dotnet/runtime#109182](https://github.com/dotnet/runtime/pull/109182) and [dotnet/runtime#109190](https://github.com/dotnet/runtime/pull/109190).

#### Challenge 6: Cloning

With all that in place, the abstraction for the `...readonly_array_via_interface` is largely gone: the enumerator object is now replaced by locals, one per field. And so the JIT is poised to apply its full suite of loop optimization techniques, in particular IV analysis, bounds check elimination, and so on.

However, there is one last hurdle to overcome: the loop "header" is also the start of a try region. The JIT often relies on cloning to enable bounds check elimination: it analyzes the loop body, and if it sees that the array accesses in the loop can be proven to be in-bounds by a check made before the loop starts, it duplicates the loop, adds the checks, and so ends up with a "fast path" loop with no (or fewer) bounds checks, and a slow path loop with full checking. (Footnote 7).

So we did work to enable cloning when the loop header is also a try entry [dotnet/runtime#108604](https://github.com/dotnet/runtime/pull/108604). But when working on this we realized it was going to lead to a lot more cloning, and so we first had to put some more realistic limits on how much code the JIT would be willing to duplicate to try and optimize [dotnet/runtime#108771](https://github.com/dotnet/runtime/pull/108771).

### Simple case (as of 01-Dec-24)

With all that the "simple" case `...readonly_array_via_interface` is now a good deal faster than it was in .NET 9, but still not perfect:

| Method                                                       | Mean       | Ratio | Allocated |
|------------------------------------------------------------- |-----------:|------:|----------:|
| foreach_static_readonly_array                                |   153.4 ns |  1.00 |         - |
| foreach_static_readonly_array_via_interface (.NET 10)        |  295.7 ns  | 1.93 |          - |
| foreach_static_readonly_array_via_interface (.NET 9)         |   781.2 ns |  5.09 |      32 B |
| foreach_static_readonly_array_via_interface (.NET 9, no PGO) | 2,304.5 ns | 15.03 |      32 B |

It's worth calling out that the .NET 10 version does not heap allocate. The enumerator object is now on stack (and promoted).

The inner loop is middle-entry, up-counting and not strength reduced or IV-widened. There are still some artifacts from the way the inlined enumeration happens that we need to iron out to unblock these last few optimizations. But we're very close.

```asm
       align    [6 bytes for IG03]

G_M36467_IG03:  ;; offset=0x0020
       mov      edx, edx
       add      eax, dword ptr [rcx+4*rdx+0x10]

G_M36467_IG04:  ;; offset=0x0026
       inc      edx
       cmp      edx, 512
       jb       SHORT G_M36467_IG03
```

#### Challenge 7: Fully Enabling Loop Opts

To be determined!

### The "complex" case

Now let's turn our attention back to the more complex case, where the only way to learn the collection type is via PGO. Did it see any benefit from all the work above? It did, a little:

| Method                                                       | Mean       | Ratio | Allocated |
|------------------------------------------------------------- |-----------:|------:|----------:|
| foreach_static_readonly_array                                |   153.4 ns |  1.00 |         - |
| foreach_opaque_array_via_interface (.NET 10)                 | 680.4 ns | 4.44 |      32 B |
| foreach_opaque_array_via_interface (.NET 9)                  |   843.2 ns |  5.50 |      32 B |
| foreach_opaque_array_via_interface (.NET 9 ,no PGO)         | 2,076.4 ns | 13.54 |      32 B |

But it still allocates, so the enumerator object is still a heap object.

We now need to deal with having the upper GDV diamond, so in the code below, we cannot resolve the GDV tests, since we are not 100% sure which type of object we have for `e`. PGO can tell us the likely type, and GDV can guess for that type, but we don't have any guarantees.

How are we going to arrange to have these guarantees? If you've been reading the footnotes, or perhaps have a good intuitive feel for this sort of thing, the answer is that we are going to clone and create a specialized version of this code for the case where the upper GDV test succeeds; once we do that this case more or less reduces to the case above. (Footnote 8)

Easier said than done. Cloning takes JIT time and creates a lot more code, so we really only want the JIT to do this transformation if it will in fact enable stack allocation (and promotion), but the JIT seemingly has to clone and try to optimize in order to find out if it is worth cloning (and then perhaps undo it all if it is not worthwhile). This kind of "do-check-undo" approach to optimization might be feasible in compilers that have generous compile time budgets, but the JIT does not. We need to know up front whether the whole thing is going to pay off.

What this means in practice is that when we run escape analysis, we need it to analyze the code as if we'd cloned and split off the good path from the upper GDV diamond, without actually having done so. And, let's not forget the empty array optimization is still in there possibly confusing things too.

### Conditional Escape Analysis

Thanks to PGO and GDV, if we knew all the lower GDV tests succeeded, then we would know the enumerator object could not escape. So the first key insight is that the problematic cases are the ones where the lower GDV tests fail. The second key insight is that the failing side of each GDV diamond is particularly simple: an interface call where the enumerator is the `this`. And the third key insight is that all the lower GDV tests are equivalent and only depend on the value produced at the bottom of the upper GDV diamond.

So when doing escape analysis, if we can determine that all escaping accesses are interface calls of that form, and all are made under a failing GDV test checking the type of the object produced by the upper GDV diamond, we've conditionally proven that those references won't cause escape, if we can arrange for the GDV test to always succeed, which we can do by cloning the entire region from the enumerator allocation site in the upper diamond down through all the uses in the lower diamonds.

#### Escape Analysis

To put the changes needed in escape analysis into context, we first need to understand a little about how it works. The JIT arranges things so that each explicit object allocation site in a method stores its results to a unique local variable (say `Ai`).

The JIT then analyzes all uses of all variables.
* if a variable is "address exposed" then it is marked as escaping (this means more or less that the JIT cannot reliably track where all reference to that variable might occur)
* if a variable's value is copied to another variable (say `X = Y`), and edge is added to the connection graph `X -> Y`, meaning if `X` escapes then so will `Y`.
* if a variable is otherwise used, the JIT does local analysis to decide
if that use is escaping.

The JIT then marks any variable connected to an escaping variable as escaping. This process continues until it reaches transitive closure.

If a given `Ai` escapes, then the associated allocation escapes and must be heap allocated. Otherwise the allocation may be eligible for stack allocation (other restrictions apply, which further winnows down this set).

The eventual set of surviving allocations are then transformed to be new struct-valued locals, and references to the associated `Ai` in the JIT IR are rewritten to be the address of this struct.

 Let's see how this plays out in a simplified example. Suppose we have
 ```C#
    IEnumerable<int> e = o.GetEnumerator();
    o.MoveNext();
 ```
 After GDV expansion and inlining, this will look something like:
 ```C#
    IEnumerable<int> t = null;
    if (o.Type == O)
    {
        var ac = new E();
        t = ac;
    }
    else
    {
        t = o.GetEnumerator();
    }
    var e = t;

    if (e.Type == E)
    {
        // inlined e.MoveNext
        var ea = (E) e;
        // enumerator ref may be copied to other locals
        var tt = ea;
        tt.field--;
    }
    else
    {
        e.MoveNext();
    }
 ```
First let's just do the normal escape analysis. The analysis is flow-insensitive so we can look at the operations in the example in any order. We'll go top to bottom:
* `t = null`: no impact
* `o.Type == O`: no impact
* `ac = new E`: no impact
* `t = ac`:  add edge `t -> ac`
* `t = o.GetEnumerator()`: `o` escapes
* `e -> t`: add edge `e -> t`
* `e.Type == T`: no impact
* `ea = (E) e`: add edge `ea -> e`
* `tt = ea` : add edge `tt -> ea`
* `tt.field--`: no impact
* `e.MoveNext()`: `e` escapes

Then closure computation runs:
* `o` escapes (no impact)
* since `e` escapes and `e -> t`, `t` escapes
* since `t` escapes and `t -> ac`, `ac` escapes

Since `ac` escapes, the `new E()` must be a heap allocation.

#### Pseudo-Variables

If you look closely at the simplified example, `new E()` cannot actually escape, because if that path of the upper GDV is taken, then the `e.MoveNext()` on the failing side of the lower GDV cannot happen. Establishing that requires  conditional escape analysis.

To model conditional escape, the JIT first identifies allocation sites like
`ac = new E()` that happen on the successful side of GDV diamonds, where a reference to the allocated object can become the GDV result, and finds the local that holds the result of that GDV (here it is `e`). For each such `E, e` pair the JIT creates a new pseudo-variable `P` that will connect up references to `e` or copies thereof that happen under a failed GDV guard that tests the type of `e` against `E`.

So it creates a mapping `(e, E) -> P`.

Escape analysis then runs as normal, save that any otherwise escaping variable reference under a failed `(e.Type == E)` test is modelled as an assignment to `P`. Thus we have (ignoring the statements that have no impact)
* `t = ac`:  add edge `t -> ac`
* `t = o.GetEnumerator()`: `o` escapes
* `e -> t`: add edge `e -> t`
* `ea = (E) e`: add edge `ea -> e`
* `tt = ea` : add edge `tt -> ea`
* `e.MoveNext()`: `e` escapes, but model this as `P -> e`.

Then closure computation runs:
* `o` escapes (no impact)

So we have proven that if can we arrange things so that any GDV test of the form `(e.Type == E)` succeeds, then the `new E()` does not escape and can be stack allocated. (Footnote 9)

Note it is also possible (in some other example) that there is an escaping reference that is not under a suitable failing GDV. In such a case the object escapes and is heap allocated. So it's only when all potentially escaping references are under a suitable failing GDV that we move on to the next stage to attempt stack allocation.

#### Challenge 8: Flow-Insensitivity

You will notice in the algorithm sketch above we are implicitly relying on certain ordering properties in the code, e.g. the allocation at `ac = new E()` reaches to `e = t` via a chain of temp copies, and that that assignment to `e` reaches to at each `(e.Type == E)` test. But since the analysis is flow-insensitive, we have not yet proven any of this.

To establish these facts the JIT also tracks all appearances of all variables that might refer to the object, and then, after the initial round of escape analysis, relies on dominance information to demonstrate that those appearances all must refer to the allocated object.

With those flow relationships established, we have finally shown that *if* we can do suitable cloning, the object will not escape.

#### Challenge 9: Cloning

If you refer back to the expanded `foreach` above, you will notice that the lower GDV tests on `e` are located within a loop, and that loop is within a try/finally region. To fully split off this code the JIT must also clone along the path from the allocation site to the assignment to `e`, and any code that can refer to `e` thereafter.

The JIT has had the ability to clone loops for a long time, but not this more general kind of region-based cloning, and especially not when the cloning involves cloning a try region.

Cloning a try region turns out to be fairly involved; in addition to the `try` region itself, cloning must also encompass any handler (here a `finally`), any code outside the try that facilitates executing the handlers (aka the `callfinally` region), any promises made to create suitable throw helper blocks within the try or handler (aka `ACDs`), any nested `try` regions (within the `try` or the `finally`), and the associated EH table entries.

Support for cloning a try was added in [runtime/dotnet#110020](https://github.com/dotnet/runtime/pull/110020).

The plan is to use this as part of a larger region-based cloning effort.

There are other considerations to handle here too. A method may have multiple instances of conditional escape for different objects. If the cloning regions for these overlap, then the problem is more complicated. Our plan for now is only allow cloning in cases where there is no overlap. (Footnote 10)

If for some reason cloning can't (or won't) be done, then the JIT marks the associated pseudo variable as escaping, and restarts the escape closure analysis.

If we do clone, we make the cloned version the "fast path" and the original the "slow path".

When cloning we must also modify the cloned GDV tests to always succeed. We would figure this out on our own in time, but leaving the code for the failing GDV tests intact will cause later phases to assume that code can be reached, and address expose `e`.

We also modify the GDV tests in the original code to always fail. This is not strictly necessary, but we do not want later phases to re-clone any loops here in the hope that those GDV tests might pass. (Footnote 11)

#### Challenge 10: Finally Cloning

Once the JIT has cloned to prevent escape, and swapped out the set of enumerating-reference vars to enable promotion, what's left? One thing we need to consider is that the cloned (fast) path and original (slow) path likely involve try/finallys, and because they may share other variable references (for non-enumerator things) we may need to somewhat aggressively optimize the slow path in order to ensure we get the best code in the fast path. In particular the JIT may need to also do "finally cloning" along the slow path even if believes this code is very unlikely to execute.

It looks like the existing heuristic to not clone any finally that seems to be rarely executed almost never fires, so we can perhaps simply relax that; alternatively, we can mark the finally in some way so that a different heuristic can be applied to the slow/cold finally.

This was addressed by [dotnet/runtime#110483](https://github.com/dotnet/runtime/pull/110483).

#### Challenge 11: Profit?

Once we've established that we can indeed clone, one last question remains: is the benefit from all this code duplication worthwhile? Our initial experience indicates that simply stack allocating an object may not provide enough benefit; we also need to ensure the object can be promoted. (Footnote 12)

To help ensure stack allocation we extend the cloning to also re-write all of the local vars that might reference the `new E()` so that downstream phases (also somewhat flow-insensitive) do not see references to the same locals in the cloned code and in the original code.

#### Challenge 12: Multi-Guess GDV

The JIT is also able to expand a GDV into multiple guesses. This is not (yet) the default, but it would be ideal if it was compatible with conditional escape analysis. Under multi-guess GDV we may see a conditional escape protected by two or more failing guards, and more than one conditional enumerator allocation under a successful (and perhaps some failing) upper GDV guards.

Generalizing what we have above to handle this case seems fairly straightforward, provided we are willing to create one clone region per distinct GDV guard type.

If (say for two-guess GDV) the likelihoods of the two guesses are similar, we may also need to handle the case where the order of the guards can change in the lower GDV region, because our class profiling is somewhat stochastic, and each opportunity is profiled separately, and the JIT will always test for the most likely outcome at each point. Say one lower GDV profile shows A-55%, B-45%, other-0%, and a second lower GDV profile has B-55%, A-45%, other-0%. The first GDV will test for A outermost; the second, B.

Another possibility is that the upper GDV diamond contains multiple allocations of the same enumerator type (eg `A` is `int[]` and `B` is some user written class that wraps an `int[]` and delegates enumeration to the array enumerator). This is similar in spirit to the empty array case -- the JIT can show (via cloning) neither enumerator escapes, but not know which enumerator object is in use, so is unable to promote the field accesses.

#### Challenge 13: Exact Multi-Guess GDV

Under NAOT there is one other similar possibility: thanks to whole-program analysis, ILC can enumerate all the classes that can implement, say, `IEnumerable<int>`, and if there are just a small number of possible implementing classes, tell the JIT to guess for each in turn. Handling this would require some extra work as well; the JIT could clone for each known class and then the original code would become unreachable.

## `List<T>`

`List` is another very common concrete collection type. Let's see how deabstraction fares when the collection is a list.

Similar to the array case we'll look at 3 examples: one where the language compiler knows the concrete type, one where the JIT can discover this via normal optimization, and one where the JIT can only learn about the type via GDV:

```C#
    List<int> m_list = Enumerable.Repeat(0, 512).ToList();

    [MethodImpl(MethodImplOptions.NoInlining)]
    IEnumerable<int> get_opaque_list() => m_list;

    [Benchmark]
    public int foreach_member_list()
    {
        List<int> e = m_list;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_member_list_via_interface()
    {
        IEnumerable<int> e = m_list;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_opaque_list_via_interface()
    {
        IEnumerable<int> e = get_opaque_list();
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }
```
Once again we'll use .NET 9 as a baseline:

| (.NET 9) Method                   |   Mean     | Ratio   | Allocated |
|---------------------------------- | ---------: | ------: | --------: |
| foreach_member_list               |   247.1 ns |   1.00  |         - |
| foreach_member_list_via_interface |   832.7 ns |   3.37  |      40 B |
| foreach_opaque_list_via_interface |   841.7 ns |   3.41  |      40 B |
| foreach_member_list_via_interface (no PGO) | 3,269.2 ns |    13.23      |      40 B |
| foreach_opaque_list_via_interface (no PGO) | 3,462.4 ns |   14.02      |      40 B |

In relative terms the abstraction penalty is better than for arrays, but since `List<T>` is a wrapper around an array, we might want to consider the array case as the true baseline (~150ns) in which case the penalty is more like the 5.5x we see in .NET 9 for arrays.

And clearly PGO plays a big role in getting the penalty that low; without it the penalty is ~13x over the base list case, and 21x over the base array case.

Why is list enumeration relatively more costly? Lets's look at the implementation:
```C#
    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        Count == 0 ? SZGenericArrayEnumerator<T>.Empty :
        GetEnumerator();

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly List<T> _list;
        private int _index;
        private readonly int _version;
        private T? _current;

        internal Enumerator(List<T> list)
        {
            _list = list;
            _index = 0;
            _version = list._version;
            _current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            List<T> localList = _list;

            if (_version == localList._version && ((uint)_index < (uint)localList._size))
            {
                _current = localList._items[_index];
                _index++;
                return true;
            }
            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            if (_version != _list._version)
            {
                ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
            }

            _index = _list._size + 1;
            _current = default;
            return false;
        }

        public T Current => _current!;

    }
```
A couple of things stand out here:
* There is some extra state (the `_version` field and related checks) to try and guard against modification of the list while enumeration is active. We didn't see that for arrays because the array length can't be modified after the array is created, but the number of list elements can vary.
* The enumerator does not cache the underlying array instance, so `MoveNext()` has to first fetch the list from the enumerator, then fetch the array from the list, then fetch the element from the array. This is a three-level dependent chain of memory accesses. (Footnote 13)
* This is a `struct` enumerator, not a `class` enumerator. This is a common pattern to avoid allocation in cases where enumeration is not quite as simple as arrays and the language compiler knows the collection type. And indeed in the base case there is no heap allocation. But not so in the other cases.
* The `GetEnumerator` implementation also has a variant of the empty-collection optimization; interestingly enough it delegates the empty case to the static empty array enumerator, so `GetEnumerator` does not return an object of definite type. (Footnote 14)
* There is effectively a double bounds-check, as first the enumerator checks against the size of the list, and then the array checks against the length of the array.

As we mentioned above, the `struct` enumerator only avoids allocation when the collection type is known to the language compiler; if we disguise the type as in the second benchmark, or make it unknowable as in the third, the struct instance produced by `GetEnumerator` is boxed so that it becomes an object that can participate in interface dispatch (for the subsequent `MoveNext`, etc). Since the concrete type is unknowable and the list length not knowable either, the JIT can't resolve these dispatches via dataflow analysis and must rely on GDV to devirtualize the calls. Thus we see the second and third cases take the same amount of time.

Happily most of the work we outlined above for the ref class array enumerator works perfectly well for a boxed struct list enumerator. The only real difference is that there is an extra copy from the struct returned by `GetEnumerator()` to the box payload, but once the box is stack allocated, both the struct fields and the box payload fields are promotable, and the JIT can see through the copy. We can also do the same empty collection trick we do for arrays and force all cases to create a new enumerator (which is now cheap). Currently we don't do this, so enumerating an empty list is not quite as well optimized as it could be.

In cases where the loop body is very simple (like the above) The JIT is also able to clean up some of the cost of the `_version` checks. While the JIT can't prove that the list won't be modified by some other thread, it is allowed to make optimizations that can change the behavior of "racy" code. So unless there is some kind of synchronization point (method call, etc) in the loop body, the JIT is free to assume the list hasn't changed, so the `_version` field is a loop invariant. It can also remove the list-based bounds check, but currently the array check still remains; this is the case in all versions.

(as of 07-Dec-24)

| Method                                     |   Mean     | Ratio   | Allocated |
|------------------------------------------- | ---------: | ------: | --------: |
| foreach_member_list  (.NET 9)              |   247.1 ns |   1.00  |         - |
| foreach_opaque_list_via_interface (.NET 10)|   247.8 ns |   1.00  |         - |
| foreach_member_list_via_interface (.NET 10)|   249.5 ns |   1.01  |         - |
| foreach_member_list_via_interface (.NET 9) |   832.7 ns |   3.37  |      40 B |
| foreach_opaque_list_via_interface (.NET 9) |   841.7 ns |   3.41  |      40 B |

The upshot is the inner loop currently looks like this (where we are summing a `List<int>`):
```asm
G_M17657_IG03:  ;; offset=0x0040
       mov      r8, gword ptr [rcx+0x08]
       cmp      edx, dword ptr [r8+0x08]
       jae      G_M17657_IG12
       mov      r8d, dword ptr [r8+4*rdx+0x10]
       inc      edx
       add      ebx, r8d
						;; size=24 bbWeight=513.74 PerfScore 4366.81
G_M17657_IG04:  ;; offset=0x0058
       cmp      edx, eax
       jb       SHORT G_M17657_IG03
```

It might be that with proper inversion, further cloning, etc, we could eliminate the bounds check, hoist out the fetch of the array, and strength reduce here.

## Other Collection Types

I have also looked into `HashSet` and `Dictionary` KVP enumeration. Both of these look pretty good, though (as with list) there is some inefficiency in the residual loop optimizations in both the baseline version and the conditional-escape optimized variants that we might be able to address.

## Other Limitations

As we've seen from the above, *if* we can arrange to stack allocate the enumerator, we can greatly reduce the abstraction penalty from enumeration. But there are various limitations on stack allocation itself that will sometimes block this from happening. Let's look at a few of them.

### Allocations in loops

The optimization that turns a heap allocation into a stack allocation does so by creating a fixed set of slots on the stack frame for the object. This doesn't work properly if the allocation site can be visited more than once during a call to a method, since it's possible the storage allocated from an early visit is still in use (aka "live") when a later visit happens. Note if there are multiple visits to a bit of code in a method it generally means the method contains a loop (Footnote 15).

There are a couple of different ways to try and handle these cases. One is to replace the fixed stack allocation with a dynamic one. There is already a mechanism (`stackalloc` in C#) for this, and we could consider using it, but this currently won't interact well with promotion, so the subsequent optimizations won't happen. And there is some extra up-front cost to each method call. And the stack itself is a limited resource, so we don't want to be allocating a potentially large amount of space. So there are tradeoffs here that are not yet well understood.

The second is to prove that the stack allocated instance is no longer live on any subsequent visit, so that the fixed bit of storage can be recycled. This is perhaps doable, and should be the common case with enumerators, so we will likely be considering it as an enhancement.

### Multiple Enumerations

What if there are multiple enumerations happening in a method? Currently we require that each instance be separate from the others; this is checked by ensuring that the cloning regions are disjoint. So for example if there is a nested `foreach` only one of the two will end up being eligible. It turns out with the current implementation that we'll chose the outer one, since we encounter opportunities via RPO and so outer to inner. Likely it would be better to choose the inner one. (Footnote 16)

It also turns out that if there are sequential `foreach` of the same local that the C# compiler will re-use the same enumerator var. Luckily for the JIT these two different sets of defs and uses are distinct and our current analysis handles it without any special casing.

I haven't yet checked what happens if there are nested `foreach` with the same collection, presumably this can't share the enumerator var and ends up just like the nested case.

Another area to explore are `Linq` methods that take two (or more) `IEnumerable<T>` as arguments, for instance `Zip` or `Union` or `Intersect`. Some of these (but not all) will look like nested cases.

### GDV Chaining

In some cases two enumerator methods are called back to back without any control flow. An optimization known as GDV chaining can alter what would be two adjacent diamonds into a slighly different flow shape, and when this happens the "slow path" call in the second GDV will no longer be dominated by its own check, appear to be reachable by both paths from the upper GDV check. Seems like we ought to either disable GDV chaining for these types of tests, or else enhance the flow logic to understand the shape produced by GDV chaining.

## Linq

Ideally the work above would show improvements in `Linq` methods. However the situation with `Linq` is more complex (and one can safely infer from this that we are not yet seeing many improvements). Let's dig in a little.

First, let's contrast a "naive" `Linq` implementation for `Where` against the actual implementation, both for .NET 9 and with the changes above, on an array:
```C#
    public static IEnumerable<T> NaiveWhere<T>(IEnumerable<T> e, Func<T, bool> p)
    {
        foreach (var i in e)
        {
            if (p(i)) yield return i;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int help_sum_positive_where_naive(IEnumerable<int> e)
    {
        int sum = 0;
        foreach (int i in NaiveWhere(e, x => x > 0))
        {
            sum += i;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int help_sum_positive_where(IEnumerable<int> e)
    {
        int sum = 0;
        foreach (int i in e.Where(x => x > 0))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int sum_positive_where_naive()
    {
        return help_sum_positive_where_naive(s_array);
    }
    [Benchmark]
    public int sum_positive_where()
    {
        return help_sum_positive_where(s_array);
    }
```

| Method                   | Toolchain | Mean       | Ratio | Allocated |
|------------------------- |---------- |-----------:|------:|----------:|
| sum_positive_where_naive | net10.0   | 1,243.4 ns |  1.02 |     104 B |
| sum_positive_where_naive | net9.0    | 1,224.9 ns |  1.00 |     104 B |
|                          |           |            |       |           |
| sum_positive_where       | net10.0   |   783.2 ns |  1.28 |      48 B |
| sum_positive_where       | net9.0    |   612.7 ns |  1.00 |      48 B |

Two things stand out: the naive version is about 2x the actual, and the optimizations above have no impact (in fact .net 10 is currently slower).

### Naive Where

First consider the naive version. `NaiveWhere` uses `yield return` which causes the C# compiler to create a state machine and object holding the state. `NaiveWhere` allocates one of these objects and returns it. This object has a made-up type name; we'll call it `<NaiveWhere>d__50<T>`. It implements both `IEnumerable<T>` and `IEnumerator<T>`, and has fields referencing the base enumerable and the predicate.

Our benchmark then calls `GetEnumerator()` on this object. Given that the object is already an enumerator this may seen a bit odd. This method checks various bits of the original object's state, and if things look good, just returns it. If not, another `<NaiveWhere>d__50<T>` is allocated and initialized with the same state as in the constructor, and that is returned instead. (The exact code here is in IL only, but is similar to [this C# version](https://github.com/dotnet/runtime/blob/5c6d1b3f7b63a3150ce6c737aeb4af03b3cce621/src/libraries/System.Linq/src/System/Linq/Iterator.cs#L64-L77) in Linq).

So as with the "empty collection" case we see here that there are two different possible enumerator objects of the same type that may get enumerated&mdash;the one initially created, or a clone. But unlike those cases above the first allocation dominates the second, so the flow relationship is a bit more nuanced.

In our benchmark the "initially created" path is always taken, so the "clone" path is cold, and because of that the constructor it invokes is not inlined, so the allocation done on the clone path escapes. That is likely ok because the escape happens "before" the clone path merges back into the "just created" path, and so we should still be able to prove the just created object does not escape.

But, not so fast. The `MoveNext()` for `<NaiveWhere>d__50<T>` is also not inlined, despite being "hot," and that causes the original allocation to escape as well. Why? The primary reason is that the `MoveNext()` contains a `try/finally` and the JIT is currently unable to inline methods with EH. But the method is also quite complicated, so it's not clear if the JIT would inline it even if there was no EH, and it is not so easy to see what else might trip things up after all that, but it looks like the internal IEenumerable's `GetEnumerator()` is invoked conditionally and we'd at least need to be able to prove we knew that this always happens on the first "dominating" call to `MoveNext()`. (Footnote 17)

In my initial examples the JIT also did not inline `get_Current` despite it being very simple: GDV claims it does not have a good class guess. It turns out it was just an artifact of my particular test case&mdash;no element passed the filter so `MoveNext` returned false on the first call, so `get_Current` was never called and so had no class profile. (Footnote 18)

The upshot is that our inner loop still has two interface calls:
```asm
G_M8017_IG05:  ;; offset=0x00AF
       mov      rcx, gword ptr [rbp-0x28]
       mov      r11, 0x7FF8046202B0      ; code for System.Collections.IEnumerator:MoveNext():ubyte:this
       call     [r11]System.Collections.IEnumerator:MoveNext():ubyte:this
       test     eax, eax
       je       SHORT G_M8017_IG07
						;; size=21 bbWeight=1 PerfScore 5.50
G_M8017_IG06:  ;; offset=0x00C4
       mov      rcx, gword ptr [rbp-0x28]
       mov      r11, 0x7FF8046202B8      ; code for System.Collections.Generic.IEnumerator`1[int]:get_Current():int:this
       call     [r11]System.Collections.Generic.IEnumerator`1[int]:get_Current():int:this
       add      esi, eax
       jmp      SHORT G_M8017_IG05
```
(todo: take a closer look at `MoveNext`).

### Linq Where

Now let's briefly look at the `Linq` case ([link to source](https://github.com/dotnet/runtime/blob/4951e38fc5882ddf7df68c956acb6f5586ef47dd/src/libraries/System.Linq/src/System/Linq/Where.cs#L12)). Here things are similar but different. `Where` orchestrates handling for arrays and lists via special helper objects, and for other collection types relies on something like our `NaiveWhere`.

For arrays the `ArrayWhereIterator` accesses the array directly rather than creating and using the standard array enumerator; this is presumably the reason for less allocation and faster iteration. The `MoveNext` does not contain EH and gets inlined, but again for the same reason as above `Current` is cold and so not inlined. (Footnote 19)

The JIT also does GDV on the delegate invocation, so the inner "filter" loop is not too bad (given lack of promotion):
```asm

G_M3835_IG11:  ;; offset=0x012B
       mov      edx, edi
       mov      r13d, dword ptr [r14+4*rdx+0x10]
       mov      edi, dword ptr [rbx+0x0C]
       lea      edx, [rdi+0x01]
       mov      dword ptr [rbx+0x0C], edx
       mov      r12, gword ptr [rbx+0x20]
       mov      rdx, 0x7FF8050EAD90      ; code for ArrayDeabstraction+<>c:<help_sum_positive_where>b__52_0(int):ubyte:this
       cmp      qword ptr [r12+0x18], rdx
       jne      G_M3835_IG17
       test     r13d, r13d
       jg       G_M3835_IG18
						;; size=50 bbWeight=505.85 PerfScore 6702.47
G_M3835_IG12:  ;; offset=0x015D
       cmp      r15d, edi
       ja       SHORT G_M3835_IG11
```
However we don't clone for the delegate and in the outer loop there is still a ton of enumerator state manipulation.

Cloning for the delegate is blocked by two issues:
* The JIT loses track of an exact type and so the code in loop cloning doesn't recognize the delegate GDV. This was fixed by [dotnet/runtime#110675](https://github.com/dotnet/runtime/pull/110675). But this suggests that object stack allocation (or similar) might also be able to do type refinement, if we fleshed out the points-to side a bit more. Currently this phase tracks which locals can refer to which other locals, and since there is a single-def local associated with each allocation, this also tracks which locals can refer to each allocation. But the analysis doesn't track what other things a local might refer to (yet).
* With that fixed, the analysis now doesn't realize the delegate field in the enumerator is a loop invariant. I think this is just a limitation of the simplistic invariance analysis done by cloning -- the delegate local is assigned in the loop, but the value being assigned is invariant. I also need to look at this case more closely, because the `Linq` sources often hoist the fetch of the delegate (`_predicate`) out of the enumeration loop, but not always (a simple local experiment to "fix" all of `Linq` to consistently hoist these invariants in the C# sources didn't yield interesting benchmark wins).

## Footnotes

(1) We generally don't want enumeration to modify `o`, so the enumeration state `e` (which generally is mutable) must be kept somewhere else. Most of this note is really trying to figure out how to arrange it so that in the important cases "somewhere else" is CPU registers and not stack slots or heap fields.

Note if the collection `o` is empty then it is possible that `e` not be mutable (`e.MoveNext()` can simply return `false`). This will come up again later.

(2) These benchmarks represent a "worst-case" assessment of enumeration overhead. In more realistic code there would be more work done per enumerated `t` and so the relative overhead would be smaller. But there is still an allocation cost.

(3) PGO can never prove that these are the only types, but it can say with reasonable confidence that other types are unlikely.

(4) Arrays were generic types before there were true generic types, so they were handled specially.

(5) The `GetEnumerator` method on arrays can be a bit too complex to be inlined normally, so we added `AggressiveInlining`.

(6) It is interesting to think how the JIT might be able to prove this on its own without some oracular knowledge, but it seems hard. And arrays are important enough collections to warrant some built-in special casing.

(7) You will note a general theme here between cloning and GDV. Both duplicate code and ask questions and then optimize one of the copies when the answers to those questions are favorable. If you look at it properly, inlining is like this, too.

(8) In runtime systems that support de-opt (eg Java) one can get by with only creating the optimized code variant, and bail out via a complex de-opt transition to unoptimized code if things ever go wrong. This is not something we've seriously considered for >NET, though you can see the appeal. Note if the optimized version stack allocates objects, these must also be undone.

(9) In the current implementation the JIT is given some upfront hints about which allocations and variables to track, so that the number of possible pseudo-variables is known up front. So in the example above the JIT knows that the `e` and `new E()` are conditional escape candidates. While this seems expedient, it also does not seem to be strictly necessary.

(10) If it turns out that there are frequently multiple conditionally escaping objects (under different conditions) we will of course reconsider.

(11) Note we don't know what type of enumerator gets produced if the upper GDV test fails, since we don't know what kind of collection we're enumerating. It could well produce an enumerator of type `E`. So it's possible that those original lower GDVs could succeed even if the upper one fails.

(12) There is a general benefit to reducing heap allocation, but the code paths for a heap allocated object and a stack allocated but not promoted object will often be quite similar (save perhaps for lack of write barriers), so there may not be a clear performance benefit for the method itself. But if we somehow knew this allocation site was responsible for a lot of heap allocation, we might reconsider.

(13) Given the `_version` checks it seems plausible the enumerator could safely cache the `_items` array; it would increase the enumerator size a bit but make enumeration a bit faster, so may or may not be a good tradeoff. (sub-footnote: I tried this and did not see any benefit, though it could be once the loop is optimized further this might pay off.)

(14) Interestingly there are [vestiges of an empty-list static enumerator](https://github.com/dotnet/runtime/blame/5ae9467d6ab7b31bf1cda35ca15ca5a2d21d3046/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L1187), so presumably at one point there was a static enumerator instance like we hae for the empty array case, but we realized that empty list enumeration could share the empty array instance. See [dotnet/runtime#82499)](https://github.com/dotnet/runtime/pull/82499).

(15) Aside from explicit looping, the JIT is able to transform recursive tail calls into loops, though this can only be done when the live part of the method state is the arguments to the call. Note thanks to inlining and GDV this can kick in in cases where the method is part of a recursive cycle of calls.

(16) We could (and likely should)  try to handle both, but we need to be thoughtful about how much cloning this entails. A naive version would end up creating 4 copies of the inner foreach, likely we don't want to have the version where we have the fast outer path and slow inner path, unless saving a heap allocation ends up being super-valuable.

(17) Inlining methods with EH is on the .NET 10 roadmap, and perhaps this is one case to use as motivation.

More broadly, we should consider having the inliner aggressively inline if a call site can be passed a locally allocated object. Here it is tricky because the connection between object allocation and call is indirect, and we don't do data flow during inlining. Now, if we did escape analysis before inlining, we could perhaps target the methods that cause the allocations to escape, but we'd then need to update / redo the analysis for each inline so we can handle the transitive cases... (this is where some kind of up-front hinting would be very useful, so the JIT would know which inlines could end up preventing escape).

(18) Overall this seems like something we could improve on. The `MoveNext` call site must dominate the `get_Current` site, so surely the class profile for the one likely can be applied to the other. But we have limited abilities to make such inferences today, as all this is done very early in the jit. And (as here) if `get_Current` does not postdominate `MoveNext` we can't be sure that the conditional logic in between is doing some computation that might alter the profile. But when the dominating profile is monomorphic then projecting it to the dominated site is probably a safe bet.

(19) Note this is an instance where inlining a "cold" method is beneficial, as it touches the same state as a hot path. We have seen these before in other contexts.