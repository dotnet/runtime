"""A reward wrapper for the CSE environment that provides rewards based not just on the change in
performance score, but also on the quality of the CSE choices made."""

from typing import List, SupportsFloat
import gymnasium as gym
import numpy as np

from .method_context import CseCandidate, MethodContext
from .jit_cse import JitCseEnvState, JitCseEnv
from .superpmi import SuperPmi

REWARD_OPTIMAL = 1.0        # Agent found the best CSE
REWARD_IMPROVEMENT = 0.5    # Agent improved the performance score, but there were better options.
REWARD_NEUTRAL = -0.25      # Don't reward for equal performance, CSEs have a cost.
PENALTY_WORSE = -0.5        # Agent made the performance score worse.
PENALTY_INVALID = -1.0      # Agent made an invalid choice.
REWARD_CORRECT_STOP = 0.25  # Agent stopped when there were no better options.
PENALTY_BAD_STOP = -0.5     # Agent stopped early, but there were better options.

class DeepCseRewardWrapper(gym.RewardWrapper):
    """A wrapper for the CSE environment that provides rewards based not just on the change in
    performance score, but also on the quality of the CSE choices made."""
    def __init__(self, env : JitCseEnv):
        super().__init__(env)
        self.superpmi : SuperPmi = env.create_superpmi()
        self.superpmi.start()

    def reward(self, reward : SupportsFloat) -> SupportsFloat:
        """Returns the reward based on the change in performance score."""
        # pylint: disable=too-many-branches
        state : JitCseEnvState = self.env.state

        # If the state is truncated, we will defer to the reward passed in.
        if state.truncated:
            return reward

        if not state.last_action_valid:
            return PENALTY_INVALID

        # Don't use the reward passed in, we will calculate our own.
        reward = 0.0
        previous_score = state.previous.perf_score

        # Did we choose to end optimization?
        if state.last_action is None:
            # Were there more CSEs we could have applied?
            alternate_cses = self._get_alternate_cses(state, None)
            if alternate_cses:
                best = min(alternate_cses, key=lambda x: x.perf_score)
                if not np.isclose(best.perf_score, previous_score) and best.perf_score < previous_score:
                    reward = PENALTY_BAD_STOP
                else:
                    reward = REWARD_CORRECT_STOP

            else:
                # We had no choices and chose to stop, perfect.
                reward = REWARD_CORRECT_STOP

        # We chose a CSE.
        else:
            curr = state.current
            if np.isclose(curr.perf_score, previous_score):
                reward = REWARD_NEUTRAL

            elif curr.perf_score > previous_score:
                reward = PENALTY_WORSE

            else:
                # We improved the performance score, but was it the best choice?
                alternate_cses = self._get_alternate_cses(state, state.choices[-1])
                if not alternate_cses:
                    reward = REWARD_OPTIMAL

                else:
                    best = min(alternate_cses, key=lambda x: x.perf_score)
                    if np.isclose(best.perf_score, curr.perf_score) or curr.perf_score < best.perf_score:
                        reward = REWARD_OPTIMAL
                    else:
                        reward = REWARD_IMPROVEMENT

        return reward

    def _get_alternate_cses(self, state, chosen_index):
        m_id = state.current.index
        cses_not_chosen = [self.jit_method(m_id, state.choices, x) for x in state.previous.cse_candidates
                           if x.can_apply and x.index != chosen_index]

        cses_not_chosen = [x for x in cses_not_chosen if x is not None]
        return cses_not_chosen

    def jit_method(self, m_id : int, choices : List[int], cse : CseCandidate) -> MethodContext:
        """JITs a method with the given choices plus cse."""

        choices = list(choices)
        choices.append(cse.index)

        return self.superpmi.jit_with_retry(m_id, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=choices)
