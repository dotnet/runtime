## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 33 of 243
- s5b mature requests x2.0 rep0: exit=timeout ''
- s5b nursery requests x2.0 rep0: exit=timeout ''
- base mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0b nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5b mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5b nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0b nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5b nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0b mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5b nursery requests x2.0 rep1: exit=timeout ''
- s5b mature requests x2.0 rep1: exit=timeout ''
- s5b nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0b mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5b mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0b nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0b nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5b nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5b mature requests x2.0 rep2: exit=timeout ''
- s5b nursery requests x2.0 rep2: exit=timeout ''
- s0b nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s5b mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0b mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0b nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s5b nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s5b nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | base | 3/3 | 6061 | 1.00 | 10305 | 2956 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | s0b | 3/3 | 5918 | 0.98 | 10010 | 2984 | 68 | 0 | 151 |
| binarytrees | 2 | nonmoving | s5b | 3/3 | 6113 | 1.01 | 10055 | 3030 | 67 | 0 | 146 |
| binarytrees | 2 | nursery | base | 3/3 | 7275 | 1.00 | 12950 | 4238 | 67 | 0 | 146 |
| binarytrees | 2 | nursery | s0b | 3/3 | 7155 | 0.98 | 12372 | 4152 | 67 | 0 | 146 |
| binarytrees | 2 | nursery | s5b | 3/3 | 8532 | 1.17 | 15831 | 5472 | 88 | 0 | 140 |
| binarytrees | 2 | mature | base | 3/3 | 7225 | 1.00 | 13123 | 4299 | 68 | 0 | 151 |
| binarytrees | 2 | mature | s0b | 3/3 | 7370 | 1.02 | 13437 | 4292 | 69 | 0 | 148 |
| binarytrees | 2 | mature | s5b | 3/3 | 9271 | 1.28 | 16814 | 6224 | 89 | 0 | 140 |
| binarytrees | 4 | nonmoving | base | 3/3 | 4933 | 1.00 | 6662 | 2040 | 18 | 0 | 229 |
| binarytrees | 4 | nonmoving | s0b | 3/3 | 4767 | 0.97 | 6171 | 1828 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | s5b | 3/3 | 5026 | 1.02 | 6855 | 2051 | 18 | 0 | 229 |
| binarytrees | 4 | nursery | base | 3/3 | 5367 | 1.00 | 7188 | 2399 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s0b | 3/3 | 5598 | 1.04 | 7688 | 2633 | 18 | 0 | 229 |
| binarytrees | 4 | nursery | s5b | 3/3 | 5244 | 0.98 | 6564 | 2367 | 19 | 0 | 198 |
| binarytrees | 4 | mature | base | 3/3 | 5581 | 1.00 | 7454 | 2573 | 17 | 0 | 229 |
| binarytrees | 4 | mature | s0b | 3/3 | 5604 | 1.00 | 7973 | 2678 | 18 | 0 | 229 |
| binarytrees | 4 | mature | s5b | 3/3 | 5149 | 0.92 | 6471 | 2252 | 19 | 0 | 203 |
| binarytrees | 8 | nonmoving | base | 3/3 | 5153 | 1.00 | 5476 | 1429 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s0b | 3/3 | 5206 | 1.01 | 5496 | 1457 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s5b | 3/3 | 5134 | 1.00 | 5422 | 1402 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | base | 3/3 | 5631 | 1.00 | 6110 | 1857 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s0b | 3/3 | 5534 | 0.98 | 5970 | 1849 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s5b | 3/3 | 5916 | 1.05 | 6838 | 2033 | 8 | 0 | 259 |
| binarytrees | 8 | mature | base | 3/3 | 5662 | 1.00 | 6146 | 1953 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s0b | 3/3 | 5766 | 1.02 | 6254 | 2017 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s5b | 3/3 | 5824 | 1.03 | 6724 | 1932 | 8 | 0 | 259 |
| requests | 2 | nonmoving | base | 3/3 | 41096 | 1.00 | 43244 | 23916 | 535 | 0 | 184 |
| requests | 2 | nonmoving | s0b | 3/3 | 39974 | 0.97 | 42105 | 23318 | 529 | 0 | 184 |
| requests | 2 | nonmoving | s5b | 3/3 | 42749 | 1.04 | 45129 | 24857 | 534 | 0 | 184 |
| requests | 2 | nursery | base | 3/3 | 48751 | 1.00 | 51245 | 31569 | 536 | 0 | 184 |
| requests | 2 | nursery | s0b | 3/3 | 48645 | 1.00 | 51104 | 31600 | 531 | 0 | 184 |
| requests | 2 | nursery | s5b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | base | 3/3 | 48695 | 1.00 | 51238 | 31414 | 535 | 0 | 184 |
| requests | 2 | mature | s0b | 3/3 | 49491 | 1.02 | 51913 | 32216 | 532 | 0 | 184 |
| requests | 2 | mature | s5b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | base | 3/3 | 31178 | 1.00 | 32787 | 19064 | 57 | 51 | 368 |
| requests | 4 | nonmoving | s0b | 3/3 | 30674 | 0.98 | 32613 | 18684 | 57 | 51 | 369 |
| requests | 4 | nonmoving | s5b | 3/3 | 31499 | 1.01 | 33343 | 19326 | 57 | 51 | 369 |
| requests | 4 | nursery | base | 3/3 | 35789 | 1.00 | 37613 | 23350 | 57 | 51 | 368 |
| requests | 4 | nursery | s0b | 3/3 | 36061 | 1.01 | 38442 | 23560 | 57 | 51 | 369 |
| requests | 4 | nursery | s5b | 3/3 | 41226 | 1.15 | 46033 | 9723 | 70 | 0 | 376 |
| requests | 4 | mature | base | 3/3 | 41760 | 1.00 | 44227 | 29226 | 57 | 51 | 369 |
| requests | 4 | mature | s0b | 3/3 | 40770 | 0.98 | 43068 | 28506 | 57 | 51 | 368 |
| requests | 4 | mature | s5b | 3/3 | 40939 | 0.98 | 46221 | 10277 | 66 | 0 | 394 |
| requests | 8 | nonmoving | base | 3/3 | 27174 | 1.00 | 29748 | 11081 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s0b | 3/3 | 25408 | 0.94 | 27863 | 10301 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s5b | 3/3 | 26468 | 0.97 | 29012 | 10894 | 22 | 16 | 659 |
| requests | 8 | nursery | base | 3/3 | 28769 | 1.00 | 32229 | 13524 | 22 | 16 | 658 |
| requests | 8 | nursery | s0b | 3/3 | 29084 | 1.01 | 32358 | 13584 | 22 | 16 | 658 |
| requests | 8 | nursery | s5b | 3/3 | 54058 | 1.88 | 60279 | 8015 | 27 | 0 | 657 |
| requests | 8 | mature | base | 3/3 | 33134 | 1.00 | 36593 | 17207 | 22 | 16 | 658 |
| requests | 8 | mature | s0b | 3/3 | 31501 | 0.95 | 34736 | 16478 | 22 | 16 | 658 |
| requests | 8 | mature | s5b | 3/3 | 52889 | 1.60 | 59247 | 9045 | 27 | 0 | 687 |
| listsuffix | 2 | nonmoving | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s0b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s5b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s0b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s5b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s0b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s5b | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | base | 3/3 | 22719 | 1.00 | 57365 | 12374 | 53 | 0 | 305 |
| listsuffix | 4 | nonmoving | s0b | 3/3 | 22010 | 0.97 | 54471 | 11826 | 53 | 0 | 297 |
| listsuffix | 4 | nonmoving | s5b | 3/3 | 22647 | 1.00 | 56471 | 12274 | 53 | 0 | 302 |
| listsuffix | 4 | nursery | base | 3/3 | 53558 | 1.00 | 144629 | 43249 | 63 | 0 | 302 |
| listsuffix | 4 | nursery | s0b | 3/3 | 52026 | 0.97 | 140588 | 42467 | 64 | 0 | 299 |
| listsuffix | 4 | nursery | s5b | 3/3 | 42367 | 0.79 | 88587 | 26911 | 242 | 0 | 244 |
| listsuffix | 4 | mature | base | 3/3 | 56961 | 1.00 | 153060 | 47199 | 63 | 0 | 302 |
| listsuffix | 4 | mature | s0b | 3/3 | 57387 | 1.01 | 153501 | 47875 | 64 | 0 | 307 |
| listsuffix | 4 | mature | s5b | 3/3 | 38125 | 0.67 | 85371 | 24552 | 216 | 0 | 241 |
| listsuffix | 8 | nonmoving | base | 3/3 | 25250 | 1.00 | 42923 | 17563 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | s0b | 3/3 | 25095 | 0.99 | 42445 | 17773 | 15 | 12 | 490 |
| listsuffix | 8 | nonmoving | s5b | 3/3 | 23666 | 0.94 | 39921 | 16442 | 15 | 12 | 489 |
| listsuffix | 8 | nursery | base | 3/3 | 32392 | 1.00 | 58690 | 24615 | 15 | 10 | 531 |
| listsuffix | 8 | nursery | s0b | 3/3 | 30847 | 0.95 | 58118 | 22873 | 15 | 9 | 529 |
| listsuffix | 8 | nursery | s5b | 3/3 | 37640 | 1.16 | 103722 | 28719 | 30 | 0 | 513 |
| listsuffix | 8 | mature | base | 3/3 | 32411 | 1.00 | 61041 | 24489 | 15 | 10 | 530 |
| listsuffix | 8 | mature | s0b | 3/3 | 32645 | 1.01 | 61052 | 24687 | 15 | 9 | 528 |
| listsuffix | 8 | mature | s5b | 3/3 | 43364 | 1.34 | 118053 | 33411 | 34 | 0 | 515 |
