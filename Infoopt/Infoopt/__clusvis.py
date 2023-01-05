import matplotlib.pyplot as plt
import sys

def parseClusterData(fh):
    line = fh.readline()
    xs, ys, cs = [], [], []
    while line:
        (clus, x, y) = line.strip().split('\t')
        xs.append(int(x))
        ys.append(int(y))
        cs.append(int(clus))
        line = fh.readline()
    return xs, ys, cs


if __name__ == '__main__':
    xs, ys, cs = parseClusterData(sys.stdin)

    plt.scatter(xs, ys, c=cs, s=8.0, alpha=0.8, cmap='nipy_spectral')
    plt.axis('off')
    plt.savefig('__clusvis.png')