"""A reward wrapper for the CSE environment that provides rewards based not just on the change in
performance score, but also on the quality of the CSE choices made."""

from typing import List, SupportsFloat
import gymnasium as gym
import numpy as np

from .method_context import CseCandidate, MethodContext
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
            alternates = self._get_alternate_cses(state)
            best_perf_score = min(alternates, key=lambda x: x.perf_score).perf_score if alternates else np.inf

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
                alternates = self._get_alternate_cses(state)
                best_perf_score = min(alternates, key=lambda x: x.perf_score).perf_score if alternates else np.inf
                if np.isclose(best_perf_score, curr.perf_score) or curr.perf_score < best_perf_score:
                    reward += OPTIMAL_BONUS

        return reward

    def _get_alternate_cses(self, state : JitCseEnvState):
        m_id = state.current.index
        cses_not_chosen = [self.superpmi.jit_with_retry(m_id, JitMetrics=1, JitRLHook=1,
                                                        JitRLHookCSEDecisions=state.choices[:-1])
                           for x in state.previous.cse_candidates
                           if x.can_apply]

        cses_not_chosen = [x for x in cses_not_chosen if x is not None]
        return cses_not_chosen

    def jit_method(self, m_id : int, choices : List[int], cse : CseCandidate) -> MethodContext:
        """JITs a method with the given choices plus cse."""

        choices = list(choices)
        choices.append(cse.index)

        return self.superpmi.jit_with_retry(m_id, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=choices)
