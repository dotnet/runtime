"""Constants and parameters for the project."""

from typing import Sequence

import numpy as np
from .method_context import MethodContext

MIN_CSE = 3
MAX_CSE = 16

INVALID_ACTION_PENALTY = -0.05
INVALID_ACTION_LIMIT = 20

def is_acceptable_for_cse(method):
    """Returns True if the method is acceptable for training on JitCseEnv."""
    applicable = len([x for x in method.cse_candidates if x.viable])
    return MIN_CSE <= applicable and len(method.cse_candidates) <= MAX_CSE

def split_for_cse(methods : Sequence['MethodContext'], test_percent=0.1):
    """Splits the methods into those that can be used for training and those that can't.
    Returns the test and train sets."""
    method_by_cse = {}

    for x in methods:
        if is_acceptable_for_cse(x):
            method_by_cse.setdefault(x.num_cse, []).append(x)

    # convert method_by_cse to a list of methods
    methods_list = []
    for value in method_by_cse.values():
        methods_list.append(value)

    test = []
    train = []

    # use a fixed seed so subsequent calls line up
    # Sort the groups of methods by length to ensure we don't care what order we process them in.
    # Then sort each method by id before shuffling to (again) ensure we get the same result.
    methods_list.sort(key=len)
    for method_group in methods_list:
        split = int(len(method_group) * test_percent)

        # Discard any groups that are too small to split.
        if split > 0:
            method_group.sort(key=lambda x: x.index)
            np.random.default_rng(seed=42).shuffle(method_group)
            test.extend(method_group[:split])
            train.extend(method_group[split:])

    return test, train
