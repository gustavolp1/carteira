open System
open System.IO
open Simulate
open Utils
open FSharp.Collections.ParallelSeq
open System.Diagnostics

let allDowTickers =
    [ "AAPL"; "MSFT"; "JPM"; "V"; "PG"; "UNH"; "HD"; "KO"; "DIS"; "INTC";
      "IBM"; "WMT"; "AXP"; "NKE"; "MCD"; "TRV"; "BA"; "CAT"; "CSCO"; "CVX";
      "GS"; "HON"; "JNJ"; "MMM"; "MRK"; "WBA"; "DOW"; "AMGN"; "VZ"; "RTX" ]

let rec combinations k list =
    match k, list with
    | 0, _ -> [ [] ]
    | _, [] -> []
    | k, x::xs ->
        let withX = combinations (k-1) xs |> List.map (fun l -> x::l)
        let withoutX = combinations k xs
        withX @ withoutX

let runAllCombinations () =
    File.WriteAllText("RUNNING", DateTime.Now.ToString("u"))

    let outputFile = "./results.csv"
    let totalCombos = combinations 25 allDowTickers

    let completedCombos =
        if File.Exists(outputFile) then
            let lines = File.ReadLines(outputFile) |> Seq.toList
            match lines with
            | _header :: rest ->
                rest
                |> Seq.map (fun line -> line.Split(',').[0])
                |> Set.ofSeq
            | [] ->
                Set.empty
        else
            Set.empty

    let remainingCombos =
        totalCombos
        |> List.filter (fun combo ->
            let comboKey = String.Join(";", combo |> List.toArray)
            not (completedCombos.Contains comboKey)
        )

    let total = totalCombos.Length
    let remaining = remainingCombos.Length
    let completed = total - remaining

    printfn "🔁 Resume mode active: %d/%d combinations already completed." completed total

    use sw = new StreamWriter(outputFile, append = true)
    if completed = 0 then
        sw.WriteLine("Tickers,SharpeRatio,Weights")

    let stopwatch = Stopwatch.StartNew()
    let counter = ref completed

    remainingCombos
    |> PSeq.withDegreeOfParallelism Environment.ProcessorCount
    |> PSeq.iter (fun tickers ->
        try
            let bestSharpe, weights = simulatePortfolios tickers 500
            let tickersStr = String.Join(";", tickers |> List.toArray)
            let weightsStr = String.Join(";", weights |> Array.map string)
            lock sw (fun () ->
                sw.WriteLine($"{tickersStr},{bestSharpe},{weightsStr}")
                sw.Flush()
            )

            let i = System.Threading.Interlocked.Increment(counter)
            let elapsed = stopwatch.Elapsed
            let percent = float i / float total * 100.0
            printfn "✅ %d/%d (%.2f%%) - Sharpe: %.4f - Time: %02i:%02i:%02i" 
                i total percent bestSharpe elapsed.Hours elapsed.Minutes elapsed.Seconds
        with ex ->
            printfn $"⚠️ Error: {ex.Message}"
    )

    printfn "✅ Finished all combinations! Total time: %s" (stopwatch.Elapsed.ToString())

[<EntryPoint>]
let main _ =
    runAllCombinations()
    0
