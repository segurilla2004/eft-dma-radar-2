# RadarBase
This is not completed and has a ***LOT*** of bugs still. I was able to get this working on Customs and completed a raid with mostly correct positions.

### Instructions
1. You need a DMA Device (Screamer, Raptor DMA,etc.) installed on your game PC with (hopefully) good/safe firmware. Don't ask me how.
2. Copy the .DLL files in the Resources folder to your application folder.
3. Import any maps you would like to use (.PNG files only) into the \\Maps folder. Make sure you have a corresponding .JSON file with the same name. For example, `Customs.PNG` and `Custom.JSON`
4. Run the program on your 2nd PC that has the USB Cable plugged into. Click the Map button to cycle through maps if you need.

### Map JSON Info
Format your JSON as above. The X,Y values are the pixel coordinates on your .PNG image at game location 0,0,0 (this can be found on the right hand side of the window). You may need to play with the scale value to find the right setting depending on your map.

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
![Demo](https://user-images.githubusercontent.com/42287509/147501772-1b66cda5-1309-44bb-9325-c9d5d16fe47c.png)
