namespace SkyrimAlchemy

type IngredientId = IngredientId of string with
    override x.ToString() =
        match x with
        | IngredientId str -> str

type Ingredient = {
    IngredientId : IngredientId
    // TODO Ingredient has exactly four effects; refactor to tuple
    Effects : EffectId list
    Weight : float
    Value : int
    Locations : string
    Since : string } with
    override x.ToString() =
         let { IngredientId = name } = x
         name.ToString()

type IngredientTable = Map<IngredientId, Ingredient>

module Ingredients =
    let createTable ingredients : IngredientTable =
        ingredients
        |> Seq.map (fun i ->
            let { IngredientId = name} = i
            name, i)
        |> Map.ofSeq

    let hasEffect effect { Effects = effects } =
        List.exists ((=) effect) effects

    let nameOf { IngredientId = name } = name
