# Portfolio Simulation using F#

## Overview

This project simulates 142,506 combinations of 25-stock portfolios selected from the 30 companies of the Dow Jones Industrial Average. For each combination, it generates 1,000 randomly weighted long-only portfolios and computes the one with the best Sharpe Ratio. The goal is to identify the most efficient portfolio using real stock data.

## How to Install

1. **Clone the repository**

```bash
git clone https://github.com/gustavolp1/carteira
cd carteira
```

2. **Install Python (3.10+) and required packages**

We recommend using a virtual environment.

```bash
pip install yfinance pandas
```

3. **Install .NET SDK**

Download and install the [.NET SDK (version 9.0 or later)](https://dotnet.microsoft.com/download).

You’ll also need `FSharp.Data` and `FSharp.Collections.ParallelSeq`, but they are referenced automatically in the project file.

---

## How to Run

1. **Download the stock data**

From the root of the repository, run the Python script to download the Dow Jones stocks from August to December 2024:

```bash
python dataDownloader.py
```

This will save `.csv` files in the `ProjetoCarteira/data/` folder.

2. **Run the F# portfolio simulator**

Navigate to the project folder and run the simulation:

```bash
cd ProjetoCarteira
dotnet run
```

Progress will be printed in the terminal, and the best results will be saved to `results.csv`.

If interrupted, re-running will **resume from where it left off**.

---

## Optimizations

Several performance optimizations were implemented:

- **Parallel execution** of portfolio simulation using `Async.Parallel` and `ParallelSeq`
- **Precomputation of mean vectors and covariance matrices** for each combination
- **Resuming** skips combinations already processed in `results.csv`
- Efficient filtering and data alignment using functional programming patterns

---

## Output

The `results.csv` file contains one row per portfolio combination:

```
Tickers,SharpeRatio,Weights
AAPL;MSFT;JPM;...,1.5482,0.04;0.06;...;0.03
```
