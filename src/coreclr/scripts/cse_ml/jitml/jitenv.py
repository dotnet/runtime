"""A gymnasium environment for training RL to optimize the .Net JIT's CSE usage."""

from typing import Any, List, Optional
import gymnasium as gym
import numpy as np
import random

from .superpmi import SuperPmi, MethodContext

MAX_CSE = 12
MIN_CSE = 3

# 5 types (one-hot), 6 bools, 9 floats
COMPONENTS = 5 + 6 + 9
INVALID_ACTION_PENALTY = -1

class EnvState:
    def __init__(self, method : MethodContext):
        self.set_method(method)
        self.choices = []
        self.scores = []
        self.heuristic_score = method.perf_score

    def set_method(self, method):
        self.method = method
        self.cse_candidates = [x for x in method.cse_candidates if x.viable]

class JitEnv(gym.Env):
    """A gymnasium environment for the JIT."""
    def __init__(self, superpmi : SuperPmi, methods : Optional[List[MethodContext]]):
        self._state = None
        self.superpmi = superpmi
        if methods is None:
            methods = [x for x in self.superpmi.enumerate_methods()
                       if x.num_cse_candidate >= MIN_CSE and x.num_cse_candidate <= MAX_CSE]
        else:
            methods = [x for x in methods
                       if x.num_cse_candidate >= MIN_CSE and x.num_cse_candidate <= MAX_CSE]

        self.methods = methods

        # Create a box observation space
        total_dimensions = MAX_CSE * COMPONENTS
        lower_bounds = np.zeros(total_dimensions)
        upper_bounds = np.ones(total_dimensions)
        for i in range(total_dimensions):
            if i % COMPONENTS >= 11:
                upper_bounds[i] = np.inf

        self.observation_space = gym.spaces.Box(lower_bounds, upper_bounds, dtype=np.float32)
        self.action_space = gym.spaces.Discrete(MAX_CSE + 1)
        self.state = None

    def step(self, action):
        terminated = False
        truncated = False
        if action == 0:
            result = self.superpmi.jit_method(self.state.method.index, JitMetrics=1, JitRLHook=1,
                                JitRLHookCSEDecisions=self._state.choices)

            terminated = True


        else:
            self.state.choices.append(action)
            result = self.superpmi.jit_method(self.state.method.index, JitMetrics=1, JitRLHook=1,
                                JitRLHookCSEDecisions=self._state.choices)

            self.state.scores.append(result.perf_score)
            self.state.set_method(result)
            if not result.cse_candidates:
                terminated = True

        # todo: rewards and info
        reward = 0
        info = {}

        return self.state, reward, terminated, truncated, info

    def reset(self, *, seed: int | None = None, options: dict[str, Any] | None = None):
        # randomly select a method
        first = method = random.choice(self.methods)
        state = EnvState(method)

        # find the total number of method.cse_candidates where valid
        while len([x for x in method.cse_candidates if x['viable']]) < MIN_CSE:
            method = random.choice(self.methods)
            state = EnvState(method)
            if method == first:
                raise ValueError("No valid methods found")

        self.state = state

        obs, _, _, _, info = self.step(0)
        return obs, info

    def render(self) -> None:
        pass
