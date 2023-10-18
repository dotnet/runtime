import csv

with open("results.csv", "r") as csvfile:
    top = []
    lowest = 1
    min_ind = -1

    for row in csv.reader(csvfile):
        if row[5][0] == "T" and row[4][0] == "F":
            diff_instr = int(row[-1])
            base_instr = int(row[-2])
            size = int(row[1])
            percent = (diff_instr - base_instr) / size
            if len(top) < 10:
                if percent < lowest:
                    min_ind = len(top)
                    lowest = percent
                top.append((row[0], percent, size))
            elif percent > lowest:
                top[min_ind] = (row[0], percent, size)
                lowest = 1
                for i in range(len(top)):
                    if top[i][1] < lowest:
                        lowest = top[i][1]
                        min_ind = i
    for row in top:
        print(row)