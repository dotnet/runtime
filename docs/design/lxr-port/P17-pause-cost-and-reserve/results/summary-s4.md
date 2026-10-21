## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 39 of 243
- s4 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s4 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s4 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s4 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s4 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s4 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s4 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s4 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s4 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s4 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- s4 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s4 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s4 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s4 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s4 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | acc | 3/3 | 5847 | 1.00 | 9816 | 2877 | 67 | 0 | 146 |
| binarytrees | 2 | nonmoving | s3 | 3/3 | 3438 | 0.59 | 5131 | 1477 | 68 | 0 | 134 |
| binarytrees | 2 | nonmoving | s4 | 3/3 | 3151 | 0.54 | 4768 | 1081 | 68 | 0 | 134 |
| binarytrees | 2 | nursery | acc | 3/3 | 6828 | 1.00 | 12238 | 3951 | 67 | 0 | 147 |
| binarytrees | 2 | nursery | s3 | 3/3 | 4491 | 0.66 | 7776 | 2552 | 86 | 0 | 143 |
| binarytrees | 2 | nursery | s4 | 3/3 | 3784 | 0.55 | 6406 | 1793 | 75 | 0 | 144 |
| binarytrees | 2 | mature | acc | 3/3 | 7074 | 1.00 | 12813 | 4081 | 68 | 0 | 147 |
| binarytrees | 2 | mature | s3 | 3/3 | 4405 | 0.62 | 7669 | 2572 | 81 | 0 | 141 |
| binarytrees | 2 | mature | s4 | 3/3 | 3992 | 0.56 | 7237 | 2065 | 84 | 0 | 143 |
| binarytrees | 4 | nonmoving | acc | 3/3 | 4815 | 1.00 | 6486 | 1991 | 18 | 0 | 229 |
| binarytrees | 4 | nonmoving | s3 | 3/3 | 2724 | 0.57 | 3973 | 1062 | 18 | 0 | 192 |
| binarytrees | 4 | nonmoving | s4 | 3/3 | 2417 | 0.50 | 3610 | 738 | 18 | 0 | 189 |
| binarytrees | 4 | nursery | acc | 3/3 | 5327 | 1.00 | 7111 | 2539 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s3 | 3/3 | 2517 | 0.47 | 3596 | 989 | 19 | 0 | 182 |
| binarytrees | 4 | nursery | s4 | 3/3 | 2350 | 0.44 | 3467 | 723 | 20 | 0 | 182 |
| binarytrees | 4 | mature | acc | 3/3 | 5443 | 1.00 | 7632 | 2613 | 18 | 0 | 227 |
| binarytrees | 4 | mature | s3 | 3/3 | 2712 | 0.50 | 3902 | 1075 | 19 | 0 | 185 |
| binarytrees | 4 | mature | s4 | 3/3 | 2282 | 0.42 | 3333 | 691 | 19 | 0 | 181 |
| binarytrees | 8 | nonmoving | acc | 3/3 | 5125 | 1.00 | 5425 | 1489 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s3 | 3/3 | 2123 | 0.41 | 2218 | 486 | 7 | 0 | 243 |
| binarytrees | 8 | nonmoving | s4 | 3/3 | 1895 | 0.37 | 1990 | 261 | 7 | 0 | 243 |
| binarytrees | 8 | nursery | acc | 3/3 | 5516 | 1.00 | 5997 | 1815 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s3 | 3/3 | 2101 | 0.38 | 2352 | 531 | 8 | 0 | 188 |
| binarytrees | 8 | nursery | s4 | 3/3 | 1942 | 0.35 | 2238 | 336 | 8 | 0 | 188 |
| binarytrees | 8 | mature | acc | 3/3 | 6445 | 1.00 | 6979 | 2323 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s3 | 3/3 | 2140 | 0.33 | 2413 | 540 | 8 | 0 | 188 |
| binarytrees | 8 | mature | s4 | 3/3 | 1934 | 0.30 | 2247 | 340 | 8 | 0 | 188 |
| requests | 2 | nonmoving | acc | 3/3 | 40647 | 1.00 | 42738 | 23578 | 535 | 0 | 184 |
| requests | 2 | nonmoving | s3 | 3/3 | 23457 | 0.58 | 24953 | 11488 | 532 | 0 | 149 |
| requests | 2 | nonmoving | s4 | 3/3 | 18693 | 0.46 | 20231 | 5400 | 516 | 0 | 149 |
| requests | 2 | nursery | acc | 3/3 | 47615 | 1.00 | 49971 | 30784 | 534 | 0 | 184 |
| requests | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | nursery | s4 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | acc | 3/3 | 47680 | 1.00 | 50024 | 30730 | 532 | 0 | 184 |
| requests | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | s4 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | acc | 3/3 | 30873 | 1.00 | 32407 | 18876 | 57 | 51 | 368 |
| requests | 4 | nonmoving | s3 | 3/3 | 22965 | 0.74 | 24111 | 13267 | 57 | 51 | 295 |
| requests | 4 | nonmoving | s4 | 3/3 | 20758 | 0.67 | 21483 | 10910 | 57 | 51 | 295 |
| requests | 4 | nursery | acc | 3/3 | 34840 | 1.00 | 37154 | 22711 | 57 | 51 | 368 |
| requests | 4 | nursery | s3 | 3/3 | 10277 | 0.29 | 13335 | 4069 | 69 | 0 | 281 |
| requests | 4 | nursery | s4 | 3/3 | 8448 | 0.24 | 11662 | 2104 | 69 | 0 | 281 |
| requests | 4 | mature | acc | 3/3 | 39397 | 1.00 | 41127 | 27488 | 57 | 51 | 367 |
| requests | 4 | mature | s3 | 3/3 | 10402 | 0.26 | 13972 | 4535 | 67 | 0 | 303 |
| requests | 4 | mature | s4 | 3/3 | 8575 | 0.22 | 11903 | 2546 | 66 | 0 | 305 |
| requests | 8 | nonmoving | acc | 3/3 | 26857 | 1.00 | 29316 | 11386 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s3 | 3/3 | 14819 | 0.55 | 16144 | 6710 | 22 | 16 | 509 |
| requests | 8 | nonmoving | s4 | 3/3 | 14919 | 0.56 | 16274 | 6361 | 22 | 16 | 508 |
| requests | 8 | nursery | acc | 3/3 | 32561 | 1.00 | 35814 | 16597 | 22 | 16 | 658 |
| requests | 8 | nursery | s3 | 3/3 | 9061 | 0.28 | 12326 | 3167 | 25 | 0 | 508 |
| requests | 8 | nursery | s4 | 3/3 | 7872 | 0.24 | 10972 | 1693 | 25 | 0 | 508 |
| requests | 8 | mature | acc | 3/3 | 33591 | 1.00 | 36991 | 17893 | 22 | 16 | 658 |
| requests | 8 | mature | s3 | 3/3 | 9073 | 0.27 | 11892 | 3305 | 26 | 0 | 502 |
| requests | 8 | mature | s4 | 3/3 | 7477 | 0.22 | 10187 | 1792 | 26 | 0 | 500 |
| listsuffix | 2 | nonmoving | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s4 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s4 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s4 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | acc | 3/3 | 20580 | 1.00 | 50892 | 11068 | 53 | 0 | 303 |
| listsuffix | 4 | nonmoving | s3 | 3/3 | 12216 | 0.59 | 25341 | 6022 | 52 | 0 | 268 |
| listsuffix | 4 | nonmoving | s4 | 3/3 | 11694 | 0.57 | 24417 | 5125 | 52 | 0 | 267 |
| listsuffix | 4 | nursery | acc | 3/3 | 50199 | 1.00 | 135375 | 41077 | 63 | 0 | 303 |
| listsuffix | 4 | nursery | s3 | 3/3 | 13395 | 0.27 | 27519 | 6953 | 53 | 0 | 238 |
| listsuffix | 4 | nursery | s4 | 3/3 | 14358 | 0.29 | 31679 | 7282 | 54 | 0 | 258 |
| listsuffix | 4 | mature | acc | 3/3 | 51609 | 1.00 | 138463 | 42701 | 63 | 0 | 300 |
| listsuffix | 4 | mature | s3 | 3/3 | 26304 | 0.51 | 64021 | 19184 | 65 | 0 | 257 |
| listsuffix | 4 | mature | s4 | 3/3 | 16535 | 0.32 | 37208 | 9617 | 64 | 0 | 242 |
| listsuffix | 8 | nonmoving | acc | 3/3 | 21815 | 1.00 | 37383 | 15040 | 15 | 12 | 491 |
| listsuffix | 8 | nonmoving | s3 | 3/3 | 16545 | 0.76 | 24797 | 10685 | 15 | 12 | 419 |
| listsuffix | 8 | nonmoving | s4 | 3/3 | 17151 | 0.79 | 28781 | 10819 | 15 | 10 | 464 |
| listsuffix | 8 | nursery | acc | 3/3 | 28447 | 1.00 | 53954 | 21197 | 15 | 9 | 528 |
| listsuffix | 8 | nursery | s3 | 3/3 | 17021 | 0.60 | 45039 | 11732 | 19 | 0 | 454 |
| listsuffix | 8 | nursery | s4 | 3/3 | 16853 | 0.59 | 45809 | 11534 | 17 | 0 | 454 |
| listsuffix | 8 | mature | acc | 3/3 | 29886 | 1.00 | 54446 | 22694 | 15 | 10 | 528 |
| listsuffix | 8 | mature | s3 | 3/3 | 23970 | 0.80 | 57900 | 17941 | 35 | 0 | 447 |
| listsuffix | 8 | mature | s4 | 3/3 | 17366 | 0.58 | 47749 | 11734 | 19 | 0 | 455 |
