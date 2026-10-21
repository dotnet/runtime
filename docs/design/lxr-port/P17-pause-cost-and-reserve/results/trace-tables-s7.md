| workload | mode | int wall ms | int GC ms | int peak RSS MiB | s7 wall ms | s7 GC ms | s7 peak RSS MiB | checksums | failed runs |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | 2764 (2862, 2666) | 538 (581, 496) | 192 (193, 191) | 2360 (2255, 2465) | 295 (268, 322) | 145 (144, 146) | equal | none |
| binarytrees | nursery copy | 2617 (2540, 2695) | 596 (550, 642) | 198 (197, 200) | 2331 (2340, 2322) | 468 (481, 456) | 131 (130, 131) | equal | none |
| requests | non-moving | 20462 (20735, 20189) | 9456 (9709, 9203) | 295 (295, 295) | 16068 (15919, 16217) | 6063 (6015, 6111) | 177 (178, 176) | equal | none |
| requests | nursery copy | 8819 (8783, 8855) | 1296 (1303, 1289) | 284 (284, 284) | 7618 (7596, 7640) | 1025 (1013, 1037) | 164 (166, 162) | equal | none |
| listsuffix | non-moving | 11925 (11296, 12554) | 4723 (4293, 5153) | 266 (268, 265) | 10237 (9983, 10491) | 3650 (3574, 3725) | 213 (210, 216) | equal | none |
| listsuffix | nursery copy | 12639 (11633, 13645) | 6040 (5077, 7003) | 253 (255, 251) | 10790 (10263, 11317) | 4703 (4109, 5297) | 182 (165, 200) | equal | none |
| pinheavy | non-moving | 4120 (4039, 4256, 4065) | 610 (591, 628, 610) | 204 (204, 204, 204) | 3932 (4230, 3652, 3914) | 551 (590, 525, 537) | 115 (115, 115, 116) | equal | int 1, s7 1 |
| pinheavy | nursery copy | 3629 (3605, 3644, 3542, 3726) | 319 (324, 318, 315, 318) | 160 (160, 160, 160, 160) | 3727 (3749, 3800, 3660, 3699) | 316 (317, 322, 311, 313) | 115 (115, 116, 116, 115) | equal | none |

Native planes and committed bytes (maximum over the run, MiB; median of the runs)

| workload | mode | int native planes | int managed committed | s7 native planes | s7 managed committed |
|---|---|---|---|---|---|
| binarytrees | non-moving | 212.0 | 32.0 | 160.0 | 32.0 |
| binarytrees | nursery copy | 212.0 | 32.0 | 160.0 | 32.0 |
| requests | non-moving | 456.3 | 74.3 | 336.0 | 74.3 |
| requests | nursery copy | 456.3 | 74.3 | 336.0 | 74.3 |
| listsuffix | non-moving | 212.0 | 32.0 | 160.0 | 32.0 |
| listsuffix | nursery copy | 212.0 | 32.0 | 160.0 | 32.0 |
| pinheavy | non-moving | 284.0 | 56.0 | 256.0 | 56.0 |
| pinheavy | nursery copy | 284.0 | 56.0 | 256.0 | 56.0 |

Pause table (stop-the-world time of the S0 timers, pooled over the runs): n, median ms, maximum ms

| workload | mode | pause kind | int | s7 |
|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 32, 4.5, 181.3 | 32, 4.0, 20.9 |
| binarytrees | non-moving | InitialMark | 2, 237.6, 289.8 | 2, 196.5, 217.0 |
| binarytrees | non-moving | FinalMark | 2, 52.1, 54.7 | - |
| binarytrees | nursery copy | RefCount | 36, 5.0, 81.9 | 36, 4.8, 137.2 |
| binarytrees | nursery copy | InitialMark | 2, 308.3, 317.5 | 2, 147.8, 151.3 |
| binarytrees | nursery copy | FinalMark | 1, 73.8, 73.8 | 1, 46.7, 46.7 |
| requests | non-moving | RefCount | 8, 5.1, 30.3 | 8, 5.2, 31.3 |
| requests | non-moving | InitialMark | 2, 104.5, 148.5 | 2, 19.5, 20.7 |
| requests | non-moving | FinalMark | 2, 73.4, 75.6 | 2, 101.2, 122.4 |
| requests | non-moving | Full | 104, 177.2, 257.0 | 104, 115.5, 142.7 |
| requests | nursery copy | RefCount | 127, 12.8, 74.1 | 125, 11.7, 64.4 |
| requests | nursery copy | InitialMark | 6, 83.5, 105.5 | 6, 20.9, 22.8 |
| requests | nursery copy | FinalMark | 6, 20.1, 26.5 | 6, 13.8, 25.4 |
| listsuffix | non-moving | RefCount | 129, 34.6, 422.3 | 126, 34.4, 475.8 |
| listsuffix | non-moving | InitialMark | 2, 490.7, 513.9 | 2, 445.1, 450.2 |
| listsuffix | non-moving | FinalMark | 3, 127.6, 151.4 | 2, 122.2, 130.6 |
| listsuffix | nursery copy | RefCount | 105, 53.6, 454.1 | 105, 51.0, 435.4 |
| listsuffix | nursery copy | InitialMark | 2, 546.6, 556.3 | 2, 391.4, 458.2 |
| listsuffix | nursery copy | FinalMark | 2, 214.1, 251.3 | 2, 162.9, 211.8 |
| pinheavy | non-moving | RefCount | 757, 2.2, 5.0 | 656, 2.2, 6.6 |
| pinheavy | nursery copy | RefCount | 328, 3.8, 5.4 | 329, 3.7, 6.2 |

Pins (pin-heavy program): pinned roots per pause and bytes of pinned objects promoted in place (last event of the run)

The counter that the S7 build calls pinned roots (id 458) is the touched-list count in the build before S7, so the
column of the earlier build shows n/a.

| mode | int mean pinned roots per pause | int in-place pinned bytes | s7 mean pinned roots per pause | s7 in-place pinned bytes |
|---|---|---|---|---|
| non-moving | n/a | 0 | 249.9 | 0 |
| nursery copy | n/a | 107412664 | 248.7 | 108604836 |
