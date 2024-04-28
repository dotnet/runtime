"""Constants, parameters, and basic data structures for the JIT ML project."""

from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, ValidationError, field_validator

JITTYPE_ONEHOT_SIZE = 6
MAX_CSE = 16
MIN_CSE = 3

BOOLEAN_FEATURES = JITTYPE_ONEHOT_SIZE + 7
FLOAT_FEATURES = 9
FEATURES = BOOLEAN_FEATURES + FLOAT_FEATURES

REWARD_SCALE = 10
REWARD_MIN = -1.0
REWARD_MAX = 1.0

FOUND_BEST_REWARD = 0.1
NO_BETTER_METHOD_REWARD = 0.01

INVALID_ACTION_PENALTY = -0.05
INVALID_ACTION_LIMIT = 20

class JitType(Enum):
    """The type of a CSE candidate.  Mirrors CSE_HeuristicRLHook's enum."""
    OTHER : int = 0
    INT : int = 1
    LONG : int = 2
    FLOAT : int = 3
    DOUBLE : int = 4
    STRUCT : int = 5
    SIMD : int = 6

class CseCandidate(BaseModel):
    """A CSE candidate.  Mirrors CSE_Candidate features in CSE_HeuristicRLHook.cpp."""
    index : int
    applied : Optional[bool] = False
    viable : bool
    live_across_call : bool
    const : bool
    shared_const : bool
    make_cse : bool
    has_call : bool
    containable : bool
    type : JitType
    cost_ex : int
    cost_sz : int
    use_count : int
    def_count : int
    use_wt_cnt : int
    def_wt_cnt : int
    distinct_locals : int
    local_occurrences : int
    enreg_count : int

    @field_validator('applied', 'viable', 'live_across_call', 'const', 'shared_const', 'make_cse', 'has_call',
                     'containable', mode='before')
    @classmethod
    def validate_bool(cls, v):
        """Validates that the value is a boolean or is a 0 or 1."""
        if isinstance(v, int) and v in [0, 1]:
            return bool(v)

        if isinstance(v, bool):
            return v

        raise ValidationError(f"Value must be either 1, 0, or a boolean, got {v}")

    @property
    def can_apply(self):
        """Returns True if the candidate is viable and not applied."""
        return self.viable and not self.applied

class MethodContext(BaseModel):
    """A superpmi method context."""
    index : int
    name : str
    hash : str
    total_bytes : int
    prolog_size : int
    instruction_count : int
    perf_score : float
    bytes_allocated : int
    num_cse : int
    num_cse_candidate : int
    heuristic : str
    heuristic_sequence : List[int]
    cse_candidates : List[CseCandidate]

    def __str__(self):
        return f"{self.index}: {self.name}"
