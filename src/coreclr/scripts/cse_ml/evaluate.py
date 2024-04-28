#!/usr/bin/python

"""Evaluates a model on a given dataset."""
from enum import Enum
import json
import os
import argparse
import shutil
import numpy as np
import pandas
import tqdm

from jitml import SuperPmi, JitRLModel, get_observation

class ModelResult(Enum):
    """Analysis errors."""
    OK = 0
    JIT_FAILED = 1
    SELECTED_NO_CSE = 2
    SELECTED_OUT_OF_BOUNDS = 3
    SELECTED_NON_VIABLE = 4
    SELECTED_ALREADY_APPLIED = 5

def jit_with_retry(superpmi, m_id, *args, **kwargs):
    """Attempts to JIT a method, retrying if necessary."""
    result = superpmi.jit_method(m_id, *args, **kwargs)
    if result is None:
        superpmi.stop()
        superpmi.start()
        result = superpmi.jit_method(m_id, *args, **kwargs)

    return result

def set_result(data, m_id, heuristic_score, no_cse_score, model_score, error = ModelResult.OK):
    """Sets the results for the given method id."""
    data["method_id"].append(m_id)
    data["heuristic_score"].append(heuristic_score)
    data["no_cse_score"].append(no_cse_score)
    data["model_score"].append(model_score)
    data["failed"].append(error)

def test_model(superpmi, jitrl : JitRLModel, method_ids, model_name):
    """Tests the model on the test set."""
    data = {
        "method_id" : [],
        "heuristic_score" : [],
        "no_cse_score" : [],
        "model_score" : [],
        "failed" : []
    }

    for m_id in tqdm.tqdm(method_ids,
                            desc=f"Processing {model_name}",
                            colour='green',
                            ncols=shutil.get_terminal_size().columns - 8,
                            ascii=False):
        # the original JIT method
        original = jit_with_retry(superpmi, m_id, JitMetrics=1)
        no_cse = jit_with_retry(superpmi, m_id, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=[0])

        if original is None or no_cse is None:
            set_result(results, m_id, 0, 0, 0, ModelResult.JIT_FAILED)
            continue

        choices = []
        results = []
        while True:
            prev_method = results[-1] if results else no_cse

            # If we have no more CSEs to apply, we are done.  We expect this not to happen on the first
            # iteration because we filter out methods that have no CSEs to apply.
            if not any(x.can_apply for x in prev_method.cse_candidates):
                assert choices
                set_result(data, m_id, original.perf_score, no_cse.perf_score, prev_method.perf_score)
                break

            obs = get_observation(prev_method)
            action_probabilities = jitrl.action_probabilities(obs)
            actions = [x for x in np.flip(np.argsort(action_probabilities))[0]
                      if x - 1 < len(prev_method.cse_candidates) and prev_method.cse_candidates[x - 1].can_apply]

            if not actions:
                set_result(data, m_id, original.perf_score, no_cse.perf_score, prev_method.perf_score,
                            ModelResult.SELECTED_NON_VIABLE)
                break

            if not choices and len(actions) > 1 and actions[0] == 0:
                actions = actions[1:]

            action = actions[0]
            if action == 0:
                set_result(data, m_id, original.perf_score, no_cse.perf_score, prev_method.perf_score)
                break

            # apply the CSE
            choices.append(action)
            new_method = jit_with_retry(superpmi, m_id, JitMetrics=1, JitRLHook=1, JitRLHookCSEDecisions=choices)
            if new_method is None:
                set_result(data, m_id, original.perf_score, no_cse.perf_score, prev_method.perf_score,
                            ModelResult.JIT_FAILED)
                break

            results.append(new_method)

            # mark choices as applied
            for c in choices:
                new_method.cse_candidates[c - 1].applied = True

    return pandas.DataFrame(data)

def load_data(data_dir):
    """Loads the data from the specified directory."""
    training_file = os.path.join(data_dir, "train.json")
    test_file = os.path.join(data_dir, "test.json")

    if not os.path.exists(training_file):
        raise FileNotFoundError(f"Training file {training_file} does not exist.")

    if not os.path.exists(test_file):
        raise FileNotFoundError(f"Test file {test_file} does not exist.")

    with open(training_file, 'r', encoding="utf8") as f:
        train = json.load(f)

    with open(test_file, 'r', encoding="utf8") as f:
        test = json.load(f)

    return test, train

def print_result(result, model, kind):
    """Prints the results."""

    print('-' * 40 + f" {model} results " + '-' * 40)
    print()

    print(f"{kind} results:")
    print()

    print("Comparisons:")
    no_jit_failure = result[result['failed'] != ModelResult.JIT_FAILED]

    # next calculate how often we improved on the heuristic
    improved = no_jit_failure[no_jit_failure['model_score'] < no_jit_failure['heuristic_score']]
    underperformed = no_jit_failure[no_jit_failure['model_score'] > no_jit_failure['heuristic_score']]
    print(f"Better than heuristic: {len(improved)}")
    print(f"Worse than heuristic: {len(underperformed)}")
    print(f"Same as heuristic: {len(no_jit_failure) - len(improved) - len(underperformed)}")
    print()

    # next calculate how often we improved on the no CSE score
    improved = no_jit_failure[no_jit_failure['model_score'] < no_jit_failure['no_cse_score']]
    underperformed = no_jit_failure[no_jit_failure['model_score'] > no_jit_failure['no_cse_score']]
    print(f"Better than no CSE: {len(improved)}")
    print(f"Worse than no CSE: {len(underperformed)}")
    print(f"Same as no CSE: {len(no_jit_failure) - len(improved) - len(underperformed)}")
    print()

    print("Failures:")
    print(f"Total: {len(result)}")
    print(f"Failed: {len(result[result['failed'] != ModelResult.OK])}")
    print(f"JIT Failed: {len(result[result['failed'] == ModelResult.JIT_FAILED])}")
    print(f"Selected No CSE: {len(result[result['failed'] == ModelResult.SELECTED_NO_CSE])}")
    print(f"Selected Out of Bounds: {len(result[result['failed'] == ModelResult.SELECTED_OUT_OF_BOUNDS])}")
    print(f"Selected Non Viable: {len(result[result['failed'] == ModelResult.SELECTED_NON_VIABLE])}")
    print(f"Selected Already Applied: {len(result[result['failed'] == ModelResult.SELECTED_ALREADY_APPLIED])}")
    print()

def evaluate(superpmi, jitrl, methods, model_name, csv_file) -> pandas.DataFrame:
    """Evaluate the model and save to the specified CSV file."""
    if os.path.exists(csv_file):
        return pandas.read_csv(csv_file)

    result = test_model(superpmi, jitrl, methods, model_name)
    result.to_csv(csv_file)
    return result

def enumerate_models(data_dir):
    """Enumerates the models in the specified directory."""
    def extract_number(file):
        return int(file.split("_")[-1]) if file.split("_")[-1].isdigit() else 100000000

    files = [os.path.splitext(file)[0] for file in os.listdir(data_dir) if file.endswith(".zip")]
    return sorted(files, key=extract_number, reverse=True)

def parse_args():
    """usage:  train.py [-h] [--core_root CORE_ROOT] [--parallel n] [--iterations i] model_path mch"""
    parser = argparse.ArgumentParser()
    parser.add_argument("model_path", help="The directory to load the model from.")
    parser.add_argument("mch", help="The mch file of functions to train on.")
    parser.add_argument("--core_root", default=None, help="The coreclr root directory.")
    parser.add_argument("--algorithm", default="PPO", help="The algorithm to use. (default: PPO)")

    args = parser.parse_args()
    if args.core_root is None:
        args.core_root = os.environ.get("CORE_ROOT", None)
        if args.core_root is None:
            raise ValueError("--core_root must be specified or set as the environment variable CORE_ROOT.")

    return args

def main(args):
    """Main entry point."""
    data_dir = os.path.join(args.model_path, args.algorithm)

    if not os.path.exists(data_dir):
        raise FileNotFoundError(f"Model directory {data_dir} does not exist.")

    # Load data.
    test_methods, train_methods = load_data(data_dir)

    for file in enumerate_models(data_dir):
        with SuperPmi(args.core_root, args.mch) as superpmi:
            # load the underlying model
            jitrl = JitRLModel(args.algorithm, data_dir)
            jitrl.load(os.path.join(data_dir, file))

            print(f"Evaluting model {file} on training and test data:")

            model_name = os.path.splitext(file)[0]

            filename = os.path.join(data_dir, f"{model_name}_test.csv")
            result = evaluate(superpmi, jitrl, test_methods, model_name, filename)
            print_result(result, model_name, "Test")

            filename = os.path.join(data_dir, f"{model_name}_train.csv")
            result = evaluate(superpmi, jitrl, train_methods, model_name, filename)
            print_result(result, model_name, "Train")

if __name__ == "__main__":
    main(parse_args())
