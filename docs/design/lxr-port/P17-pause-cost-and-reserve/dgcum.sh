python3 - "$@" <<'EOF'
import re, sys
def load(f):
    rows = []
    for l in open(f):
        if l.startswith('DIAG'):
            rows.append({k: float(v) for k, v in re.findall(r'(\w+)=([-\d.]+)', l)})
    return rows
names = sys.argv[1:]
data = []
for n in names:
    d = load('/root/p17/dg/%s.err' % n)
    cum = 0.0
    for r in d:
        cum += r['young']
        r['cum'] = cum / 1048576.0
    data.append(d)
marks = [200, 400, 600, 800, 1000, 1500, 2000, 3000, 4000, 6000, 8000, 12000, 16000, 24000]
print('cumulative young MiB | ' + ' | '.join('%-24s' % n for n in names))
print('                     | ' + ' | '.join('pause young_MiB avail ' for n in names))
for m in marks:
    out = []
    for d in data:
        r = next((x for x in d if x['cum'] >= m), None)
        idx = d.index(r) + 1 if r else 0
        out.append('%5d %8.2f %6d  ' % (idx, r['young'] / 1048576, r['avail']) if r else ' ' * 24)
    print('%20d | ' % m + ' | '.join(out))
print('totals: ' + ', '.join('%s pauses=%d cum=%.0f MiB' % (n, len(d), d[-1]['cum']) for n, d in zip(names, data)))
EOF
