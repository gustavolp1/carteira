module Simulate

open System
open Utils

/// Loads daily return series for a list of tickers and aligns them by common dates
let loadAlignedReturns (tickers: string list) : (DateTime list * float[][]) =
    let returnsByTicker =
        tickers
        |> List.map (fun ticker ->
            let prices = loadAdjustedClose ticker
            let returns = computeDailyReturns prices
            ticker, Map.ofList returns
        )

    let commonDates =
        returnsByTicker
        |> List.map (fun (_, m) -> m |> Map.toSeq |> Seq.map fst |> Set.ofSeq)
        |> List.reduce Set.intersect
        |> Set.toList
        |> List.sort

    let matrix =
        returnsByTicker
        |> List.map (fun (_, map) ->
            commonDates |> List.map (fun date -> Map.find date map) |> List.toArray
        )
        |> List.toArray
        |> Array.transpose

    commonDates, matrix

/// Computes volatility from a precomputed covariance matrix
let volatilityFromCov (weights: float[]) (covMatrix: float[][]) =
    let weighted =
        Array.mapi (fun i wi ->
            Array.mapi (fun j wj -> wi * wj * covMatrix.[i].[j]) weights
            |> Array.sum
        ) weights
        |> Array.sum
    sqrt (weighted * 252.0)

/// Computes Sharpe Ratio using precomputed means and covariances
let sharpeRatioFast (weights: float[]) (meanVector: float[]) (covMatrix: float[][]) =
    let dailyReturn = Array.map2 (*) weights meanVector |> Array.sum
    let sigma = volatilityFromCov weights covMatrix
    if sigma = 0.0 then 0.0 else (dailyReturn * 252.0) / sigma

/// Simulates `n` portfolios and returns the one with highest Sharpe Ratio
let simulatePortfolios (tickers: string list) (n: int) =
    let _, matrix = loadAlignedReturns tickers

    // Precompute once per combination
    let meanVector =
        [| for i in 0 .. tickers.Length - 1 ->
            matrix |> Array.averageBy (fun row -> row.[i]) |]

    let covMatrix =
        let n = tickers.Length
        Array.init n (fun i ->
            Array.init n (fun j ->
                matrix
                |> Array.map (fun row ->
                    (row.[i] - meanVector.[i]) * (row.[j] - meanVector.[j]))
                |> Array.average
            )
        )

    let simulateOne () =
        async {
            let weights = sampleValidWeights tickers.Length |> List.toArray
            let sharpe = sharpeRatioFast weights meanVector covMatrix
            return (sharpe, weights)
        }

    let simulations = List.init n (fun _ -> simulateOne ())
    let results = Async.RunSynchronously (Async.Parallel simulations)

    results |> Array.maxBy fst
