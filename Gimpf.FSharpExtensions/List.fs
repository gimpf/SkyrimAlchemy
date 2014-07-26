module List

let combinations size set = 
    let rec combinations acc size set = seq {
        match size, set with 
        | n, x::xs -> 
            if n > 0 then yield! combinations (x::acc) (n - 1) xs
            if n >= 0 then yield! combinations acc n xs 
        | 0, [] -> yield acc 
        | _, [] -> () }
    combinations [] size set

let union left right =
    left |> Set.ofList |> Set.union (right |> Set.ofList) |> Set.toList

let intersection left right =
    left |> Set.ofList |> Set.intersect (right |> Set.ofList) |> Set.toList
