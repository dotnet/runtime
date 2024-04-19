This folder contains code that samples with AllocationTick and AllocationSampled + upscale the size/count.
200000 instances of each custom type (once from the smaller to the larger and once from the larger to the smaller). The resulting assembly is then run by corerun via VS with the right properties. It stops to let you copy the process ID to pass to the _DynamicAllocationSampling_ events listener program to obtain the following kind of result:

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
Feel free to allocate the patterns you want in other methods of the Allocate  ___project and use the _DynamicAllocationSampling_ events listener to get a synthetic view of the different allocation events.