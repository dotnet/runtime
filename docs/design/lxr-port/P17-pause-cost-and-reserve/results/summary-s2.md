## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 40 of 243
- s2 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- l1 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- l1 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- l1 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- l1 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- l1 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep1: exit=timeout 'Out of memory.'
- l1 nursery requests x2.0 rep1: exit=timeout ''
- s2 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- l1 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- l1 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- l1 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- l1 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- l1 nursery requests x2.0 rep2: exit=timeout ''
- l1 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- l1 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- l1 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- l1 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc mature listsuffix x4.0 rep2: exit=-6 'Fatal error.\nError 2147500037 (0x80004005 (E_FAIL))'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | acc | 3/3 | 6021 | 1.00 | 9898 | 2920 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | l1 | 3/3 | 4964 | 0.82 | 7314 | 2445 | 68 | 0 | 151 |
| binarytrees | 2 | nonmoving | s2 | 3/3 | 4313 | 0.72 | 6260 | 1900 | 68 | 0 | 152 |
| binarytrees | 2 | nursery | acc | 3/3 | 7070 | 1.00 | 12591 | 4091 | 68 | 0 | 148 |
| binarytrees | 2 | nursery | l1 | 3/3 | 6694 | 0.95 | 10133 | 4085 | 76 | 0 | 155 |
| binarytrees | 2 | nursery | s2 | 3/3 | 5944 | 0.84 | 9904 | 3739 | 83 | 0 | 154 |
| binarytrees | 2 | mature | acc | 3/3 | 7319 | 1.00 | 12920 | 4187 | 67 | 0 | 147 |
| binarytrees | 2 | mature | l1 | 3/3 | 6262 | 0.86 | 9716 | 3976 | 76 | 0 | 156 |
| binarytrees | 2 | mature | s2 | 3/3 | 5835 | 0.80 | 9440 | 3595 | 81 | 0 | 153 |
| binarytrees | 4 | nonmoving | acc | 3/3 | 4815 | 1.00 | 6316 | 1858 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | l1 | 3/3 | 4007 | 0.83 | 5418 | 1971 | 18 | 0 | 223 |
| binarytrees | 4 | nonmoving | s2 | 3/3 | 3300 | 0.69 | 4501 | 1422 | 18 | 0 | 229 |
| binarytrees | 4 | nursery | acc | 3/3 | 5420 | 1.00 | 7209 | 2502 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | l1 | 3/3 | 4580 | 0.85 | 4964 | 2271 | 18 | 0 | 144 |
| binarytrees | 4 | nursery | s2 | 3/3 | 4111 | 0.76 | 5547 | 2053 | 20 | 0 | 220 |
| binarytrees | 4 | mature | acc | 3/3 | 5470 | 1.00 | 7891 | 2689 | 18 | 0 | 229 |
| binarytrees | 4 | mature | l1 | 3/3 | 4237 | 0.77 | 5537 | 2526 | 19 | 0 | 214 |
| binarytrees | 4 | mature | s2 | 3/3 | 3806 | 0.70 | 5124 | 2048 | 19 | 0 | 224 |
| binarytrees | 8 | nonmoving | acc | 3/3 | 5102 | 1.00 | 5402 | 1386 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | l1 | 3/3 | 3312 | 0.65 | 3436 | 1325 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s2 | 3/3 | 2728 | 0.53 | 2826 | 893 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | acc | 3/3 | 5495 | 1.00 | 5970 | 1844 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | l1 | 3/3 | 3594 | 0.65 | 3968 | 1727 | 8 | 0 | 259 |
| binarytrees | 8 | nursery | s2 | 3/3 | 3028 | 0.55 | 3330 | 1283 | 8 | 0 | 259 |
| binarytrees | 8 | mature | acc | 3/3 | 5414 | 1.00 | 5901 | 1823 | 7 | 0 | 314 |
| binarytrees | 8 | mature | l1 | 3/3 | 3611 | 0.67 | 3973 | 1739 | 8 | 0 | 259 |
| binarytrees | 8 | mature | s2 | 3/3 | 3109 | 0.57 | 3436 | 1294 | 8 | 0 | 259 |
| requests | 2 | nonmoving | acc | 3/3 | 38119 | 1.00 | 40140 | 21918 | 534 | 0 | 184 |
| requests | 2 | nonmoving | l1 | 3/3 | 38009 | 1.00 | 39830 | 21787 | 533 | 0 | 184 |
| requests | 2 | nonmoving | s2 | 3/3 | 28246 | 0.74 | 29800 | 14169 | 527 | 0 | 184 |
| requests | 2 | nursery | acc | 3/3 | 45506 | 1.00 | 47768 | 29199 | 532 | 0 | 184 |
| requests | 2 | nursery | l1 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | acc | 3/3 | 45475 | 1.00 | 47749 | 29346 | 534 | 0 | 184 |
| requests | 2 | mature | l1 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | acc | 3/3 | 29098 | 1.00 | 30698 | 17684 | 57 | 53 | 340 |
| requests | 4 | nonmoving | l1 | 3/3 | 27447 | 0.94 | 28546 | 17124 | 57 | 51 | 369 |
| requests | 4 | nonmoving | s2 | 3/3 | 23880 | 0.82 | 25141 | 14666 | 57 | 51 | 370 |
| requests | 4 | nursery | acc | 3/3 | 33943 | 1.00 | 36156 | 22119 | 57 | 51 | 369 |
| requests | 4 | nursery | l1 | 3/3 | 17116 | 0.50 | 20851 | 9043 | 68 | 0 | 359 |
| requests | 4 | nursery | s2 | 3/3 | 14439 | 0.43 | 17756 | 7083 | 69 | 0 | 357 |
| requests | 4 | mature | acc | 3/3 | 38665 | 1.00 | 40898 | 26858 | 57 | 51 | 371 |
| requests | 4 | mature | l1 | 3/3 | 17119 | 0.44 | 20972 | 9296 | 67 | 0 | 381 |
| requests | 4 | mature | s2 | 3/3 | 14539 | 0.38 | 17990 | 7469 | 66 | 0 | 383 |
| requests | 8 | nonmoving | acc | 3/3 | 24609 | 1.00 | 26896 | 10064 | 22 | 16 | 658 |
| requests | 8 | nonmoving | l1 | 3/3 | 18414 | 0.75 | 20073 | 10126 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s2 | 3/3 | 14886 | 0.60 | 16295 | 7597 | 22 | 16 | 658 |
| requests | 8 | nursery | acc | 3/3 | 29289 | 1.00 | 32466 | 14009 | 22 | 16 | 659 |
| requests | 8 | nursery | l1 | 3/3 | 15081 | 0.51 | 18713 | 7459 | 25 | 0 | 658 |
| requests | 8 | nursery | s2 | 3/3 | 12498 | 0.43 | 15845 | 5604 | 25 | 0 | 658 |
| requests | 8 | mature | acc | 3/3 | 29955 | 1.00 | 33001 | 15422 | 22 | 16 | 658 |
| requests | 8 | mature | l1 | 3/3 | 14567 | 0.49 | 17821 | 7369 | 26 | 0 | 654 |
| requests | 8 | mature | s2 | 3/3 | 12858 | 0.43 | 15762 | 5974 | 26 | 0 | 653 |
| listsuffix | 2 | nonmoving | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | l1 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | l1 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | l1 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | acc | 3/3 | 19918 | 1.00 | 49584 | 10819 | 53 | 0 | 305 |
| listsuffix | 4 | nonmoving | l1 | 3/3 | 17645 | 0.89 | 39129 | 8682 | 53 | 0 | 302 |
| listsuffix | 4 | nonmoving | s2 | 3/3 | 13573 | 0.68 | 28955 | 5996 | 53 | 0 | 300 |
| listsuffix | 4 | nursery | acc | 3/3 | 46627 | 1.00 | 126359 | 38215 | 63 | 0 | 303 |
| listsuffix | 4 | nursery | l1 | 3/3 | 43465 | 0.93 | 111173 | 34589 | 62 | 0 | 288 |
| listsuffix | 4 | nursery | s2 | 3/3 | 32729 | 0.70 | 82768 | 24749 | 58 | 0 | 287 |
| listsuffix | 4 | mature | acc | 2/3 | 50429 | 1.00 | 134557 | 41662 | 63 | 0 | 308 |
| listsuffix | 4 | mature | l1 | 3/3 | 44891 | 0.89 | 114930 | 35935 | 70 | 0 | 283 |
| listsuffix | 4 | mature | s2 | 3/3 | 43227 | 0.86 | 108406 | 34620 | 73 | 0 | 280 |
| listsuffix | 8 | nonmoving | acc | 3/3 | 20753 | 1.00 | 35883 | 14352 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | l1 | 3/3 | 19020 | 0.92 | 30108 | 13379 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | s2 | 3/3 | 16311 | 0.79 | 25941 | 11207 | 15 | 12 | 495 |
| listsuffix | 8 | nursery | acc | 3/3 | 28271 | 1.00 | 53387 | 21029 | 15 | 9 | 533 |
| listsuffix | 8 | nursery | l1 | 3/3 | 23709 | 0.84 | 63937 | 16801 | 18 | 0 | 524 |
| listsuffix | 8 | nursery | s2 | 3/3 | 21357 | 0.76 | 56381 | 15349 | 18 | 0 | 524 |
| listsuffix | 8 | mature | acc | 3/3 | 30458 | 1.00 | 55175 | 23169 | 15 | 10 | 532 |
| listsuffix | 8 | mature | l1 | 3/3 | 26109 | 0.86 | 70221 | 19362 | 21 | 0 | 518 |
| listsuffix | 8 | mature | s2 | 3/3 | 23056 | 0.76 | 62068 | 16361 | 21 | 0 | 517 |
