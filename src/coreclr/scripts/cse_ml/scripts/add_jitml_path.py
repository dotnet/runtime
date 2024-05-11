"""Adds jitml to the path for imports.  Import add_jitml_path before any jitml."""

import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.realpath(__file__))))
