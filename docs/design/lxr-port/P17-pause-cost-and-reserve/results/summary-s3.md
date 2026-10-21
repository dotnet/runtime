## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 39 of 243
- s2 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | acc | 3/3 | 5864 | 1.00 | 9938 | 2924 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | s2 | 3/3 | 4062 | 0.69 | 5793 | 1814 | 68 | 0 | 151 |
| binarytrees | 2 | nonmoving | s3 | 3/3 | 3515 | 0.60 | 5187 | 1493 | 68 | 0 | 139 |
| binarytrees | 2 | nursery | acc | 3/3 | 7028 | 1.00 | 12867 | 4091 | 67 | 0 | 146 |
| binarytrees | 2 | nursery | s2 | 3/3 | 5162 | 0.73 | 8061 | 3109 | 75 | 0 | 156 |
| binarytrees | 2 | nursery | s3 | 3/3 | 4493 | 0.64 | 7491 | 2518 | 86 | 0 | 142 |
| binarytrees | 2 | mature | acc | 3/3 | 7009 | 1.00 | 12511 | 4031 | 67 | 0 | 148 |
| binarytrees | 2 | mature | s2 | 3/3 | 5634 | 0.80 | 9027 | 3456 | 75 | 0 | 154 |
| binarytrees | 2 | mature | s3 | 3/3 | 4188 | 0.60 | 7033 | 2361 | 74 | 0 | 143 |
| binarytrees | 4 | nonmoving | acc | 3/3 | 4708 | 1.00 | 6177 | 1814 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | s2 | 3/3 | 3297 | 0.70 | 4531 | 1447 | 18 | 0 | 224 |
| binarytrees | 4 | nonmoving | s3 | 3/3 | 2653 | 0.56 | 3892 | 1028 | 18 | 0 | 190 |
| binarytrees | 4 | nursery | acc | 3/3 | 5652 | 1.00 | 7756 | 2693 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s2 | 3/3 | 3662 | 0.65 | 4944 | 1852 | 20 | 0 | 221 |
| binarytrees | 4 | nursery | s3 | 3/3 | 2744 | 0.49 | 3987 | 1111 | 19 | 0 | 183 |
| binarytrees | 4 | mature | acc | 3/3 | 5538 | 1.00 | 7847 | 2663 | 18 | 0 | 229 |
| binarytrees | 4 | mature | s2 | 3/3 | 3894 | 0.70 | 5382 | 2019 | 20 | 0 | 226 |
| binarytrees | 4 | mature | s3 | 3/3 | 2271 | 0.41 | 2499 | 646 | 18 | 0 | 111 |
| binarytrees | 8 | nonmoving | acc | 3/3 | 5114 | 1.00 | 5408 | 1445 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s2 | 3/3 | 3122 | 0.61 | 3244 | 1068 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s3 | 3/3 | 2100 | 0.41 | 2197 | 484 | 7 | 0 | 243 |
| binarytrees | 8 | nursery | acc | 3/3 | 5592 | 1.00 | 6057 | 1859 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s2 | 3/3 | 3057 | 0.55 | 3360 | 1278 | 8 | 0 | 259 |
| binarytrees | 8 | nursery | s3 | 3/3 | 2190 | 0.39 | 2463 | 542 | 8 | 0 | 188 |
| binarytrees | 8 | mature | acc | 3/3 | 5515 | 1.00 | 6001 | 1867 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s2 | 3/3 | 3103 | 0.56 | 3447 | 1291 | 8 | 0 | 259 |
| binarytrees | 8 | mature | s3 | 3/3 | 2109 | 0.38 | 2399 | 548 | 8 | 0 | 188 |
| requests | 2 | nonmoving | acc | 3/3 | 39142 | 1.00 | 41202 | 22587 | 532 | 0 | 184 |
| requests | 2 | nonmoving | s2 | 3/3 | 30548 | 0.78 | 32241 | 15247 | 533 | 0 | 184 |
| requests | 2 | nonmoving | s3 | 3/3 | 23069 | 0.59 | 24547 | 11310 | 531 | 0 | 149 |
| requests | 2 | nursery | acc | 3/3 | 47486 | 1.00 | 49859 | 30641 | 530 | 0 | 184 |
| requests | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | acc | 3/3 | 46838 | 1.00 | 49249 | 30248 | 536 | 0 | 184 |
| requests | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | acc | 3/3 | 30731 | 1.00 | 32618 | 18694 | 57 | 53 | 341 |
| requests | 4 | nonmoving | s2 | 3/3 | 26484 | 0.86 | 27723 | 16676 | 57 | 51 | 370 |
| requests | 4 | nonmoving | s3 | 3/3 | 23935 | 0.78 | 25110 | 14036 | 57 | 51 | 295 |
| requests | 4 | nursery | acc | 3/3 | 37634 | 1.00 | 39599 | 24965 | 57 | 51 | 368 |
| requests | 4 | nursery | s2 | 3/3 | 15090 | 0.40 | 18632 | 7429 | 69 | 0 | 356 |
| requests | 4 | nursery | s3 | 3/3 | 10660 | 0.28 | 13828 | 4182 | 69 | 0 | 282 |
| requests | 4 | mature | acc | 3/3 | 41340 | 1.00 | 43850 | 29182 | 57 | 51 | 368 |
| requests | 4 | mature | s2 | 3/3 | 15221 | 0.37 | 18854 | 7794 | 67 | 0 | 382 |
| requests | 4 | mature | s3 | 3/3 | 10718 | 0.26 | 14161 | 4568 | 67 | 0 | 304 |
| requests | 8 | nonmoving | acc | 3/3 | 26964 | 1.00 | 29437 | 11803 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s2 | 3/3 | 16882 | 0.63 | 18361 | 8770 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s3 | 3/3 | 15650 | 0.58 | 17092 | 7540 | 22 | 16 | 509 |
| requests | 8 | nursery | acc | 3/3 | 28606 | 1.00 | 31854 | 13743 | 22 | 16 | 658 |
| requests | 8 | nursery | s2 | 3/3 | 12896 | 0.45 | 16453 | 5863 | 25 | 0 | 658 |
| requests | 8 | nursery | s3 | 3/3 | 10414 | 0.36 | 13882 | 3354 | 25 | 0 | 508 |
| requests | 8 | mature | acc | 3/3 | 30938 | 1.00 | 34095 | 16004 | 22 | 16 | 658 |
| requests | 8 | mature | s2 | 3/3 | 13444 | 0.43 | 16416 | 6087 | 26 | 0 | 650 |
| requests | 8 | mature | s3 | 3/3 | 9465 | 0.31 | 12243 | 3431 | 26 | 0 | 497 |
| listsuffix | 2 | nonmoving | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | acc | 3/3 | 22102 | 1.00 | 57058 | 12153 | 53 | 0 | 299 |
| listsuffix | 4 | nonmoving | s2 | 3/3 | 15187 | 0.69 | 31912 | 6749 | 53 | 0 | 306 |
| listsuffix | 4 | nonmoving | s3 | 3/3 | 13129 | 0.59 | 27121 | 6499 | 52 | 0 | 270 |
| listsuffix | 4 | nursery | acc | 3/3 | 51351 | 1.00 | 140110 | 41684 | 63 | 0 | 305 |
| listsuffix | 4 | nursery | s2 | 3/3 | 41363 | 0.81 | 103291 | 33177 | 58 | 0 | 290 |
| listsuffix | 4 | nursery | s3 | 3/3 | 19765 | 0.38 | 45522 | 13043 | 54 | 0 | 242 |
| listsuffix | 4 | mature | acc | 3/3 | 51523 | 1.00 | 142244 | 41747 | 63 | 0 | 303 |
| listsuffix | 4 | mature | s2 | 3/3 | 41050 | 0.80 | 104454 | 32758 | 68 | 0 | 285 |
| listsuffix | 4 | mature | s3 | 3/3 | 35509 | 0.69 | 91549 | 28301 | 68 | 0 | 254 |
| listsuffix | 8 | nonmoving | acc | 3/3 | 22577 | 1.00 | 38908 | 15913 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | s2 | 3/3 | 17534 | 0.78 | 26680 | 12251 | 15 | 12 | 491 |
| listsuffix | 8 | nonmoving | s3 | 3/3 | 17272 | 0.77 | 26563 | 11150 | 15 | 10 | 458 |
| listsuffix | 8 | nursery | acc | 3/3 | 29318 | 1.00 | 53825 | 22171 | 15 | 10 | 530 |
| listsuffix | 8 | nursery | s2 | 3/3 | 23098 | 0.79 | 61296 | 16239 | 18 | 0 | 524 |
| listsuffix | 8 | nursery | s3 | 3/3 | 16040 | 0.55 | 38601 | 10231 | 19 | 0 | 456 |
| listsuffix | 8 | mature | acc | 3/3 | 33189 | 1.00 | 59853 | 25517 | 15 | 10 | 533 |
| listsuffix | 8 | mature | s2 | 3/3 | 22654 | 0.68 | 59842 | 16375 | 21 | 0 | 515 |
| listsuffix | 8 | mature | s3 | 3/3 | 23997 | 0.72 | 62518 | 18188 | 33 | 0 | 447 |
