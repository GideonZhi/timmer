# TIMMER

Commandline TIM processing utility, forked from https://github.com/Eight-Mansions/timmer

Changelog:
* Updated to compile with .NET 10 instead of 4.8.
  * Removes dependency on `System.Drawing.Common` Bitmap types, which are Windows-only as of .NET 7. 
  * Replaces with SkiaSharp SKBitmap.
  * Updates Color types to SKColor as well.
* Fixes a bug with output file processing during `insert` operations
* Fixes a bug related to case sensitivity mismatch in input/output filenames
* Makes semi-transparent processing a commandline switch, defaults to false

Code could probably still use a bunch of cleanup but absolute first goal was just to get it building on a non-Windows platform.

Building on linux: `dotnet build` in repo root. Windows is probably similar?

Use: `timmer [extract|insert] [opts]`


`extract` options:
```
  -i, --infile         Filename to extract graphic from.

  -o, --outfile      Filename to save graphic to (only PNGs are supported).

  -p, --pixeldata    Position of pixel data.

  -c, --clutdata     Position of clut/palette data.

  -b, --bpp          Bits per pixel.

  -w, --width        Width of image.

  -h, --height       Height of image.

  -t, --timdata      Position of tim data.

  --help             Display this help screen.

  --version          Display version information.
```
Sample command: `.\timmer extract -i .\assets\gfx\fontwin\wintex.bin -o .\assets\gfx\fontwin\wintex\`

`insert` options:
```
  -i, --infile       Filename to insert graphic into.

  -o, --outfile      Filename of graphic to insert.

  -f, --filter       Filter to find files to insert graphics into.

  -p, --pixeldata    Position of pixel data of original graphic.

  -c, --clutdata     Position of clut/palette data of original graphic.

  -b, --bpp          Bits per pixel.

  -t, --timdata      Position of tim data.

  -m, --mask         Mask to use when inserting pixels.

  --help             Display this help screen.

  --version          Display version information.
```
Sample command: `timmer insert -i ./rebuild/TITLE/SUBTITLE.BIN -o ./assets/gfx/SUBTITLE.BIN.0001_0000.png`