"""Functions for interacting with SuperPmi."""

import json
import os
import subprocess
import re
from typing import Iterable, List, Optional
from pydantic import BaseModel, field_validator

from .method_context import MethodContext

class SuperPmiContext(BaseModel):
    """Information about how to construct a SuperPmi object.  This tells us where to find CLR's CORE_ROOT with
    the superpmi and jit, and which .mch file to use.  Additionally, it tells us which methods to use for training
    and testing."""
    core_root : str
    mch : str
    jit : Optional[str] = None
    methods : Optional[List[MethodContext]] = []

    @field_validator('core_root', 'mch', mode='before')
    @classmethod
    def _validate_path(cls, v):
        if not os.path.exists(v):
            raise FileNotFoundError(f"{v} does not exist.")

        return v

    @field_validator('jit', mode='before')
    @classmethod
    def _validate_optional_path(cls, v):
        if v is not None and not os.path.exists(v):
            raise FileNotFoundError(f"{v} does not exist.")

        return v

    @staticmethod
    def create_from_mch(mch : str, core_root : str,  jit : Optional[str] = None) -> 'SuperPmiContext':
        """Loads the SuperPmiContext from the specified arguments."""
        result = SuperPmiContext(core_root=core_root, mch=mch, jit=jit)

        methods = []
        with SuperPmi(result) as superpmi:
            for method in superpmi.enumerate_methods():
                methods.append(method)

        result.methods = methods
        return result

    def save(self, file_path:str):
        """Saves the SuperPmiContext to a file."""
        with open(file_path, 'w', encoding="utf8") as f:
            json.dump(self.model_dump(), f)

    @staticmethod
    def load(file_path:str):
        """Loads the SuperPmiContext from a file."""
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"{file_path} does not exist.")

        with open(file_path, 'r', encoding="utf8") as f:
            data = json.load(f)
            return SuperPmiContext(**data)

    def create_superpmi(self, verbosity:str = 'q'):
        """Creates a SuperPmi object from this context."""
        return SuperPmi(self, verbosity)

class SuperPmi:
    """Controls one instance of superpmi."""
    def __init__(self, context : SuperPmiContext, verbosity:str = 'q'):
        """Constructor.
        core_root is the path to the coreclr build, usually at [repo]/artifiacts/bin/coreclr/[arch]/.
        verbosity is the verbosity level of the superpmi process. Default is 'q'."""
        self._process = None
        self._feature_names = None
        self.context = context
        self.verbose = verbosity

        if os.name == 'nt':
            self.superpmi_path = os.path.join(context.core_root, 'superpmi.exe')
            self.jit_path = os.path.join(context.core_root, context.jit if context.jit else 'clrjit.dll')
        else:
            self.superpmi_path = os.path.join(context.core_root, 'superpmi')
            self.jit_path = os.path.join(context.core_root, context.jit if context.jit else 'libclrjit.so')

        if not os.path.exists(self.superpmi_path):
            raise FileNotFoundError(f"superpmi {self.superpmi_path} does not exist.")

        if not os.path.exists(self.jit_path):
            raise FileNotFoundError(f"jit {self.jit_path} does not exist.")

    def __del__(self):
        self.stop()

    def __enter__(self):
        self.start()
        return self

    def __exit__(self, *_):
        self.stop()

    def jit_method(self, method_or_id : int | MethodContext, retry=1, **options) -> MethodContext:
        """Attempts to jit the method, and retries if it fails up to "retry" times."""
        if retry < 1:
            raise ValueError("retry must be greater than 0.")

        for _ in range(retry):
            result = self.__jit_method(method_or_id, **options)
            if result is not None:
                return result

            self.stop()
            self.start()

        return None

    def __jit_method(self, method_or_id : int | MethodContext, **options) -> MethodContext:
        """Jits the method given by id or MethodContext."""
        process = self._process
        if process is None:
            raise ValueError("SuperPmi process is not running.  Use a 'with' statement.")

        if isinstance(method_or_id, MethodContext):
            method_or_id = method_or_id.index

        if "JitMetrics" not in options:
            options["JitMetrics"] = 1

        if self._feature_names is None and "JitRLHook" in options:
            options['JitRLHookEmitFeatureNames'] = 1

        torun = f"{method_or_id}!"
        torun += "!".join([f"{key}={value}" for key, value in options.items()])

        if not process.poll():
            self.stop()
            process = self.start()

        process.stdin.write(f"{torun}\n".encode('utf-8'))
        process.stdin.flush()

        result = None
        output = ""

        while not output.startswith('[streaming] Done.'):
            output = process.stdout.readline().decode('utf-8').strip()
            if output.startswith(';'):
                result = self._parse_method_context(output)

        return result

    def enumerate_methods(self) -> Iterable[MethodContext]:
        """List all methods in the mch file."""
        params = [self.superpmi_path, self.jit_path, self.context.mch, '-v', 'q', '-jitoption', 'JitMetrics=1',
                  '-jitoption', 'JitRLHook=1', '-jitoption', 'JitRLHookEmitFeatureNames=1']

        try:
            # pylint: disable=consider-using-with
            process = subprocess.Popen(params, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            for line in process.stdout:
                line = line.decode('utf-8').strip()
                if line.startswith(';'):
                    yield self._parse_method_context(line)

        finally:
            if process.poll():
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()

    def _parse_method_context(self, line:str) -> MethodContext:
        if self._feature_names is None:
            # find featureNames in line
            feature_names_header = 'featureNames '
            start = line.find(feature_names_header)
            stop = line.find(' ', start + len(feature_names_header))
            if start > 0:
                self._feature_names = line[start + len(feature_names_header):stop].split(',')
                self._feature_names.insert(0, 'id')

        properties = {}
        properties['index'] = int(re.search(r'spmi index (\d+)', line).group(1))
        properties['name'] = re.search(r'for method ([^ ]+):', line).group(1)
        properties['hash'] = re.search(r'MethodHash=([0-9a-f]+)', line).group(1)
        properties['total_bytes'] = int(re.search(r'Total bytes of code (\d+)', line).group(1))
        properties['prolog_size'] = int(re.search(r'prolog size (\d+)', line).group(1))
        properties['instruction_count'] = int(re.search(r'instruction count (\d+)', line).group(1))
        properties['perf_score'] = float(re.search(r'PerfScore ([0-9.]+)', line).group(1))
        properties['bytes_allocated'] = int(re.search(r'allocated bytes for code (\d+)', line).group(1))
        properties['num_cse'] = int(re.search(r'num cse (\d+)', line).group(1))
        properties['num_cse_candidate'] = int(re.search(r'num cand (\d+)', line).group(1))
        properties['heuristic'] = re.search(r'num cand \d+ (.+) ', line).group(1)

        seq = re.search(r'seq ([0-9,]+) spmi', line)
        if seq is not None:
            properties['cses_chosen'] = [int(x) for x in seq.group(1).split(',')]
        else:
            properties['cses_chosen'] = []

        cse_candidates = None
        if self._feature_names is not None:
            # features CSE #032,3,10,3,3,150,150,1,1,0,0,0,0,0,0,37
            candidates = re.findall(r'features #([0-9,]+)', line)
            if candidates is not None:
                cse_candidates = [{self._feature_names[i]: int(x) for i, x in enumerate(candidate.split(','))}
                                  for candidate in candidates]

                for i, candidate in enumerate(cse_candidates):
                    candidate['index'] = i
                    if i in properties['cses_chosen']:
                        candidate['applied'] = True

        properties['cse_candidates'] = cse_candidates if cse_candidates is not None else []

        return MethodContext(**properties)

    def start(self):
        """Starts and returns the superpmi process."""
        if self._process is None:
            params = [self.superpmi_path, self.jit_path, '-streaming', 'stdin', self.context.mch]
            if self.verbose is not None:
                params.extend(['-v', self.verbose])

            # pylint: disable=consider-using-with
            self._process = subprocess.Popen(params, stdin=subprocess.PIPE, stdout=subprocess.PIPE)

        return self._process

    def stop(self):
        """Closes the superpmi process."""
        if self._process is not None:
            self._process.stdin.write(b"quit\n")
            self._process.terminate()
            self._process = None

__all__ = [
    SuperPmi.__name__,
    SuperPmiContext.__name__,
]
