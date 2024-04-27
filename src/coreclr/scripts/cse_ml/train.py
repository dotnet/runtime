#!/usr/bin/python

"""Trains on superpmi data."""
import os

from jitml import SuperPmi, JitEnv, JitRLModel

core_root = os.path.expanduser("~/git/runtime/artifacts/bin/coreclr/linux.x64.Checked")
mch = os.path.expanduser("~/git/runtime/artifacts/spmi/mch/8f046bcb-ca5f-4692-9277-898b71cb7938.linux.x64/libraries_tests_no_tiered_compilation.run.linux.x64.Release.mch")
with SuperPmi(core_root, mch) as pmi:
    print("Finding acceptable methods to train on...")

    acceptable = []
    for method in pmi.enumerate_methods():
        if JitEnv.is_acceptable(method):
            acceptable.append(method)
            if len(acceptable) > 10:
                break

            if len(acceptable) % 10_000 == 0:
                print(f"Found {len(acceptable)} acceptable methods so far...")


    rl = JitRLModel(os.path.expanduser("~/jitml"))
    rl.train(pmi, acceptable, iterations=10_000_000)
    rl.save(os.path.expanduser("~/jitml.zip"))
