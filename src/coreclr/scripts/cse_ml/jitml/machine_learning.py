"""The machine learning agent which drives CSE optimization."""

import os
from typing import List

from stable_baselines3 import A2C, DQN, PPO
from stable_baselines3.common.env_util import make_vec_env
from stable_baselines3.common.vec_env import SubprocVecEnv

from .jitenv import JitEnv
from .superpmi import MethodContext

class JitRLModel:
    """The raw implementation of the machine learning agent."""
    def __init__(self, algorithm, model_path, device='auto', ent_coef=0.01, verbose=False):
        if algorithm not in ('PPO', 'A2C', 'DQN'):
            raise ValueError(f"Unknown algorithm {algorithm}.  Must be one of: PPO, A2C, DQN")

        self.algorithm = algorithm
        self.model_path = model_path
        self.device = device
        self.ent_coef = ent_coef
        self.verbose = verbose
        self._model = None

    def load(self, path):
        """Loads the model from the specified path."""
        alg = self.__get_algorithm()
        self._model = alg.load(path, device=self.device)
        return self._model

    def save(self, path):
        """Saves the model to the specified path."""
        self._model.save(path)

    @property
    def num_timesteps(self):
        """Returns the number of timesteps the model has been trained for."""
        return self._model.num_timesteps if self._model is not None else 0

    def predict(self, obs, deterministic = False):
        """Predicts the action to take based on the observation."""
        action, _ = self._model.predict(obs, deterministic=deterministic)
        return action

    def train(self, core_root : str, mch : str, methods : List[MethodContext] = None,
              iterations = None, parallel = None):
        """Trains the model from scratch."""
        model_dir = os.path.join(self.model_path)
        os.makedirs(model_dir, exist_ok=True)

        iterations = 100_000 if iterations is None else iterations

        def make_env():
            return JitEnv(core_root, mch, methods)

        if parallel is not None and parallel > 1:
            env = make_vec_env(make_env, n_envs=parallel, vec_env_cls=SubprocVecEnv)
        else:
            env = make_env()

        try:
            ml_model = self._create(env, tensorboard_log=os.path.join(model_dir, 'logs'))
            ml_model.learn(iterations, progress_bar=True)

        finally:
            env.close()

    def _create(self, env, **kwargs):
        alg = self.__get_algorithm()
        return alg('MlpPolicy', env, device=self.device, ent_coef=self.ent_coef, verbose=self.verbose, **kwargs)

    def __get_algorithm(self):
        match self.algorithm:
            case 'PPO':
                return PPO
            case 'A2C':
                return A2C
            case 'DQN':
                return DQN
            case _:
                raise ValueError(f"Unknown algorithm {self.algorithm}.  Must be one of: PPO, A2C, DQN")
