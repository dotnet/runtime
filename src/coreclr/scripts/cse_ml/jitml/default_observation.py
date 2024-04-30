"""Returns an observation for a MethodContext."""

from typing import List
import numpy as np
import gymnasium as gym

from .constants import MAX_CSE
from .method_context import CseCandidate, MethodContext

JITTYPE_ONEHOT_SIZE = 6
BOOLEAN_FEATURES = 7
FLOAT_FEATURES = 9
FEATURES = JITTYPE_ONEHOT_SIZE + BOOLEAN_FEATURES + FLOAT_FEATURES

def one_hot(t) -> List[float]:
    """Returns a one hot encoding of the type."""
    result = [0.0] * 6
    result[t.value - 1] = 1.0
    return result

def get_tensor(cse : CseCandidate):
    """Returns a tensor for a single cse candidate."""
    result = np.zeros(FEATURES)

    result[:JITTYPE_ONEHOT_SIZE] = one_hot(cse.type)
    result[JITTYPE_ONEHOT_SIZE:JITTYPE_ONEHOT_SIZE + BOOLEAN_FEATURES] = _get_boolean_features(cse)
    result[JITTYPE_ONEHOT_SIZE + BOOLEAN_FEATURES:] = _get_float_features(cse)

    return result

def _get_boolean_features(cse : CseCandidate):
    return [cse.can_apply, cse.live_across_call, cse.const, cse.shared_const, cse.make_cse, cse.has_call,
            cse.containable]

def _get_float_features(cse : CseCandidate):
    return [cse.cost_ex, cse.cost_sz, cse.use_count, cse.def_count, cse.use_wt_cnt, cse.def_wt_cnt,
            cse.distinct_locals, cse.local_occurrences, cse.enreg_count]

def create_observation():
    """Returns an observation space for the CSE candidates."""
    lower_bounds = np.zeros((MAX_CSE, FEATURES))
    upper_bounds = np.ones((MAX_CSE, FEATURES))
    upper_bounds[:, JITTYPE_ONEHOT_SIZE + BOOLEAN_FEATURES:] = np.full((MAX_CSE, FLOAT_FEATURES), np.inf)

    return gym.spaces.Box(lower_bounds, upper_bounds, dtype=np.float32)

def get_observation(method : MethodContext):
    """Builds the observation from a method."""
    tensors = [get_tensor(x) for i, x in enumerate(method.cse_candidates) if i < MAX_CSE]
    while len(tensors) < MAX_CSE:
        tensors.append(np.zeros(FEATURES))

    result = np.vstack(tensors)
    return result

def get_observation_columns():
    """Returns a stable list of column names."""
    return [f"JitType{i}" for i in range(1, 7)] + \
           [ "CanApply", "LiveAcrossCall", "Const", "SharedConst", "MakeCse", "HasCall", "Containable"
             "CostEx", "CostSz", "UseCount", "DefCount", "UseWtCnt", "DefWtCnt", "DistinctLocals", "LocalOccurrences",
             "EnregCount"
            ]

__all__ = [
    get_observation.__name__,
    get_observation_columns.__name__,
    create_observation.__name__
]
