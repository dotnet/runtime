| workload | heap x | mode | wall ms acc | wall ms s5b | wall ms s6 | s6/acc wall (repetitions) | coll. acc | coll. s5b | coll. s6 | gc ms acc | gc ms s5b | gc ms s6 | RSS MB acc | RSS MB s5b | RSS MB s6 | hiccup max ms acc | hiccup max ms s5b | hiccup max ms s6 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 5905 (3/3) | 3259 (3/3) | 2817 (3/3) | 0.48 (0.46, 0.48, 0.47) | 67/0 | 68/0 | 68/0 | 2902 | 1135 | 751 | 147 | 134 | 140 | 229 | 188 | 245 |
| binarytrees | 2 | nursery | 7099 (3/3) | 4409 (3/3) | 3697 (3/3) | 0.52 (0.48, 0.45, 0.57) | 67/0 | 80/0 | 81/0 | 4062 | 2217 | 1763 | 146 | 143 | 144 | 288 | 309 | 292 |
| binarytrees | 2 | mature | 7342 (3/3) | 4449 (3/3) | 3857 (3/3) | 0.53 (0.42, 0.52, 0.54) | 67/0 | 82/0 | 75/0 | 4179 | 2403 | 1813 | 146 | 144 | 144 | 286 | 320 | 326 |
| binarytrees | 4 | nonmoving | 4779 (3/3) | 2474 (3/3) | 2228 (3/3) | 0.47 (0.47, 0.45, 0.52) | 17/0 | 18/0 | 18/0 | 1840 | 733 | 500 | 229 | 188 | 187 | 303 | 223 | 212 |
| binarytrees | 4 | nursery | 5708 (3/3) | 2540 (3/3) | 2079 (3/3) | 0.36 (0.39, 0.38, 0.32) | 17/0 | 19/0 | 18/0 | 2635 | 740 | 183 | 229 | 182 | 111 | 449 | 212 | 45 |
| binarytrees | 4 | mature | 5431 (3/3) | 2157 (3/3) | 2285 (3/3) | 0.42 (0.38, 0.42, 0.42) | 18/0 | 18/0 | 19/0 | 2633 | 432 | 526 | 229 | 111 | 185 | 432 | 62 | 260 |
| binarytrees | 8 | nonmoving | 5381 (3/3) | 1858 (3/3) | 1717 (3/3) | 0.32 (0.33, 0.31, 0.34) | 7/0 | 7/0 | 7/0 | 1508 | 258 | 51 | 314 | 243 | 243 | 274 | 47 | 16 |
| binarytrees | 8 | nursery | 5563 (3/3) | 1947 (3/3) | 1786 (3/3) | 0.32 (0.32, 0.32, 0.31) | 7/0 | 8/0 | 8/0 | 1846 | 334 | 164 | 314 | 188 | 188 | 313 | 84 | 69 |
| binarytrees | 8 | mature | 5702 (3/3) | 2026 (3/3) | 1825 (3/3) | 0.32 (0.32, 0.32, 0.34) | 7/0 | 8/0 | 8/0 | 1848 | 373 | 165 | 314 | 188 | 188 | 327 | 95 | 70 |
| requests | 2 | nonmoving | 38739 (3/3) | 19680 (3/3) | 14578 (3/3) | 0.38 (0.40, 0.36, 0.39) | 538/0 | 534/0 | 514/0 | 22400 | 5745 | 1603 | 184 | 149 | 149 | 133 | 35 | 27 |
| requests | 2 | nursery | 47253 (3/3) | fail (0/3) | fail (0/3) | - | 533/0 | fail/fail | fail/fail | 30636 | fail | fail | 184 | fail | fail | 208 | fail | fail |
| requests | 2 | mature | 47830 (3/3) | fail (0/3) | fail (0/3) | - | 536/0 | fail/fail | fail/fail | 30881 | fail | fail | 184 | fail | fail | 211 | fail | fail |
| requests | 4 | nonmoving | 30652 (3/3) | 20813 (3/3) | 19518 (3/3) | 0.64 (0.67, 0.64, 0.63) | 57/51 | 57/51 | 57/51 | 18721 | 10935 | 9606 | 368 | 294 | 296 | 386 | 257 | 239 |
| requests | 4 | nursery | 33842 (3/3) | 8626 (3/3) | 7750 (3/3) | 0.23 (0.22, 0.23, 0.23) | 57/51 | 70/0 | 69/0 | 22175 | 2116 | 1268 | 369 | 280 | 281 | 481 | 97 | 80 |
| requests | 4 | mature | 39811 (3/3) | 8875 (3/3) | 7793 (3/3) | 0.20 (0.20, 0.20, 0.19) | 57/51 | 66/0 | 67/0 | 27825 | 2571 | 1791 | 368 | 302 | 298 | 595 | 271 | 255 |
| requests | 8 | nonmoving | 25934 (3/3) | 13365 (3/3) | 12584 (3/3) | 0.49 (0.48, 0.49, 0.51) | 22/16 | 22/16 | 22/16 | 10898 | 5287 | 4462 | 658 | 509 | 509 | 845 | 344 | 356 |
| requests | 8 | nursery | 28254 (3/3) | 7897 (3/3) | 7368 (3/3) | 0.26 (0.26, 0.25, 0.27) | 22/16 | 25/0 | 25/0 | 13278 | 1755 | 1191 | 658 | 508 | 508 | 1027 | 250 | 233 |
| requests | 8 | mature | 31472 (3/3) | 7387 (3/3) | 7335 (3/3) | 0.23 (0.24, 0.23, 0.22) | 22/16 | 26/0 | 26/0 | 16442 | 1780 | 1206 | 658 | 498 | 502 | 1206 | 223 | 215 |
| listsuffix | 2 | nonmoving | fail (0/3) | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 2 | nursery | fail (0/3) | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 2 | mature | fail (0/3) | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 4 | nonmoving | 21348 (3/3) | 13603 (3/3) | 11784 (3/3) | 0.55 (0.55, 0.55, 0.54) | 53/0 | 53/0 | 53/0 | 11861 | 6673 | 5249 | 302 | 269 | 267 | 689 | 612 | 534 |
| listsuffix | 4 | nursery | 49047 (3/3) | 28845 (3/3) | 19658 (3/3) | 0.40 (0.23, 0.66, 0.40) | 63/0 | 55/0 | 56/0 | 39907 | 22393 | 12853 | 303 | 256 | 256 | 948 | 646 | 660 |
| listsuffix | 4 | mature | 53391 (3/3) | 21200 (3/3) | 31949 (3/3) | 0.60 (0.65, 0.25, 0.60) | 63/0 | 67/0 | 65/0 | 44465 | 14250 | 24393 | 305 | 251 | 263 | 886 | 482 | 641 |
| listsuffix | 8 | nonmoving | 26788 (3/3) | 20092 (3/3) | 17548 (3/3) | 0.66 (0.66, 0.64, 0.69) | 15/12 | 15/10 | 15/10 | 19132 | 12584 | 10906 | 490 | 462 | 460 | 1691 | 1115 | 924 |
| listsuffix | 8 | nursery | 32745 (3/3) | 18633 (3/3) | 16683 (3/3) | 0.51 (0.44, 0.51, 0.57) | 15/9 | 17/0 | 17/0 | 24229 | 12695 | 11526 | 529 | 456 | 458 | 2121 | 1023 | 1133 |
| listsuffix | 8 | mature | 34672 (3/3) | 19644 (3/3) | 18667 (3/3) | 0.54 (0.53, 0.61, 0.57) | 15/9 | 19/0 | 19/0 | 26442 | 14092 | 13252 | 532 | 452 | 455 | 2317 | 1094 | 1059 |

cells compared=22; s6/acc geometric mean: wall 0.405 (min 0.196, max 0.655), gc ms 0.193, peak RSS 0.797 (min 0.485, max 0.987); all repetitions faster in 22 cells, all slower in 0, mixed (noise) in 0

failed or timed-out runs: 39
- s5b requests x2 nursery rep0: exit=-6 'Out of memory.'
- s5b requests x2 nursery rep1: exit=-6 'Out of memory.'
- s5b requests x2 nursery rep2: exit=-6 'Out of memory.'
- s6 requests x2 nursery rep0: exit=-6 'Out of memory.'
- s6 requests x2 nursery rep1: exit=-6 'Out of memory.'
- s6 requests x2 nursery rep2: exit=-6 'Out of memory.'
- s5b requests x2 mature rep0: exit=-6 'Out of memory.'
- s5b requests x2 mature rep1: exit=-6 'Out of memory.'
- s5b requests x2 mature rep2: exit=-6 'Out of memory.'
- s6 requests x2 mature rep0: exit=-6 'Out of memory.'
- s6 requests x2 mature rep1: exit=-6 'Out of memory.'
- s6 requests x2 mature rep2: exit=-6 'Out of memory.'
- acc listsuffix x2 nonmoving rep0: exit=-6 'Out of memory.'
- acc listsuffix x2 nonmoving rep1: exit=-6 'Out of memory.'
- acc listsuffix x2 nonmoving rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep2: exit=-6 'Out of memory.'
- s6 listsuffix x2 nonmoving rep0: exit=-6 'Out of memory.'
- s6 listsuffix x2 nonmoving rep1: exit=-6 'Out of memory.'
- s6 listsuffix x2 nonmoving rep2: exit=-6 'Out of memory.'
- acc listsuffix x2 nursery rep0: exit=-6 'Out of memory.'
- acc listsuffix x2 nursery rep1: exit=-6 'Out of memory.'
- acc listsuffix x2 nursery rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep2: exit=-6 'Out of memory.'
- s6 listsuffix x2 nursery rep0: exit=-6 'Out of memory.'
- s6 listsuffix x2 nursery rep1: exit=-6 'Out of memory.'
- s6 listsuffix x2 nursery rep2: exit=-6 'Out of memory.'
- acc listsuffix x2 mature rep0: exit=-6 'Out of memory.'
- acc listsuffix x2 mature rep1: exit=-6 'Out of memory.'
- acc listsuffix x2 mature rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep2: exit=-6 'Out of memory.'
- s6 listsuffix x2 mature rep0: exit=-6 'Out of memory.'
- s6 listsuffix x2 mature rep1: exit=-6 'Out of memory.'
- s6 listsuffix x2 mature rep2: exit=-6 'Out of memory.'
