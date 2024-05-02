"""A reward wrapper for the CSE environment that provides rewards based not just on the change in
performance score, but also on the quality of the CSE choices made."""

from typing import List, Optional, SupportsFloat
import gymnasium as gym
import numpy as np

from .method_context import MethodContext
from .jit_cse import JitCseEnv
from .superpmi import SuperPmi

OPTIMAL_BONUS = 0.05
SUBOPTIMAL_PENALTY = -0.01
NEUTRAL_PENALTY = -0.005

class OptimalCseWrapper(gym.Wrapper):
    """A wrapper for the CSE environment that provides rewards based not just on the change in
    performance score, but also on the quality of the CSE choices made."""
    def __init__(self, env : JitCseEnv):
        super().__init__(env)
        self.superpmi : SuperPmi = env.unwrapped.pmi_context.create_superpmi()
        self.superpmi.start()

    def step(self, action):
        """Steps the environment."""
        observation, reward, terminated, truncated, info = self.env.step(action)
        reward = self._get_reward(reward, info)
        return observation, reward, terminated, truncated, info

    def _get_reward(self, reward : SupportsFloat, info) -> SupportsFloat:
        # We'll let the parent class handle the reward in these cases.
        if info['truncated'] or not info['action_is_valid']:
            return reward

        m_idx = info['method_index']
        current = info['current']
        previous = info['previous']
        previous_score = previous.perf_score

        # Did we choose to end optimization?
        if info['action'] is None:
            all_cses = self._get_all_cses(m_idx, previous, None)
            best_perf_score = min(all_cses, key=lambda x: x.perf_score).perf_score if all_cses else np.inf

            if not np.isclose(best_perf_score, previous_score) and best_perf_score < previous_score:
                reward += SUBOPTIMAL_PENALTY

        # Otherwise we chose a CSE
        else:
            # We apply a tiny penalty for choosing a CSE that matches the previous score.  Choosing a CSE that
            # doesn't change the score still has a cost, but we don't want this penalty to be so high that the
            # agent avoids making choices.
            if np.isclose(current.perf_score, previous_score):
                reward += NEUTRAL_PENALTY

            # If we improved the performance score, give a bonus for choosing the best option out of all of them.
            elif current.perf_score < previous_score:
                # We improved the performance score, but was it the best choice?
                all_cses = self._get_all_cses(m_idx, previous, current.cses_chosen[-1])
                best_perf_score = min(all_cses, key=lambda x: x.perf_score).perf_score if all_cses else np.inf
                if np.isclose(best_perf_score, current.perf_score) or current.perf_score < best_perf_score:
                    reward += OPTIMAL_BONUS

        return reward

    def _get_all_cses(self, m_idx, previous : MethodContext, selected : Optional[int]) -> List[MethodContext]:
        # If we aren't given a current method, then no CSEs were applied.
        assert selected not in previous.cses_chosen

        all_cses = [self.superpmi.jit_method(m_idx, JitMetrics=1, JitRLHook=1,
                                                 JitRLHookCSEDecisions=previous.cses_chosen + [x.index])
                    for x in previous.cse_candidates
                    if x.index != selected and x.can_apply]

        all_cses = [x for x in all_cses if x is not None]
        return all_cses


class NormalizeFeaturesWrapper(gym.ObservationWrapper):
    """Removes unused features from the observation space."""

    # Calculated using scripts/calculate_feature_norm.py
    obs_subtract = np.array([0.0, 0.0, 0.0, 0.0, 0, 0.0, 0.0, 0.0, 0.0, 0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0,
                             0.0, 0.0, 0.0], dtype=np.float32)
    obs_scale = np.array([1.0, 1.0, 1.0, 1.0, 1, 1.0, 1.0, 1.0, 1.0, 1, 1.0, 1.0, 1.0, 0.012658227848101266,
                       0.018867924528301886, 0.001763668430335097, 0.005291005291005291, 0.06988495589628721,
                       0.07344255107610509, 0.2, 0.16666666666666666, 0.0013623978201634877], dtype=np.float32)
    obs_log1p = np.array([False, False, False, False, False, False, False, False, False, False, False, False, False,
                          False, False, False, False, True, True, False, False, False], dtype=bool)


    unused_features = ['shared_const', 'type_struct']

    def __init__(self, env):
        super().__init__(env)

        # Remove the unused features from the observation space.
        self.filter = np.array([name in NormalizeFeaturesWrapper.unused_features for name in env.observation_columns],
                          dtype=bool)

        self.observation_space = gym.spaces.Box(
            low=env.observation_space.low[:, self.filter],
            high=env.observation_space.high[:, self.filter],
            dtype=env.observation_space.dtype
        )

    def observation(self, observation):
        """Builds the observation from a method."""
        observation[:, self.obs_log1p] = np.log1p(observation[:, self.obs_log1p])
        observation = (observation - self.obs_subtract) * self.obs_scale

        # We still need to clip the data since there could be some values we didn't encounter when building
        # the scaling factors
        np.clip(observation, 0.0, 1.0, out=observation)
        return observation[:, self.filter]

__all__ = [NormalizeFeaturesWrapper.__name__, OptimalCseWrapper.__name__]
