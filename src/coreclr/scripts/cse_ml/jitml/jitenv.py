"""A gymnasium environment for training RL to optimize the .Net JIT's CSE usage."""

from typing import Any, List, Optional
import gymnasium as gym
import numpy as np

from .superpmi import SuperPmi, MethodContext
from .conversions import get_observation
from .constants import (MAX_CSE, MIN_CSE, BOOLEAN_FEATURES, FLOAT_FEATURES, FEATURES, REWARD_SCALE, REWARD_MIN,
                        REWARD_MAX, FOUND_BEST_REWARD, NO_BETTER_METHOD_REWARD, INVALID_ACTION_PENALTY,
                        INVALID_ACTION_LIMIT)

class JitEnvState:
    """The state of the JIT environment."""
    def __init__(self, no_cse_method : MethodContext, heuristic_method : MethodContext):
        self.no_cse_method = no_cse_method
        self.heuristic_method = heuristic_method
        self.choices = []
        self.results = []
        self.invalid_action_count = 0
        self.total_reward = 0.0

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
                continue

            if JitEnv.is_acceptable(no_cse):
                original_heuristic = self._jit_method(index, JitMetrics=1)
                if original_heuristic is None:
                    continue
                break

            failure_count += 1
            if failure_count > 512:
                raise ValueError("No valid methods found")

        self._state = JitEnvState(no_cse, original_heuristic)
        obs = self.get_observation(no_cse)
        info = self.get_info(self._state, False)
        return obs, info

    def step(self, action):
        # the last action is always to terminate
        if action == self.action_space.n - 1:
            action = None

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
            info = self.get_info(state, terminated)
            reward = INVALID_ACTION_PENALTY

            state.total_reward += reward
            return observation, reward, terminated, truncated, info

        # _perform_cse will return False if there was an error and we need to truncate
        truncated = not self._perform_cse(action, state)

        current = state.current
        observation = self.get_observation(current)
        terminated = action is None or not any((x for x in current.cse_candidates if x.can_apply))
        reward = self.get_rewards(state, terminated) if not truncated else INVALID_ACTION_PENALTY
        state.total_reward += reward

        # must be after updating total_reward
        info = self.get_info(state, terminated)

        if terminated or truncated:
            self._state = None

        return observation, reward, terminated, truncated, info

    def get_info(self, state : JitEnvState, terminated : bool):
        """Returns the info dictionary for the current state."""
        result = {
            'no_cse_method': state.no_cse_method,
            'heuristic_method': state.heuristic_method,
            'method' : state.current,
            'choices': state.choices,
            'results': state.results,
            'invalid_action_count': state.invalid_action_count
        }

        if terminated:
            result['heuristic_score'] = state.heuristic_score
            result['no_cse_score'] = state.no_cse_score
            result['final_score'] = state.current.perf_score
            result['choices'] = state.choices
            result['total_reward'] = state.total_reward
            result['invalid_actions'] = state.invalid_action_count

        return result

    def get_rewards(self, state : JitEnvState, completed : bool):
        """Returns the reward based on the change in performance score."""

        # always reward for how much better/worse we got for this choice
        prev = state.previous_score
        curr = state.current.perf_score
        rewards = (prev - curr) / prev

        # if we are done, check some extra conditions
        if completed:
            # First, check if there is a CSE we could have applied for an immediate improvement
            # and penalize for not finding that.
            any_other_cses = any((x for x in state.current.cse_candidates if x.can_apply))
            better_method = None
            if any_other_cses:

                better_method = self._find_best_cse(state)
                if better_method is not None:
                    # if there was a better method, penalize for that.
                    prev = better_method.perf_score
                    curr = state.current.perf_score
                    rewards += (prev - curr) / prev

                else:
                    # otherwise give a tiny reward for not taking a bad CSE
                    rewards += NO_BETTER_METHOD_REWARD

            # Next, check if we are better than the heuristic and reward/penalize for that.
            prev = state.heuristic_score
            curr = state.current.perf_score
            heuristic_reward = (prev - curr) / prev

            # But if there was a better method, don't reward for beating the heuristic.  In that case,
            # we don't want to reward the early termination
            if better_method is None:
                rewards += heuristic_reward

            elif heuristic_reward < 0:
                rewards += heuristic_reward

            # If we beat the heuristic and there wasn't another CSE we should have immediately applied,
            # add an additional reward for besting the heuristic.
            if any_other_cses and better_method is None and state.heuristic_score > state.current.perf_score:
                rewards += FOUND_BEST_REWARD

        rewards *= REWARD_SCALE
        rewards = np.clip(rewards, REWARD_MIN, REWARD_MAX)

        return rewards

    def _find_best_cse(self, state : JitEnvState):
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


    def get_observation(self, method : MethodContext):
        """Builds the observation from a method."""
        return get_observation(method)

    def _is_valid_action(self, action):
        state = self._state

        # Terminating is only valid if we have performed a CSE.  Doing no CSEs isn't allowed.
        if action is None:
            return state.choices

        curr = state.current
        candidate = curr.cse_candidates[action] if action < len(curr.cse_candidates) else None
        return candidate is not None and candidate.can_apply

    def _perform_cse(self, action, state : JitEnvState):
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
        superpmi = self.__get_superpmi()
        result = superpmi.jit_method(m_id, *args, **kwargs)

        # retry once
        if result is None:
            superpmi = self.__reset_superpmi()
            result = superpmi.jit_method(m_id, *args, **kwargs)

        if result is None:
            self.__remove_method(m_id)

        elif np.isclose(result.perf_score, 0.0, rtol=1e-05, atol=1e-08, equal_nan=False):
            self.__remove_method(m_id)
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
        return MIN_CSE <= len(method.cse_candidates) <= MAX_CSE

    def render(self) -> None:
        state = self._state
        if state is not None:
            scores = [x.perf_score for x in state.results]
            print(f"{state.no_cse_method.index} heuristic_score: {state.heuristic_score} "
                  f"no_cse_score: {state.no_cse_score} choices:{state.choices} results:{scores}"
                  f"invalid_count:{state.invalid_action_count} ({state.no_cse_method.name})")
