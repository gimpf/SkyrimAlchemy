Gimpf.SkyrimAlchemy
===================

**Hack** of a Skyrim Recipe recommender which does not optimize coin, but preferred effects.

It might be useful to lower/lower-mid level characters, where money won't buy you anything worthwhile yet (will it ever?), and interesting potions are hard to come by.  In such a case you do not want to waste ingredients on stuff you don't need and won't even provide 10 coins due to lacking perks in the Speech skill tree and the low quality of your potions.


Usage (after compiling...)
--------------------------

1. `data/available-ingredients.csv`: set the ingredients that you have available for use
2. `data/preferences.csv`: set your preferences as weight + preferenceFunction; see below for details
3. `data/known-recipes.csv`: set the recipes your character knows; for a new game, this file starts empty, and later the calculator provides an update file that you can use if you choose so
4. execute Gimpf.SkyrimAlchemyCalculator/bin/Release/Gimpf.SkyrimAlchemyCalculator.exe
5. do whatever you want with the recommended recipes
6. if you happen to choose every recommended recipe at least once, update `data/available-ingredients.csv` by replacing it with `data/available-ingredients-new.csv`; otherwise you need to remove the unused recommendation from that file, in case your character didn't know it yet; oh my, that's what I call convenient user interfaces...

Although this process is involved, for me it's still orders of magnitude faster than doing all the thinking in my head.  YMMV.

As an example, this is how a recommendation could look like:

```
1 × Bee + Blue Dartwing + Charred Skeever Hide => Restore Stamina, Restore Health
2 × Briar Heart + Hagraven Claw + Swamp Fungal Pod => Paralysis, Lingering Damage Magicka
1 × Cyrodilic Spadetail + Fire Salts + Salt Pile => Fortify Restoration, Regenerate Magicka
7 × Deathbell + Ectoplasm + River Betty => Damage Health, Slow
5 × Blue Mountain Flower + Butterfly Wing => Restore Health
1 × Creep Cluster + Elves Ear => Restore Magicka
1 × Dwarven Oil + Frost Salts => Restore Magicka
```


Configuring Preferences
-----------------------

The recommendation is only as good as the configuration in `data/preferences.csv`.  This files stores a list of preference functions and their weight.

* Each preference function returns a value in the inclusive range from -1.0 to +1.0 for a single recipe.
* Each preference gets an associated weight, which is multiplied with the result of the preference function
* The total score for a recipe is the sum of weighted scores from the preference functions.
* Only recipes with a weight strictly larger than 0.0 will be recommended.

A configuration looks like:

```csv
40.00,preferUnknown
10.00,avoidInpure
7.00,preferEffect,Restore Health
7.00,preferEffect,Restore Magicka
1.00,preferEffect,Fortify Destruction
```

* `preferUnknown`: learn a wide variety of effects quickly, without going to hunt down all the ingredients for the perfect-learning-recipe-list.
* `avoidInpure`: avoid mixing poisonous and ... potionous? ... effects if you plan to use it instead of sell it (you don't want to poison yourself or help your opponent)
* `preferPure`: like `avoidInpure`, but generally less helpful; all possible potions with just one effect are pure, so using this will by default lead to a lot of wasted ingredients
* `preferEffect` and `avoidEffect`: use this to select what you're currently interested in; very character specific, of course, but "Restore" and "Regenerate" potions are usually high on the "prefer" list, along with some "Resist", as well as "Fortify" for skills you need.  Poisons I don't know about, as a Mage you seldom get the chance to use them, and so I always carry too many of them anyways

`preferUnknown` returns 1.0 for having the maximum possible number of new effects: twelve new effects (three ingredients with four effects each); I would want to create a potion where I learn only four new effects, even if it is a total crapshot (like Fortify Health and Lingering Health Poison at once), so `preferUnknown` has a very high weight to counteract the `avoidInpure`, which in turn is so high to counteract potions with two useful effects and one devastating one, even if I like every single one of them.

This list above is a good starting point for relative weights, proved to be effective by trial and error.  Error, mostly.


Warning
-------

* zero tests (it worked for me!)
* naive search, not parallelized, very slow (with many different ingredients, the search can easily take a minute or more)
* still does not guarantee optimal results (it doesn't even use a beam-search, it just takes the best possible recipe for each iteration and continues until no recommended recipe is found; this can lead to a lot of waste, theoretically; in practice, it doesn't matter _that_ much)


Missing Features
----------------

(will probably remain very missing, in rough order of missingness)

* handle ingredient specific modifications (like Crimson Nirnroot's magnitude boost; they are just too rare to care about that)
* keep rare ingredients in reserve if the best potion possible now is not good enough
* better recipe-selection by using beam search
* get rid of all ingredients that remain after preferences have been met by using the rest as coin-maximizers
  * which requires: optimize by price (but there are many calculators on the Internet that already do that)
* calculate strength by Skyrim character (I'm only interested in relative weights)
* ...


Structure and Code
------------------

* `data`: Files storing Effects, Ingredients, your own state (already known recipes), and configuration (preferences)
* `Gimpf.FSharpExtensions`: Mostly some functions to extend FSharp's Seq, List etc. modules with something I needed at the time.
* `Gimpf.SkyrimAlchemy`: The F# library implementing the Recipe search, loading data and printing the results.
* `Gimpf.SkyrimAlchemyCalculator`: A console program playing piano on the library.


Thanks to
---------

The people making available the list of effects and ingredients at

* http://www.uesp.net/wiki/Skyrim:Alchemy_Effects
* http://www.uesp.net/wiki/Skyrim:Ingredients
* http://elderscrolls.wikia.com/wiki/Ingredients_%28Skyrim%29


License
-------

GPLv3, but I don't even care enough to add the license text.