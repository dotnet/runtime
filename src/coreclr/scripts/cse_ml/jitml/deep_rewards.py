"""A reward wrapper for the CSE environment that provides rewards based not just on the change in
performance score, but also on the quality of the CSE choices made."""

from typing import SupportsFloat
import gymnasium as gym
import numpy as np

from .jit_cse import JitCseEnvState, JitCseEnv
from .superpmi import SuperPmi

OPTIMAL_BONUS = 0.05
SUBOPTIMAL_PENALTY = -0.01
NEUTRAL_PENALTY = -0.005

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

        # We'll let the parent class handle the reward in these cases.
        if state.truncated or not state.last_action_valid:
            return reward

        previous_score = state.previous.perf_score

        # Did we choose to end optimization?
        if state.last_action is None:
            all_cses = self._get_all_cses(state)
            best_perf_score = min(all_cses, key=lambda x: x.perf_score).perf_score if all_cses else np.inf

            if not np.isclose(best_perf_score, previous_score) and best_perf_score < previous_score:
                reward += SUBOPTIMAL_PENALTY

        # Otherwise we chose a CSE
        else:
            curr = state.current

            # We apply a tiny penalty for choosing a CSE that matches the previous score.  Choosing a CSE that
            # doesn't change the score still has a cost, but we don't want this penalty to be so high that the
            # agent avoids making choices.
            if np.isclose(curr.perf_score, previous_score):
                reward += NEUTRAL_PENALTY

            # If we improved the performance score, give a bonus for choosing the best option out of all of them.
            elif curr.perf_score < previous_score:
                # We improved the performance score, but was it the best choice?
                all_cses = self._get_all_cses(state)
                best_perf_score = min(all_cses, key=lambda x: x.perf_score).perf_score if all_cses else np.inf
                if np.isclose(best_perf_score, curr.perf_score) or curr.perf_score < best_perf_score:
                    reward += OPTIMAL_BONUS

        return reward

    def _get_all_cses(self, state : JitCseEnvState):
        m_id = state.current.index
        previous = state.previous
        selected = state.current.cses_chosen[-1]
        assert selected not in previous.cses_chosen

        all_cses = [self.superpmi.jit_with_retry(m_id, JitMetrics=1, JitRLHook=1,
                                                 JitRLHookCSEDecisions=previous.cses_chosen + [x.index])
                    for x in previous.cse_candidates
                    if x.index != selected and x.can_apply]

        all_cses = [x for x in all_cses if x is not None]
        return all_cses

__all__ = [DeepCseRewardWrapper.__name__]
