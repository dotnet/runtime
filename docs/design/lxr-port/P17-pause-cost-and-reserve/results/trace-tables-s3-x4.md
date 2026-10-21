## Pause time per kind

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S2: n, median, max ms | S3: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 46, 84, 259 | 48, 57, 226 | 48, 33, 212 |
| binarytrees | non-moving | InitialMark | 3, 286, 291 | 3, 220, 234 | 3, 221, 223 |
| binarytrees | non-moving | FinalMark | 3, 141, 164 | 3, 58, 94 | 3, 46, 62 |
| binarytrees | nursery copy | RefCount | 48, 108, 356 | 55, 73, 111 | 54, 31, 135 |
| binarytrees | nursery copy | InitialMark | 3, 313, 331 | - | 2, 246, 293 |
| binarytrees | nursery copy | FinalMark | 3, 84, 143 | - | 2, 111, 126 |
| requests | non-moving | RefCount | 11, 103, 184 | 12, 68, 87 | 12, 44, 65 |
| requests | non-moving | InitialMark | 3, 180, 181 | 2, 177, 179 | 3, 138, 145 |
| requests | non-moving | FinalMark | 2, 176, 178 | 2, 164, 168 | 3, 153, 161 |
| requests | non-moving | Full | 157, 313, 615 | 158, 271, 341 | 156, 229, 282 |
| requests | nursery copy | RefCount | 11, 140, 283 | 189, 95, 158 | 189, 53, 110 |
| requests | nursery copy | InitialMark | 4, 207, 271 | 9, 167, 192 | 9, 123, 161 |
| requests | nursery copy | FinalMark | 3, 236, 258 | 9, 124, 147 | 9, 63, 95 |
| requests | nursery copy | Full | 156, 393, 497 | - | - |
| listsuffix | non-moving | RefCount | 178, 356, 711 | 163, 65, 425 | 156, 61, 412 |
| listsuffix | non-moving | InitialMark | 4, 516, 547 | 3, 452, 473 | 3, 429, 450 |
| listsuffix | non-moving | FinalMark | 1, 269, 269 | 4, 207, 228 | 3, 126, 222 |
| listsuffix | nursery copy | RefCount | 187, 562, 1151 | 214, 438, 580 | 166, 345, 671 |
| listsuffix | nursery copy | InitialMark | 4, 499, 740 | 3, 423, 444 | 3, 394, 411 |
| listsuffix | nursery copy | FinalMark | - | 4, 155, 377 | 6, 213, 318 |

## Phase shares of the RefCount pause

| workload | mode | build | RefCount pauses | median pause ms | reclaim_nursery % of pause | release_epoch % of pause | sweep_blocks % of pause | scan_lease_finalizers % of pause | scan_heap_statistics % of pause | retire_sources % of pause | root_capture % of pause |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | s2 | 48 | 57 | 35.1 | 53.7 | 18.5 | 22.3 | 2.0 | 0.0 | 22.7 |
| binarytrees | non-moving | s3 | 48 | 33 | 0.0 | 29.1 | 29.1 | 31.1 | 4.9 | 0.0 | 31.6 |
| binarytrees | nursery copy | s2 | 55 | 73 | 27.7 | 58.4 | 14.9 | 29.7 | 1.8 | 15.8 | 30.0 |
| binarytrees | nursery copy | s3 | 54 | 31 | 0.0 | 33.1 | 31.4 | 32.7 | 7.8 | 1.6 | 33.3 |
| requests | non-moving | s2 | 12 | 68 | 35.2 | 55.4 | 20.2 | 24.4 | 5.9 | 0.0 | 24.8 |
| requests | non-moving | s3 | 12 | 44 | 0.0 | 29.9 | 29.9 | 28.2 | 21.0 | 0.0 | 28.8 |
| requests | nursery copy | s2 | 189 | 95 | 19.3 | 54.6 | 19.3 | 25.4 | 5.2 | 16.0 | 25.6 |
| requests | nursery copy | s3 | 189 | 53 | 0.0 | 36.1 | 34.9 | 21.7 | 16.8 | 1.2 | 22.2 |
| listsuffix | non-moving | s2 | 163 | 65 | 7.9 | 10.1 | 2.2 | 9.0 | 4.4 | 0.0 | 9.2 |
| listsuffix | non-moving | s3 | 156 | 61 | 0.0 | 2.8 | 2.8 | 11.8 | 11.2 | 0.0 | 12.0 |
| listsuffix | nursery copy | s2 | 214 | 438 | 2.0 | 5.2 | 0.8 | 4.4 | 1.4 | 2.4 | 4.5 |
| listsuffix | nursery copy | s3 | 166 | 345 | 0.0 | 1.2 | 1.0 | 3.8 | 4.4 | 0.2 | 3.9 |

## Median time of single phases in RefCount pauses (ms)

| workload | mode | build | reclaim_nursery median ms | scan_lease_finalizers median ms | scan_heap_statistics median ms | release_epoch median ms |
|---|---|---|---|---|---|---|
| binarytrees | non-moving | s2 | 24.1 | 15.1 | 1.2 | 37.0 |
| binarytrees | non-moving | s3 | 0.0 | 13.3 | 1.9 | 13.0 |
| binarytrees | nursery copy | s2 | 21.2 | 22.5 | 1.3 | 43.9 |
| binarytrees | nursery copy | s3 | 0.0 | 12.0 | 2.6 | 11.6 |
| requests | non-moving | s2 | 24.8 | 17.5 | 4.3 | 38.3 |
| requests | non-moving | s3 | 0.0 | 13.7 | 10.4 | 14.0 |
| requests | nursery copy | s2 | 18.7 | 24.5 | 5.3 | 52.6 |
| requests | nursery copy | s3 | 0.0 | 11.7 | 9.2 | 19.4 |
| listsuffix | non-moving | s2 | 10.1 | 11.4 | 5.6 | 12.9 |
| listsuffix | non-moving | s3 | 0.0 | 11.5 | 10.9 | 2.7 |
| listsuffix | nursery copy | s2 | 9.4 | 17.8 | 5.5 | 22.6 |
| listsuffix | nursery copy | s3 | 0.0 | 11.0 | 12.9 | 2.8 |

## Native plane commit size and managed committed bytes

| workload | heap MiB | mode | S2 native planes max (MiB) | S3 native planes max (MiB) | saved (MiB) | S2 managed committed max (MiB) | S3 managed committed max (MiB) |
|---|---|---|---|---|---|---|---|
| binarytrees | 40 | non-moving | 244.7 | 212.0 | 32.6 | 32.0 | 32.0 |
| binarytrees | 40 | nursery copy | 208.7 | 212.0 | -3.4 | 32.0 | 32.0 |
| requests | 84 | non-moving | 531.6 | 456.3 | 75.3 | 74.3 | 74.3 |
| requests | 84 | nursery copy | 531.6 | 456.3 | 75.3 | 74.3 | 74.3 |
| listsuffix | 40 | non-moving | 244.7 | 212.0 | 32.6 | 32.0 | 32.0 |
| listsuffix | 40 | nursery copy | 244.7 | 212.0 | 32.6 | 32.0 | 32.0 |
