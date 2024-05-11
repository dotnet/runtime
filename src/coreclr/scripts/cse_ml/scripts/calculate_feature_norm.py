#!/usr/bin/python
"""Uses a single .mch file to calculate feature normalization to scale all features to [0, 1]."""

import argparse
import os

import numpy as np
import tqdm
from pandas import DataFrame, read_csv

import add_jitml_path as _ # pylint: disable=import-error
from jitml import SuperPmiContext, JitCseEnv
from train import validate_core_root

def get_feature_data(ctx : SuperPmiContext, save=True) -> DataFrame:
    """Returns a DataFrame of all features in a .mch file."""

    csv_file = ctx.mch + ".features.csv"
    if save and os.path.exists(csv_file):
        return read_csv(csv_file)

    feature_data = {}
    column_names = JitCseEnv.observation_columns
    for col in column_names:
        feature_data[col] = []

    ctx.resplit_data(0)
    with ctx.create_superpmi() as superpmi:
        for m_id in tqdm.tqdm(ctx.training_methods):
            method = superpmi.jit_method(m_id, JitMetrics=1, JitRLHook=1)
            if method is None:
                continue

            observation = JitCseEnv.get_observation(method, fill=False)
            for features in observation:
                for c, name in enumerate(column_names):
                    value = features[c]
                    feature_data[name].append(value)

    if save:
        df = DataFrame(feature_data)
        df.to_csv(csv_file, index=False)

    return df

def get_scaling(df : DataFrame):
    """Calculate scaling."""
    df = df.copy()

    # Heuristic for using log1p:
    # If the standard deviation is greater than 1000 and the max is greater than 10000, use log1p.
    std_over_1000 = (df.std() > 100) & (df.max() > 10000)
    use_log1p = std_over_1000.values

    # apply log1p to columns that need it
    log1p_cols = df.columns[use_log1p]
    for col in log1p_cols:
        df[col] = np.log1p(df[col])

    # calculate the scaling after log1p
    diff = df.max() - df.min()
    subtract = [0] * len(diff)
    scale = [1] * len(diff)

    for i, col in enumerate(df.columns):
        if df[col].max() == df[col].min():
            continue

        diff_col = diff[col]
        subtract[i] = df[col].min()
        scale[i] = 1 / diff_col

    return subtract, scale, list(use_log1p)

def _print_stats(df):
    stats = {
        "min": df.min(),
        "max": df.max(),
        "avg": df.mean(),
        "std": df.std(),
    }
    df_stats = DataFrame(stats)
    print(df_stats)

def parse_args():
    """Parses the command line arguments."""
    parser = argparse.ArgumentParser()
    parser.add_argument("mch", help="The mch file to calculate feature normalization on.")
    parser.add_argument("--core_root", default=None, help="The core_root directory.")

    args = parser.parse_args()
    args.core_root = validate_core_root(args.core_root)
    return parser.parse_args()

def main(args):
    """Entry point for the script."""
    file_path = args.mch + ".json"
    if os.path.exists(file_path):
        ctx = SuperPmiContext.load(file_path)

    else:
        ctx = SuperPmiContext(core_root=args.core_root, mch=args.mch)
        ctx.find_methods_and_split(0.1)
        ctx.save(file_path)

    features = get_feature_data(ctx)
    print("Unnormalized feature statistics:")
    _print_stats(features)
    print()

    # Normalize
    subtract, scale, use_log1p = get_scaling(features)
    for i, col in enumerate(features.columns):
        if use_log1p[i]:
            features[col] = np.log1p(features[col])

    features = (features - subtract) * scale

    print("Normalized feature statistics:")
    _print_stats(features)

    print()
    print(f"subract = {repr(subtract)}")
    print(f"scales = {repr(scale)}")
    print(f"use_log1p = {repr(use_log1p)}")

if __name__ == "__main__":
    main(parse_args())
