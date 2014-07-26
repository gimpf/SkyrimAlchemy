open IO
open SkyrimAlchemy
open SkyrimAlchemy.Brew

let time f =
    let watch = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    printfn "Elapsed: %A ms" watch.ElapsedMilliseconds
    result

let convertWikiImportedData () =
    let effects = Importer.Effects.readAll <| fromFile @"data\Skyrim Effects.txt"
    FileIO.writeEffects (toFile @"data\alchemy-effects.csv") effects
    let ingredients = Importer.Ingredients.readAll <| fromFile @"data\Skyrim Ingredients.txt"
    FileIO.writeIngredients (toFile @"data\alchemy-ingredients.csv") ingredients

let formatRecipe (Recipe(effect, ingredients)) =
    let ingredientList =
        match ingredients with
        | [ { IngredientId = i1 } ; { IngredientId = i2 } ] -> sprintf "%s + %s" (string i1) (string i2)
        | [ { IngredientId = i1 } ; { IngredientId = i2 } ; { IngredientId = i3 } ] -> sprintf "%s + %s + %s" (string i1) (string i2) (string i3)
        | _ -> invalidArg "ingredients" "Recipe contained invalid ingredient list"
    let effect = effect |> List.map string |> String.concat ", "
    sprintf "%s => %s" ingredientList effect

let showPreferedRecipes () =
    let effects      = FileIO.readEffects     (fromFile @"data\alchemy-effects.csv")
    let ingredients  = FileIO.readIngredients (fromFile @"data\alchemy-ingredients.csv")
    let drugs        = Drugs.createDescription effects ingredients
    let available    = FileIO.readStoreItems  (fromFile @"data\available-ingredients.csv")
                       |> (toStore drugs)
    let knownRecipes = FileIO.readRecipeIngredientsList (fromFile @"data\known-recipes.csv")
                       |> List.choose (Recipes.makeRecipeFromNotes drugs)
    let brewery = knownRecipes |> learnAll { Store = available ; Known = Map.empty }

    // alternative: specify stuff like this:
    // let preferredDrugs = [
    //     40.00 , preferUnknown
    //     10.00, avoidInpure
    //     7.00 , preferEffect "Restore Health"
    //     7.00 , preferEffect "Restore Magicka" ]
    let preferredDrugs = FileIO.readPreferences (fromFile @"data\preferences.csv")

    let knownRecipeUpdate =
        fun () -> generateRecipes drugs brewery preferredDrugs
                  |> Seq.groupBy snd
                  |> Seq.map (fun (recipe, entries) ->
                                printfn "%d × %s" (Seq.length entries) (formatRecipe recipe)
                                recipe)
                  |> Seq.toList
        |> time

    let knownRecipes = List.concat [ knownRecipes ; knownRecipeUpdate ]
                       |> Seq.distinct

    FileIO.writeRecipeIngredientsList (toFile @"data\known-recipes-new.csv") knownRecipes

[<EntryPoint>]
let main argv =
    showPreferedRecipes ()
    0
