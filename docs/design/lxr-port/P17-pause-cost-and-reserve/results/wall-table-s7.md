
wall ms

| workload | mode | int | s7 |
|---|---|---|---|
| binarytrees | nonmoving | 2100 (2110, 2091) | 1961 (2008, 1914) |
| binarytrees | nursery | 1950 (1751, 2149) | 1846 (1694, 1998) |
| requests | nonmoving | 19607 (19491, 19723) | 15236 (15261, 15211) |
| requests | nursery | 7754 (7767, 7742) | 6620 (6566, 6675) |
| listsuffix | nonmoving | 12628 (13151, 12106) | 10289 (10111, 10467) |
| listsuffix | nursery | 20714 (11934, 29494) | 14194 (17845, 10542) |

GC ms (runtime counter)

| workload | mode | int | s7 |
|---|---|---|---|
| binarytrees | nonmoving | 439 (426, 452) | 438 (443, 433) |
| binarytrees | nursery | 334 (171, 498) | 302 (152, 452) |
| requests | nonmoving | 9775 (9664, 9886) | 6271 (6270, 6272) |
| requests | nursery | 1349 (1321, 1377) | 1078 (1082, 1073) |
| listsuffix | nonmoving | 6536 (7124, 5947) | 4466 (4445, 4487) |
| listsuffix | nursery | 14752 (5910, 23593) | 8616 (12291, 4942) |

peak RSS MiB

| workload | mode | int | s7 |
|---|---|---|---|
| binarytrees | nonmoving | 185 (184, 186) | 136 (137, 134) |
| binarytrees | nursery | 146 (110, 182) | 108 (88, 128) |
| requests | nonmoving | 279 (294, 264) | 159 (176, 143) |
| requests | nursery | 280 (281, 280) | 159 (159, 159) |
| listsuffix | nonmoving | 274 (277, 270) | 225 (226, 224) |
| listsuffix | nursery | 256 (257, 255) | 202 (203, 202) |

checksums per workload: binarytrees equal; listsuffix equal; requests equal
