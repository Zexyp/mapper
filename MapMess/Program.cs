using System;
using System.Diagnostics;
using System.Linq;

using Mapper;

namespace MapMess
{
    class Program
    {
        static void Main(string[] args)
        {
            //BinaryReader binaryReader = new BinaryReader(File.Open("bila.bin", FileMode.Open));
            //MapReader mapReader = new MapReader(binaryReader);
            //mapReader.debug = true;
            //mapReader.ReadBinary();
            //mapReader.ReadHeader();
            //mapReader.ReadMesh();
            //binaryReader.Close();
            //MapDownloader.GetConfig();
            
            Mapper.Log.SetMessageCallback((msg, l) => Console.WriteLine($"[Mapper][{l}]: {msg}"));

            var cfg = MapDowloader.GetConfig("https://mapserver-3d.mapy.cz/scenes/latest/mapConfig.json");
            var mysurface = cfg.Surfaces.Where(i => i.Id == "cities").First();
            var tile = mysurface.TileRange.First();
            var mesh = MapParser.ParseMesh(MapDowloader.GetMesh(cfg, mysurface, 19, 142606, 88677));
            for (int i = 0; i < mesh.Submeshes[0].Vertices.Length; i++)
            {
                Console.WriteLine(mesh.Submeshes[0].Vertices.GetValue(i));
            }
        }
    }
}
