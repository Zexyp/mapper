using System;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Drawing;
using System.Text;

namespace Mapper
{
    public class MapDowloader
    {
        static public Stream GetMapMesh(int lod, int x, int y)
        {
            try
            {
                string fullurl = Config.BaseURL + Config.MapMeshAddress
                    .Replace("{lod}", lod.ToString())
                    .Replace("{x}", x.ToString())
                    .Replace("{y}", y.ToString());
                Log.Communication("[Mapper] dowloading map mesh from: " + fullurl);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullurl);
                request.AutomaticDecompression = DecompressionMethods.GZip;
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
                Log.Error("[Mapper] map mesh download failed (" + e.Message + ")");
                return Stream.Null;
            }
        }

        static public Image GetMapTexture(int lod, int x, int y)
        {
            try
            {
                string fullurl = Config.BaseURL + Config.MapTextureAddress
                    .Replace("{lod}", lod.ToString())
                    .Replace("{x}", x.ToString())
                    .Replace("{y}", y.ToString())
                    .Replace("{sub}", 0.ToString());
                Log.Communication("[Mapper] dowloading map texture from: " + fullurl);


                WebRequest request = WebRequest.Create(fullurl);
                WebResponse response = request.GetResponse();

                Image image = Image.FromStream(response.GetResponseStream());
                //bitmap.Save("boi.jpg");

                response.Close();
                response.Dispose();

                return image;
            }
            catch (WebException e)
            {
                Log.Error("[Mapper] map texture download failed (" + e.Message + ")");
                return null;
            }
        }
    }
}
