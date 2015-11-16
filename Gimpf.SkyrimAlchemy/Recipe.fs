namespace SkyrimAlchemy

type Recipe = Recipe of EffectId list * Ingredient list with
    member x.ToKnownRecipeList() =
        match x with
        | Recipe (_, ingredients) ->
            sprintf "[ %s ]" (ingredients |> List.map (fun { IngredientId = x } -> "\"" + x.ToString() + "\"") |> String.concat " ; ")

(* ignore incomplete matches *)
#nowarn "25"
module Recipes =
    open FSharp.Collections.ParallelSeq
    open System.Collections.Generic

    let memoize fn =
      let cache = new System.Collections.Concurrent.ConcurrentDictionary<_,_>()
      fun x -> cache.GetOrAdd(key= x, valueFactory= fun k -> fn k)

    let getCommonEffectsBase (ingredients : Ingredient list) =
        // according to the profiler, this is the single most important function;
        // and this naive implementation turned out to be quite fast already
        ingredients
        |> Seq.collect (fun x -> x.Effects)
        |> Seq.groupBy id
        |> Seq.where (fun (_, items) -> 1 < Seq.length items)
        |> Seq.map fst
        |> Seq.toList

    let getCommonEffects = memoize getCommonEffectsBase

    let private makeRecipeOfTwoBase ([ ingredient1 ; ingredient2 ] as ingredients) =
        assert (ingredients = (List.sort ingredients))
        let common = getCommonEffects ingredients
        if not <| Seq.isEmpty common then
            Some <| Recipe(common, ingredients)
        else
            None

    let private makeRecipeOfTwo = memoize makeRecipeOfTwoBase

    let private makeRecipeOfThreeBase ([ingredient1 ; ingredient2 ; ingredient3 ] as ingredients) =
        assert (ingredients = (List.sort ingredients))
        let common = getCommonEffects ingredients
        let l = getCommonEffects [ ingredient1 ; ingredient2 ]
        let m = getCommonEffects [ ingredient2 ; ingredient3 ]
        let r = getCommonEffects [ ingredient1 ; ingredient3 ]
        if (not <| Seq.isEmpty common) && l <> common && m <> common && r <> common then
            assert ((l |> Seq.append m |> Seq.append r |> Seq.distinct |> Seq.length) > 1)
            Some <| Recipe(common, ingredients)
        else
            None

    let private makeRecipeOfThree = memoize makeRecipeOfThreeBase

    let makeRecipe ingredients =
        let ingredients = ingredients |> List.sort
        match ingredients |> List.length with
        | 2 -> makeRecipeOfTwo ingredients
        | 3 -> makeRecipeOfThree ingredients
        | _ -> None

    let makeRecipeFromNotes allIngredients recipeIngredients =
        let recipeIngredients = recipeIngredients
                                |> List.map (Drugs.getIngredient allIngredients)
        makeRecipe recipeIngredients

    let private getPossibleRecipesBase (ingredients : Ingredient list) =
        assert (ingredients.Length = (ingredients |> Seq.distinct |> Seq.length))
        let s1 = ingredients |> List.combinations 2 |> Seq.map List.sort |> Seq.choose makeRecipeOfTwo
        let s2 = ingredients |> List.combinations 3 |> Seq.map List.sort |> Seq.choose makeRecipeOfThree
        Seq.append s1 s2 |> Seq.toArray

    let getPossibleRecipes = memoize getPossibleRecipesBase
    
    let private EmptyRecipe = Recipe([], [])

    let private getBestRecipe weighting ingredients =
        let maybeBest = getPossibleRecipes ingredients
                        |> PSeq.map (fun x -> weighting x, x)
                        |> PSeq.fold (fun ((bestWeighting, bestRecipe) as best) ((weighting, recipe) as current) ->
                                        if bestWeighting < weighting
                                        then current
                                        else best)
                                    (0.0, EmptyRecipe)
        if (snd maybeBest) <> EmptyRecipe then Some(maybeBest) else None

    let private getRecipes weighting ingredients =
        getPossibleRecipes ingredients
        |> PSeq.map (fun x -> weighting x, x)
        |> PSeq.filter (((<) 0.0) << fst)
        |> PSeq.sortBy fst
        |> PSeq.map snd

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
