"""A gymnasium environment for training RL to optimize the .Net JIT's CSE usage."""

import random
from typing import Any, List, Optional
import gymnasium as gym
import numpy as np

from .superpmi import CseCandidate, SuperPmi, MethodContext, JITTYPE_ONEHOT_SIZE

MAX_CSE = 16
MIN_CSE = 3

BOOLEAN_FEATURES = JITTYPE_ONEHOT_SIZE + 7
FLOAT_FEATURES = 9
FEATURES = BOOLEAN_FEATURES + FLOAT_FEATURES

INVALID_ACTION_PENALTY = -0.01
INVALID_ACTION_LIMIT = 128

class JitEnvState:
    """The state of the JIT environment."""
    def __init__(self, method : MethodContext, heuristic_score : float, no_cse_score : float):
        self.method = method
        self.heuristic_score = heuristic_score
        self.no_cse_score = no_cse_score
        self.choices = []
        self.results = []
        self.invalid_action_count = 0

    def choose(self, action : int, result : MethodContext):
        """Chooses an action and updates the state."""
        self.choices.append(action)
        self.results.append(result)
        for choice in self.choices:
            index = choice - 1
            if index >= 0:
                assert result.cse_candidates[index].index == index
                result.cse_candidates[index].applied = True

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

        return self.method

    @property
    def previous(self):
        """The previous method JIT'ed."""
        if not self.results:
            return None

        if len(self.results) > 1:
            return self.results[-2]

        return self.method

class JitEnv(gym.Env):
    """A gymnasium environment for the JIT."""
    def __init__(self, core_root : str, mch : str, methods : Optional[List[int]] = None):
        self.core_root = core_root
        self.mch = mch
        self._state = None

        self.__superpmi = None
        self.methods = methods

        lower_bounds = np.zeros((MAX_CSE, FEATURES))
        upper_bounds = np.ones((MAX_CSE, FEATURES))
        upper_bounds[:, BOOLEAN_FEATURES:] = np.full((MAX_CSE, FLOAT_FEATURES), np.inf)
        self.observation_space = gym.spaces.Box(lower_bounds, upper_bounds, dtype=np.float32)

        self.action_space = gym.spaces.Discrete(MAX_CSE + 1)

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
                print(f"Failed to JIT method {index}")
                continue

            if JitEnv.is_acceptable(no_cse):
                original_heuristic = self._jit_method(index, JitMetrics=1)
                if original_heuristic is None:
                    print(f"Failed to JIT method {index}")
                    continue
                break

            failure_count += 1
            if failure_count > 512:
                raise ValueError("No valid methods found")


        no_cse_score = no_cse.perf_score
        heuristic_score = original_heuristic.perf_score

        self._state = JitEnvState(no_cse, heuristic_score, no_cse_score)
        obs = self.get_observation(no_cse)
        info = self.get_info(self._state)
        return obs, info

    def step(self, action):
        state = self._state
        if state is None:
            raise ValueError("Must call reset() before step()")

        terminated = False
        truncated = False

        # validate that the selected the action is valid
        if not self._is_valid_action(action):
            state.invalid_action_count += 1

            truncated = state.invalid_action_count >= INVALID_ACTION_LIMIT
            if terminated or truncated:
                self._state = None

            observation = self.get_observation(state.current)
            info = self.get_info(state)
            return observation, INVALID_ACTION_PENALTY, terminated, truncated, info

        # JIT the method and update state with the result, this can return False if there was an
        # issue with JIT'ing the method.
        truncated = not self._take_one_action(action, state)

        current = state.current
        observation = self.get_observation(current)
        terminated = action == 0 or not any((x for x in current.cse_candidates if x.can_apply))
        reward = self.get_rewards(state, terminated) if not truncated else 0.0
        info = self.get_info(state)

        if terminated or truncated:
            self._state = None

        return observation, reward, terminated, truncated, info

    def get_info(self, state : JitEnvState):
        """Returns the info dictionary for the current state."""
        return {
            'method': state.method,
            'candidates' : state.current.cse_candidates,
            'choices': state.choices,
            'results': state.results,
            'invalid_action_count': state.invalid_action_count
        }

    def get_rewards(self, state : JitEnvState, completed : bool):
        """Returns the reward based on the change in performance score."""
        prev = state.heuristic_score if completed else state.previous_score
        curr = state.current.perf_score

        change = (prev - curr) / prev
        return change

    def get_observation(self, method : MethodContext):
        """Builds the observation from a method."""
        tensors = [self._get_tensor(x) for i, x in enumerate(method.cse_candidates) if i < MAX_CSE]
        while len(tensors) < MAX_CSE:
            tensors.append(np.zeros(FEATURES))

        result = np.vstack(tensors)

        return result

    def _get_tensor(self, cse : CseCandidate):
        result = np.zeros(FEATURES)

        result[:JITTYPE_ONEHOT_SIZE] = cse.type.one_hot
        bool_features = self._get_boolean_features(cse)
        result[JITTYPE_ONEHOT_SIZE:JITTYPE_ONEHOT_SIZE + len(bool_features)] = bool_features
        result[JITTYPE_ONEHOT_SIZE + len(bool_features):] = self._get_float_features(cse)

        return result

    def _get_boolean_features(self, cse : CseCandidate):
        return [cse.applied, cse.live_across_call, cse.const, cse.shared_const, cse.make_cse, cse.has_call,
                cse.containable]

    def _get_float_features(self, cse : CseCandidate):
        return [cse.cost_ex, cse.cost_sz, cse.use_count, cse.def_count, cse.use_wt_cnt, cse.def_wt_cnt,
                cse.distinct_locals, cse.local_occurrences, cse.enreg_count]

    def _is_valid_action(self, action):
        state = self._state
        if action == 0:
            return not state.choices or state.choices[-1] != 0

        index = action - 1
        curr = state.current
        candidate = curr.cse_candidates[index] if index < len(curr.cse_candidates) else None
        if candidate is None:
            return False

        return candidate.can_apply

    def _take_one_action(self, action, state : JitEnvState):
        result = self._jit_method(state.method.index, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=state.choices)
        if result is None:
            return False

        state.choose(action, result)
        return True

    def _jit_method(self, index, *args, **kwargs):
        superpmi = self.__get_superpmi()
        result = superpmi.jit_method(index, *args, **kwargs)

        if result is None:
            superpmi = self.__reset_superpmi()
            result = superpmi.jit_method(index, *args, **kwargs)

        if result is None:
            print (f"Failed to JIT method {index} - removing")
            self.__remove_method(index)

        elif np.isclose(result.perf_score, 0.0, rtol=1e-05, atol=1e-08, equal_nan=False):
            print(f"Method {index} has a perf score of 0.0 - removing")
            self.__remove_method(index)
            result = None

        return result

    def __reset_superpmi(self):
        superpmi = self.__superpmi
        self.__superpmi = None
        if superpmi is not None:
            superpmi.stop()

        return self.__get_superpmi()

    def __select_method(self):
        if self.methods is None:
            superpmi = self.__get_superpmi()
            self.methods = [x.index for x in superpmi.enumerate_methods() if JitEnv.is_acceptable(x)]

        return np.random.choice(self.methods)

    def __remove_method(self, index):
        if self.methods is None:
            return

        self.methods = [x for x in self.methods if x != index]

    def __get_superpmi(self):
        if self.__superpmi is None:
            self.__superpmi = SuperPmi(self.core_root, self.mch)
            self.__superpmi.start()

        return self.__superpmi

    @staticmethod
    def is_acceptable(method : MethodContext):
        """Returns True if the method is acceptable for training."""
        applicable = len([x for x in method.cse_candidates if x.can_apply])
        return MIN_CSE <= applicable <= MAX_CSE

    def render(self) -> None:
        state = self._state
        if state is not None:
            scores = [x.perf_score for x in state.results]
            print(f"{state.method.index} heuristic_score: {state.heuristic_score} no_cse_score: {state.no_cse_score} "
                  f"choices:{state.choices} results:{scores} {state.invalid_action_count} ({state.method.name})")
