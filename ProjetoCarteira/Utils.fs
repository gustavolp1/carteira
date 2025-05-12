module Utils

open System
open System.IO
open FSharp.Data

// Strongly-typed CSV provider for Yahoo Finance export format
type PriceRow = CsvProvider<"Date,Open,High,Low,Close,Adj Close,Volume", HasHeaders=true>

/// Loads the adjusted close prices for a given ticker
let loadAdjustedClose (ticker: string) : (DateTime * float) list =
    // Get the directory of the running assembly
    let projectRoot =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
        |> Path.GetFullPath

    let path = Path.Combine(projectRoot, "data", $"{ticker}.csv")

    if File.Exists(path) then
        let csv = PriceRow.Load(path)
        csv.Rows
        |> Seq.map (fun row -> DateTime.Parse(row.Date), float row.``Adj Close``)
        |> Seq.sortBy fst
        |> Seq.toList
    else
        failwith $"File not found: {path}"

/// Computes discrete daily returns from a sorted price list
let computeDailyReturns (prices: (DateTime * float) list) : (DateTime * float) list =
    prices
    |> List.pairwise
    |> List.map (fun ((_, p1), (d2, p2)) -> d2, (p2 / p1) - 1.0)

/// Normalizes a weight vector so the sum is 1.0
let normalizeWeights (weights: float list) : float list =
    let total = List.sum weights
    weights |> List.map (fun w -> w / total)

/// Checks if a weight vector is valid (long-only, sum = 1, max 20% per asset)
let isValidWeights (weights: float list) =
    let sumClose = abs (List.sum weights - 1.0) < 1e-6
    let allPositive = weights |> List.forall (fun w -> w >= 0.0)
    let allBelowMax = weights |> List.forall (fun w -> w <= 0.20)
    sumClose && allPositive && allBelowMax

/// Samples a random valid long-only weight vector of length n (recursive retry)
let rec sampleValidWeights (n: int) : float list =
    let rand = System.Random()
    let raw = List.init n (fun _ -> rand.NextDouble())
    let normalized = normalizeWeights raw
    if normalized |> List.exists (fun w -> w > 0.20) then
        sampleValidWeights n // retry
    else
        normalized
