"""Returns an observation for a MethodContext."""

from typing import List
import numpy as np
from jitml.constants import FEATURES, JITTYPE_ONEHOT_SIZE, MAX_CSE, CseCandidate
from jitml.superpmi import MethodContext


def one_hot(t) -> List[float]:
    """Returns a one hot encoding of the type."""
    result = [0.0] * 6
    result[t.value - 1] = 1.0
    return result

def get_tensor(cse : CseCandidate):
    """Returns a tensor for a single cse candidate."""
    result = np.zeros(FEATURES)

    result[:JITTYPE_ONEHOT_SIZE] = one_hot(cse.type)
    bool_features = _get_boolean_features(cse)
    result[JITTYPE_ONEHOT_SIZE:JITTYPE_ONEHOT_SIZE + len(bool_features)] = bool_features
    result[JITTYPE_ONEHOT_SIZE + len(bool_features):] = _get_float_features(cse)

    return result

def _get_boolean_features(cse : CseCandidate):
    return [cse.can_apply, cse.live_across_call, cse.const, cse.shared_const, cse.make_cse, cse.has_call,
            cse.containable]

def _get_float_features(cse : CseCandidate):
    return [cse.cost_ex, cse.cost_sz, cse.use_count, cse.def_count, cse.use_wt_cnt, cse.def_wt_cnt,
            cse.distinct_locals, cse.local_occurrences, cse.enreg_count]

def get_observation(method : MethodContext):
    """Builds the observation from a method."""
    tensors = [get_tensor(x) for i, x in enumerate(method.cse_candidates) if i < MAX_CSE]
    while len(tensors) < MAX_CSE:
        tensors.append(np.zeros(FEATURES))

    result = np.vstack(tensors)

    return result

__all__ = ['get_observation']
