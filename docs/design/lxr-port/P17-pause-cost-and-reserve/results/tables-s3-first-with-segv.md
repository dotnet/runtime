##### SUMMARY (base acc)
## Checksums
- binarytrees: one checksum 14985972
- listsuffix: one checksum 301262627042
- requests: one checksum 317308156512

## Failed or excluded runs: 43 of 243
- s3 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep0: exit=-11 ''
- s2 mature requests x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature requests x4.0 rep0: exit=-11 ''
- s3 mature requests x8.0 rep0: exit=-11 ''
- s3 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep0: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep1: exit=-11 ''
- s2 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep1: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep1: exit=-6 'Out of memory.'
- s3 mature requests x4.0 rep1: exit=-11 ''
- s3 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s3 nonmoving listsuffix x2.0 rep1: exit=-6 'Out of memory.'
- s2 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery requests x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature requests x2.0 rep2: exit=-6 'Out of memory.'
- s3 mature requests x2.0 rep2: exit=-11 ''
- s3 mature requests x4.0 rep2: exit=-11 ''
- s3 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- acc mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 mature listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s3 nursery listsuffix x2.0 rep2: exit=-6 'Out of memory.'
- s2 nonmoving listsuffix x2.0 rep2: exit=-6 'Out of memory.'

## Medians (ok runs / all runs)
| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | acc | 3/3 | 5741 | 1.00 | 9675 | 2813 | 67 | 0 | 145 |
| binarytrees | 2 | nonmoving | s2 | 3/3 | 4058 | 0.71 | 5891 | 1810 | 68 | 0 | 146 |
| binarytrees | 2 | nonmoving | s3 | 3/3 | 3557 | 0.62 | 5192 | 1500 | 67 | 0 | 134 |
| binarytrees | 2 | nursery | acc | 3/3 | 6847 | 1.00 | 12456 | 3976 | 67 | 0 | 147 |
| binarytrees | 2 | nursery | s2 | 3/3 | 5271 | 0.77 | 8158 | 3162 | 76 | 0 | 156 |
| binarytrees | 2 | nursery | s3 | 3/3 | 4458 | 0.65 | 7493 | 2549 | 82 | 0 | 143 |
| binarytrees | 2 | mature | acc | 3/3 | 6985 | 1.00 | 12527 | 4051 | 67 | 0 | 147 |
| binarytrees | 2 | mature | s2 | 3/3 | 5621 | 0.80 | 9056 | 3544 | 79 | 0 | 155 |
| binarytrees | 2 | mature | s3 | 3/3 | 4992 | 0.71 | 8836 | 3078 | 88 | 0 | 140 |
| binarytrees | 4 | nonmoving | acc | 3/3 | 4764 | 1.00 | 6426 | 1958 | 18 | 0 | 229 |
| binarytrees | 4 | nonmoving | s2 | 3/3 | 3317 | 0.70 | 4559 | 1441 | 18 | 0 | 227 |
| binarytrees | 4 | nonmoving | s3 | 3/3 | 2659 | 0.56 | 3830 | 1004 | 18 | 0 | 188 |
| binarytrees | 4 | nursery | acc | 3/3 | 5225 | 1.00 | 7106 | 2378 | 17 | 0 | 229 |
| binarytrees | 4 | nursery | s2 | 3/3 | 3764 | 0.72 | 5040 | 1887 | 20 | 0 | 219 |
| binarytrees | 4 | nursery | s3 | 3/3 | 2525 | 0.48 | 3604 | 964 | 19 | 0 | 186 |
| binarytrees | 4 | mature | acc | 3/3 | 5448 | 1.00 | 7692 | 2615 | 18 | 0 | 226 |
| binarytrees | 4 | mature | s2 | 3/3 | 3566 | 0.65 | 4774 | 1785 | 19 | 0 | 218 |
| binarytrees | 4 | mature | s3 | 3/3 | 2250 | 0.41 | 2500 | 630 | 18 | 0 | 111 |
| binarytrees | 8 | nonmoving | acc | 3/3 | 5135 | 1.00 | 5442 | 1434 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s2 | 3/3 | 2685 | 0.52 | 2785 | 898 | 7 | 0 | 314 |
| binarytrees | 8 | nonmoving | s3 | 3/3 | 2155 | 0.42 | 2256 | 483 | 7 | 0 | 243 |
| binarytrees | 8 | nursery | acc | 3/3 | 5438 | 1.00 | 5908 | 1812 | 7 | 0 | 314 |
| binarytrees | 8 | nursery | s2 | 3/3 | 3092 | 0.57 | 3403 | 1285 | 8 | 0 | 259 |
| binarytrees | 8 | nursery | s3 | 3/3 | 2777 | 0.51 | 3078 | 707 | 8 | 0 | 188 |
| binarytrees | 8 | mature | acc | 3/3 | 5546 | 1.00 | 6021 | 1860 | 7 | 0 | 314 |
| binarytrees | 8 | mature | s2 | 3/3 | 3080 | 0.56 | 3397 | 1267 | 8 | 0 | 259 |
| binarytrees | 8 | mature | s3 | 3/3 | 2141 | 0.39 | 2406 | 538 | 8 | 0 | 188 |
| requests | 2 | nonmoving | acc | 3/3 | 40167 | 1.00 | 42257 | 23156 | 534 | 0 | 184 |
| requests | 2 | nonmoving | s2 | 3/3 | 29699 | 0.74 | 31327 | 14906 | 534 | 0 | 184 |
| requests | 2 | nonmoving | s3 | 3/3 | 23408 | 0.58 | 24888 | 11387 | 510 | 0 | 149 |
| requests | 2 | nursery | acc | 3/3 | 47085 | 1.00 | 49439 | 30107 | 532 | 0 | 184 |
| requests | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | acc | 3/3 | 46867 | 1.00 | 49162 | 30237 | 535 | 0 | 184 |
| requests | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 4 | nonmoving | acc | 3/3 | 29574 | 1.00 | 31309 | 18031 | 57 | 53 | 340 |
| requests | 4 | nonmoving | s2 | 3/3 | 25789 | 0.87 | 27179 | 16018 | 57 | 51 | 370 |
| requests | 4 | nonmoving | s3 | 3/3 | 21936 | 0.74 | 23069 | 12516 | 57 | 51 | 295 |
| requests | 4 | nursery | acc | 3/3 | 33774 | 1.00 | 36007 | 21965 | 57 | 51 | 370 |
| requests | 4 | nursery | s2 | 3/3 | 14514 | 0.43 | 17910 | 7157 | 70 | 0 | 356 |
| requests | 4 | nursery | s3 | 3/3 | 11212 | 0.33 | 14852 | 4536 | 69 | 0 | 281 |
| requests | 4 | mature | acc | 3/3 | 39123 | 1.00 | 41427 | 27404 | 57 | 51 | 369 |
| requests | 4 | mature | s2 | 3/3 | 14510 | 0.37 | 18033 | 7467 | 67 | 0 | 380 |
| requests | 4 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| requests | 8 | nonmoving | acc | 3/3 | 27505 | 1.00 | 30376 | 11975 | 23 | 17 | 658 |
| requests | 8 | nonmoving | s2 | 3/3 | 15361 | 0.56 | 16783 | 7954 | 22 | 16 | 658 |
| requests | 8 | nonmoving | s3 | 3/3 | 16405 | 0.60 | 17797 | 8073 | 22 | 16 | 509 |
| requests | 8 | nursery | acc | 3/3 | 29662 | 1.00 | 32832 | 13722 | 22 | 16 | 658 |
| requests | 8 | nursery | s2 | 3/3 | 12720 | 0.43 | 16110 | 5647 | 25 | 0 | 658 |
| requests | 8 | nursery | s3 | 3/3 | 8801 | 0.30 | 11518 | 3156 | 25 | 0 | 508 |
| requests | 8 | mature | acc | 3/3 | 30897 | 1.00 | 34258 | 15741 | 22 | 16 | 658 |
| requests | 8 | mature | s2 | 3/3 | 11983 | 0.39 | 14860 | 5568 | 26 | 0 | 654 |
| requests | 8 | mature | s3 | 2/3 | 9808 | 0.32 | 12593 | 3506 | 26 | 0 | 494 |
| listsuffix | 2 | nonmoving | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nonmoving | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | nursery | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | acc | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s2 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 2 | mature | s3 | 0/3 | 0 | - | 0 | 0 | 0 | 0 | 0 |
| listsuffix | 4 | nonmoving | acc | 3/3 | 20914 | 1.00 | 52081 | 11366 | 54 | 0 | 301 |
| listsuffix | 4 | nonmoving | s2 | 3/3 | 13957 | 0.67 | 29275 | 6026 | 52 | 0 | 299 |
| listsuffix | 4 | nonmoving | s3 | 3/3 | 12118 | 0.58 | 25004 | 5798 | 52 | 0 | 268 |
| listsuffix | 4 | nursery | acc | 3/3 | 48672 | 1.00 | 132385 | 39696 | 63 | 0 | 304 |
| listsuffix | 4 | nursery | s2 | 3/3 | 36449 | 0.75 | 93254 | 27936 | 59 | 0 | 285 |
| listsuffix | 4 | nursery | s3 | 3/3 | 14035 | 0.29 | 29204 | 7448 | 53 | 0 | 238 |
| listsuffix | 4 | mature | acc | 3/3 | 51480 | 1.00 | 138433 | 42445 | 63 | 0 | 307 |
| listsuffix | 4 | mature | s2 | 3/3 | 40339 | 0.78 | 103287 | 31926 | 69 | 0 | 275 |
| listsuffix | 4 | mature | s3 | 3/3 | 36171 | 0.70 | 95944 | 28872 | 67 | 0 | 257 |
| listsuffix | 8 | nonmoving | acc | 3/3 | 21768 | 1.00 | 37215 | 15094 | 15 | 12 | 489 |
| listsuffix | 8 | nonmoving | s2 | 3/3 | 17607 | 0.81 | 27627 | 12063 | 15 | 12 | 493 |
| listsuffix | 8 | nonmoving | s3 | 3/3 | 16598 | 0.76 | 25976 | 10696 | 15 | 12 | 418 |
| listsuffix | 8 | nursery | acc | 3/3 | 29428 | 1.00 | 54877 | 22107 | 15 | 10 | 528 |
| listsuffix | 8 | nursery | s2 | 3/3 | 22346 | 0.76 | 58395 | 15758 | 18 | 0 | 521 |
| listsuffix | 8 | nursery | s3 | 3/3 | 15936 | 0.54 | 39385 | 10415 | 19 | 0 | 452 |
| listsuffix | 8 | mature | acc | 3/3 | 31405 | 1.00 | 57475 | 23933 | 15 | 10 | 528 |
| listsuffix | 8 | mature | s2 | 3/3 | 22297 | 0.71 | 58602 | 16241 | 21 | 0 | 519 |
| listsuffix | 8 | mature | s3 | 3/3 | 21024 | 0.67 | 56596 | 15350 | 26 | 0 | 448 |
##### S3 vs S2 elapsed
| workload | heap x | mode | base median | s3 median | ratio | per-repetition ratios |
|---|---|---|---|---|---|---|
| binarytrees | 2 | mature | 5621 | 4992 | 0.888 | 1.06, 0.89, 0.80 |
| binarytrees | 2 | nonmoving | 4058 | 3557 | 0.877 | 0.88, 0.89, 0.85 |
| binarytrees | 2 | nursery | 5271 | 4458 | 0.846 | 0.85, 0.60, 0.89 |
| binarytrees | 4 | mature | 3566 | 2250 | 0.631 | 0.77, 0.68, 0.63 |
| binarytrees | 4 | nonmoving | 3317 | 2659 | 0.802 | 0.83, 0.80, 0.77 |
| binarytrees | 4 | nursery | 3764 | 2525 | 0.671 | 0.68, 0.59, 0.73 |
| binarytrees | 8 | mature | 3080 | 2141 | 0.695 | 0.69, 0.71, 0.67 |
| binarytrees | 8 | nonmoving | 2685 | 2155 | 0.802 | 0.90, 0.78, 0.75 |
| binarytrees | 8 | nursery | 3092 | 2777 | 0.898 | 0.81, 0.93, 0.87 |
| listsuffix | 4 | mature | 40339 | 36171 | 0.897 | 0.95, 0.90, 0.80 |
| listsuffix | 4 | nonmoving | 13957 | 12118 | 0.868 | 0.91, 0.85, 0.88 |
| listsuffix | 4 | nursery | 36449 | 14035 | 0.385 | 0.33, 0.57, 0.46 |
| listsuffix | 8 | mature | 22297 | 21024 | 0.943 | 1.18, 0.73, 0.96 |
| listsuffix | 8 | nonmoving | 17607 | 16598 | 0.943 | 1.01, 0.94, 0.94 |
| listsuffix | 8 | nursery | 22346 | 15936 | 0.713 | 0.71, 0.83, 0.72 |
| requests | 2 | nonmoving | 29699 | 23408 | 0.788 | 0.79, 0.79, 0.77 |
| requests | 4 | nonmoving | 25789 | 21936 | 0.851 | 0.94, 0.90, 0.83 |
| requests | 4 | nursery | 14514 | 11212 | 0.772 | 0.72, 0.89, 0.76 |
| requests | 8 | mature | 11983 | 9808 | 0.818 | 0.81, 0.77 |
| requests | 8 | nonmoving | 15361 | 16405 | 1.068 | 1.13, 1.07, 0.92 |
| requests | 8 | nursery | 12720 | 8801 | 0.692 | 0.71, 0.69, 0.70 |

cells=21 geometric mean ratio=0.788 min=0.385 max=1.068
##### S3 vs ACC elapsed
| workload | heap x | mode | base median | s3 median | ratio | per-repetition ratios |
|---|---|---|---|---|---|---|
| binarytrees | 2 | mature | 6985 | 4992 | 0.715 | 0.73, 0.71, 0.66 |
| binarytrees | 2 | nonmoving | 5741 | 3557 | 0.620 | 0.60, 0.64, 0.61 |
| binarytrees | 2 | nursery | 6847 | 4458 | 0.651 | 0.65, 0.52, 0.66 |
| binarytrees | 4 | mature | 5448 | 2250 | 0.413 | 0.52, 0.41, 0.41 |
| binarytrees | 4 | nonmoving | 4764 | 2659 | 0.558 | 0.57, 0.56, 0.56 |
| binarytrees | 4 | nursery | 5225 | 2525 | 0.483 | 0.48, 0.42, 0.52 |
| binarytrees | 8 | mature | 5546 | 2141 | 0.386 | 0.38, 0.39, 0.39 |
| binarytrees | 8 | nonmoving | 5135 | 2155 | 0.420 | 0.46, 0.43, 0.39 |
| binarytrees | 8 | nursery | 5438 | 2777 | 0.511 | 0.47, 0.53, 0.51 |
| listsuffix | 4 | mature | 51480 | 36171 | 0.703 | 0.71, 0.65, 0.69 |
| listsuffix | 4 | nonmoving | 20914 | 12118 | 0.579 | 0.62, 0.54, 0.58 |
| listsuffix | 4 | nursery | 48672 | 14035 | 0.288 | 0.26, 0.29, 0.35 |
| listsuffix | 8 | mature | 31405 | 21024 | 0.669 | 0.89, 0.56, 0.67 |
| listsuffix | 8 | nonmoving | 21768 | 16598 | 0.762 | 0.80, 0.78, 0.76 |
| listsuffix | 8 | nursery | 29428 | 15936 | 0.542 | 0.53, 0.63, 0.55 |
| requests | 2 | nonmoving | 40167 | 23408 | 0.583 | 0.59, 0.58, 0.56 |
| requests | 4 | nonmoving | 29574 | 21936 | 0.742 | 0.78, 0.74, 0.74 |
| requests | 4 | nursery | 33774 | 11212 | 0.332 | 0.31, 0.38, 0.33 |
| requests | 8 | mature | 30897 | 9808 | 0.317 | 0.35, 0.30 |
| requests | 8 | nonmoving | 27505 | 16405 | 0.596 | 0.66, 0.60, 0.49 |
| requests | 8 | nursery | 29662 | 8801 | 0.297 | 0.36, 0.30, 0.31 |

cells=21 geometric mean ratio=0.509 min=0.288 max=0.762
##### S3 vs S2 rss
| workload | heap x | mode | base median | s3 median | ratio | per-repetition ratios |
|---|---|---|---|---|---|---|
| binarytrees | 2 | mature | 158836 | 143268 | 0.902 | 0.89, 0.94, 0.90 |
| binarytrees | 2 | nonmoving | 149564 | 137560 | 0.920 | 0.92, 0.88, 0.96 |
| binarytrees | 2 | nursery | 159848 | 146292 | 0.915 | 0.89, 0.93, 0.91 |
| binarytrees | 4 | mature | 222964 | 113616 | 0.510 | 0.85, 0.77, 0.51 |
| binarytrees | 4 | nonmoving | 232196 | 192556 | 0.829 | 0.85, 0.84, 0.82 |
| binarytrees | 4 | nursery | 223988 | 189992 | 0.848 | 0.91, 0.51, 0.82 |
| binarytrees | 8 | mature | 265196 | 192432 | 0.726 | 0.72, 0.73, 0.72 |
| binarytrees | 8 | nonmoving | 321204 | 248412 | 0.773 | 0.77, 0.77, 0.77 |
| binarytrees | 8 | nursery | 265332 | 192368 | 0.725 | 0.72, 0.73, 0.73 |
| listsuffix | 4 | mature | 281740 | 263368 | 0.935 | 0.95, 0.90, 0.96 |
| listsuffix | 4 | nonmoving | 306084 | 274892 | 0.898 | 0.91, 0.90, 0.88 |
| listsuffix | 4 | nursery | 292244 | 243688 | 0.834 | 0.83, 0.84, 0.84 |
| listsuffix | 8 | mature | 531124 | 458600 | 0.863 | 0.87, 0.87, 0.86 |
| listsuffix | 8 | nonmoving | 505224 | 427972 | 0.847 | 0.85, 0.87, 0.85 |
| listsuffix | 8 | nursery | 533476 | 463208 | 0.868 | 0.88, 0.86, 0.87 |
| requests | 2 | nonmoving | 188356 | 152684 | 0.811 | 0.81, 0.81, 0.81 |
| requests | 4 | nonmoving | 378516 | 302192 | 0.798 | 0.80, 0.79, 0.87 |
| requests | 4 | nursery | 364856 | 287536 | 0.788 | 0.79, 0.82, 0.79 |
| requests | 8 | mature | 669656 | 505434 | 0.755 | 0.76, 0.75 |
| requests | 8 | nonmoving | 674064 | 520780 | 0.773 | 0.77, 0.77, 0.77 |
| requests | 8 | nursery | 673676 | 520268 | 0.772 | 0.77, 0.77, 0.77 |

cells=21 geometric mean ratio=0.808 min=0.510 max=0.935
##### S3 vs ACC rss
| workload | heap x | mode | base median | s3 median | ratio | per-repetition ratios |
|---|---|---|---|---|---|---|
| binarytrees | 2 | mature | 150572 | 143268 | 0.951 | 0.94, 0.95, 0.95 |
| binarytrees | 2 | nonmoving | 148724 | 137560 | 0.925 | 0.92, 0.91, 0.97 |
| binarytrees | 2 | nursery | 150148 | 146292 | 0.974 | 0.95, 1.00, 0.94 |
| binarytrees | 4 | mature | 231352 | 113616 | 0.491 | 0.81, 0.49, 0.49 |
| binarytrees | 4 | nonmoving | 234548 | 192556 | 0.821 | 0.82, 0.84, 0.82 |
| binarytrees | 4 | nursery | 234372 | 189992 | 0.811 | 0.86, 0.49, 0.81 |
| binarytrees | 8 | mature | 321312 | 192432 | 0.599 | 0.60, 0.60, 0.60 |
| binarytrees | 8 | nonmoving | 321252 | 248412 | 0.773 | 0.77, 0.77, 0.77 |
| binarytrees | 8 | nursery | 321248 | 192368 | 0.599 | 0.60, 0.60, 0.60 |
| listsuffix | 4 | mature | 314440 | 263368 | 0.838 | 0.86, 0.81, 0.85 |
| listsuffix | 4 | nonmoving | 308604 | 274892 | 0.891 | 0.90, 0.89, 0.90 |
| listsuffix | 4 | nursery | 311728 | 243688 | 0.782 | 0.79, 0.77, 0.79 |
| listsuffix | 8 | mature | 540756 | 458600 | 0.848 | 0.85, 0.85, 0.84 |
| listsuffix | 8 | nonmoving | 501004 | 427972 | 0.854 | 0.86, 0.94, 0.85 |
| listsuffix | 8 | nursery | 540264 | 463208 | 0.857 | 0.87, 0.86, 0.86 |
| requests | 2 | nonmoving | 188264 | 152684 | 0.811 | 0.81, 0.81, 0.81 |
| requests | 4 | nonmoving | 348284 | 302192 | 0.868 | 0.87, 0.80, 0.87 |
| requests | 4 | nursery | 378676 | 287536 | 0.759 | 0.76, 0.79, 0.75 |
| requests | 8 | mature | 673960 | 505434 | 0.750 | 0.75, 0.75 |
| requests | 8 | nonmoving | 674236 | 520780 | 0.772 | 0.77, 0.77, 0.77 |
| requests | 8 | nursery | 674056 | 520268 | 0.772 | 0.77, 0.77, 0.77 |

cells=21 geometric mean ratio=0.788 min=0.491 max=0.974
##### S3 vs S2 gc_ms
| workload | heap x | mode | base median | s3 median | ratio | per-repetition ratios |
|---|---|---|---|---|---|---|
| binarytrees | 2 | mature | 3544 | 3078 | 0.869 | 1.16, 0.87, 0.74 |
| binarytrees | 2 | nonmoving | 1810 | 1500 | 0.829 | 0.84, 0.81, 0.80 |
| binarytrees | 2 | nursery | 3162 | 2549 | 0.806 | 0.81, 0.47, 0.84 |
| binarytrees | 4 | mature | 1785 | 630 | 0.353 | 0.64, 0.43, 0.33 |
| binarytrees | 4 | nonmoving | 1441 | 1004 | 0.697 | 0.72, 0.70, 0.65 |
| binarytrees | 4 | nursery | 1887 | 964 | 0.511 | 0.51, 0.34, 0.57 |
| binarytrees | 8 | mature | 1267 | 538 | 0.425 | 0.42, 0.43, 0.42 |
| binarytrees | 8 | nonmoving | 898 | 483 | 0.538 | 0.61, 0.53, 0.54 |
| binarytrees | 8 | nursery | 1285 | 707 | 0.550 | 0.49, 0.56, 0.55 |
| listsuffix | 4 | mature | 31926 | 28872 | 0.904 | 0.97, 0.90, 0.79 |
| listsuffix | 4 | nonmoving | 6026 | 5798 | 0.962 | 0.96, 0.91, 0.99 |
| listsuffix | 4 | nursery | 27936 | 7448 | 0.267 | 0.22, 0.45, 0.35 |
| listsuffix | 8 | mature | 16241 | 15350 | 0.945 | 1.24, 0.70, 0.98 |
| listsuffix | 8 | nonmoving | 12063 | 10696 | 0.887 | 0.95, 0.88, 0.87 |
| listsuffix | 8 | nursery | 15758 | 10415 | 0.661 | 0.65, 0.81, 0.64 |
| requests | 2 | nonmoving | 14906 | 11387 | 0.764 | 0.77, 0.76, 0.75 |
| requests | 4 | nonmoving | 16018 | 12516 | 0.781 | 0.88, 0.83, 0.75 |
| requests | 4 | nursery | 7157 | 4536 | 0.634 | 0.59, 0.68, 0.62 |
| requests | 8 | mature | 5568 | 3506 | 0.630 | 0.61, 0.62 |
| requests | 8 | nonmoving | 7954 | 8073 | 1.015 | 1.00, 1.02, 0.81 |
| requests | 8 | nursery | 5647 | 3156 | 0.559 | 0.50, 0.56, 0.54 |

cells=21 geometric mean ratio=0.659 min=0.267 max=1.015
