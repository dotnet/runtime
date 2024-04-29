#!/usr/bin/python

"""Trains JIT reinforcment learning on superpmi data."""
import os
import json
import time
import argparse
import numpy as np

from jitml import SuperPmi, JitCseEnv, JitRLModel

def enumerate_methods(core_root, mch):
    """Enumerates all methods in the mch file."""
    fn = os.path.basename(mch)
    print(f"Parsing {fn} (this may take a while)...")
    start = time.time()
    found = []
    with SuperPmi(core_root, mch) as pmi:
        for method in pmi.enumerate_methods():
            yield method
            found.append(method)

            if len(found) % 10000 == 0:
                print('.', end='', flush=True)

    print()
    print(f"Parsed {mch} in {time.time() - start:.2f} seconds.")
    print()

def get_acceptable_methods(core_root, mch):
    """Returns a list of acceptable methods to train on."""
    json_file = f"{mch}.json"

    if os.path.exists(json_file):
        with open(json_file, 'r', encoding="utf8") as f:
            return [int(x) for x in json.load(f)]

    sequence = enumerate_methods(core_root, mch)
    acceptable = [method.index for method in sequence if JitCseEnv.is_acceptable(method)]

    with open(json_file, 'w', encoding="utf8") as f:
        json.dump(acceptable, f)

    return acceptable

def split_data(output_dir, data, percent):
    """Splits the data into training and testing sets."""
    np.random.shuffle(data)

    num_test = int(len(data) * percent)
    test, train = data[num_test:], data[:num_test]

    with open(os.path.join(output_dir, "train.json"), 'w', encoding="utf8") as f:
        json.dump(train, f)

    with open(os.path.join(output_dir, "test.json"), 'w', encoding="utf8") as f:
        json.dump(test, f)

    return test, train

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

    args = parser.parse_args()
    if args.core_root is None:
        args.core_root = os.environ.get("CORE_ROOT", None)
        if args.core_root is None:
            raise ValueError("--core_root must be specified or set as the environment variable CORE_ROOT.")

    return args

def main(args):
    """Main entry point."""

    # Create directories.
    iterations = args.iterations if args.iterations is not None else 1_000_000
    output = os.path.join(args.model_path, args.algorithm)
    model_path = os.path.join(output, "model.zip")

    if not os.path.exists(output):
        os.makedirs(output, exist_ok=True)

    # Load data.
    acceptable = get_acceptable_methods(args.core_root, args.mch)
    test, train = split_data(output, acceptable, args.test_percent)
    print(f"Training with {len(train)} methods, holding back {len(test)} for testing.")

    # Train the model.
    rl = JitRLModel(args.algorithm, output)
    rl.train(args.core_root, args.mch, train, iterations=iterations, parallel=args.parallel)
    rl.save(model_path)

if __name__ == "__main__":
    main(parse_args())
