****************
* COLOR STUDIO *
* README FILE  *
****************


How to use this asset
---------------------
1) Inside Unity Editor, select top menu Window -> Color Studio -> Palette Manager or Pixel Painter.

2) It's recommended to drag & drop the interface windows and and dock them to other windows as shown in this video:
https://youtu.be/Qe2yHCqWhoU

3) Click on the ? tab of Color Studio window for quick instructions

4) Use the Pixel Painter window to create your pixel art

5) Add the "Recolor" script to a gameobject or sprite to change its colors at runtime



Scripting support
-----------------

Although Color Studio is designed as an Editor extension with an UI interface, some features can be exploited using scripting.
Particularly, the CSPalette class can be instantiated as follows:

// Create a palette which uses a split complementary design based on red color as primary:
CSPalette palette = ScriptableObject.CreateInstance<CSPalette>();
palette.ConfigurePalette(ColorScheme.SplitComplementary, Color.red, splitAmount: 0.6f);

// Build chromatic colors
palette.BuildHueColors();

// Output the palette hue colors
for (int k=0;k<palette.colorsCount;k++) {
    Debug.Log(palette.colors[k]);
}

// Build palette colors (including shades)
palette.BuildPaletteColors();
for (int k=0;k<paletteColors.Length;k++) {
    Debug.Log(palette.paletteColors[k]);
}

Other useful methods:

- Texture2D ExportLUT(): builds a LUT based on the current color palette.
- Texture2D ExportTexture(): builds a texture containing the color palettee.
- Color ApplyLUT(Color, LUT): transforms a color based on a given LUT.
- Color GetNearestColor(Color, ColorMatchMode): returns the nearest color of the palette.
- Color[] GetNearestColors(Color[] originalColors): returns the nearest colors of the palette.
- Color[] GetNearestColors(Texture, ColorMatchMode, ...): returns the nearest colors of the texture colors based on the palette.
- Texture2D GetNearestTexture(Texture, ColorMatchMode, ...): transforms the given texture based on the current palette.


Support
-------
Find useful topics and script samples on our support forum. Any suggestion or support request should be forwarded to:

* Support Forum: https://kronnect.com/support
* Email support: contact@kronnect.com
* Twitter: @Kronnect


Version history
---------------

Version 3.2.1
- [Fix] Fixed an issue when using ReColor to change only the main color and model has no texture or has incompatible compression mode

Version 3.2
- Added new Bake method to ReColor capable of exporting separate material/texture files

Version 3.1.1
- [Fix] Fixes for Unity 2021.3

Version 3.1
- Added "Bake" option to ReColor component. Let you store modifications into the gameobject permanently and remove recolor component

Version 3.0.1
- Recolor memory optimizations

Version 3.0
- Added zoom support

Version 2.9
- API: added CSPalette.ConfigurePalette(...) allows custom palette creation with default key colors (see https://kronnect.com/support/index.php/topic,5320.0.html)

Version 2.8
- Added "Pick Original Color from Scene View" button in Recolor inspector. Grabs the exact original color by clicking on the object in SceneView which is more accurate than using the color picker
- Fixes and optimizations

Version 2.7
- Allows more compact layout

Version 2.6
- Added automatic scrollbars to Pixel Painter window

Version 2.5
- Color wheel tab: options for entering primary color values directly
- Redesigned Palette RGB input section using a single line and a dropdown for color encoding option

Version 2.4
- Support for linear (non SRGB) textures

Version 2.3.2
- [Fix] Fixed wrong output size when saving a texture after resizing a canvas
- [Fix] Updated links to support site

Version 2.3.1
- [Fix] Fixed build issue with addressables

Version 2.3
- Added "Save As New" separate button to clear confusion when duplicating an open palette
- Fixes

Version 2.2
- Recolor scripts now update automatically when palette changes are saved from Color Studio window
- Prevents a runtime error when some material doesn't exist or has been disposed in a renderer with multiple materials
- Ensure palette preview is refreshed in inspector
- Removes a console warning when moving the color threshold slider in the inspector of Recolor script

Version 2.1
- Improved performance of Recolor operations

Version 2.0.3
- [Fix] Fixed quad-mode mirror drawing issue

Version 2.0
- New "Pixel Painter" window

Version 1.5
- Recolor: LUT performance optimizations

Version 1.4
- Recolor: added LUT option
- Recolor: added Color Correction options (vibrance, contrast, brightness, tinting)

Version 1.3
- Recolor now supports vertex colors transformations

Version 1.2
- Added Color Threshold option
- Realtime preview in Editor
- Added command button to add main texture colors to Color Operations section

Version 1.1
- Added Color Match mode option
- Can specify custom color operations

Version 1.0
- Initial version




