module Seq

let splitBy f input =
    let i = ref 0
    input
    |> Seq.groupBy (fun x -> if f x then incr i
                             !i)
    |> Seq.map snd

let splitAfter f input =
    let onNext = ref false
    let i = ref 0
    input
    |> Seq.groupBy (fun x -> if !onNext then incr i; onNext := false 
                             if f x then onNext := true
                             !i)
    |> Seq.map snd
