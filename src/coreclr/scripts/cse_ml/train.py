#!/usr/bin/python

"""Trains JIT reinforcment learning on superpmi data."""
import os
import argparse

from jitml import SuperPmiContext, JitCseModel, OptimalCseWrapper, NormalizeFeaturesWrapper, split_for_cse

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
    parser.add_argument("--test-percent", type=float, default=0.1,
                        help="The percentage of data to use for testing. (default: 0.1)")
    parser.add_argument("--reward-optimal-cse", action='store_true', help="Use smarter rewards. (default: False)")
    parser.add_argument("--normalize-features", action='store_true', help="Normalize features. (default: False)")

    args = parser.parse_args()
    args.core_root = validate_core_root(args.core_root)
    return args

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
        ctx = SuperPmiContext.create_from_mch(args.mch, args.core_root)
        ctx.save(spmi_file)

    test_methods, training_methods = split_for_cse(ctx.methods, 0.1)
    print(f"Training with {len(training_methods)} methods, holding back {len(test_methods)} for testing.")

    # Define our own environment (with wrappers) if requested.

    # Train the model.
    rl = JitCseModel(args.algorithm)

    wrappers = []
    if args.reward_optimal_cse:
        wrappers.append(OptimalCseWrapper)

    if args.normalize_features:
        wrappers.append(NormalizeFeaturesWrapper)

    iterations = args.iterations if args.iterations is not None else 1_000_000
    path = rl.train(ctx, training_methods, output_dir, iterations=iterations, parallel=args.parallel, wrappers=wrappers)
    print(f"Model saved to: {path}")

if __name__ == "__main__":
    main(parse_args())
