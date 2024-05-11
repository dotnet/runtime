"""A gymnasium environment for training RL to optimize the .Net JIT's CSE usage."""

from typing import Any, Dict, List, Optional
import gymnasium as gym
import numpy as np

from .method_context import JitType, MethodContext
from .superpmi import SuperPmi, SuperPmiContext
from .constants import (INVALID_ACTION_PENALTY, INVALID_ACTION_LIMIT, MAX_CSE, is_acceptable_for_cse)

# observation space
JITTYPE_ONEHOT_SIZE = 6
BOOLEAN_FEATURES = 7
FLOAT_FEATURES = 9
FEATURES = JITTYPE_ONEHOT_SIZE + BOOLEAN_FEATURES + FLOAT_FEATURES

# Scale up the reward to make it more meaningful.
REWARD_SCALE = 5.0

class JitCseEnv(gym.Env):
    """A gymnasium environment for CSE optimization selection in the JIT."""
    observation_columns : List[str] = [f"type_{JitType(i).name.lower()}" for i in range(1, 7)] + \
        [
            "can_apply", "live_across_call", "const", "shared_const", "make_cse", "has_call", "containable",
            "cost_ex", "cost_sz", "use_count", "def_count", "use_wt_cnt", "def_wt_cnt", "distinct_locals",
            "local_occurrences", "enreg_count"
        ]

    def __init__(self, context : SuperPmiContext, methods : Optional[List[int]] = None, **kwargs):
        super().__init__(**kwargs)

        self.pmi_context = context
        self.methods = methods or context.training_methods
        if not self.methods:
            raise ValueError("No methods to train on.")

        self.__superpmi : SuperPmi = None
        self.action_space = gym.spaces.Discrete(MAX_CSE + 1)
        self.observation_space = gym.spaces.Box(np.zeros((MAX_CSE, FEATURES)),
                                                np.ones((MAX_CSE, FEATURES)),
                                                dtype=np.float32)

        self.last_info : Optional[Dict[str,object]] = None

    def __del__(self):
        self.close()

    def close(self):
        """Closes the environment and cleans up resources."""
        super().close()
        if self.__superpmi is not None:
            self.__superpmi.stop()
            self.__superpmi = None

    def reset(self, *, seed: int | None = None, options: dict[str, Any] | None = None):
        super().reset(seed=seed, options=options)
        self.last_info = None

        failure_count = 0
        while True:
            index = self.__select_method()
            no_cse = self._jit_method_with_cleanup(index, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=[])
            original_heuristic = self._jit_method_with_cleanup(index, JitMetrics=1)
            if no_cse and original_heuristic:
                break

            failure_count += 1
            if failure_count > 512:
                raise ValueError("No valid methods found")

        observation = self.get_observation(no_cse)
        self.last_info = {
            'invalid_actions' : 0,
            'method_index' : index,
            'heuristic_method' : original_heuristic,
            'no_cse_method' : no_cse,
            'current' : no_cse,
            'total_reward' : 0.0,
            'observation' : observation,
            'action_is_valid' : None
        }

        return observation, self.last_info

    def step(self, action):
        # the last action is always to terminate
        if action == self.action_space.n - 1:
            action = None

        last_info = self.last_info
        if last_info is None:
            raise ValueError("Must call reset() before step()")

        info = last_info.copy()
        self.last_info = None

        # update action, ensure we have an up to date observation
        info['action'] = action
        del info['observation']

        # Note that we have not yet updated the info dictionary for previous and current, which means
        # info['current'] is the previous method at this point.  We do not update info's previous/current
        # until we are sure the method is JIT'ed successfully.
        previous = info['current']

        # Ensure the selected action is valid.
        info['action_is_valid'] = self._is_valid_action(action, previous)
        if info['action_is_valid']:
            current = self._jit_method_with_cleanup(info['method_index'], JitMetrics=1, JitRLHook=1,
                                    JitRLHookCSEDecisions=previous.cses_chosen + [action])

            if current is not None:
                observation = self.get_observation(current)
                truncated = False
                terminated = not current.cse_candidates or action is None
                reward = self.get_rewards(previous, current)

                info['previous'] = previous
                info['current'] = current

            else:
                # Don't set current or observation, as we should not be using them.
                observation = last_info['observation']
                truncated = True
                terminated = False
                reward = INVALID_ACTION_PENALTY

        else:
            # action was invalid
            info['invalid_actions'] += 1

            truncated = info['invalid_actions'] >= INVALID_ACTION_LIMIT
            terminated = False
            observation = last_info['observation']
            reward = INVALID_ACTION_PENALTY

        info['observation'] = observation
        info['total_reward'] += reward
        info['terminated'] = terminated
        info['truncated'] = truncated

        # These are reported only once, when the episode is done.
        if terminated:
            info['heuristic_score'] = info['heuristic_method'].perf_score
            info['no_cse_score'] = info['no_cse_method'].perf_score
            info['total_reward'] = info['total_reward']
            info['invalid_actions'] = info['invalid_actions']
            if 'current' in info:
                info['final_score'] = info['current'].perf_score

        self.last_info = info
        return observation, reward, terminated, truncated, info

    def get_rewards(self, prev_method : MethodContext, curr_method : MethodContext):
        """Returns the reward based on the change in performance score."""
        prev = prev_method.perf_score
        curr = curr_method.perf_score

        # should not happen
        if np.isclose(prev, 0.0):
            return 0.0

        return REWARD_SCALE * (prev - curr) / prev

    def _is_valid_action(self, action, method):
        # Terminating is only valid if we have performed a CSE.  Doing no CSEs isn't allowed.
        if action is None:
            return bool(method.cses_chosen)

        candidate = method.cse_candidates[action] if action < len(method.cse_candidates) else None
        return candidate is not None and candidate.can_apply

    @classmethod
    def get_observation(cls, method : MethodContext, fill=True):
        """Builds the observation from a method without normalizing the data."""
        tensors = []
        for cse in method.cse_candidates:
            tensor = []

            # one-hot encode the type
            one_hot = [0.0] * 6
            one_hot[cse.type - 1] = 1.0
            tensor.extend(one_hot)

            # boolean features
            tensor.extend([
                cse.can_apply, cse.live_across_call, cse.const, cse.shared_const, cse.make_cse, cse.has_call,
                cse.containable
            ])

            # float features
            tensor.extend([
                cse.cost_ex, cse.cost_sz, cse.use_count, cse.def_count, cse.use_wt_cnt, cse.def_wt_cnt,
                cse.distinct_locals, cse.local_occurrences, cse.enreg_count
            ])

            tensors.append(tensor)

        if fill:
            while len(tensors) < MAX_CSE:
                tensors.append([0.0] * FEATURES)

        observation = np.vstack(tensors)
        return observation


    def _jit_method_with_cleanup(self, m_id, *args, **kwargs):
        """Jits a method, but if it fails, we remove it from future consideration.  Note that the
        SuperPmi class will retry before returning None, so we know this method is not going to work."""
        superpmi = self.__get_or_create_superpmi()

        result = superpmi.jit_method(m_id, retry=2, *args, **kwargs)
        if result is None:
            self.__remove_method(m_id)

        elif np.isclose(result.perf_score, 0.0):
            self.__remove_method(m_id)
            result = None

        return result

    def __select_method(self):
        if self.methods is None:
            superpmi = self.__get_or_create_superpmi()
            self.methods = [x.index for x in superpmi.enumerate_methods() if is_acceptable_for_cse(x)]

        return np.random.choice(self.methods)

    def __remove_method(self, index):
        if self.methods is None:
            return

        self.methods = [x for x in self.methods if x != index]

    def __get_or_create_superpmi(self):
        if self.__superpmi is None:
            self.__superpmi = self.pmi_context.create_superpmi()
            self.__superpmi.start()

        return self.__superpmi

    def render(self) -> None:
        info = self.last_info
        if info is not None:
            print(f"{info['method_index']} heuristic_score: {info['heuristic_method'].perf_score} "
                  f"no_cse_score: {info['no_cse_method'].perf_score} choices:{info['current'].cses_chosen} "
                  f"invalid_count:{info['invalid_actions']} ({info['current'].name})")

__all__ = [JitCseEnv.__name__]
