#!/usr/bin/python

"""Trains JIT reinforcment learning on superpmi data."""
import os
import json
import time
import argparse

from jitml import SuperPmi, JitEnv, JitRLModel

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
    acceptable = [method.index for method in sequence if JitEnv.is_acceptable(method)]

    with open(json_file, 'w', encoding="utf8") as f:
        json.dump(acceptable, f)

    return acceptable

def parse_args():
    """usage:  train.py [-h] [--core_root CORE_ROOT] [--parallel n] [--iterations i] model_path mch"""
    parser = argparse.ArgumentParser()
    parser.add_argument("model_path", help="The directory to save the model to.")
    parser.add_argument("mch", help="The mch file of functions to train on.")
    parser.add_argument("--core_root", default=None, help="The coreclr root directory.")
    parser.add_argument("--parallel", type=int, default=None, help="The number of parallel environments to use.")
    parser.add_argument("--iterations", type=int, default=None, help="The number of iterations to train for.")
    parser.add_argument("--algorithm", default="PPO", help="The algorithm to use. (default: PPO)")

    args = parser.parse_args()
    if args.core_root is None:
        args.core_root = os.environ.get("CORE_ROOT", None)
        if args.core_root is None:
            raise ValueError("--core_root must be specified or set as the environment variable CORE_ROOT.")

    return args

def main(args):
    """Main entry point."""
    acceptable = get_acceptable_methods(args.core_root, args.mch)
    print(f"Trainig with {len(acceptable)} methods.")

    iterations = args.iterations if args.iterations is not None else 1_000_000

    model_path = os.path.join(args.model_path, args.algorithm)
    if not os.path.exists(model_path):
        os.makedirs(model_path, exist_ok=True)

    model = os.path.join(model_path, "model.zip")

    rl = JitRLModel(args.algorithm, model_path)
    rl.train(args.core_root, args.mch, acceptable, iterations=iterations, parallel=args.parallel)
    rl.save(os.path.expanduser(model))

if __name__ == "__main__":
    main(parse_args())
