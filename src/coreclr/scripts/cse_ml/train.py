"""Trains on superpmi data."""
import os
import time

from jitml import SuperPmi

core_root = os.path.expanduser("~/git/runtime/artifacts/bin/coreclr/linux.x64.Checked")
mch = os.path.expanduser("~/git/runtime/artifacts/spmi/mch/8f046bcb-ca5f-4692-9277-898b71cb7938.linux.x64/libraries_tests_no_tiered_compilation.run.linux.x64.Release.mch")
with SuperPmi(core_root, mch) as pmi:
    start_time = time.time()
    print(pmi.jit_method(105189))

    seq = [6, 10, 15, 7, 2]

    for i in range(len(seq)):
        replay = seq[:i + 1]
        replay.append(0)
        print(pmi.jit_method(105189, JitRLCSE="", JitRLCSEAlpha=0.02, JitRandomCSE=60000, JitReplayCSE=replay,
                            JitReplayCSEReward=[0,0]))

    print(f"Time taken: {time.time() - start_time:.2f}s")
