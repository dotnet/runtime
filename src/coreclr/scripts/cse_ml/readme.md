# Setup

## Environment setup

Ubuntu 22 is easiest
Python 3.10
pip install -r requirements.txt

core_root=artifacts/bin/coreclr/{platform.arch.Checked}/

## Runtime setup

./build.sh -subset clr -c Checked
./build.sh -subset libs -c Release -rc Checked
./src/tests/build.sh x64 checked skipmanaged skipnative
python src/coreclr/scripts/superpmi.py download

## SuperPmi locations

artifacts/spmi/mch/{build}/*mch

# SuperPMI Notes

List all methods:

`superpmi libclrjit.so -v q -jitoption JitMetrics=1 {superpmi_path}/coreclr_tests.run.linux.x64.checked.mch`

Streaming:

```
superpmi libclrjit.so -v q -streaming stdi {mch}

[method_id]!JitMetrics=1!Var1=Value1!Var2=Value2
```


# Lee's Dumb Questions about CSE

* Can we ever choose a CSE which is bad or neutral, but makes *other* CSEs more valuable?  E.G., let's say we have two CSEs: A and B.  Choosing A by itself leads to -1.0 perfscore.  Choosing B by itself leads to +1 perfscore.  Is there ever a case where choosing A, would cause choosing be to be worth more than +1?  If so, what if you chose them in the opposite order?
* Does order matter generally?

# Before Checkin:

* Write this document.
* Make environments modifiable.
* add sequence to jit output
