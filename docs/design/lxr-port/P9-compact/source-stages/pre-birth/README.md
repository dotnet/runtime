# Pre-birth growth source-stage binding

This identifies the **already measured, retained** compact growth stage after
the root-batching review fix and before the cleared-span birth optimization.
It is not the final `birth-final-*` stage, a new build or a new study.

| OS | Actual retained Release framework | Collector SHA256 |
|---|---|---|
| Windows | This worktree's `artifacts/P9/final-Release-windows` | `8dd3bf19f1ddb522c46c3a7171e8615881d248d72cc8eaa14d34fd2d0c269051` |
| Linux | `/root/lxr-compact-collector/artifacts/P9/final-Release-linux` | `6638a4c10af70cfe8d79c46e8f142fbb48fc68c5affd208b3f43e5f2dc99636e` |

The parent `module-identities.csv` binds every physical framework file in these
two additional roots. The existing `growth-final-{windows,linux}.csv` rows,
schedules, raw logs and summaries were not modified. Each compact arm has
nine scheduled observations carrying the module hash above and the immutable
fixture `a8fb7abdca38c21b4acb9fd5a89f1d8fd824d8729c1dcadb3b52c415cfbf2c30`.

## Original evidence versus reconstructed stage

`native-source-windows.csv` and `native-source-linux.txt` are **byte-for-byte
copies of original contemporaneous source records**, not freshly rehashed
final source:

- Windows: `artifacts/P9/review-fixed-native-Release-windows/source-identities.csv`,
  SHA256 `9dc0af07e7877f9d972744c477d87884f2ad90b88f650bd23e39bcc996e87771`.
- Linux:
  `/root/lxr-compact-collector/artifacts/P9/review-fixed-native-Release-linux/source-identities.txt`,
  SHA256 `796cfda9616cb132eb870d62584c7cf96b7dedbd68afcc964bcb2a81a0865522`.

The Debug records have the same identities. `product-build-{windows,linux}.log`
are the original incremental Release collector build logs preceding creation
of the retained frameworks. These native records do not include all host
translation units; the build logs are not presented as content hashes.

`from-final-a818ad9.patch` is an **explicit reconstruction** of this source
stage from committed final implementation
`a818ad9949ca499f12572e8f7df574bc16cd555a`. It reverses only the later birth
group in six files: `reference_count.{cpp,h}`, `lxr_reference_counting.{cpp,h}`,
`lxr_gc.cpp` and the native fixture's two fast-birth call sites. It preserves
the root-batching fix, compact LOS/accounting/storage ownership and retirement
changes. It does not invent an intermediate commit or source stamp.
Patch SHA256 is
`adb5004db0beef5d310d033d7f0b56d5eeef1b39f2ebae1593f28bcc6e460740`.

`source-inputs.csv` records the reconstructed normalized Git-source hashes
and physical Windows-source hashes for the same 54 canonical input paths;
SHA256 is
`b0227c7c07b201e0a0e07c0a7ce88ebfb54e2b00d0fa26f17b071ad9665b6b45`.
All 26 entries of the original Windows native record match this reconstruction
exactly. All 30 overlapping entries of the original Linux record also match;
their original LF/CRLF byte basis is explicitly listed in
`linux-native-source-validation.csv`, not normalized away.

The engine's original physical source hash is
`77d4c5e50b81181d6232a770c48743a7d3ea43cd5f4efff9117ec1ddb13af499`,
and its header is
`05f255870ae431e82509a3afa8ba54cd57fa3eaf87f1da1aaeed22cbeb614904`.
They are distinct from the final birth-witness input hashes. For files absent
from the contemporaneous native records, the manifest labels the evidence as
the recorded birth-only reversal or unchanged final commit bytes, **not** a
new contemporaneous compiler-input capture.

No original layout, fixture, measured row or raw log was rewritten, and no
source reconstruction was built or timed for this record-binding completion.
