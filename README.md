# mapper
This project is about reading binary mesh files from [Mapy.cz](https://mapy.cz/) (similar site to Google Maps).
The main part is `MapReader` class in [`Mapper.cs`](MapMess/src/Mapper.cs).\
The code is basicaly rewritten and coppied to some extend to work in **C#**. Code comes from JavaScript used by the web page to read the binary files.

## Map Tile Viewer
A simple way to preview tiles. Also can preview textures but the UV data is kinda broken now. It's possible to preview more tiles at once.
I'm not very knowledgeable of WPF and this is my second time trying it. It's made just to see something with it.
