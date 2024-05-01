"""Constants and parameters for the project."""

MIN_CSE = 3
MAX_CSE = 16

INVALID_ACTION_PENALTY = -0.05
INVALID_ACTION_LIMIT = 20

def is_acceptable_method(method):
    """Returns True if the method is acceptable for training."""
    applicable = len([x for x in method.cse_candidates if x.viable])
    return MIN_CSE <= applicable and len(method.cse_candidates) <= MAX_CSE
