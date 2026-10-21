#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Prove that mixed-case collector keys defeat the old cleanup but not this runner."""

import importlib.util
from pathlib import Path


path = Path(__file__).with_name("run-vertical.py")
spec = importlib.util.spec_from_file_location("vertical", path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

cases = (
    "DOTNET_GCPATH", "dOtNeT_GcPaTh", "COMPLUS_GCPATH", "cOmPlUs_GcPaTh",
    "DOTNET_GCNAME", "cOmPlUs_GcNaMe", "dotnet_gcServer",
    "DoTnEt_GCHeapHardLimit", "cOmPlUs_LXRManagedHeapHardLimitMB",
)
old_names = {
    "DOTNET_GCPath", "COMPlus_GCPath", "DOTNET_GCName", "COMPlus_GCName",
    "DOTNET_gcServer", "COMPlus_gcServer", "DOTNET_GCStress", "COMPlus_GCStress",
}
for key in cases:
    original = {key: "contamination", "P8_UNRELATED_SENTINEL": "preserved"}
    old = dict(original)
    for name in tuple(old):
        if name.startswith(("DOTNET_LXR", "COMPlus_LXR", "DOTNET_GCHeapHardLimit", "COMPlus_GCHeapHardLimit")) or name in old_names:
            del old[name]
    assert key in old, f"The control no longer demonstrates the former defect: {key}"
    result = module.collector_environment(original)
    assert key not in result and result["P8_UNRELATED_SENTINEL"] == "preserved", key
    assert original[key] == "contamination", "Cleanup mutated its caller's environment"
print(f"PASS: {len(cases)} mixed-case environment contamination controls")
