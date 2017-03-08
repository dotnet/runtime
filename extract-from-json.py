#!/usr/bin/python

import argparse
import json
import sys

def parse_args():
    parser = argparse.ArgumentParser(
        description="""Extracts information from a json file by navigating the JSON object using a
            sequence of property accessors and returning the JSON subtree, or the raw data, found
            at that location."""
    )

    parser.add_argument(
        '-f', '--file',
        metavar='<project.json>',
        help="Path to project.json file to parse",
        required=True,
    )

    parser.add_argument(
        'property',
        metavar='property_name',
        help="""Name of property to extract using object notation.
            Pass multiple values to drill down into nested objects (in order).""",
        nargs='*',
    )

    parser.add_argument(
        '-r', '--raw',
        help="""Dumps the raw object found at the requested location.
            If omitted, returns a JSON formatted object instead.""",
        action='store_true',
        default=False
    )

    return parser.parse_args()

def main():
    args = parse_args()

    with open(args.file) as json_file:
        selected_property = json.load(json_file)

    for prop in args.property:
        selected_property = selected_property[prop]

    if args.raw:
        print(selected_property)
    else:
        print(json.dumps(selected_property))

    return 0

if __name__ == "__main__":
    sys.exit(main())
