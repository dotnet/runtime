## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 28 of 243
- base nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0off nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0off mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0off nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s0off nursery listsuffix x4.0 rep0: exit=-6 ')\n   at System.IO.ReadLinesIterator.MoveNext()\n   at GcBench.StatusKb(System.String)\n   at GcBench.Main(System.String[])'
- base mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0off mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0off nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0off nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s0 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0off mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0off nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s0off nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | base | 3/3 | 6177 | 1.00 | 10834 | 3091 | 68 | 0 | 146 |
| binarytrees | 2 | nonmoving | s0 | 3/3 | 6309 | 1.02 | 10964 | 3159 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | s0off | 3/3 | 6287 | 1.02 | 10540 | 3089 | 68 | 0 | 146 |
| binarytrees | 2 | nursery | base | 3/3 | 7346 | 1.00 | 13092 | 4191 | 67 | 0 | 146 |
| binarytrees | 2 | nursery | s0 | 3/3 | 7302 | 0.99 | 12971 | 4203 | 68 | 0 | 147 |
| binarytrees | 2 | nursery | s0off | 3/3 | 7492 | 1.02 | 13671 | 4357 | 67 | 0 | 146 |
| binarytrees | 2 | mature | base | 3/3 | 7334 | 1.00 | 13380 | 4257 | 68 | 0 | 146 |
| binarytrees | 2 | mature | s0 | 3/3 | 7303 | 1.00 | 13423 | 4243 | 67 | 0 | 146 |
| binarytrees | 2 | mature | s0off | 3/3 | 7288 | 0.99 | 13056 | 4203 | 67 | 0 | 146 |
| binarytrees | 4 | nonmoving | base | 3/3 | 4928 | 1.00 | 6381 | 1918 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | s0 | 3/3 | 4891 | 0.99 | 6750 | 2060 | 18 | 0 | 226 |
| binarytrees | 4 | nonmoving | s0off | 3/3 | 4689 | 0.95 | 6152 | 1861 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | base | 3/3 | 5236 | 1.00 | 7137 | 2407 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s0 | 3/3 | 5537 | 1.06 | 7701 | 2626 | 18 | 0 | 228 |
| binarytrees | 4 | nursery | s0off | 3/3 | 5492 | 1.05 | 7596 | 2612 | 17 | 0 | 229 |
| binarytrees | 4 | mature | base | 3/3 | 5667 | 1.00 | 7919 | 2769 | 18 | 0 | 229 |
| binarytrees | 4 | mature | s0 | 3/3 | 5567 | 0.98 | 7885 | 2685 | 18 | 0 | 229 |
| binarytrees | 4 | mature | s0off | 3/3 | 6607 | 1.17 | 8777 | 3087 | 17 | 0 | 229 |
| binarytrees | 8 | nonmoving | base | 3/3 | 5334 | 1.00 | 5669 | 1510 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s0 | 3/3 | 5273 | 0.99 | 5614 | 1545 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s0off | 3/3 | 5888 | 1.10 | 6278 | 1685 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | base | 3/3 | 6256 | 1.00 | 6782 | 2192 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s0 | 3/3 | 5713 | 0.91 | 6203 | 1948 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s0off | 3/3 | 6057 | 0.97 | 6592 | 2088 | 7 | 0 | 314 |
| binarytrees | 8 | mature | base | 3/3 | 5833 | 1.00 | 6357 | 2014 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s0 | 3/3 | 5971 | 1.02 | 6458 | 1994 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s0off | 3/3 | 5765 | 0.99 | 6275 | 1917 | 7 | 0 | 314 |
| requests | 2 | nonmoving | base | 3/3 | 39925 | 1.00 | 41481 | 23137 | 534 | 0 | 184 |
| requests | 2 | nonmoving | s0 | 3/3 | 43865 | 1.10 | 46427 | 25273 | 533 | 0 | 184 |
| requests | 2 | nonmoving | s0off | 3/3 | 39237 | 0.98 | 41272 | 22808 | 534 | 0 | 184 |
| requests | 2 | nursery | base | 3/3 | 47404 | 1.00 | 49906 | 30435 | 529 | 0 | 184 |
| requests | 2 | nursery | s0 | 3/3 | 49291 | 1.04 | 51976 | 31675 | 534 | 0 | 184 |
| requests | 2 | nursery | s0off | 3/3 | 45775 | 0.97 | 48079 | 29523 | 531 | 0 | 184 |
| requests | 2 | mature | base | 3/3 | 48990 | 1.00 | 51662 | 31417 | 531 | 0 | 184 |
| requests | 2 | mature | s0 | 3/3 | 48128 | 0.98 | 50679 | 30940 | 530 | 0 | 184 |
| requests | 2 | mature | s0off | 3/3 | 46807 | 0.96 | 49204 | 30393 | 535 | 0 | 184 |
| requests | 4 | nonmoving | base | 3/3 | 30308 | 1.00 | 31621 | 18394 | 57 | 53 | 340 |
| requests | 4 | nonmoving | s0 | 3/3 | 29064 | 0.96 | 30900 | 17391 | 57 | 51 | 369 |
| requests | 4 | nonmoving | s0off | 3/3 | 29131 | 0.96 | 31036 | 17443 | 57 | 51 | 368 |
| requests | 4 | nursery | base | 3/3 | 34617 | 1.00 | 37010 | 22668 | 57 | 51 | 374 |
| requests | 4 | nursery | s0 | 3/3 | 36138 | 1.04 | 38693 | 23551 | 57 | 51 | 369 |
| requests | 4 | nursery | s0off | 3/3 | 34186 | 0.99 | 36693 | 22199 | 57 | 51 | 375 |
| requests | 4 | mature | base | 3/3 | 39820 | 1.00 | 41637 | 27736 | 57 | 52 | 340 |
| requests | 4 | mature | s0 | 3/3 | 39359 | 0.99 | 41634 | 27347 | 57 | 51 | 368 |
| requests | 4 | mature | s0off | 3/3 | 39668 | 1.00 | 42191 | 27863 | 57 | 51 | 369 |
| requests | 8 | nonmoving | base | 3/3 | 24948 | 1.00 | 27229 | 9955 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s0 | 3/3 | 25436 | 1.02 | 27907 | 10306 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s0off | 3/3 | 24992 | 1.00 | 27297 | 9932 | 22 | 16 | 658 |
| requests | 8 | nursery | base | 3/3 | 28700 | 1.00 | 31964 | 13014 | 22 | 16 | 658 |
| requests | 8 | nursery | s0 | 3/3 | 27829 | 0.97 | 31080 | 12656 | 22 | 16 | 658 |
| requests | 8 | nursery | s0off | 3/3 | 27448 | 0.96 | 30646 | 12671 | 22 | 16 | 658 |
| requests | 8 | mature | base | 3/3 | 30571 | 1.00 | 33951 | 15492 | 22 | 16 | 658 |
| requests | 8 | mature | s0 | 3/3 | 29990 | 0.98 | 33081 | 15246 | 22 | 16 | 658 |
| requests | 8 | mature | s0off | 3/3 | 30981 | 1.01 | 34211 | 15998 | 22 | 16 | 658 |
| listsuffix | 2 | nonmoving | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s0 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s0off | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s0 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s0off | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s0 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s0off | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | base | 3/3 | 22446 | 1.00 | 55691 | 12073 | 53 | 0 | 300 |
| listsuffix | 4 | nonmoving | s0 | 3/3 | 24960 | 1.11 | 62704 | 14294 | 55 | 0 | 303 |
| listsuffix | 4 | nonmoving | s0off | 3/3 | 22821 | 1.02 | 56352 | 12357 | 53 | 0 | 302 |
| listsuffix | 4 | nursery | base | 3/3 | 51816 | 1.00 | 137931 | 42583 | 63 | 0 | 300 |
| listsuffix | 4 | nursery | s0 | 3/3 | 55308 | 1.07 | 147496 | 45498 | 64 | 0 | 300 |
| listsuffix | 4 | nursery | s0off | 2/3 | 52488 | 1.01 | 141841 | 42360 | 63 | 0 | 302 |
| listsuffix | 4 | mature | base | 3/3 | 52955 | 1.00 | 143149 | 42960 | 63 | 0 | 304 |
| listsuffix | 4 | mature | s0 | 3/3 | 56208 | 1.06 | 150336 | 46390 | 64 | 0 | 305 |
| listsuffix | 4 | mature | s0off | 3/3 | 54227 | 1.02 | 146579 | 44202 | 63 | 0 | 299 |
| listsuffix | 8 | nonmoving | base | 3/3 | 23018 | 1.00 | 39012 | 16247 | 15 | 12 | 488 |
| listsuffix | 8 | nonmoving | s0 | 3/3 | 23119 | 1.00 | 39308 | 16334 | 15 | 12 | 487 |
| listsuffix | 8 | nonmoving | s0off | 3/3 | 23254 | 1.01 | 39290 | 16563 | 15 | 12 | 488 |
| listsuffix | 8 | nursery | base | 3/3 | 31069 | 1.00 | 56651 | 23472 | 15 | 10 | 528 |
| listsuffix | 8 | nursery | s0 | 3/3 | 29713 | 0.96 | 56037 | 21879 | 15 | 10 | 528 |
| listsuffix | 8 | nursery | s0off | 3/3 | 30337 | 0.98 | 56814 | 22793 | 15 | 10 | 529 |
| listsuffix | 8 | mature | base | 3/3 | 31961 | 1.00 | 57871 | 24418 | 15 | 10 | 526 |
| listsuffix | 8 | mature | s0 | 3/3 | 31110 | 0.97 | 57694 | 23415 | 15 | 9 | 529 |
| listsuffix | 8 | mature | s0off | 3/3 | 32048 | 1.00 | 59818 | 24099 | 15 | 10 | 526 |
