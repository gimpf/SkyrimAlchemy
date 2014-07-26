namespace SkyrimAlchemy

type IngredientStore = Map<Ingredient, int>

type Brewery = { Store : IngredientStore ; Known : EffectIngredientTable }

module Brew =
    let toStore drugs list : IngredientStore =
        list
        |> Seq.map (fun (name, count) -> (Drugs.getIngredient drugs name), count)
        |> Map.ofSeq

    let private brewDrug (store : IngredientStore) (Recipe(_, ingredients)) : IngredientStore =
        ingredients
        |> List.fold (fun store ingredient ->
                        let inStore = Map.find ingredient store
                        if inStore <= 0 then invalidOp "cannot use missing ingredient"
                        Map.add ingredient (inStore - 1) store)
                     store

    let private makeKnownToEffect (known : EffectIngredientTable) effect ingredient =
        if Ingredients.hasEffect effect ingredient then
            let current = Map.tryFind effect known
            let ingredientList = [ Ingredients.nameOf ingredient ]
            let result = match current with
                         | Some current -> Map.add effect (List.union current ingredientList) known
                         | None         -> Map.add effect ingredientList known
            result
        else
            known

    let private makeKnown (known : EffectIngredientTable) (Recipe(effects, ingredients)) =
        effects
        |> List.fold (fun known effect ->
                        ingredients
                        |> List.fold (fun known ingredient -> makeKnownToEffect known effect ingredient)
                                     known)
                     known

    let private isKnown ({ Known = known } as Brewery) effect ingredient =
        match known |> Map.tryFind effect with
        | None -> false
        | Some ingredients -> ingredients |> List.exists ((=) ingredient)

    let learn ({ Known = known } as brewery) recipe =
        { brewery with Known = makeKnown known recipe }

    let learnAll brewery recipes =
        List.fold learn
                  brewery
                  recipes

    let brew { Store = store ; Known = known } recipe =
        let store = brewDrug store recipe
        let known = makeKnown known recipe
        { Store = store ; Known = known }

    let getAvailableIngredients { Store = available } =
        available |> Map.toSeq |> Seq.where (((<) 0) << snd) |> Seq.map fst |> Seq.toList

    let getBestRecipeFor store weighting =
        Recipes.getBestRecipeBy weighting (getAvailableIngredients store)

    let generateRecipes drugs brewery weighting =
        Seq.unfold (fun brewery ->
                        let weighting = weighting |> List.map (fun (w,x)-> w , (x drugs brewery))
                        let recipe = getBestRecipeFor brewery weighting
                        recipe
                        |> Option.map (fun recipe -> recipe, brew brewery (snd recipe)))
                    brewery

    let preferPure drugs brewery recipe =
        if Recipes.isPureRecipe drugs recipe then 1.0 else 0.0

    let avoidInpure drugs brewery recipe =
        if Recipes.isPureRecipe drugs recipe then 0.0 else -1.0

    let preferUnknown drugs brewery (Recipe(recipeEffects, ingredients)) =
        let discovered = ingredients
                         |> List.fold (fun discovered { IngredientId = ingredient ; Effects = effects } ->
                                let discoveredNow =
                                    List.intersection effects recipeEffects
                                    |> List.fold (fun unknown effect ->
                                            unknown + (if isKnown brewery effect ingredient then 0 else 1))
                                            0
                                discovered + discoveredNow)
                                0
        (double discovered) / 12.0

    let preferEffect effectName drugs brewery (Recipe(effects, _)) =
        if effects |> List.exists ((=) (EffectId effectName)) then 1.0 else 0.0

    let avoidEffect effectName drugs brewery (Recipe(effects, _)) =
        if effects |> List.exists ((=) (EffectId effectName)) then -1.0 else 0.0

    let keepReserve effectName drugs { Store = store } (Recipe(effects, ingredients)) =
        if effects |> List.exists ((=) (EffectId effectName)) then
            0.0
        else
            let rare = ingredients
                       |> Seq.map (fun x -> x, Map.find x store)
                       |> Seq.where (fun (_,cnt) -> cnt < 2)
                       |> Seq.map fst
            if rare |> Seq.exists (fun { Effects = effects } -> effects |> List.exists ((=) (EffectId effectName))) then
                -1.0
            else
                0.0
