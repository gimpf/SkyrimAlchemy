namespace SkyrimAlchemy

type EffectIngredientTable = Map<EffectId, IngredientId list>

type DrugDescription = {
    Effects : EffectTable
    Ingredients : IngredientTable
    EffectIngredients : EffectIngredientTable }

module Drugs =
    let getEffect { Effects = effects ; EffectIngredients = _ } name =
        Map.find name effects

    let getIngredient { Effects = _ ; Ingredients = ingredients } name =
        Map.find name ingredients

    let createTable effects ingredients : EffectIngredientTable =
        effects
        |> Seq.map (fun e ->
             let { EffectId = effectId } = e
             let usable = ingredients
                          |> Seq.choose (fun i ->
                             let { IngredientId = ingredientId ; Effects = effects } = i
                             if effects |> Seq.exists ((=) effectId)
                                 then Some ingredientId
                                 else None)
                          |> Seq.toList
                          |> List.sort
             effectId, usable)
        |> Map.ofSeq

    let createDescription effects ingredients =
        { Effects           = Effects.createTable effects
          Ingredients       = Ingredients.createTable ingredients
          EffectIngredients = createTable effects ingredients }

