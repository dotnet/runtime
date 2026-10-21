python3 - "$@" <<'EOF'
import re, sys
def load(f):
    rows = []
    for l in open(f):
        if l.startswith('DIAG'):
            rows.append({k: float(v) for k, v in re.findall(r'(\w+)=([-\d.]+)', l)})
    return rows
names = sys.argv[1:]
data = [load('/root/p17/dg/%s.err' % n) for n in names]
idx = [1, 2, 3, 4, 5, 6, 8, 10, 15, 20, 30, 40, 50, 75, 100, 150, 200, 300, 400, 500, 600, 800, 1000, 1500, 2000]
print('pause | ' + ' | '.join('%-34s' % n for n in names))
print('      | ' + ' | '.join('young  clean  free  reus  avail  surv' for n in names))
for i in idx:
    out = []
    for d in data:
        if i <= len(d):
            r = d[i - 1]
            out.append('%5.1f %5.1f %5d %5d %6d %5d' % (r['young'] / 1048576, r['clean'] / 1048576, r['endfree'], r['reusable'], r['avail'], r['surv'] / 1024))
        else:
            out.append(' ' * 34)
    print('%5d | ' % i + ' | '.join(out))
EOF
