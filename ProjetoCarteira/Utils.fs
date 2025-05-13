module Utils

open System
open System.IO
open FSharp.Data

type PriceRow = CsvProvider<"Date,Open,High,Low,Close,Adj Close,Volume", HasHeaders=true>

let loadAdjustedClose (ticker: string) : (DateTime * float) list =
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

let computeDailyReturns (prices: (DateTime * float) list) : (DateTime * float) list =
    prices
    |> List.pairwise
    |> List.map (fun ((_, p1), (d2, p2)) -> d2, (p2 / p1) - 1.0)

let normalizeWeights (weights: float list) : float list =
    let total = List.sum weights
    weights |> List.map (fun w -> w / total)

let isValidWeights (weights: float list) =
    let sumClose = abs (List.sum weights - 1.0) < 1e-6
    let allPositive = weights |> List.forall (fun w -> w >= 0.0)
    let allBelowMax = weights |> List.forall (fun w -> w <= 0.20)
    sumClose && allPositive && allBelowMax

let rec sampleValidWeights (n: int) : float list =
    let rand = System.Random()
    let raw = List.init n (fun _ -> rand.NextDouble())
    let normalized = normalizeWeights raw
    if normalized |> List.exists (fun w -> w > 0.20) then
        sampleValidWeights n
    else
        normalized
