import numpy as np
import ruptures as rpt
import sys

data = open(sys.argv[1], "r").read()
points = data.split(',')

for i in range(0, len(points)):
    points[i] = float (points[i])

points = np.concatenate([points])

algo = rpt.Pelt(model="mahalanobis", jump=1, min_size=3).fit(points)
results = algo.predict(pen=4*np.log(len(points)))
print(results)