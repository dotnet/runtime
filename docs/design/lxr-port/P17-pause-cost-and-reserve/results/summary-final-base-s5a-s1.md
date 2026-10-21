## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 39 of 243
- s5e nursery requests x2.0 rep0: exit=timeout ''
- s1d mature requests x2.0 rep0: exit=timeout ''
- s1d nursery requests x2.0 rep0: exit=timeout ''
- s5e mature requests x2.0 rep0: exit=timeout ''
- s5e mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s1d nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s1d mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s1d nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5e nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s5e nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s1d nursery requests x2.0 rep1: exit=timeout ''
- s5e mature requests x2.0 rep1: exit=timeout ''
- s5e nursery requests x2.0 rep1: exit=timeout ''
- s1d mature requests x2.0 rep1: exit=timeout ''
- s1d nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s1d nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s1d mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5e nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5e mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5e nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s5e mature requests x2.0 rep2: exit=timeout ''
- s1d nursery requests x2.0 rep2: exit=timeout ''
- s5e nursery requests x2.0 rep2: exit=timeout ''
- s1d mature requests x2.0 rep2: exit=timeout ''
- s5e nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s1d mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s1d nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- base nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s5e mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s5e nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s1d nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | base | 3/3 | 6199 | 1.00 | 10838 | 3050 | 68 | 0 | 146 |
| binarytrees | 2 | nonmoving | s1d | 3/3 | 5994 | 0.97 | 8761 | 2984 | 68 | 0 | 147 |
| binarytrees | 2 | nonmoving | s5e | 3/3 | 6227 | 1.00 | 10911 | 3193 | 68 | 0 | 152 |
| binarytrees | 2 | nursery | base | 3/3 | 7813 | 1.00 | 13896 | 4496 | 68 | 0 | 146 |
| binarytrees | 2 | nursery | s1d | 3/3 | 7594 | 0.97 | 12761 | 4953 | 89 | 0 | 140 |
| binarytrees | 2 | nursery | s5e | 3/3 | 8593 | 1.10 | 16562 | 5850 | 89 | 0 | 138 |
| binarytrees | 2 | mature | base | 3/3 | 7697 | 1.00 | 14114 | 4457 | 68 | 0 | 146 |
| binarytrees | 2 | mature | s1d | 3/3 | 8469 | 1.10 | 13859 | 5680 | 89 | 0 | 140 |
| binarytrees | 2 | mature | s5e | 3/3 | 10291 | 1.34 | 18994 | 7106 | 93 | 0 | 138 |
| binarytrees | 4 | nonmoving | base | 3/3 | 5418 | 1.00 | 7099 | 2103 | 17 | 0 | 229 |
| binarytrees | 4 | nonmoving | s1d | 3/3 | 4528 | 0.84 | 6130 | 2321 | 18 | 0 | 228 |
| binarytrees | 4 | nonmoving | s5e | 3/3 | 4727 | 0.87 | 6760 | 2367 | 18 | 0 | 229 |
| binarytrees | 4 | nursery | base | 3/3 | 5703 | 1.00 | 7776 | 2617 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s1d | 3/3 | 5083 | 0.89 | 6912 | 2833 | 21 | 0 | 222 |
| binarytrees | 4 | nursery | s5e | 3/3 | 4959 | 0.87 | 6463 | 2631 | 19 | 0 | 198 |
| binarytrees | 4 | mature | base | 3/3 | 6037 | 1.00 | 8221 | 2850 | 17 | 0 | 229 |
| binarytrees | 4 | mature | s1d | 3/3 | 4822 | 0.80 | 6295 | 2729 | 20 | 0 | 206 |
| binarytrees | 4 | mature | s5e | 3/3 | 4806 | 0.80 | 5517 | 2366 | 19 | 0 | 144 |
| binarytrees | 8 | nonmoving | base | 3/3 | 5430 | 1.00 | 5779 | 1493 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s1d | 3/3 | 3622 | 0.67 | 3773 | 1530 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s5e | 3/3 | 3532 | 0.65 | 3829 | 1531 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | base | 3/3 | 5809 | 1.00 | 6353 | 1906 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s1d | 3/3 | 3862 | 0.66 | 4350 | 1854 | 8 | 0 | 259 |
| binarytrees | 8 | nursery | s5e | 3/3 | 4038 | 0.70 | 5008 | 2019 | 8 | 0 | 259 |
| binarytrees | 8 | mature | base | 3/3 | 6000 | 1.00 | 6510 | 1962 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s1d | 3/3 | 3841 | 0.64 | 4344 | 1874 | 8 | 0 | 259 |
| binarytrees | 8 | mature | s5e | 3/3 | 4041 | 0.67 | 5029 | 2073 | 8 | 0 | 259 |
| requests | 2 | nonmoving | base | 3/3 | 40670 | 1.00 | 42736 | 23775 | 532 | 0 | 184 |
| requests | 2 | nonmoving | s1d | 3/3 | 43048 | 1.06 | 45187 | 24477 | 530 | 0 | 184 |
| requests | 2 | nonmoving | s5e | 3/3 | 41571 | 1.02 | 43723 | 24315 | 528 | 0 | 184 |
| requests | 2 | nursery | base | 3/3 | 48551 | 1.00 | 50933 | 31588 | 530 | 0 | 184 |
| requests | 2 | nursery | s1d | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | nursery | s5e | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | base | 3/3 | 49132 | 1.00 | 51542 | 31981 | 537 | 0 | 184 |
| requests | 2 | mature | s1d | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | s5e | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | base | 3/3 | 31671 | 1.00 | 33694 | 19165 | 57 | 51 | 369 |
| requests | 4 | nonmoving | s1d | 3/3 | 29116 | 0.92 | 30114 | 18471 | 57 | 53 | 340 |
| requests | 4 | nonmoving | s5e | 3/3 | 28551 | 0.90 | 30305 | 18115 | 57 | 51 | 370 |
| requests | 4 | nursery | base | 3/3 | 35261 | 1.00 | 37265 | 23203 | 57 | 52 | 368 |
| requests | 4 | nursery | s1d | 3/3 | 17149 | 0.49 | 21051 | 9127 | 70 | 0 | 356 |
| requests | 4 | nursery | s5e | 3/3 | 18209 | 0.52 | 22709 | 9789 | 69 | 0 | 361 |
| requests | 4 | mature | base | 3/3 | 39808 | 1.00 | 42215 | 27655 | 57 | 51 | 369 |
| requests | 4 | mature | s1d | 3/3 | 17250 | 0.43 | 21256 | 9387 | 67 | 0 | 382 |
| requests | 4 | mature | s5e | 3/3 | 17987 | 0.45 | 22623 | 10045 | 67 | 0 | 383 |
| requests | 8 | nonmoving | base | 3/3 | 25786 | 1.00 | 28263 | 10723 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s1d | 3/3 | 19595 | 0.76 | 21281 | 10667 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s5e | 3/3 | 19492 | 0.76 | 21753 | 10808 | 22 | 16 | 658 |
| requests | 8 | nursery | base | 3/3 | 28520 | 1.00 | 32013 | 13607 | 22 | 16 | 658 |
| requests | 8 | nursery | s1d | 3/3 | 14705 | 0.52 | 18475 | 7231 | 26 | 0 | 657 |
| requests | 8 | nursery | s5e | 3/3 | 16281 | 0.57 | 21080 | 7814 | 26 | 0 | 657 |
| requests | 8 | mature | base | 3/3 | 31941 | 1.00 | 35262 | 16717 | 22 | 16 | 658 |
| requests | 8 | mature | s1d | 3/3 | 15076 | 0.47 | 18515 | 7328 | 26 | 0 | 654 |
| requests | 8 | mature | s5e | 3/3 | 15387 | 0.48 | 20050 | 7886 | 26 | 0 | 650 |
| listsuffix | 2 | nonmoving | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s1d | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s5e | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s1d | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s5e | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | base | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s1d | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s5e | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | base | 3/3 | 22477 | 1.00 | 59139 | 13127 | 55 | 0 | 301 |
| listsuffix | 4 | nonmoving | s1d | 3/3 | 18649 | 0.83 | 43564 | 9906 | 54 | 0 | 302 |
| listsuffix | 4 | nonmoving | s5e | 3/3 | 21197 | 0.94 | 53867 | 11478 | 53 | 0 | 297 |
| listsuffix | 4 | nursery | base | 3/3 | 48884 | 1.00 | 133380 | 39204 | 62 | 0 | 299 |
| listsuffix | 4 | nursery | s1d | 3/3 | 34307 | 0.70 | 66143 | 20493 | 265 | 0 | 275 |
| listsuffix | 4 | nursery | s5e | 3/3 | 39638 | 0.81 | 82122 | 25214 | 258 | 0 | 271 |
| listsuffix | 4 | mature | base | 3/3 | 49832 | 1.00 | 135244 | 40543 | 63 | 0 | 300 |
| listsuffix | 4 | mature | s1d | 3/3 | 30151 | 0.61 | 63876 | 18675 | 202 | 0 | 245 |
| listsuffix | 4 | mature | s5e | 3/3 | 42150 | 0.85 | 99389 | 29606 | 213 | 0 | 245 |
| listsuffix | 8 | nonmoving | base | 3/3 | 22907 | 1.00 | 39376 | 15781 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | s1d | 3/3 | 21670 | 0.95 | 35352 | 15699 | 15 | 12 | 492 |
| listsuffix | 8 | nonmoving | s5e | 3/3 | 23294 | 1.02 | 41815 | 17159 | 15 | 12 | 491 |
| listsuffix | 8 | nursery | base | 3/3 | 30671 | 1.00 | 56703 | 22951 | 15 | 10 | 533 |
| listsuffix | 8 | nursery | s1d | 3/3 | 34021 | 1.11 | 86668 | 26709 | 30 | 0 | 506 |
| listsuffix | 8 | nursery | s5e | 3/3 | 38227 | 1.25 | 104890 | 29947 | 30 | 0 | 509 |
| listsuffix | 8 | mature | base | 3/3 | 31671 | 1.00 | 57295 | 24314 | 15 | 10 | 529 |
| listsuffix | 8 | mature | s1d | 3/3 | 33181 | 1.05 | 86455 | 25664 | 34 | 0 | 505 |
| listsuffix | 8 | mature | s5e | 3/3 | 38509 | 1.22 | 106986 | 30355 | 35 | 0 | 506 |
