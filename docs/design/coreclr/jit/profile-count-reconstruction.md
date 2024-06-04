## Numeric Solvers for Profile Count Reconstruction

It may not be readily apparent how count reconstruction works. Perhaps these notes will shed some light on things.

In our flowgraph model we assume that the edge likelihoods are trustworthy and well formed (meaning each edge's likelihood is in [0,1] and the sum of all likelihoods for a block's successor edges is 1).

The appeal of edge well-formedness is easy to check and relatively easy to maintain during various optimizations. It is a *local* property.

We will use $p_{i,j}$ to denote the likelihood that block $i$ transfers control to block $j$. Thus local consistency means:

$$ 0 \le p_{i,j} \le 1 $$

and, for blocks with successors:

$$ \sum_i p_{i,j} = 1 $$

By contrast, block weight consistency requires that the flow into a block be balanced by the flow out of a block. It is a *global* property and harder to maintain during optimizations. It may also not be true initially.

We will use $w_j$ for the weight of block $j$. We will also assume there is an external source and sink of weight for some blocks (method entry and exit points), $e_j$. Then block consistency means:

$$ e_j + \sum_i w_i p_{i,j}  = \sum_k w_j p_{j,k} $$

where the LHS is flow in and the RHS is flow out of block $j$. But

$$ \sum_k w_j p_{j,k} = w_j \sum_k p_{j,k} = w_j $$

so we can restate this as saying the external flow plus the flow into the block must equal the block weight:

$$ e_j + \sum_i w_i p_{i,j} = w_j$$

The goal of this work is to explore methods for reconstructing a set of consistent block weights $w_j$ from the external weight sources and sinks $e_j$ and edge likelihoods $p_{i,j}$.

### General Solution

The above can be summarized in matrix-vector form as

$$ \boldsymbol w = \boldsymbol e + \boldsymbol P \boldsymbol w $$

where to be able to express the sum of incoming flow as a standard matrix-vector product we have:

$$ \boldsymbol P_{i,j} = { p_{j,i} } $$

(that is, in $\boldsymbol P$, the flow from block $i$ is described by the entries in the $i\text{th}$ column, and the flow into block $i$ by the $i\text{th}$ row). A bit of rearranging puts this into the standard linear equation form

$$ (\boldsymbol I - \boldsymbol P) \boldsymbol w = \boldsymbol e$$

and this can be solved (in principle) for $\boldsymbol w$ by computing the inverse of $\boldsymbol I - \boldsymbol P$ (assuming this exists), giving

$$ \boldsymbol w = {(\boldsymbol I - \boldsymbol P)}^{-1} \boldsymbol e $$

For example, given the following graph with edge likelihoods a shown:

<p align="center">
<img src="https://github.com/dotnet/runtime/assets/10121823/6b96e29e-14f6-4875-90a8-aab4a4988146" height=400 />
</p>

we have

```math
\boldsymbol P =
\begin{bmatrix}
 0 &    0 &    0 & 0 \cr
 1 &    0 &  0.8 & 0 \cr
 0 &  0.5 &    0 & 0 \cr
 0 &  0.5 &  0.2 & 0
\end{bmatrix}
```

Note each column save the last sums to 1.0, representing the fact that the outgoing likelihoods from each block must sum to 1.0, unless there are no successors.

Thus
```math
(\boldsymbol I - \boldsymbol P) =
\begin{bmatrix}
 1 &    0 &    0 & 0 \\\
-1 &    1 & -0.8 & 0 \\\
 0 & -0.5 &    1 & 0 \\\
 0 & -0.5 & -0.2 & 1
\end{bmatrix}
```
and so (details of computing the inverse left as exercise for the reader)
```math
{(\boldsymbol I - \boldsymbol P)}^{-1} =
\begin{bmatrix}
1 & 0 & 0 & 0 \\\
1.67 & 1.67 & 1.33 & 0 \\\
0.83 & 0.83 & 1.67 & 0 \\\
1 & 1 & 1 & 1
\end{bmatrix}
```
Note the elements of ${(\boldsymbol I - \boldsymbol P)}^{-1}$ are all non-negative; intuitively, if we increase flow anywhere in the graph, it can only cause weights to increase or stay the same.

If we feed 6 units of flow into A, we have
```math
\boldsymbol w = \begin{bmatrix} 6 \\\ 10 \\\ 5 \\\ 6 \end{bmatrix}
```

or graphically

<p align="center">
<img src="https://github.com/dotnet/runtime/assets/10121823/6f0d7c59-ed97-4a7e-bcba-3abd7b32c352" height=400 />
</p>

However, explicit computation of the inverse of a matrix is computationally expensive.

Also note (though it's not fully obvious from such a small example) that the matrix $(\boldsymbol I - \boldsymbol P)$ is *sparse*: a typical block has only 1 or 2 successors, so the number of nonzero entries in each column will generally be either 2 or 3, no matter how many nodes we have. The inverse of a sparse matrix is typically not sparse, so computing it is not only costly in time but also in space.

So solution techniques that can leverage sparseness are of particular interest.

### A More Practical Solution

Note the matrix $\boldsymbol I - \boldsymbol P$ has non-negative diagonal elements and negative non-diagonal elements, since all entries of $\boldsymbol P$ are in the range [0,1].

If we further restrict ourselves to the case where $p_{i,i} \lt 1$ (meaning there are are no infinite self-loops) then all the diagonal entries are positive and the matrix has an inverse with no negative elements.

Such matrices are known as M-matrices.

It is well known that for an M-matrix $(\boldsymbol I - \boldsymbol P)$ the inverse can be computed as the limit of an infinite series

$$ {(\boldsymbol I - \boldsymbol P)}^{-1} = \boldsymbol I + \boldsymbol P + \boldsymbol P^2 + \dots $$

This gives rise to a simple *iterative* procedure for computing an approximate value of $\boldsymbol w$ (here superscripts on $\boldsymbol w$ are successive iterates, not powers)

$$ \boldsymbol w^{(0)} = \boldsymbol e $$

$$ \boldsymbol w^{(1)} = (\boldsymbol I + \boldsymbol P) \boldsymbol e = \boldsymbol e + \boldsymbol P \boldsymbol w^{(0)} $$

$$ \boldsymbol w^{(2)} = (\boldsymbol I + \boldsymbol P + \boldsymbol P^2) \boldsymbol e = \boldsymbol e + \boldsymbol P \boldsymbol w^{(1)}$$

$$ \dots$$

$$ \boldsymbol w^{(k + 1)} = \boldsymbol e + \boldsymbol P \boldsymbol w^{(k)} $$

where we can achieve any desired precision for $\boldsymbol w$ by iterating until the successive $\boldsymbol w$ differ by a small amount.

Intuitively this should make sense, we are effectively pouring weight into the entry block(s) and letting the weights flow around in the graph until they reach a fixed point. If we do this for the example above, we get the following sequence of values for $\boldsymbol w^n$:

```math
\boldsymbol w^{(0)} = \begin{bmatrix} 6 \\\ 0 \\\ 0 \\\ 0 \end{bmatrix},
\boldsymbol w^{(1)} = \begin{bmatrix} 6 \\\ 6 \\\ 0 \\\ 0 \end{bmatrix},
\boldsymbol w^{(2)} = \begin{bmatrix} 6 \\\ 6 \\\ 3 \\\ 3 \end{bmatrix},
\boldsymbol w^{(3)} = \begin{bmatrix} 6 \\\ 8.4 \\\ 3 \\\ 3.6 \end{bmatrix},
\boldsymbol w^{(4)} = \begin{bmatrix} 6 \\\ 8.4 \\\ 4.2 \\\ 3.6 \end{bmatrix},
\boldsymbol w^{(5)} = \begin{bmatrix} 6 \\\ 9.36 \\\ 4.2 \\\ 3.6 \end{bmatrix},
\dots,
\boldsymbol w^{(20)} = \begin{bmatrix} 6 \\\ 9.9990 \\\ 4.9995 \\\ 5.9992 \end{bmatrix},
\dots
```

and the process converges to the weights found using the inverse. However convergence is fairly slow.

Classically this approach is known as *Jacobi's method*. At each iterative step, the new values are based only on the old values.

### Jacobi's Method

If you read the math literature on iterative solvers, Jacobi's method is often described as follows. Given a linear system $\boldsymbol A \boldsymbol x = \boldsymbol b$, a *splitting* of $\boldsymbol A$ is $\boldsymbol A = \boldsymbol M - \boldsymbol N$, where $\boldsymbol M^{-1}$ exists. Then the *iteration matrix* $\boldsymbol H$ is given by $\boldsymbol H = \boldsymbol M^{-1} \boldsymbol N$. Given some initial guess at an answer $\boldsymbol x^{(0)}$ the iteration scheme is:

$$ \boldsymbol x^{(k+1)} = \boldsymbol H \boldsymbol x^{(k)} + \boldsymbol M^{-1}\boldsymbol b$$

And provided that $\rho(\boldsymbol H) \lt 1$,

$$\lim_{k \to \infty} \boldsymbol x^{(k)}=\boldsymbol A^{-1} \boldsymbol b$$

In our case $\boldsymbol A = \boldsymbol I - \boldsymbol P$ and so the splitting is simply $\boldsymbol M = \boldsymbol I$ and $\boldsymbol N = \boldsymbol P$. Since $\boldsymbol M = \boldsymbol I$, $\boldsymbol M^{-1} = \boldsymbol I$ (the identity matrix is its own inverse), $\boldsymbol H = \boldsymbol P$, $\boldsymbol x = \boldsymbol w$ and $\boldsymbol b = \boldsymbol e$, we end up with

$$ \boldsymbol w^{(k+1)} = \boldsymbol P \boldsymbol w^{(k)} + \boldsymbol e$$

as we derived above.

As an alternative we could split $\boldsymbol A = (\boldsymbol I - \boldsymbol P)$ into diagonal part $\boldsymbol M = \boldsymbol D$ and remainder part $\boldsymbol N$. This only leads to differences from the splitting above when there are self loops, otherwise the diagonal of $\boldsymbol P$ is all zeros.

With that splitting,


```math
 \boldsymbol D^{-1}_{i,i} = 1/a_{i,i} =  1/(1 - p_{i,i})
```

so as $p_{i,i}$ gets close to 1.0 the value can be quite large: these are the count amplifications caused by self-loops. If we write things out component-wise we get the classic formulation for Jacobi iteration:

```math
 x^{(k+1)}_i = \frac{1}{a_{i,i}} \left (b_i - \sum_{j \ne i} a_{i,j} x^{(k)}_j  \right)
```

or in our block weight and edge likelihood notation

```math
 w^{(k+1)}_i = \frac{1}{(1 - p_{i,i})} \left (e_i + \sum_{j \ne i} p_{j,i} w^{(k)}_j  \right)
```

Intuitively this reads: the new value of node $i$ is the sum of the external input (if any) plus the weights flowing in from (non-self) predecessors, with the sum scaled up by the self-loop factor.

### On Convergence and Stability

While the iterative method above is guaranteed to converge when $\boldsymbol A$ is an M-matrix, its rate of convergence is potentially problematic. For an iterative scheme, the asymptotic rate of convergence can be shown to be $R \approx -log_{10} \rho(\boldsymbol H)$ digits / iteration.

Here the spectral radius $\rho(\boldsymbol H)$ is the magnitude of the largest eigenvalue of $\boldsymbol H$. For the example above $\boldsymbol H = \boldsymbol P$ and $\rho(\boldsymbol P) \approx 0.63$, giving $R = 0.2$. So to converge to $4$ decimal places takes about $20$ iterations, as the table of data above indicates.

it is also worth noting that for synthesis the matrix $(\boldsymbol I - \boldsymbol P)$ is often *ill-conditioned*, meaning that small changes in the input vector $\boldsymbol e$ (or small inaccuracies in the likelihoods $p_{i,j}$) can lead to large changes in the solution vector $\boldsymbol w$. In some sense this is a feature; we know that blocks in flow graphs can have widely varying weights, with some blocks rarely executed and others executed millions of times per call to the method. So it must be possible for $(\boldsymbol I - \boldsymbol P)$ to amplify the magnitude of a "small" input (say 1 call to the method) into large block counts.

### Accelerating Convergence I: Gauss-Seidel and Reverse Postorder

It's also well-known that Gauss-Seidel iteration often converges faster than Jacobi iteration. Here instead of always using the old iteration values, we try and use the new iteration values that are available, where we presume each update happens in order of increasing $i$:

```math
 x^{(k+1)}_i = \frac{1}{a_{i,i}} \left(b_i - \sum_{j \lt i} a_{i,j} x^{(k+1)}_j  - \sum_{j \gt i} a_{i,j} x^{(k)}_j \right) $$
```

or again in our notation

```math
 w^{(k+1)}_i = \frac{1}{(1 - p_{i,i})} \left(e_i + \sum_{j \lt i} p_{j,i} w^{(k + 1)}_j + \sum_{j \gt i} p_{j,i} w^{(k)}_j \right) $$
```

In the above scheme the order of visiting successive blocks is fixed unspecified, and (in principle) any order can be used. But by using a reverse postorder to index the blocks, we can ensure a maximal amount of forward propagation per iteration. Note that if a block has an incoming edge from a node that appears later in the reverse postorder, that block is a loop header.

If we do, that the code above nicely corresponds to our notion of forward and backward edges in the RPO:

```math
 w^{(k+1)}_i = \frac{1}{\underbrace{(1 - p_{i,i}}_\text{self edge})} \left(e_i + \underbrace{\sum_{j \lt i} p_{j,i} w^{(k + 1)}_j}_\text{forward edges in RPO} + \underbrace{\sum_{j \gt i} p_{j,i} w^{(k)}_j}_\text{backward edges in RPO} \right)
```

Note because of the order of reads and writes, $\boldsymbol w^{(k+1)}$ can share storage with $\boldsymbol w^{(k)}$.

On the example above this results in:

$$
\boldsymbol w^{(0)} = \begin{bmatrix} 6 \\\ 6 \\\ 3 \\\ 3 \end{bmatrix},
\boldsymbol w^{(1)} = \begin{bmatrix} 6 \\\ 8.4 \\\ 4.2 \\\ 5.04 \end{bmatrix},
\boldsymbol w^{(2)} = \begin{bmatrix} 6 \\\ 9.36 \\\ 4.68 \\\ 5.62 \end{bmatrix},
\boldsymbol w^{(3)} = \begin{bmatrix} 6 \\\ 9.74 \\\ 4.87 \\\ 5.85 \end{bmatrix},
\boldsymbol w^{(4)} = \begin{bmatrix} 6 \\\ 9.90 \\\ 4.95 \\\ 5.94 \end{bmatrix},
\boldsymbol w^{(5)} = \begin{bmatrix} 6 \\\ 9.96 \\\ 4.98 \\\ 5.98 \end{bmatrix},
\dots,
\boldsymbol w^{(9)} = \begin{bmatrix} 6 \\\ 9.9990 \\\ 4.9995 \\\ 5.9994 \end{bmatrix},
\dots
$$

So it is converging about twice as fast. As with the Jacobi method one can re-express this as a splitting and determine an iteration matrix $\boldsymbol H$ and determine the dominant eigenvalue, and from this the rate of convergence, but we will not do so here.

### Accelerating Convergence II: Cyclic Probabilities

A flow graph is reducible (or is said to have reducible loops) if every cycle in the graph has a block in the cycle that dominates the other blocks in the cycle. We will call such cycles natural loops, distinguished by their entry blocks.

For reducible loops we can compute the amount by which they amplify flow using a technique described by Wu and Larus: given a loop head $h$ we classify the predecessor into two sets: input edges that do not come from a block within the loop, and back edges that come from a block within the loop. We then inject one unit of flow into the block and propagate it through the loop, and compute the sum of the weights on the back edges. This value will be some $p$ where $0 \le p \le 1$. Then the *cyclic probability* $C_p$ for $h$ is $C_p(h) = 1 / (1 - p)$. To avoid dividing by zero we artificially cap $C_p$ at some value less than $1$.

Note also that the technique above won't compute an accurate $C_p$ for loops that contain improper (irreducible) loops, as solving for $C_p$ in such cases would require iteration (the single-pass $C_p$ will be an underestimate). So we must also track which loops contain improper loops.

If we add this refinement to our algorithm we end up with:

```math
 w^{(k+1)}_i =
\begin{cases}
 C_p(i) \left(e_i + \sum_{j \lt i} p_{j,i} w^{(k + 1)}_j  \right), \text{ block } i \text{ is a natural loop head, and does not contain an improper loop} \\\
 \frac{1}{(1 - p_{i,i})} \left(e_i + \sum_{j \lt i} p_{j,i} w^{(k + 1)}_j + \sum_{j \gt i} p_{j,i} w^{(k)}_j \right)
\end{cases}
```

the second clause includes both blocks without any back edges, blocks with back edges that are not headers of natural loops, and blocks that are headers of natural loops where the loop contains an improper loop.

On an example like the one above this converges in one pass. If any $C_p$ was capped then the solution will be approximate and we will have failed to achieve a global balance. But we will also (generally) have avoided creating infinite or very large counts.

One can imagine that if we cap some $C_p$ we could also try to alter some of the $p_{j,i}$ to bring things back into balance, but this seems tricky if there are multiple paths through the loop. And we're basically deciding at that point that consistency is more important than accuracy.

Since the remainder of the JIT is going to have to cope with lack of global balance anyways (recall it is hard to preserve) for now we are going to ty and tolerate reconstruction inconsistencies.

The algorithm described above is implemented in the code as the `GaussSeidel` solver.

### Cycles That Are Not Natural Loops, More Sophisticated Solvers, and Deep Nests

If the flow graph has cycles that are not natural loops (irreducible loops) the above computations will converge but again may converge very slowly. On a sample of about 500 graphs with irreducible loops the modified Gauss-Seidel approach above required more than 20 iterations in 120 cases and more than 50 iterations in 70 cases, with worst-case around 500 iterations.

SOR is a classic convergence altering technique, but unfortunately, for M-Matrices SOR can only safely be used to slow down convergence.

There does not seem to be a good analog of $C_p$ for such cases, though it's possible that "block diagonal" solvers may be tackling exactly that problem.

It's possible that more sophisticated solution techniques like BiCGstab or CGS might be worth consideration. Or perhaps a least-squares solution, if we're forced to be approximate, to try and minimize the overall approximation error.

In very deep loop nests even $C_p$ is not enough to prevent creation of large counts. We could try and adjust the cap level downwards as the loops get deeper, or distribute the $C_p$ "tax" across all the loops. This tends to only be a problem for stress test cases.

### References

Carl D. Meyer. *Matrix Analysis and Applied Linear Algebra*, in particular section 7.10.

Nick Higham. [What is an M-Matrix?](https://nhigham.com/2021/03/16/what-is-an-m-matrix/)

Youfeng Wu and James R. Larus. Static branch frequency and program profile analysis, Micro-27 (1994).

