namespace SkyrimAlchemy

module FileIO =
    let writeEffect (writer : System.IO.TextWriter) effect =
        let { EffectId = id
              BaseCost = cost
              Magnitude = mag
              Duration = dur
              Disposition = dis
              MagnitudeFixed = magF
              DurationFixed = durF } = effect
        writer.WriteLine(sprintf "%O;%f;%f;%f;%b;%b;%A" id cost mag dur magF durF dis)

    let readEffect (line : string) =
        if line |> String.startsWith "//" then None else
        let fields = String.split [| ';' |] line
        if fields.Length <> 7 then None else
        let name = EffectId fields.[0]
        let cost = String.parseDouble fields.[1]
        let mag = String.parseDouble fields.[2]
        let dur = String.parseDouble fields.[3]
        let magFixed = String.parseBool fields.[4]
        let durFixed = String.parseBool fields.[5]
        let disposition = match fields.[6] with
                          | "Poison" -> Poison
                          | "Potion RestoreEnergy" -> Potion(RestoreEnergy)
                          | "Potion Other" -> Potion(Other)
                          | _ -> failwith "invalid disposition value"
        Some <| { EffectId = name
                  Description = ""
                  BaseCost = cost
                  Magnitude = mag
                  Duration = dur
                  Disposition = disposition
                  MagnitudeFixed = magFixed
                  DurationFixed = durFixed }

    let writeEffects (writer : System.IO.TextWriter) effects =
        use writer = writer
        Seq.iter (writeEffect writer) effects
    
    let readEffects (reader : System.IO.TextReader) =
        use reader = reader
        IO.readLines reader
        |> Seq.choose readEffect
        |> Seq.toList

    let writeIngredient (writer : System.IO.TextWriter) ingredient =
        match ingredient with
        | { IngredientId = id
            Effects = [ e1 ; e2; e3; e4 ]
            Weight = weight
            Value = value
            Locations = loc
            Since = since
          } ->
                writer.WriteLine(sprintf "%O;%O;%O;%O;%O;%f;%d;%s;%s" id e1 e2 e3 e4 weight value (loc.Replace("\n", "\\n")) since)
        | _ -> invalidArg "ingredient" "Ingredient must have exactly four effects"

    let readIngredient (line : string) =
        if line |> String.startsWith "//" then None else
        let fields = String.split [| ';' |] line
        if fields.Length <> 9 then None else
        let name = IngredientId fields.[0]
        let e1 = EffectId fields.[1]
        let e2 = EffectId fields.[2]
        let e3 = EffectId fields.[3]
        let e4 = EffectId fields.[4]
        let weight = String.parseDouble fields.[5]
        let value = String.parseInt fields.[6]
        let loc = fields.[7]
        let since = fields.[8]
        Some <| { IngredientId = name
                  Effects = [ e1 ; e2 ; e3 ; e4 ]
                  Weight = weight
                  Value = value
                  Locations = loc
                  Since = since }

    let writeIngredients writer ingredients =
        use writer = writer
        Seq.iter (writeIngredient writer) ingredients

    let readIngredients (reader : System.IO.TextReader) =
        use reader = reader
        IO.readLines reader
        |> Seq.choose readIngredient
        |> Seq.toList

    let readStoreItem (line : string) =
        if line |> String.startsWith "//" then None else
        let fields = String.split [| ',' |] line
        if fields.Length <> 2 then None else
        let count = String.parseInt fields.[0]
        let name = IngredientId (String.trim fields.[1])
        Some <| (name, count)

    let readStoreItems (reader : System.IO.TextReader) =
        use reader = reader
        IO.readLines reader
        |> Seq.choose readStoreItem
        |> Seq.toList

    let writeRecipeIngredients (writer : System.IO.TextWriter) (Recipe(effect, ingredients)) =
        writer.WriteLine(ingredients |> List.map string |> String.concat ", ")

    let writeRecipeIngredientsList (writer : System.IO.TextWriter) recipes =
        use writer = writer
        Seq.iter (writeRecipeIngredients writer) recipes

    let readRecipeIngredients (line : string) =
        if line |> String.startsWith "//" then None else
        let ingredients = String.split [| ',' |] line
        if ingredients.Length < 2 || ingredients.Length > 3 then None else
        Some <| (ingredients |> Seq.map String.trim |> Seq.map IngredientId |> Seq.toList)

    let readRecipeIngredientsList (reader : System.IO.TextReader) =
        use reader = reader
        IO.readLines reader
        |> Seq.choose readRecipeIngredients
        |> Seq.toList
    
    let readPreference (line : string) =
        if line |> String.startsWith "//" then None else
        let fields = String.split [| ',' |] line
        if fields.Length < 2 || fields.Length > 3 then None else
        let weight = String.parseDouble fields.[0]
        let name = fields.[1] |> String.trim
        let option = if fields.Length = 3 then String.trim fields.[2] else ""
        let fn = match name, option with
                 | "preferUnknown", "" -> Some Brew.preferUnknown
                 | "preferPure"   , "" -> Some Brew.preferPure
                 | "avoidInpure"  , "" -> Some Brew.avoidInpure
                 | "preferEffect" , eName -> Some (Brew.preferEffect eName)
                 | "avoidEffect"  , eName -> Some (Brew.avoidEffect eName)
                 | _ -> None
        fn |> Option.map (fun fn -> weight, fn)

    let readPreferences (reader : System.IO.TextReader) =
        use reader = reader
        IO.readLines reader
        |> Seq.choose readPreference
        |> Seq.toList
