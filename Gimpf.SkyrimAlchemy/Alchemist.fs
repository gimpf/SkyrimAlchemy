namespace SkyrimAlchemy

type AlchemySkills = {
    AlchemySkill : int
    AlchemistPerkBonus : int
    PhysicianPerk : bool
    BenefactorPerk : bool
    PoisonPerk : bool }

module Alchemist =
    let skillFactor skills disposition =
        let IngredientMultiplier = 4.0 // game setting
        let SkillBaseFactor = 1.5 // game setting
        let { AlchemySkill = skill 
              AlchemistPerkBonus = alchemistPerkBonus
              PhysicianPerk = isPhysician
              BenefactorPerk = isBenefactor
              PoisonPerk = isPoisoner
            } = skills
        let isRestoration, isPotion, isPoison =
            match disposition with
            | Potion(RestoreEnergy) -> true, true, false
            | Potion(Other)         -> false, true, false
            | Poison                -> false, false, true
        let baseFactor       = IngredientMultiplier
        let skillFactor      = 1.0 + (SkillBaseFactor - 1.0) * (double skill) / 100.0
        let alchemistFactor  = 1.0 + (double alchemistPerkBonus / 100.0)
        let physicianFactor  = 1.0 + (if isPhysician  && isRestoration then 0.25 else 0.0)
        let poisonerFactor   = 1.0 + (if isPoisoner   && isPoison      then 0.25 else 0.0)
        let benefactorFactor = 1.0 + (if isBenefactor && isPotion      then 0.25 else 0.0)
        baseFactor * alchemistFactor * alchemistFactor * physicianFactor * poisonerFactor * benefactorFactor

    let magnitude { Magnitude = baseMagnitude ; MagnitudeFixed = constant ; Disposition = disposition } skill =
        if constant then baseMagnitude else baseMagnitude * skillFactor skill disposition

    let duration { Duration = baseDuration ; DurationFixed = constant ; Disposition = disposition } skill =
        if constant then baseDuration else baseDuration * skillFactor skill disposition

    let cost effect skill =
        let { BaseCost = baseCost } = effect
        let mag = magnitude effect skill
        let dur = duration effect skill
        baseCost * 0.0794328 * (if mag = 0.0 then 1.0 else mag ** 1.1) * (if dur = 0.0 then 1.0 else dur ** 1.1)
        |> floor
