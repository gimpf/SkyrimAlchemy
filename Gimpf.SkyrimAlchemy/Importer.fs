namespace SkyrimAlchemy

#nowarn "25"

module Importer =
    open System.Text.RegularExpressions
    open IO

    [<RequireQualifiedAccess>]
    module Ingredients =
        let private WikiLinkRegex =
            Regex(
                @"\s*\[\[(?:[^|\]]*\|)?([^\]]*)\]\](\S?)\s*",
                RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

        let readOne (lines : string seq) =
            let lines = lines
                        |> Seq.where (not << String.startsWith "|-")
                        |> Seq.fold (fun state item ->
                            if String.startsWith "|" item
                            then ((String.removeAtFront "|" item) |> String.trim) :: state
                            else let last :: list = state
                                 let item = String.trim item
                                 let last = if last = ""
                                            then item
                                            else last + System.Environment.NewLine + item
                                 last :: list)
                            [] // n
                        |> List.rev
            if List.length lines <> 8 then invalidArg "lines" "missing entries"
            let [ name ; e1 ; e2 ; e3 ; e4 ; weight ; value ; loc ] = lines
            let nameMatch = WikiLinkRegex.Match(name)
            let cleanName = nameMatch.Groups.Item(1).Value
            let extension = if nameMatch.Groups.Count > 1
                            then nameMatch.Groups.Item(2).Value
                            else System.String.Empty
            let extension = match extension with
                            | "*" -> "Skyrim Dawnguard"
                            | "†" -> "Skyrim Hearthfire"
                            | "‡" -> "Skyrim Dragonborn"
                            | ""  -> "Skyrim"
                            | _   -> "<<unknown>>"
            { IngredientId = IngredientId cleanName
              Effects = [ EffectId e1 ; EffectId e2 ; EffectId e3 ; EffectId e4 ]
              Weight = String.parseDouble weight
              Value = String.parseInt value
              Locations = String.trim loc
              Since = extension }

        let readAll (reader : System.IO.TextReader) =
            IO.readLines reader
            |> Seq.skipWhile (String.startsWith "!")
            |> Seq.splitBy (String.startsWith "|-")
            |> Seq.map readOne
            |> Seq.toList

    [<RequireQualifiedAccess>]
    module Effects =
        type private EffectReadState =
            | ReadName
            | ReadId
            | ExpectIngredientList
            | ReadAttributes

        let private isAboutPotion name =
            String.startsWith "Restore" name
            || String.startsWith "Regenerate" name
            || String.startsWith "Fortify" name
            || String.startsWith "Cure" name
            || name = "Invisibility"
            || name = "Waterbreathing"

        let private isRestorationPotion (name : string) = name.IndexOf("Restore") = 0

        let private isDurationFixed { EffectId = name } =
            not (name = EffectId "Paralysis" || name = EffectId "Invisibility")

        let private isMagnitudeFixed { EffectId = name ; Magnitude = mag } =
            mag = 0.0 || mag = 100.0 || name = EffectId "Slow"

        let readOne (lines : string array seq) =
            let NoEffect = { EffectId = EffectId "" ; Description = "" ; Disposition = Poison ; BaseCost = 0.0 ; Magnitude = 0.0 ; Duration = 0.0 ; MagnitudeFixed = false ; DurationFixed = false }
            let StartState = (ReadName , NoEffect , [])
            let data = lines |> Seq.fold (fun state item ->
                        match state with
                        | (ReadName, effect, []) ->
                            let disposition =  match (isAboutPotion item.[0]), (isRestorationPotion item.[0]) with
                                               | true, true  -> Potion(RestoreEnergy)
                                               | true, false -> Potion(Other)
                                               | false, _    -> Poison

                            (ReadId , { NoEffect with EffectId = EffectId item.[0] ; Disposition = disposition } , [])
                        | (ReadId, effect, [])               -> (ExpectIngredientList , effect , [])
                        | (ExpectIngredientList, effect, []) -> (ReadAttributes       , effect , [])
                        | (ReadAttributes, effect, list)
                            when item.[0] <> ""              -> (ReadAttributes       , effect , item.[0] :: list)
                        | (ReadAttributes, effect, list)
                            when item.[0] = "" && item.[1] <> "" -> let effect = { effect with
                                                                                    Description = item.[1]
                                                                                    BaseCost = String.parseDouble item.[2]
                                                                                    Magnitude = String.parseDouble item.[3]
                                                                                    Duration = String.parseDouble item.[4] }
                                                                    let effect = { effect with
                                                                                    MagnitudeFixed = isMagnitudeFixed effect
                                                                                    DurationFixed = isDurationFixed effect }
                                                                    (ReadAttributes ,  effect , list)
                        | state -> state)
                        StartState
            let (_, effect, _) = data
            effect

        let readAll (reader : System.IO.TextReader) =
            let splitCsvLine line = line |> String.split [| ';' |]

            let isFinalEffectLine fields =
                match fields with
                | [| "" ; a ; _ ; _ ; _ ; _ |] when a <> "" -> true
                | _ -> false

            IO.readLines reader
            |> Seq.skipWhile (String.startsWith "!")
            |> Seq.map splitCsvLine
            |> Seq.splitAfter isFinalEffectLine
            |> Seq.map readOne
            |> Seq.toArray

