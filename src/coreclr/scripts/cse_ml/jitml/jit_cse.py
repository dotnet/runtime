"""A gymnasium environment for training RL to optimize the .Net JIT's CSE usage."""

from typing import Any, List, Optional
import gymnasium as gym
import numpy as np

from .method_context import MethodContext
from .superpmi import SuperPmi
from .default_observation import get_observation, create_observation
from .constants import (INVALID_ACTION_PENALTY, INVALID_ACTION_LIMIT, MIN_CSE, MAX_CSE)

class JitCseEnvState:
    """The state of the JIT environment."""
    def __init__(self, no_cse_method : MethodContext, heuristic_method : MethodContext):
        self.no_cse_method = no_cse_method
        self.heuristic_method = heuristic_method
        self.choices = []
        self.results = []
        self.invalid_action_count = 0
        self.total_reward = 0.0
        self.terminated = False
        self.truncated = False
        self.last_action = None
        self.last_action_valid = False

    def choose(self, index : int, result : MethodContext):
        """Chooses an action and updates the state."""
        assert 0 <= index < MAX_CSE
        self.choices.append(index)
        self.results.append(result)
        for i in self.choices:
            assert result.cse_candidates[i].index == i
            result.cse_candidates[i].applied = True

    @property
    def heuristic_score(self):
        """Returns the score of the heuristic method."""
        return self.heuristic_method.perf_score

    @property
    def no_cse_score(self):
        """Returns the score of the PerfScore if we perform no CSEs."""
        return self.no_cse_method.perf_score

    @property
    def previous_score(self):
        """Returns the score of the previous state."""
        previous = self.previous
        if previous:
            return previous.perf_score

        return self.no_cse_score

    @property
    def current(self):
        """The current method JIT'ed up through all of the choices."""
        if self.results:
            return self.results[-1]

        return self.no_cse_method

    @property
    def previous(self):
        """The previous method JIT'ed."""
        if not self.results:
            return None

        if len(self.results) > 1:
            return self.results[-2]

        return self.no_cse_method

class JitCseEnv(gym.Env):
    """A gymnasium environment for the JIT."""
    def __init__(self, core_root : str, mch : str, methods : Optional[List[int]] = None):
        self.core_root = core_root
        self.mch = mch
        self.state = None
        self.__superpmi = None
        self.methods = methods
        self.action_space = gym.spaces.Discrete(MAX_CSE + 1)
        self.observation_space = create_observation()

    def __del__(self):
        if self.__superpmi is not None:
            self.__superpmi.stop()

    def reset(self, *, seed: int | None = None, options: dict[str, Any] | None = None):
        super().reset(seed=seed, options=options)

        failure_count = 0
        while True:
            index = self.__select_method()
            no_cse = self._jit_method(index, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=[0])
            if no_cse is None:
                continue

            if JitCseEnv.is_acceptable(no_cse):
                original_heuristic = self._jit_method(index, JitMetrics=1)
                if original_heuristic is None:
                    continue
                break

            failure_count += 1
            if failure_count > 512:
                raise ValueError("No valid methods found")

        self.state = JitCseEnvState(no_cse, original_heuristic)
        obs = self.get_observation(no_cse)
        return obs, self._get_info()

    def step(self, action):
        # the last action is always to terminate
        if action == self.action_space.n - 1:
            action = None

        state = self.state
        if state is None:
            raise ValueError("Must call reset() before step()")

        state.last_action = action

        # validate that the selected the action is valid
        state.last_action_valid = self._is_valid_action(action)
        if not state.last_action_valid:
            state.invalid_action_count += 1

            self.state.truncated = state.invalid_action_count >= INVALID_ACTION_LIMIT
            observation = self.get_observation(state.current)
            reward = INVALID_ACTION_PENALTY

            state.total_reward += reward
            return observation, reward, state.terminated, state.truncated, self._get_info()

        # _perform_cse will return False if there was an error and we need to truncate
        state.truncated = not self._perform_cse(action, state)

        current = state.current
        observation = self.get_observation(current)
        state.terminated = action is None or not any((x for x in current.cse_candidates if x.can_apply))
        reward = self.get_rewards() if not state.truncated else INVALID_ACTION_PENALTY
        state.total_reward += reward

        return observation, reward, state.terminated, state.truncated, self._get_info()

    def get_observation(self, method : MethodContext):
        """Returns the observation for the current state.  Implemented here so it can be replaced."""
        return get_observation(method)

    def _get_info(self):
        """Returns the info dictionary for the current state."""
        state = self.state
        result = {
            'no_cse_method': state.no_cse_method,
            'heuristic_method': state.heuristic_method,
            'method' : state.current,
            'choices': state.choices,
            'results': state.results,
            'invalid_action_count': state.invalid_action_count
        }

        # Reported only once, when the episode is done.
        if state.terminated:
            result['heuristic_score'] = state.heuristic_score
            result['no_cse_score'] = state.no_cse_score
            result['final_score'] = state.current.perf_score
            result['choices'] = state.choices
            result['total_reward'] = state.total_reward
            result['invalid_actions'] = state.invalid_action_count

        return result

    def get_rewards(self):
        """Returns the reward based on the change in performance score."""
        state = self.state
        prev = state.previous_score
        curr = state.current.perf_score

        # should not happen
        if np.isclose(prev, 0.0, rtol=1e-05, atol=1e-08, equal_nan=False):
            return 0.0

        return (prev - curr) / prev

    def _find_best_cse(self, state : JitCseEnvState):
        """Check to see if any of the CSE's are immediately better."""
        best = None

        for cse in state.current.cse_candidates:
            if cse.can_apply:
                method = self._jit_method(state.no_cse_method.index, JitMetrics=1, JitRLHook=1,
                                          JitRLHookCSEDecisions=state.choices)

                if method is not None:
                    if method.perf_score < state.current.perf_score:
                        if best is None or method.perf_score < best.perf_score:
                            best = method

        return best

    def _is_valid_action(self, action):
        state = self.state

        # Terminating is only valid if we have performed a CSE.  Doing no CSEs isn't allowed.
        if action is None:
            return state.choices

        curr = state.current
        candidate = curr.cse_candidates[action] if action < len(curr.cse_candidates) else None
        return candidate is not None and candidate.can_apply

    def _perform_cse(self, action, state : JitCseEnvState):
        """Performs the CSE and updates the state.  Returns True if successful, False if there was
        an error and we have to truncate this episode."""
        if action is None:
            return True    # We "successfully" performed no action, do not truncate

        result = self._jit_method(state.no_cse_method.index, JitMetrics=1, JitRLHook=1,
                                  JitRLHookCSEDecisions=state.choices)
        if result is None:
            return False

        state.choose(action, result)
        return True

    def _jit_method(self, m_id, *args, **kwargs):
        superpmi = self.__get_or_create_superpmi()

        result = superpmi.jit_with_retry(m_id, retry=2, *args, **kwargs)
        if result is None:
            self.__remove_method(m_id)

        elif np.isclose(result.perf_score, 0.0, rtol=1e-05, atol=1e-08, equal_nan=False):
            self.__remove_method(m_id)
            result = None

        return result

    def __select_method(self):
        if self.methods is None:
            superpmi = self.__get_or_create_superpmi()
            self.methods = [x.index for x in superpmi.enumerate_methods() if JitCseEnv.is_acceptable(x)]

        return np.random.choice(self.methods)

    def __remove_method(self, index):
        if self.methods is None:
            return

        self.methods = [x for x in self.methods if x != index]

    def create_superpmi(self):
        """Creates a superpmi instance."""
        return SuperPmi(self.core_root, self.mch)

    def __get_or_create_superpmi(self):
        if self.__superpmi is None:
            self.__superpmi = self.create_superpmi()
            self.__superpmi.start()

        return self.__superpmi

    @staticmethod
    def is_acceptable(method : MethodContext):
        """Returns True if the method is acceptable for training."""
        applicable = len([x for x in method.cse_candidates if x.viable])
        return MIN_CSE <= applicable and len(method.cse_candidates) <= MAX_CSE

    def render(self) -> None:
        state = self.state
        if state is not None:
            scores = [x.perf_score for x in state.results]
            print(f"{state.no_cse_method.index} heuristic_score: {state.heuristic_score} "
                  f"no_cse_score: {state.no_cse_score} choices:{state.choices} results:{scores}"
                  f"invalid_count:{state.invalid_action_count} ({state.no_cse_method.name})")

__all__ = ['JitCseEnv', 'JitCseEnvState']
