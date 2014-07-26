namespace SkyrimAlchemy

type PotionDisposition = RestoreEnergy | Other

type EffectDisposition =
    | Potion of PotionDisposition
    | Poison

type EffectId = EffectId of string with
    override x.ToString() =
        match x with
        | EffectId str -> str

type Effect = {
    EffectId : EffectId
    Description : string
    Disposition : EffectDisposition
    BaseCost : double
    Magnitude : double
    Duration : double
    MagnitudeFixed : bool
    DurationFixed : bool }

type EffectTable = Map<EffectId, Effect>

module Effects =
    let createTable effects : EffectTable =
        effects
        |> Seq.map (fun e ->
            let { EffectId = name } = e
            name, e)
        |> Map.ofSeq

    let nameOf { EffectId = name } = name
