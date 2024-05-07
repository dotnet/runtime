"""The default machine learning agent which drives CSE optimization."""

# This file is expected to contain all of the torch/stable-baselines3 related code.  If possible,
# it would be best to avoid spilling those concepts outside of this file.  This is so that JitCseEnv can
# be used without requiring folks to use torch/stable-baselines3 and instead can use their own model.

import os
import json
from typing import List, Optional

import torch
import numpy as np

from stable_baselines3 import A2C, DQN, PPO
from stable_baselines3.common.callbacks import BaseCallback
from stable_baselines3.common.env_util import make_vec_env
from stable_baselines3.common.vec_env import SubprocVecEnv
import gymnasium as gym

from .method_context import MethodContext
from .jit_cse import JitCseEnv
from .superpmi import SuperPmiContext

class JitCseModel:
    """The raw implementation of the machine learning agent."""
    def __init__(self, algorithm, device='auto', make_env=None, ent_coef=0.01, verbose=False):
        if algorithm not in ('PPO', 'A2C', 'DQN'):
            raise ValueError(f"Unknown algorithm {algorithm}.  Must be one of: PPO, A2C, DQN")

        self.algorithm = algorithm
        self.device = device
        self.ent_coef = ent_coef
        self.verbose = verbose
        self.make_env = make_env
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

    def action_probabilities(self, obs):
        """Gets the probability of every action."""
        obs_tensor = torch.tensor(obs, dtype=torch.float32).unsqueeze(0).to(self._model.device)
        action_distribution = self._model.policy.get_distribution(obs_tensor)
        probs = action_distribution.distribution.probs
        return probs.cpu().detach().numpy()[0]

    def train(self, pmi_context : SuperPmiContext, training_methods : List[MethodContext], output_dir : str,
              iterations = None, parallel = None, progress_bar = True,
              wrappers : Optional[List[gym.Wrapper]] = None) -> str:
        """Trains a model from scratch.

        Args:
            pmi_context: The SuperPmiContext to use for training.
            training_methods : The methods to train on.
            output_dir: The directory to save the model to.
            iterations: The number of iterations to train for.  Defaults to 100,000.
            parallel: The number of parallel environments to use.  Defaults to single-process (None).
            progress_bar: Whether to display a progress bar.  Defaults to True.

        Returns:
            The full path to the trained model.
        """
        training_methods = [m.index for m in training_methods]
        os.makedirs(output_dir, exist_ok=True)

        def default_make_env():
            env = JitCseEnv(pmi_context, training_methods)
            if wrappers:
                for wrapper in wrappers:
                    env = wrapper(env)
            return env

        make_env = self.make_env or default_make_env
        if parallel is not None and parallel > 1:
            env = make_vec_env(make_env, n_envs=parallel, vec_env_cls=SubprocVecEnv)
        else:
            env = make_env()

        try:
            self._model = self._create(env, tensorboard_log=os.path.join(output_dir, 'logs'))

            iterations = 100_000 if iterations is None else iterations
            callback = LogCallback(self._model, output_dir) if self.algorithm in ('PPO', 'A2C') else None
            self._model.learn(iterations, progress_bar=progress_bar, callback=callback)

            save_path = os.path.join(output_dir, self.algorithm.lower() +'.zip')
            self.save(save_path)
            return save_path

        finally:
            env.close()

    def _create(self, env, **kwargs):
        alg = self.__get_algorithm()
        if alg == PPO:
            return alg('MlpPolicy', env, device=self.device, ent_coef=self.ent_coef, verbose=self.verbose, **kwargs)

        return alg('MlpPolicy', env, device=self.device, verbose=self.verbose, **kwargs)

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

class LogCallback(BaseCallback):
    """A callback to log reward values to tensorboard and save the best models."""
    # pylint: disable=too-many-instance-attributes

    def __init__(self, model : PPO | A2C, save_dir : str, last_model_freq = 500_000):
        super().__init__()

        self.model = model
        self.next_save = model.n_steps
        self.last_model_freq = last_model_freq
        self.last_model_next_save = self.last_model_freq

        self.best_reward = -np.inf

        self.save_dir = save_dir

        self._rewards = []
        self._invalid_choices = []
        self._result_vs_heuristic = []
        self._result_vs_no_cse = []
        self._better_or_worse = []
        self._choice_count = []


    def _on_step(self) -> bool:
        self._update_stats()

        if self.n_calls > self.next_save:
            self.next_save += self.model.n_steps

            rew_mean = np.mean(self._rewards) if self._rewards else -np.inf
            if rew_mean > self.best_reward:
                self.best_reward = rew_mean
                self._save_incremental(rew_mean, os.path.join(self.save_dir, 'best_reward.zip'))

            if self.model.num_timesteps >= self.last_model_next_save:
                self.last_model_next_save += self.last_model_freq
                self._save_incremental(rew_mean, os.path.join(self.save_dir, f'ppo_{self.model.num_timesteps}.zip'))

            if self._invalid_choices:
                self.logger.record('results/invalid_choices', np.mean(self._invalid_choices))

            if self._result_vs_heuristic:
                self.logger.record('results/vs_heuristic', np.mean(self._result_vs_heuristic))

            if self._result_vs_no_cse:
                self.logger.record('results/vs_no_cse', np.mean(self._result_vs_no_cse))

            if self._better_or_worse:
                self.logger.record('results/better_than_heuristic', np.mean(self._better_or_worse))

            if self._choice_count:
                self.logger.record('results/num_cse', np.mean(self._choice_count))

            self._rewards.clear()
            self._invalid_choices.clear()
            self._result_vs_heuristic.clear()
            self._result_vs_no_cse.clear()
            self._better_or_worse.clear()
            self._choice_count.clear()

        return True

    def _update_stats(self):
        for info in self.locals['infos']:
            if 'final_score' not in info:
                continue

            final = info['final_score']
            heuristic = info['heuristic_score']
            no_cse = info['no_cse_score']

            if heuristic != 0:
                self._result_vs_heuristic.append((heuristic - final) / heuristic)

            if no_cse != 0:
                self._result_vs_no_cse.append((no_cse - final) / no_cse)

            self._better_or_worse.append(1 if final < heuristic else -1 if final < heuristic else 0)
            self._choice_count.append(len(info['current'].cses_chosen))
            self._rewards.append(info['total_reward'])

    def _save_incremental(self, reward, save_path):
        self.model.save(save_path)

        metadata = { "iterations" : self.num_timesteps, 'reward' : reward}
        with open(save_path + '.json', 'w', encoding='utf-8') as f:
            json.dump(metadata, f, indent=4)

__all__ = [JitCseModel.__name__]
