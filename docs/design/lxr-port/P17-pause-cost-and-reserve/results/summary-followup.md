## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 25 of 162
- fa mature requests x2.0 rep0: exit=-6 'Out of memory.'
- fa nursery requests x2.0 rep0: exit=timeout ''
- base nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- fa nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- fa mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- fa nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- fa nursery requests x2.0 rep1: exit=timeout ''
- fa mature requests x2.0 rep1: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- fa mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- fa nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- fa nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- fa mature listsuffix x4.0 rep1: exit=-6 'le()\n   at System.Threading.Thread.SetJoinHandle()\n   at System.Threading.Thread.StartCallback(System.Threading.Thread*)'
- fa nursery requests x2.0 rep2: exit=timeout ''
- fa mature requests x2.0 rep2: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- fa nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- fa nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- fa mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | base | 3/3 | 5502 | 1.00 | 9309 | 2708 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | fa | 3/3 | 4817 | 0.88 | 7089 | 2360 | 68 | 0 | 147 |
| binarytrees | 2 | nursery | base | 3/3 | 7018 | 1.00 | 12978 | 4087 | 68 | 0 | 146 |
| binarytrees | 2 | nursery | fa | 3/3 | 6408 | 0.91 | 10261 | 4075 | 79 | 0 | 154 |
| binarytrees | 2 | mature | base | 3/3 | 6991 | 1.00 | 12865 | 4078 | 68 | 0 | 147 |
| binarytrees | 2 | mature | fa | 3/3 | 6249 | 0.89 | 9746 | 3992 | 75 | 0 | 154 |
| binarytrees | 4 | nonmoving | base | 3/3 | 4677 | 1.00 | 6154 | 1817 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | fa | 3/3 | 3941 | 0.84 | 5309 | 1909 | 18 | 0 | 227 |
| binarytrees | 4 | nursery | base | 3/3 | 5286 | 1.00 | 7091 | 2387 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | fa | 3/3 | 4360 | 0.82 | 5860 | 2387 | 20 | 0 | 221 |
| binarytrees | 4 | mature | base | 3/3 | 5594 | 1.00 | 7981 | 2647 | 18 | 0 | 229 |
| binarytrees | 4 | mature | fa | 3/3 | 4214 | 0.75 | 5616 | 2253 | 19 | 0 | 226 |
| binarytrees | 8 | nonmoving | base | 3/3 | 5168 | 1.00 | 5480 | 1421 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | fa | 3/3 | 3233 | 0.63 | 3356 | 1342 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | base | 3/3 | 5535 | 1.00 | 6024 | 1823 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | fa | 3/3 | 3674 | 0.66 | 3981 | 1777 | 8 | 0 | 259 |
| binarytrees | 8 | mature | base | 3/3 | 5497 | 1.00 | 5993 | 1831 | 7 | 0 | 314 |
| binarytrees | 8 | mature | fa | 3/3 | 3681 | 0.67 | 4025 | 1714 | 8 | 0 | 259 |
| requests | 2 | nonmoving | base | 3/3 | 37954 | 1.00 | 39993 | 21737 | 535 | 0 | 184 |
| requests | 2 | nonmoving | fa | 3/3 | 38085 | 1.00 | 39915 | 22013 | 531 | 0 | 184 |
| requests | 2 | nursery | base | 3/3 | 45668 | 1.00 | 47988 | 29424 | 535 | 0 | 184 |
| requests | 2 | nursery | fa | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | base | 3/3 | 45649 | 1.00 | 47925 | 29418 | 530 | 0 | 184 |
| requests | 2 | mature | fa | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | base | 3/3 | 28970 | 1.00 | 30828 | 17306 | 57 | 51 | 368 |
| requests | 4 | nonmoving | fa | 3/3 | 27237 | 0.94 | 28546 | 17208 | 57 | 51 | 370 |
| requests | 4 | nursery | base | 3/3 | 33161 | 1.00 | 35458 | 21539 | 57 | 51 | 369 |
| requests | 4 | nursery | fa | 3/3 | 16709 | 0.50 | 20394 | 8873 | 68 | 0 | 359 |
| requests | 4 | mature | base | 3/3 | 38357 | 1.00 | 40704 | 26617 | 57 | 51 | 368 |
| requests | 4 | mature | fa | 3/3 | 16766 | 0.44 | 20656 | 9224 | 66 | 0 | 382 |
| requests | 8 | nonmoving | base | 3/3 | 24505 | 1.00 | 27021 | 9728 | 22 | 16 | 658 |
| requests | 8 | nonmoving | fa | 3/3 | 17217 | 0.70 | 18921 | 9255 | 22 | 16 | 658 |
| requests | 8 | nursery | base | 3/3 | 27370 | 1.00 | 30756 | 12831 | 22 | 16 | 658 |
| requests | 8 | nursery | fa | 3/3 | 15005 | 0.55 | 19055 | 7198 | 25 | 0 | 657 |
| requests | 8 | mature | base | 3/3 | 30746 | 1.00 | 34009 | 15820 | 22 | 16 | 658 |
| requests | 8 | mature | fa | 3/3 | 14296 | 0.46 | 17536 | 7230 | 26 | 0 | 654 |
| listsuffix | 2 | nonmoving | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | fa | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | fa | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | fa | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | base | 3/3 | 21840 | 1.00 | 56149 | 12298 | 54 | 0 | 302 |
| listsuffix | 4 | nonmoving | fa | 3/3 | 17232 | 0.79 | 38144 | 8349 | 53 | 0 | 302 |
| listsuffix | 4 | nursery | base | 3/3 | 47588 | 1.00 | 131617 | 38269 | 63 | 0 | 305 |
| listsuffix | 4 | nursery | fa | 3/3 | 29618 | 0.62 | 71220 | 20700 | 58 | 0 | 283 |
| listsuffix | 4 | mature | base | 3/3 | 48781 | 1.00 | 134132 | 39185 | 62 | 0 | 305 |
| listsuffix | 4 | mature | fa | 2/3 | 52207 | 1.07 | 128847 | 42884 | 80 | 0 | 304 |
| listsuffix | 8 | nonmoving | base | 3/3 | 21675 | 1.00 | 37406 | 14872 | 15 | 12 | 491 |
| listsuffix | 8 | nonmoving | fa | 3/3 | 19933 | 0.92 | 31888 | 14343 | 15 | 12 | 495 |
| listsuffix | 8 | nursery | base | 3/3 | 28884 | 1.00 | 53485 | 21756 | 15 | 10 | 528 |
| listsuffix | 8 | nursery | fa | 3/3 | 24911 | 0.86 | 66382 | 17940 | 18 | 0 | 519 |
| listsuffix | 8 | mature | base | 3/3 | 29586 | 1.00 | 53904 | 22484 | 15 | 10 | 528 |
| listsuffix | 8 | mature | fa | 3/3 | 25288 | 0.85 | 68304 | 18429 | 21 | 0 | 516 |
