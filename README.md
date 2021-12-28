# RadarBase
This is not completed and has a ***LOT*** of bugs still. I was able to get this working on Customs and completed several raids with reasonable stability.

**NOTICE:** This project is on hold since it's not a priority for me. This is working as of 12/28/21, but in the future offsets may need to be updated. Feel free to fork this repo.

### Instructions
1. You need a DMA Device (Screamer, Raptor DMA,etc.) installed on your game PC with (hopefully) good/safe firmware. Don't ask me how.
2. Build/compile the app for Release x64. Copy the .DLL files in the Resources folder to your build folder.
3. Import any maps you would like to use (.PNG files only) into the \\Maps sub-folder. Make sure you have a corresponding .JSON file with the same name. For example, `Customs.PNG` and `Customs.JSON`
4. Run the program on your **2nd PC** that has the USB Cable plugged into. Click the Map button to cycle through maps if you need.

### Map JSON Info
Format your JSON as below. The X,Y values are the pixel coordinates on your .PNG image at game location 0,0,0 (this can be found on the right hand side of the window). You may need to play with the scale value to find the right setting depending on your map.

I used the below JSON Values for the Customs map in the screenshot below:
```
Customs.JSON
{
	"x": 1292.0,
	"y": 996.0,
	"z": 0.0,
	"scale": 3.75
}
```

### Demo
![Demo](https://user-images.githubusercontent.com/42287509/147527964-abf8a25a-ba9e-4b23-af92-66738e42b053.png)
