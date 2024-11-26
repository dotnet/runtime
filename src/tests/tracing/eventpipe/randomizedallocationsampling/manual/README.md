# Manual Testing for Randomized Allocation Sampling

This folder has a test app (Allocate sub-folder) and a profiler (AllocationProfiler sub-folder) that together can be used to experimentally
observe the distribution of sampling events that are generated for different allocation scenarios. To run it:

1. Build both projects. These projects are just vanilla .NET 8 console apps and don't align with all the expectations of an in-repo automated test.
   Copy the manual folder somewhere outside the repo directory hierarchy and remove the underscore on the end of the two csproj files, then use VS
   or dotnet build to build them.
2. Run the Allocate app with corerun and use the --scenario argument to select an allocation scenario you want to validate
3. The Allocate app will print its own PID to the console and wait.
4. Run the AllocationProfiler passing in the allocate app PID as an argument
5. Hit Enter in the Allocate app to begin the allocations. You will see output in the profiler app's console showing the measurements. For example:

 ```
 Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
 -------------------------------------------------------------------------------------------
   S       1       0          24           0        24        102412           4267  System.Int16
  ST      44      61        1056        1464        24       4506128         187755  Object8
  ST       1       1          32          32        32        102416           3200  System.Reflection.MetadataImport
  ST      67      30        2144         960        32       6861872         214433  Object16
  ST      80     169        3840        8112        48       8193920         170706  Object32
   S       1       0          56           0        56        102428           1829  MemberInfoCache`1[System.Reflection.RuntimeMethodInfo]
  ST       2       3         160         240        80        204880           2561  System.String
   S       2       0         128           0        64        204864           3201  System.Reflection.RuntimeMethodBody
   S       1       0          80           0        80        102440           1280  System.Signature
  ST     143      86       11440        6880        80      14648920         183111  Object64
   S       2       0         222           0       111        204911           1846  System.Byte[]
   S       1       0          96           0        96        102448           1067  System.Reflection.RuntimeParameterInfo
   S       1       0         112           0       112        102456            914  System.Reflection.ParameterInfo[]
  ST     280     272       40320       39168       144      28692164         199251  Object128
   S       2       0       58224           0     29112        235289              8  EventMetadata[]
  ST       1       1     8388632     8388640   8388632       8388632              1  Object0[]
   T       0       1           0         336       336             0              0  System.Reflection.RuntimeFieldInfo[]
   T       0       1           0          48        48             0              0  System.Text.StringBuilder
```

- The **Tag** column shows if Allocation**T**ick and/or Allocation**S**ampled events where received for instances of a given type
- The **S**-prefixed colums refer to data from AllocationSampled events payload
- The **T**-prefixed colums refer to data from AllocationTick events payload
- The final **Upscaled**XXX columns are computed from AllocationSampled events payload

In this special case, the same number of 200000 instances were created and should be checked in the **UpscaledCount** column.

In a second case, 2 threads allocate 200000 instances of objects with x1/x2/x3 size ratio to see how the relative size distribution is conserved:

```
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST      47      67        1128        1608        24       4813364         200556  Object24
 ST      65      48        2080        1536        32       6657040         208032  Object32
 ST     108      94        5184        4512        48      11061792         230454  Object48
 ST     132     145        8448        9280        64      13521024         211266  Object64
 ST     155      87       11160        6264        72      15877580         220521  Object72
 ST     191     192       18336       18432        96      19567569         203828  Object96
 ST       2       2    16777264    16777280   8388632      16777264              2  Object0[]
```


A dedicated `AllocationsRunEventSource` has been created to allow monitoring multiple allocation runs and compute percentiles:
```
> starts 10 iterations allocating 1000000 instances
0|
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST     246     224        5904        5376        24      25193352        1049723  Allocate.WithFinalizer
 ST       5       7         320         448        64        512160           8002  System.RuntimeFieldInfoStub
 ST     702     719       50544       51768        72      71910074         998751  System.Int32[,]
 ST     946     859       90816       82464        96      96915815        1009539  System.String
 ST    1842    1887      362874      377400       197     188802295         958387  System.Byte[]
 ST       3       3    56000072    56000096  18666690      56000072              3  System.Object[]
1|
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST     283     224        6792        5376        24      28982596        1207608  Allocate.WithFinalizer
 ST     675     711       48600       51192        72      69144302         960337  System.Int32[,]
 ST     974     867       93504       83232        96      99784359        1039420  System.String
 ST    1861    1888      366617      377600       197     190749767         968272  System.Byte[]
 ST       3       3    56000072    56000096  18666690      56000072              3  System.Object[]
2|
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST     215     236        5160        5664        24      22018580         917440  Allocate.WithFinalizer
 ST       1       1          64          64        64        102432           1600  System.RuntimeFieldInfoStub
 ST     697     650       50184       46800        72      71397894         991637  System.Int32[,]
 ST     927     917       88992       88032        96      94969302         989263  System.String
 ST    1895    1886      373315      377200       197     194234717         985963  System.Byte[]
 ST       3       3    56000072    56000096  18666690      56000072              3  System.Object[]
  T       0       1           0         288       288             0              0  System.GCMemoryInfoData
3|
...
8|
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST     244     213        5856        5112        24      24988528        1041188  Allocate.WithFinalizer
 ST     710     681       51120       49032        72      72729562        1010132  System.Int32[,]
 ST     974     918       93504       88128        96      99784359        1039420  System.String
 ST    1920    1875      378240      375000       197     196797180         998970  System.Byte[]
 ST       3       3    56000072    56000096  18666690      56000072              3  System.Object[]
9|
Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name
-------------------------------------------------------------------------------------------
 ST     236     219        5664        5256        24      24169232        1007051  Allocate.WithFinalizer
 ST     698     682       50256       49104        72      71500330         993060  System.Int32[,]
 ST     940     913       90240       87648        96      96301127        1003136  System.String
 ST    1982    1874      390454      374800       197     203152089        1031228  System.Byte[]
 ST       3       3    56000072    56000096  18666690      56000072              3  System.Object[]

< run stops
```


Feel free to allocate the patterns you want in other methods of the **_Allocate_** project and use the _DynamicAllocationSampling_ events listener to get a synthetic view of the different allocation events.