"""Functions for interacting with SuperPmi."""

from enum import Enum
import os
import subprocess
import re
from typing import Dict, Iterable, List, Optional
from pydantic import BaseModel


class JitType(Enum):
    OTHER : int = 0
    INT : int = 1
    LONG : int = 2
    FLOAT : int = 3
    DOUBLE : int = 4
    STRUCT : int = 5
    SIMD : int = 6

class CseCandidate(BaseModel):
    index : int
    viable : bool
    liveAcrossCall : bool
    const : bool
    sharedConst : bool
    makeCse : bool
    hasCall : bool
    containable : bool
    type : JitType
    costEx : int
    costSz : int
    useCount : int
    defCount : int
    useWtCnt : int
    defWtCnt : int
    numDistinctLocals : int
    numLocalOccurrences : int
    enregCount : int

class MethodContext(BaseModel):
    """A superpmi method context."""
    index : int
    name : str
    hash : str
    total_bytes : int
    prolog_size : int
    instruction_count : int
    perf_score : float
    bytes_allocated : int
    num_cse : int
    num_cse_candidate : int
    heuristic : str
    heuristic_sequence : List[int]
    cse_candidates : List[CseCandidate]

    def __str__(self):
        return f"{self.index}: {self.name}"

class SuperPmi:
    """Controls one instance of superpmi."""
    def __init__(self, core_root:str, mch:str, jit:Optional[str] = None, verbosity:str = 'q'):
        """Constructor.
        core_root is the path to the coreclr build, usually at [repo]/artifiacts/bin/coreclr/[arch]/.
        jit is the full path to the jit to use. Default is None.
        verbosity is the verbosity level of the superpmi process. Default is 'q'."""
        if not os.path.exists(core_root):
            raise FileNotFoundError(f"core_root {core_root} does not exist.")

        self.mch = mch
        self.verbose = verbosity

        if os.uname().sysname == 'Windows':
            self.superpmi_path = os.path.join(core_root, 'superpmi.exe')
            self.jit_path = os.path.join(core_root, jit if jit is not None else 'clrjit.dll')
        else:
            self.superpmi_path = os.path.join(core_root, 'superpmi')
            self.jit_path = os.path.join(core_root, jit if jit is not None else 'libclrjit.so')

        if not os.path.exists(self.superpmi_path):
            raise FileNotFoundError(f"superpmi {self.superpmi_path} does not exist.")

        if not os.path.exists(self.mch):
            raise FileNotFoundError(f"mch {self.mch} does not exist.")

        if not os.path.exists(self.jit_path):
            raise FileNotFoundError(f"jit {self.jit_path} does not exist.")

        self._process = None
        self._feature_names = None

    def __del__(self):
        self.__close()

    def __enter__(self):
        self.__create_process()
        return self

    def __exit__(self, *_):
        self.__close()

    def jit_method(self, method_or_id : int | MethodContext, **options) -> MethodContext:
        """Jits the method given by id or MethodContext."""
        if self._process is None:
            raise ValueError("SuperPmi process is not running.  Use a 'with' statement.")

        if isinstance(method_or_id, MethodContext):
            method_or_id = method_or_id.index

        if "JitMetrics" not in options:
            options["JitMetrics"] = 1

        if self._feature_names is None and "JitRLHook" in options:
            options['JitRLHookEmitFeatureNames'] = 1

        torun = f"{method_or_id}!"
        torun += "!".join([f"{key}={value}" for key, value in options.items()])

        print(f"input: {torun}")

        self._process.stdin.write(f"{torun}\n".encode('utf-8'))
        self._process.stdin.flush()

        result = None
        output = ""

        while not output.startswith('[streaming] Done.'):
            output = self._process.stdout.readline().decode('utf-8').strip()
            if output.startswith(';'):
                result = self._parse_method_context(output)

        return result

    def enumerate_methods(self) -> Iterable[MethodContext]:
        """List all methods in the mch file."""
        params = [self.superpmi_path, self.jit_path, self.mch, '-v', 'q', '-jitoption', 'JitMetrics=1']
        with subprocess.Popen(params, stdout=subprocess.PIPE, stderr=subprocess.PIPE) as process:
            for line in process.stdout:
                line = line.decode('utf-8').strip()
                if line.startswith(';'):
                    yield self._parse_method_context(line)

    def _parse_method_context(self, line:str) -> MethodContext:
        if self._feature_names is None:
            # find featureNames in line
            feature_names_header = 'featureNames '
            i = line.find(feature_names_header)
            if i > 0:
                self._feature_names = line[i + len(feature_names_header):].split(',')
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
            properties['heuristic_sequence'] = [int(x) for x in seq.group(1).split(',')]
        else:
            properties['heuristic_sequence'] = []

        cse_candidates = None
        if self._feature_names is not None:
            # features CSE #032,3,10,3,3,150,150,1,1,0,0,0,0,0,0,37
            candidates = re.findall(r'features CSE #([0-9,]+)', line)
            if candidates is not None:
                cse_candidates = [{self._feature_names[i]: int(x) for i, x in enumerate(candidate.split(','))}
                                  for candidate in candidates]

                for i, candidate in enumerate(cse_candidates):
                    candidate['index'] = i

        properties['cse_candidates'] = cse_candidates if cse_candidates is not None else []

        return MethodContext(**properties)

    def __create_process(self):
        """Returns the superpmi process."""
        if self._process is None:
            params = [self.superpmi_path, self.jit_path, '-streaming', 'stdin', self.mch]
            if self.verbose is not None:
                params.extend(['-v', self.verbose])

            # pylint: disable=consider-using-with
            self._process = subprocess.Popen(params, stdin=subprocess.PIPE, stdout=subprocess.PIPE)

        return self._process

    def __close(self):
        if self._process is not None:
            self._process.stdin.write(b"quit\n")
            self._process.terminate()
            self._process = None
