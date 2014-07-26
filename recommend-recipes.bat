@echo off
Gimpf.Skyirm.AlchemyCalculator\bin\Release\Gimpf.Skyrim.AlchemyCalculator.exe
pause
copy data\known-recipes.csv data\known-recipes-old.csv
copy /y data\known-recipes-new.csv data\known-recipes.csv

