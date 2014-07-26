namespace SkyrimAlchemy

type Recipe = Recipe of EffectId list * Ingredient list with
    member x.ToKnownRecipeList() =
        match x with
        | Recipe (_, ingredients) ->
            sprintf "[ %s ]" (ingredients |> List.map (fun { IngredientId = x } -> "\"" + x.ToString() + "\"") |> String.concat " ; ")


(* ignore incomplete matches *)
#nowarn "25"
module Recipes =
    open System.Collections.Generic

    let getCommonEffects (ingredients : Ingredient seq) =
        // according to the profiler, this is the single most important function;
        // and this naive implementation turned out to be quite fast already
        ingredients
        |> Seq.collect (fun x -> x.Effects)
        |> Seq.groupBy id
        |> Seq.where (fun (_, items) -> 1 < Seq.length items)
        |> Seq.map fst
        |> Seq.toList

    let private makeRecipeOfTwo ([ ingredient1 ; ingredient2 ] as ingredients) =
        let ingredients = ingredients |> List.sort
        let common = getCommonEffects ingredients
        if not <| Seq.isEmpty common then
            Some <| Recipe(common, ingredients)
        else
            None

    let private makeRecipeOfThree ([ingredient1 ; ingredient2 ; ingredient3 ] as ingredients) =
        let ingredients = ingredients |> List.sort
        let common = getCommonEffects ingredients
        let l = getCommonEffects [ ingredient1 ; ingredient2 ]
        let r = getCommonEffects [ ingredient2 ; ingredient3 ]
        if not <| Seq.isEmpty common && l <> common && r <> common then
            Some <| Recipe(common, ingredients)
        else
            None

    let makeRecipe ingredients =
        match ingredients |> List.length with
        | 2 -> makeRecipeOfTwo ingredients
        | 3 -> makeRecipeOfThree ingredients
        | _ -> None

    let makeRecipeFromNotes allIngredients recipeIngredients =
        let recipeIngredients = recipeIngredients
//                                |> List.map IngredientId
                                |> List.map (Drugs.getIngredient allIngredients)
        makeRecipe recipeIngredients

    let getPossibleRecipes (ingredients : Ingredient list) =
        let s1 = ingredients |> List.combinations 2 |> Seq.choose makeRecipeOfTwo
        let s2 = ingredients |> List.combinations 3 |> Seq.choose makeRecipeOfThree
        Seq.append s1 s2

    let private getBestRecipe weighting ingredients =
        let EmptyRecipe = Recipe([], [])
        let maybeBest = getPossibleRecipes ingredients
                        |> Seq.map (fun x -> weighting x, x)
                        |> Seq.fold (fun ((bestWeighting, bestRecipe) as best) ((weighting, recipe) as current) ->
                                        if bestWeighting < weighting
                                        then current
                                        else best)
                                    (0.0, EmptyRecipe)
        if (snd maybeBest) <> EmptyRecipe then Some(maybeBest) else None

    let private getRecipes weighting ingredients =
        getPossibleRecipes ingredients
        |> Seq.map (fun x -> weighting x, x)
        |> Seq.where (((<) 0.0) << fst)
        |> Seq.sortBy fst
        |> Seq.map snd

    let isPureRecipe drugs (Recipe(effect :: rest, ingredients)) =
        let { Disposition = preferedDis } = Drugs.getEffect drugs effect
        rest
        |> List.forall (fun i ->
            let { Disposition = dis } = Drugs.getEffect drugs i
            preferedDis = dis)

    let private combineWeight (prioritizedWeightings : (float * (Recipe -> float)) list) recipe =
        prioritizedWeightings
        |> List.fold (fun acc (priority, weighting) ->
            acc + priority * (weighting recipe))
            0.0

    let getRecipesBy weightingList ingredients =
        getRecipes (combineWeight weightingList) ingredients

    let getBestRecipeBy weightingList ingredients =
        getBestRecipe (combineWeight weightingList) ingredients
