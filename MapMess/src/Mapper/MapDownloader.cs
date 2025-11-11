using System;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Drawing;
using System.Text;
using System.Linq;

namespace Mapper
{
    public class MapDowloader
    {
        // TODO: fix duplicate streams
        
        private static string FillTileUrlPattern(string inp, int lod, int x, int y, int sub = 0)
        {
            return inp.Replace("{lod}", lod.ToString())
                      .Replace("{x}", x.ToString())
                      .Replace("{y}", y.ToString())
                      .Replace("{sub}", sub.ToString());
        }

        private static string Dirname(string path)
        {
            if (path.EndsWith("/"))
            {
                path = path.TrimEnd('/');
            }

            string[] segments = path.Split('/');

            return string.Join("/", segments, 0, segments.Length - 1);
        }

        public static Stream GetMesh(MapConfig cfg, MapConfigSurface scfg, int lod, int x, int y)
        {
            try
            {
                var url = Path.Join(Dirname(cfg.Root), scfg.MeshUrl);
                url = FillTileUrlPattern(url, lod, x, y);

                Log.Communication($"dowloading mesh '{url}'");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip; // this might be a problem later
                WebResponse response = request.GetResponse();

                MemoryStream stream = new MemoryStream();
                response.GetResponseStream().CopyTo(stream);

                response.Close();
                response.Dispose();

                stream.Position = 0;

                return stream;
            }
            catch (WebException e)
            {
                Log.Error($"mesh download failed: \n{e.Message}");
                return Stream.Null;
            }
        }

        public static Stream GetMapTexture(MapConfig cfg, MapConfigSurface scfg, int lod, int x, int y)
        {
            try
            {
                var url = Path.Join(Dirname(cfg.Root), scfg.TextureUrl);
                url = FillTileUrlPattern(url, lod, x, y);

                Log.Communication($"dowloading texture '{url}'");

                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();

                MemoryStream stream = new MemoryStream();
                response.GetResponseStream().CopyTo(stream);
                
                response.Close();
                response.Dispose();

                stream.Position = 0;

                return stream;
            }
            catch (WebException e)
            {
                Log.Error($"texture download failed: \n{e.Message}");
                return null;
            }
        }

        public static MapConfig GetConfig(string url)
        {
            try
            {
                Log.Communication($"dowloading config '{url}'");

                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();

                MapConfig mc = new MapConfig();
                mc.Root = url;

                using (JsonDocument document = JsonDocument.Parse(response.GetResponseStream()))
                {
                    var surfaces = document.RootElement.GetProperty("surfaces");
                    mc.Surfaces = new MapConfigSurface[surfaces.GetArrayLength()];

                    int i = 0;
                    foreach (var surface in surfaces.EnumerateArray())
                    {
                        var mapSurface = new MapConfigSurface();

                        mapSurface.Id = surface.GetProperty("id").GetString();
                        mapSurface.MeshUrl = surface.GetProperty("meshUrl").GetString();
                        mapSurface.TextureUrl = surface.GetProperty("textureUrl").GetString();
                        mapSurface.LodRange = surface.GetProperty("lodRange").EnumerateArray().Select(e => e.GetInt32()).ToArray();
                        mapSurface.TileRange = surface.GetProperty("tileRange").EnumerateArray().Select(e => e.EnumerateArray().Select(e => e.GetInt32()).ToArray()).ToArray();

                        mc.Surfaces[i] = mapSurface;
                        i++;
                    }


                }

                response.Close();
                response.Dispose();

                return mc;
            }
            catch (WebException e)
            {
                Log.Error($"config download failed: \n{e.Message}");
                return null;
            }
        }
    }
}
