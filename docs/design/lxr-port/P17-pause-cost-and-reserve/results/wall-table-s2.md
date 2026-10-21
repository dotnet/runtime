| workload | heap x | mode | acc ms | l1 ms | s2 ms | s2/l1 | s2/acc | paired repetitions s2/l1 | reading |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 6021 | 4964 | 4313 | 0.87 | 0.72 | 0.85, 0.85, 0.88 | all 3 below 0.95 |
| binarytrees | 2 | nursery | 7070 | 6694 | 5944 | 0.89 | 0.84 | 0.88, 0.93, 0.83 | all 3 below 0.95 |
| binarytrees | 2 | mature | 7319 | 6262 | 5835 | 0.93 | 0.80 | 0.70, 1.04, 0.93 | mixed |
| binarytrees | 4 | nonmoving | 4815 | 4007 | 3300 | 0.82 | 0.69 | 0.74, 0.83, 0.82 | all 3 below 0.95 |
| binarytrees | 4 | nursery | 5420 | 4580 | 4111 | 0.90 | 0.76 | 0.86, 0.98, 0.93 | mixed |
| binarytrees | 4 | mature | 5470 | 4237 | 3806 | 0.90 | 0.70 | 0.78, 1.15, 0.89 | mixed |
| binarytrees | 8 | nonmoving | 5102 | 3312 | 2728 | 0.82 | 0.53 | 0.82, 0.86, 0.81 | all 3 below 0.95 |
| binarytrees | 8 | nursery | 5495 | 3594 | 3028 | 0.84 | 0.55 | 0.83, 0.88, 0.84 | all 3 below 0.95 |
| binarytrees | 8 | mature | 5414 | 3611 | 3109 | 0.86 | 0.57 | 0.85, 0.87, 0.86 | all 3 below 0.95 |
| requests | 2 | nonmoving | 38119 | 38009 | 28246 | 0.74 | 0.74 | 0.76, 0.75, 0.74 | all 3 below 0.95 |
| requests | 4 | nonmoving | 29098 | 27447 | 23880 | 0.87 | 0.82 | 0.87, 0.87, 0.88 | all 3 below 0.95 |
| requests | 4 | nursery | 33943 | 17116 | 14439 | 0.84 | 0.43 | 0.84, 0.91, 0.82 | all 3 below 0.95 |
| requests | 4 | mature | 38665 | 17119 | 14539 | 0.85 | 0.38 | 0.84, 0.87, 0.85 | all 3 below 0.95 |
| requests | 8 | nonmoving | 24609 | 18414 | 14886 | 0.81 | 0.60 | 0.83, 0.81, 0.78 | all 3 below 0.95 |
| requests | 8 | nursery | 29289 | 15081 | 12498 | 0.83 | 0.43 | 0.83, 0.80, 0.94 | all 3 below 0.95 |
| requests | 8 | mature | 29955 | 14567 | 12858 | 0.88 | 0.43 | 0.91, 0.86, 0.88 | all 3 below 0.95 |
| listsuffix | 4 | nonmoving | 19918 | 17645 | 13573 | 0.77 | 0.68 | 0.76, 0.78, 0.78 | all 3 below 0.95 |
| listsuffix | 4 | nursery | 46627 | 43465 | 32729 | 0.75 | 0.70 | 0.76, 0.40, 0.81 | all 3 below 0.95 |
| listsuffix | 4 | mature | 50429 | 44891 | 43227 | 0.96 | 0.86 | 0.94, 0.96, 0.98 | mixed |
| listsuffix | 8 | nonmoving | 20753 | 19020 | 16311 | 0.86 | 0.79 | 0.81, 0.86, 0.86 | all 3 below 0.95 |
| listsuffix | 8 | nursery | 28271 | 23709 | 21357 | 0.90 | 0.76 | 0.90, 0.89, 0.95 | mixed |
| listsuffix | 8 | mature | 30458 | 26109 | 23056 | 0.88 | 0.76 | 0.85, 0.90, 0.88 | all 3 below 0.95 |

cells=22; geometric mean s2/l1=0.852 (min 0.743, max 0.963); s2/acc=0.642 (min 0.376, max 0.857); cells with all 3 repetitions below 0.95: 17
