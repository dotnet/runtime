#!/usr/bin/python

"""Trains JIT reinforcment learning on superpmi data."""
import os
import argparse

from jitml import SuperPmiContext, JitCseEnv, JitCseModel, DeepCseRewardWrapper

def validate_core_root(core_root):
    """Validates and returns the core_root directory."""
    core_root = core_root or os.environ.get("CORE_ROOT", None)
    if core_root is None:
        raise ValueError("--core_root must be specified or set as the environment variable CORE_ROOT.")

    return core_root

def parse_args():
    """usage:  train.py [-h] [--core_root CORE_ROOT] [--parallel n] [--iterations i] model_path mch"""
    parser = argparse.ArgumentParser()
    parser.add_argument("model_path", help="The directory to save the model to.")
    parser.add_argument("mch", help="The mch file of functions to train on.")
    parser.add_argument("--core_root", default=None, help="The coreclr root directory.")
    parser.add_argument("--parallel", type=int, default=None, help="The number of parallel environments to use.")
    parser.add_argument("--iterations", type=int, default=None, help="The number of iterations to train for.")
    parser.add_argument("--algorithm", default="PPO", help="The algorithm to use. (default: PPO)")
    parser.add_argument("--test_percent", type=float, default=0.1,
                        help="The percentage of data to use for testing. (default: 0.1)")
    parser.add_argument("--deep_rewards", action='store_true', help="Use smarter rewards. (default: False)")

    args = parser.parse_args()
    args.core_root = validate_core_root(args.core_root)
    return args

def deep_reward_make_env(spmi_ctx: SuperPmiContext):
    """Returns a JitCseEnv with deep rewards."""
    def make_env():
        env = JitCseEnv(spmi_ctx, spmi_ctx.training_methods)
        env = DeepCseRewardWrapper(env)
        return env

    return make_env

def main(args):
    """Main entry point."""
    output_dir = args.model_path
    if not os.path.exists(output_dir):
        os.makedirs(output_dir, exist_ok=True)

    # Load or create the superpmi context.
    spmi_file = args.mch + ".json"
    if os.path.exists(spmi_file):
        ctx = SuperPmiContext.load(spmi_file)
    else:
        print(f"Creating SuperPmiContext '{spmi_file}', this may take several minutes...")
        ctx = SuperPmiContext(core_root=args.core_root, mch=args.mch)
        ctx.find_methods_and_split(args.test_percent)
        ctx.save(spmi_file)

    print(f"Training with {len(ctx.training_methods)} methods, holding back {len(ctx.test_methods)} for testing.")

    # Define our own environment (with wrappers) if requested.
    make_env = deep_reward_make_env(ctx) if args.deep_rewards else None

    # Train the model.
    rl = JitCseModel(args.algorithm, make_env=make_env)

    iterations = args.iterations if args.iterations is not None else 1_000_000
    path = rl.train(ctx, output_dir, iterations=iterations, parallel=args.parallel)
    print(f"Model saved to: {path}")

if __name__ == "__main__":
    main(parse_args())
