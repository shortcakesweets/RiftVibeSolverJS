# Rift Vibe Solver

This is a mod for Rift of the Necrodancer which computes (mostly) optimal Vibe activation timings for any chart you play

## To Use

1. Download the lastest version of BepInEx at <https://github.com/BepInEx/BepInEx/releases/latest>
2. Extract the contents of the BepInEx zip archive to your Rift of the Necrodancer game directory (C:\Program Files (x86)\Steam\steamapps\common\RiftOfTheNecroDancerOSTVolume1)
3. Open Rift of the Necrodancer to generate BepInEx config files
4. Download the latest version of RiftVibeSolver at <https://github.com/DominicAglialoro/RiftVibeSolver/releases/latest>
5. Extract the contents of the RiftVibeSolver zip archive to your BepInEx plugins folder (C:\Program Files (x86)\Steam\steamapps\common\RiftOfTheNecroDancerOSTVolume1\BepInEx\plugins)
6. Open Rift of the Necrodancer and play through a chart with the Golden Lute modifier enabled
7. Upon reaching the results screen, a text file (*ChartName*_Vibes.txt) listing the optimal Vibe activations will be written to your Documents folder

## Text File Details

Each line of the generated text file takes the following form:

Beat *StartingBeat* -> *EndingBeat* (-*Tolerance*) \[*Vibes* vibe -> *Score* points\]

* StartingBeat: The beat number of the first enemy you hit after activating Vibe
* EndingBeat: The beat number of the last enemy you hit before Vibe ends
* Tolerance: The maximum amount of time before the first enemy that you can activate Vibe while still getting the maximum possible score value
* Vibes: The number of bars of Vibe you have upon activating Vibe
* Score: The total amount of additional points you gain from using Vibe

The final line of the file indicates the total amount of bonus points you get can get by performing optimal Vibe activations

## Known Issues

* The solver does not account for your ability to squeeze additional hits into a Vibe activation by deliberately hitting enemies early or late
* The solver does not always account for the option to activate Vibe very early to ensure that you don't clear an additional upcoming Vibe phrase before your current Vibe ends
* The solver does not account for the small amount of additional time Vibe remains active after the Vibe meter hits zero
