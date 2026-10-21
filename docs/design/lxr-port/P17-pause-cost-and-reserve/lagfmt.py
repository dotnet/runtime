import json
import sys

for line in sys.stdin:
    line = line.strip()
    if not line.startswith("{"):
        print(line)
        continue
    d = json.loads(line)
    p = d.pop("phases_ms", {})
    print(json.dumps(d))
    print("phases>20ms:", json.dumps({k: v for k, v in p.items() if v > 20}))
