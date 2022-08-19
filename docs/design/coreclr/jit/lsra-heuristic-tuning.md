- [Background](#background)
- [Register selection heuristics](#register-selection-heuristics)
- [Impact measurement](#impact-measurement)
- [Genetic Algorithm](#genetic-algorithm)
- [Experiments](#experiments)
  * [Setup](#setup)
  * [Outcome](#outcome)
- [Conclusion](#conclusion)

## Background

RyuJIT's implements [linear scan register allocation](https://en.wikipedia.org/wiki/Register_allocation#Linear_scan) (LSRA) algorithm to perform the register assignment of generated code. During register selection, LSRA has various heuristics (17 to be precise) to pick the best register candidate at a given point. Each register candidate falls in one of the two categories. Either they do not contain any variable value and so are "free" to get assigned to hold a variable value. Otherwise, they already hold some variable value and hence, are "busy". If one of the busy registers is selected during assignment, the value it currently holds needs to be first stored into memory (also called "spilling the variable") before they are assigned to something else. RyuJIT's LSRA has the heuristics (14 of them) to pick one of the free registers first, and if none found, has heuristics (4 of them) to select one of the busy registers. Busy register is selected depending on which register is cheaper to spill.

We noticed that it is not always beneficial to give preference to free register candidates during register selection. Sometimes, it is better to pick a busy register and retain the free register for the future reference points that are part of hot code path. See [the generated code](https://sharplab.io/#v2:EYLgxg9gTgpgtADwGwBYA0AXEBDAzgWwB8ABABgAJiBGAOgCUBXAOwwEt8YaBhCfAB1YAbGFADKIgG6swMXAG4AsAChlxAMyUATOS7kA3svJHyygNoBZGBgAWEACYBJfoIAUlm/ad9BAeT5sIJlwaADkIByZBViZogHMASgBdVSokcmiMcgBxKzpsJjteF3j9ZQBIYgB2clJFJQBfZTN3W0dnNytWr19/VkDgsIiomKYE5KVqNOIUcgAFKAyXDPIEEoMlMoB6TcoqAE4XVbrGlQnUyhnRbGcYAEFi0o2JbChybHIAXmzc/ML8YrqZWer2An2+GDyBSK8UBwPIYDBOQhv2hsJe5DsiJ+UP+MPKcJgWOROIB+PRADMiZC/qSnujYlSUbi0a9rIySXiNuToOQXHCEGDaityAAechUUhCgDUUtWjzKQPRCHeXyR1NR5UVrwQoNV2JpnIV/IReuJBsBWpWmNN6uZmv5hJtTNpRqVlKdHIt80WysNZW9LEOwD9AYwhzAIYWgYQdkjPpgcej5MNJ39UbD2ENoZcwcB2YjefTLljhcWCdLgeTFbDCWrLmsnJO9SAA===) taken from [dotnet/runtime Issue#8846](https://github.com/dotnet/runtime/issues/8846). In this example, free registers are allocated to the variables that are out of the for-loop. During the register assignment for variables inside the loop, no free registers are available, and the algorithm spills a busy register to store their value. Astonishingly, it picks the same register for all the variables inside the loop and spill the previous variable values repeatedly. Our understanding is that it happens because of the ordering of heuristics in which we perform register selection. Perhaps, instead of having a fixed heuristics order, we should tweak the order to *sometimes* select busy registers first, before selecting from the pool of free registers. That was the inception of the idea of tuning the register selection heuristics described in [dotnet/runtime Issue# 43318](https://github.com/dotnet/runtime/issues/43318) and we wanted to conduct experiments to understand if we can do better register selection using different criteria. In this document, we will go over in detail to understand what made us pick genetic algorithm to do this experiment and what were the outcome of it.

## Register selection heuristics

Below are the heuristics implemented in RyuJIT to select a register:

| Shorthand | Name                 | Description                                                                                             |
|-----------|----------------------|---------------------------------------------------------------------------------------------------------|
| A         | `FREE`               | Not currently assigned to an *active* interval.                                                         |
| B         | `CONST_AVAILABLE`    | A constant value that is already available in a register.                                               |
| C         | `THIS_ASSIGNED`      | Register already assigned to the current interval.                                                      |
| D         | `COVERS`             | Covers the interval's current lifetime.                                                                 |
| E         | `OWN_PREFERENCE`     | Set of preferred registers of current interval.                                                         |
| F         | `COVERS_RELATED`     | Set of preferred registers of interval that is related to the current interval and covers the lifetime. |
| G         | `RELATED_PREFERENCE` | Set of preferred registers of interval that is related to the current interval.                         |
| H         | `CALLER_CALLEE`      | Caller or callee-saved registers.                                                                       |
| I         | `UNASSIGNED`         | Not currently assigned to any active or inactive interval.                                              |
| J         | `COVERS_FULL`        | Covers the interval's current lifetime until the end.                                                   |
| K         | `BEST_FIT`           | Available range is the closest match to the full range of the interval                                  |
| L         | `IS_PREV_REG`        | Register was previously assigned to the current interval                                                |
| M         | `REG_ORDER`          | Tie-breaker. Just pick the 1st available "free" register.                                               |
| N         | `SPILL_COST`         | Lowest spill cost of all the candidates.                                                                |
| O         | `FAR_NEXT_REF`       | It has farther next reference than the best candidate so far.                                           |
| P         | `PREV_REG_OPT`       | The previous reference of the current assigned interval was optional.                                   |
| Q         | `REG_NUM`            | Tie-breaker. Just pick the 1st available "busy" register.                                               |

Heuristic `A` thru `M` are for selecting one of the free registers, while `N` thru `Q` are for selecting one of the busy registers. A simple demonstration of how heuristic selection worked earlier is shown below. We start with free candidates and for each heuristic, narrow those candidates. Whenever, we see that there are more than one registers to pick from, we keep trying heuristics (in the above order) until a point when there is just one register left. If we don't find any register, we continue our search using heuristic `N` to find one of the busy registers that can be spilled.

```c#
registerCandidates = 0; // bit-mask of all registers

LinearScan::allocateReg(RefPosition refPosition, Interval* interval)
{
    bool found = false;
    registerCandidates = allFreeCandidates();

    if (!found) {
        found = applyHeuristics(FREE, FREE_Candidates());
    }

    if (!found) {
        found = applyHeuristics(CONST_AVAILABLE_Candidates());
    }

    ...

    if (!found) {
        found = applyHeuristics(REG_ORDER_Candidates());
    }

    // No free register was available, try to select one of
    // the busy register
    registerCandidates = allBusyCandidates();
    if (!found) {
        found = applyHeuristics(SPILL_COST_Candidates());
    }

    if (!found) {
        found = applyHeuristics(FAR_NEXT_REF_Candidates());
    }
    ...
}

// Filters the register candidates and returns true only there
// is one candidate.
bool applyHeuristics(selected_candidates)
{
    filtered_candidates = registerCandidates & selected_candidates;
    if (filtered_candidates != 0) {
        registerCandidates = filtered_candidates;
        return isSingleRegister(registerCandidates);
    }
    return false;
}

```

If we wanted to change the order of heuristics, we would have to update above code to rearrange the portion of heuristics we apply. To experiment with different heuristics ordering, it is not feasible to do such refactoring for every combination. After doing some research on which design pattern to pick for such problems, we went the old school way and moved the individual heuristics code in its own method (marked with `__forceinline`, to eliminate the throughput impact of refactoring changes). We could use function pointer to invoke one of these methods in any order we wanted. The last bit was an ability to add a way for user to specify heuristic order they want to try. We assigned a single letter to each heuristic (`Shorthand` column in above table) and we exposed `COMPlus_JitLsraOrdering` environment variable to specify the ordering. The default ordering is `"ABCDEFGHIJKLMNOPQ"` (the current order), but if given something else like `"PEHDCGAIJNLKOBFMQ"`, it would apply heuristic in that order. In this example, heuristic corresponding to `P` is `PREV_REG_OPT` and thus would apply busy register heuristics first, followed by `OWN_PREFERENCE`, `CALLER_CALLEE` and so forth. As you notice, now we will be able to apply the busy register heuristics before applying the ones for free registers.

After stitching all this together, the refactored code looked like this:

```c#

typedef void (RegisterSelection::*HeuristicFn)();
HashTable<char, HeuristicFn> ScoreMappingTable = {
    {'A', try_FREE},
    {'B', try_CONST_AVAILABLE},
    ...
    {'Q', try_REG_NUM}
};

LinearScan::allocateReg(RefPosition refPosition, Interval* interval)
{
    char *ordering = Read_COMPlus_LsraOrdering();
    HeuristicFn fn;
    for (char order in ordering) {
        if (ScoreMappingTable->Lookup(order, &fn)) {
            bool found = (this->*fn)();
            if (found) {
                break;
            }
        }
    }
}

bool LinearScan::try_FREE() {
    ...
    return applyHeuristics();
}
...
bool LinearScan::try_CONST_AVAILABLE() {
    ...
    return applyHeuristics();
}
...
bool LinearScan::try_REG_NUM() {
    ...
    return applyHeuristics();
}
```

[dotnet/runtime #52832](https://github.com/dotnet/runtime/pull/52832) contains all the refactoring changes that are described above.

## Impact measurement

Now that rearranging the heuristic ordering is possible with `COMPlus_JitLsraOrdering`, we decided to measure the impact of the reordering by running [superpmi](https://github.com/dotnet/runtime/blob/e063533eb79eace045f43b41980cbed21c8d7365/src/coreclr/ToolBox/superpmi/readme.md) tool. `superpmi` tool JITs all the methods of a given assembly file (`*.dll` or `*.exe`) without executing the generated machine code. Given two versions of `clrjit.dll` (RyuJIT binary), it also has an ability to perform the comparison of generated code and reporting back the number of methods that got improved/regressed in terms of `CodeSize` (machine code size), `PerfScore` (instruction latency/throughput measurements), `InstructionCount` (number of instructions present), etc. We picked `PerfScore` metrics because that accurately includes the cost of register spilling. If LSRA doesn't come up with optimal register choice, we would see several `mov` instructions that load/store into memory and that would decrease the throughput, increase the latency, and hence lower the `PerfScore`. If the spilling happens inside a loop, `PerfScore` metrics accounts for that by considering the product of loop block weights and `PerfScore`. Thus, our goal would be to reduce the `PerfScore` as much possible, lower the `PerfScore`, better is the code we generated. The baseline for the comparison was the default ordering, and we wanted to compare it with an ordering specified in `COMPlus_JitLsraOrdering`. We could specify any combination of sequence `A` thru `Q` and tweak the LSRA algorithm to apply a different heuristics order. But since there are 17 heuristics, there would be **355,687,428,096,000** (17!) possibilities to try out and it will not be practical to do so. We ought to find a better way!

## Genetic Algorithm

[Genetic algorithm](https://en.wikipedia.org/wiki/Genetic_algorithm) is the perfect solution to solve these kind of problems. For those who are not familiar, here is a quick summary - The algorithm starts with a community that has few candidates whose fitness score is predetermined. Each candidate is made up of sequence of genes and all candidates have same number of genes in them. The algorithm picks a pair of fit candidates (parents) and mutate their genes to produce offsprings. The algorithm calculates the fitness of the new offsprings and add them (along with the fitness score) back to the community pool. As the community evolves, more and more candidates who has fitness score equivalent or better than the initial population are added to the community. Of course, the community cannot grow infinitely, so the least fit candidates die. When there are no more candidates that are fit than the fittest candidate, the algorithm stops, giving us a set of fit candidates.

This can be perfectly mapped to the heuristic selection ordering problem. We want to start with `"ABCDEFGHIJKLMNOPQ"` (default selection order) and each letter in this combination can be represented as a gene. Genetic algorithm would mutate the gene to produce a different order say `"ABMCDEFGHIKLNJOPQ"` and we will set that value in `COMPlus_JitLsraOrdering` variable. We would then run `superpmi.py` to produce the generated code and compare the `PerfScore` with that of the one produced by the default order. `PerfScore` represents the fitness, lower the value of that metric, more fit is the corresponding candidate, in our case, better is the heuristic ordering.

Below is the pseudo code of genetic algorithm that we experimented with to find optimal heuristic ordering.

```c#
// Maximum population per generation
int MaxPopulation = 100;

HashMap<string, float> Community = new HashMap<string, float>();
HashMap<string, float> NextGen = new HashMap<string, float>();


void GeneticAlgorithm() {
    PopulateCommunity();

    do {
        // new generation
        NextGen = new HashMap<string, float>();
        candidateCount = 0;

        while(candidateCount++ < MaxPopulation) {
            // Use tournament selection method to pick
            // 2 candidates from "Community".
            // https://en.wikipedia.org/wiki/Tournament_selection
            (parent1, parent2) = DoSelection();

            // Mutate genes of parent1 and parent2 to produce
            // 2 offsprings
            (offspring0, offspring1) = MutateGenes(parent1, parent2)

            // Add offsprings to the community
            AddNewOffspring(offspring0)
            AddNewOffspring(offspring1)
        }
        Community = NextGen;

        // Loop until there are unique candidates are being produced in the
        // community

    } while (uniqueCandidates);

}

// Populate the community with random candidates
void PopulateCommunity() {
    candidateCount = 0;
    while(candidateCount < MaxPopulation) {
        newCandidate = GetRandomCombination("ABCDEFGHIJKLMNOPQ")
        AddNewOffspring(newCandidate)
    }
}

// Trigger superpmi tool and read back the PerfScore
void ComputeFitness(candidate) {
    perfScore = exec("superpmi.py asmdiffs -base_jit_path default\clrjit.dll -diff_jit_path other\clrjit.dll -diff_jit_option JitLsraOrdering=" + candidate)
    return perfScore
}

// Compuate fitness for both offsprings
// and add them to the community
void AddNewOffspring(candidate) {
    Community[candidate] = ComputeFitness(candidate)

    // Evict less fit candidate
    if (Community.Count > MaxPopulation) {
        weakCandidate = CandidateWithHighestPerfScore(Community);
        Community.Remove(weakCandidate)
    }
}

// Perform crossover and mutation techniques
void MutateGenes(offspring0, offspring1) {
    assert(offspring0.Length == offspring1.Length)

    // crossover
    crossOverPoint = random(0, offspring0.Length)
    i = 0
    while (i++ < crossOverPoint) {
        char c = offspring0[i]
        offspring0[i] = offspring1[i]
        offspring1[i] = c
    }

    // mutation
    randomIndex = random(0, offspring0.Length)
    char c = offspring0[randomIndex]
    offspring0[randomIndex] = offspring1[randomIndex]
    offspring1[randomIndex] = c

    return offspring0, offspring1
}
```

With genetic algorithm in place, we were ready to perform some experiments to find an optimal heuristic order.

## Experiments

With `superpmi`, we have an ability to run JIT against all the methods present in .NET libraries and [Microbenchmarks](https://github.com/dotnet/performance/tree/main/src/Benchmarks/micro). We also need to conduct this experiment for all OS/architecture that we support - Windows/x64, Windows/arm64, Linux/x64, Linux/arm and Linux/arm64.

### Setup

To conduct experiments, we made few changes to the way superpmi gathers `PerfScore` and reports it back.

1. `superpmi.exe` was modified to aggregate **relative** `PerfScore` difference of code generated by default and modified LSRA ordering. When `superpmi.exe` is run in parallel (which is by default), this number was reported back on the console by each parallel process.
2. `superpmi.py` was modified to further aggregate the relative `PerfScore` differences of parallel `superpmi.exe` processes and report back the final relative `PerfScore` difference.
3. LSRA has many asserts throughout the codebase. They assume that during register selection, all the free registers are tried first before checking for busy registers. Since we wanted to understand the impact of preferring busy registers as well, we had to disable those asserts.
4. `superpmi.exe asmdiffs` takes two versions of `clrjit.dll` that you want to compare. Both must be from different location. In our case, we only wanted to experiment with different heuristic ordering by passing different values for `COMPlus_JitLsraOrdering`, we made a copy of `clrjit.dll` -> `copy_clrjit.dll` and passed various ordering to the copied `copy_clrjit.dll`.

Here is the sample invocation of `superpmi.py` that genetic algorithm invoked to get the `PerfScore` (fitness score) of each experimented ordering:

```
python superpmi.py asmdiffs -f benchmarks -base_jit_path clrjit.dll -diff_jit_path copy_clrjit.dll -target_os windows -target_arch x64 -error_limit 10 -diff_jit_option JitLsraOrdering=APCDEGHNIOFJKLBMQ -log_file benchmarks_APCDEGHNIOFJKLBMQ.log
```

All the above changes are in the private branch [lsra-refactoring branch](https://github.com/kunalspathak/runtime/tree/lsra-refactoring).

### Outcome

Below are the heuristic ordering that genetic algorithm came up with for different configuration (scenarios/OS/architectures). The `PerfScore` column represent the aggregate of relative difference of `PerfScore` of all the methods. We preferred relative difference rather than absolute difference of `PerfScore` because we didn't want a dominant method's numbers hide the impact of other smaller methods.

| Configuration           | Ordering           | PerfScore   |
|-------------------------|--------------------|-------------|
| windows-x64 Benchmarks  | `EHPDGAJCBNKOLFIMQ`  | -36.540712  |
| windows-x64 Libraries   | `PEHDCGAIJNLKOBFMQ`  | -271.749901 |
| windows-x86 Benchmarks  | `EHDCFPGJBIALNOKMQ`  | -73.004577  |
| windows-x86 Libraries   | `APCDEGHNIOFJKLBMQ`  | -168.335079 |
| Linux-x64 Benchmarks    | `HGIDJNLCPOBKAEFMQ`  | -96.966704  |
| Linux-x64 Libraries     | `HDGAECNIPLBOFKJMQ`  | -391.835935 |
| Linux-arm64 Libraries   | `HECDBFGIANLOKJMPQ`  | -249.900161 |

As seen from the table, there are lot of better ordering than the default `"ABCDEFGHIJKLMNOPQ"`, which if used, can give us better register selection and hence, better performance. But we can also see that not all ordering that genetic algorithm came up with are same for all configurations. We wanted to find a common and similar ordering that can benefit all the scenarios across multiple platforms. As a last step of experiment, we tried to apply each of the best ordering that we had to other configurations and see how they perform. For example, `"EHPDGAJCBNKOLFIMQ"` is the most optimal ordering for windows/x64/Benchmarks configuration and we wanted to evaluate if that ordering could also be beneficial to Linux/arm64/Libraries. Likewise, for `"PEHDCGAIJNLKOBFMQ"` (optimal ordering for windows/x64/Libraries) and so forth.

Below table shows the compiled data of `PerfScore` that we get when we applied best ordering of individual configuration to other configurations. Each row contains a configuration along with the optimal ordering that genetic algorithm came up with. The columns represent the `PerfScore` we get if we apply the optimal ordering to the configuration listed in the column title.

| Configuration          | Optimal Ordering  | Linux-x64 Benchmarks  | windows-x64 Benchmarks | windows-arm64 Benchmarks | Linux-x64 Libraries   | Linux-arm64 Libraries | windows-x64 Libraries | windows-arm64 Libraries.pmi | windows-x86 Benchmarks | Linux-arm Libraries   | windows-x86 Libraries |
|------------------------|-------------------|-----------------------|------------------------|--------------------------|-----------------------|-----------------------|-----------------------|-----------------------------|------------------------|-----------------------|-----------------------|
| windows-x64 Benchmarks | `EHPDGAJCBNKOLFIMQ` | -83.496405            | **-36.540712**         | -19.09969                | -340.009195           | -103.340802           | -265.397122           | -113.718544                 | -62.126579             | 11292.33497           | 18.510854             |
| windows-x64 Libraries  | `PEHDCGAIJNLKOBFMQ` | -85.572973            | -35.853492             | -19.07247                | -355.615641           | -103.028599           | **-271.749901**       | -114.1154                   | -70.087852             | 31974.87698           | -46.803569            |
| windows-x86 Benchmarks | `EHDCFPGJBIALNOKMQ` | **-101.903471**       | -19.844343             | -41.041839               | **-419.933377**       | -247.95955            | -179.127655           | -265.675453                 | **-73.004577**         | 10679.36843           | -136.780091           |
| windows-x86 Libraries  | `APCDEGHNIOFJKLBMQ` | -26.907257            | -0.284718              | -30.144657               | -164.340576           | -220.351459           | -73.413256            | -232.256476                 | -10.25733              | 31979.07983           | **-168.335079**       |
| linux-x64 Benchmarks   | `HGIDJNLCPOBKAEFMQ` | -96.966704            | -9.29483               | -50.215283               | -361.159848           | -221.622609           | -64.308995            | -244.127555                 | 13.188704              | 8392.714652           | 397.994465            |
| linux-x64 Libraries    | `HDGAECNIPLBOFKJMQ` | -97.682606            | -13.882952             | -51.929281               | -391.835935           | -240.63813            | -101.495244           | -262.746033                 | -22.621316             | 8456.327283           | 165.982045            |
| linux-arm64 Libraries  | `HECDBFGIANLOKJMPQ` | -97.259922            | -11.159774             | **-54.424627**           | -330.340402           | **-249.900161**       | -52.359275            | **-270.482763**             | -35.304525             | **2404.874376**       | 125.707741            |
|                        |  Max PerfScore    | **`EHDCFPGJBIALNOKMQ`** | **`EHPDGAJCBNKOLFIMQ`**  | **`HECDBFGIANLOKJMPQ`**    | **`HDGAECNIPLBOFKJMQ`** | **`HECDBFGIANLOKJMPQ`** | **`PEHDCGAIJNLKOBFMQ`** | **`HECDBFGIANLOKJMPQ`**       | **`EHDCFPGJBIALNOKMQ`**  | **`HECDBFGIANLOKJMPQ`** | **`APCDEGHNIOFJKLBMQ`** |

 The last row in the above table tells the best ordering for the configuration (of that column) out of optimal orderings of all configurations. Below table summarizes 1st and 2nd best ordering for individual configuration.

| Configuration            | 1st best             | 2nd best             |
|--------------------------|----------------------|----------------------|
| windows-x64 Benchmarks   | `EHPDGAJCBNKOLFIMQ`  | `PEHDCGAIJNLKOBFMQ`  |
| windows-x64 Libraries    | `PEHDCGAIJNLKOBFMQ`  | `EHPDGAJCBNKOLFIMQ`  |
| windows-x86 Benchmarks   | `EHDCFPGJBIALNOKMQ`  | `PEHDCGAIJNLKOBFMQ`  |
| windows-x86 Libraries    | `APCDEGHNIOFJKLBMQ`  | `EHDCFPGJBIALNOKMQ`  |
| windows-arm64 Benchmarks | `HECDBFGIANLOKJMPQ`  | `HDGAECNIPLBOFKJMQ`  |
| windows-arm64 Libraries  | `HECDBFGIANLOKJMPQ`  | `EHDCFPGJBIALNOKMQ`  |

If we see the pattern under the "1st best" column, we see that the sequence `E` and `H` are towards the beginning, meaning that overall, it is profitable to have `OWN_PREFERENCE` (one of the preferred registers for a given interval) or `CALLEE_CALLER` (caller and callee registers) as one of the first heuristic criteria. Next, most of the ordering has `C` and `D` that are also popular that maps to `THIS_ASSIGNED` (already assigned to the current interval) and `COVERS` (covers the lifetime of an interval). One of the busy register heuristics `P` that maps to `PREV_REG_OPT` (Previous reference of the currently assigned interval was optional) is also present at the beginning.

While these ordering gives good `PerfScore`, there were several regressions observed for other methods. Most of the regressions falls under one or more of the following categories:
1. There are some key challenges in LSRA's resolution phase highlighted in [dotnet/runtime #47194](https://github.com/dotnet/runtime/issues/47194). Once resolution moves are identified for all the blocks, there is a need to revisit those moves to see if there are some that can be optimized out. Several methods regressed their `PerfScore` because we added lot of resolution moves at block boundaries.
2. Even though there is a flexibility of trying different register selection ordering, LSRA has limited knowledge about the method and portion of code for which it is allocating register. For example, during allocation, it doesn't know if it is allocating for code inside loop and that it should keep spare registers to use in that code. There has to be a phase before LSRA that consolidates this information in a data structure that can be used by LSRA during register selection.
3. While doing the experiments, we realized other low hanging fruits in LSRA that amplifies the regression caused by reordering the register selection heuristics. For example, if a variable is defined just once, it can be spilled at the place where it is defined and then, it doesn't need to be spilled throughout the method. This was achieved in [dotnet/runtime #54345](https://github.com/dotnet/runtime/pull/54345).

## Conclusion

Register allocation is a complex topic, slight change in algorithm could have huge impact on the generated code. We explored various ideas for finding optimal heuristic selection ordering. Using Genetic algorithm, we could find optimal ordering and there was also some commonality in the heuristics order that was performance efficient for majority of configuration that we tested. However, with many improvements, there were also regressions in many methods across all configurations. We discovered that there was other area of improvements that need to be fixed first before we enable heuristic tuning feature, [[RyuJIT][LSRA]](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+%22%5BRyuJIT%5D%5BLSRA%5D%22+) captures some of the issues. Hence, we decided not to change any heuristic ordering at the current time. We will focus on fixing these issues first and once the existing LSRA weakness are addressed, we can choose to return to this experiment and use the techniques, tools, and knowledge here to inform a heuristic re-ordering. Going forward, we could also auto tune the heuristic ordering based on various factors like how many method parameters are present, if loops are present or not, exception handling, etc. Opportunities are endless, time is limited, so got to make better choices!